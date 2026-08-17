using System.Security.Cryptography;
using System.Text;

namespace FruitShop.Server.Services;

internal static class PasswordVerifier
{
    public static string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        const int iterations = 100_000;
        var salt = RandomNumberGenerator.GetBytes(16);
        var subkey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA512, 32);
        var output = new byte[13 + salt.Length + subkey.Length];
        output[0] = 0x01;
        WriteNetworkByteOrder(output, 1, (int)KeyDerivationPrf.HMACSHA512);
        WriteNetworkByteOrder(output, 5, iterations);
        WriteNetworkByteOrder(output, 9, salt.Length);
        Buffer.BlockCopy(salt, 0, output, 13, salt.Length);
        Buffer.BlockCopy(subkey, 0, output, 13 + salt.Length, subkey.Length);
        return Convert.ToBase64String(output);
    }

    public static bool Verify(string password, string storedValue)
    {
        if (string.IsNullOrWhiteSpace(storedValue) || string.IsNullOrWhiteSpace(password))
            return false;

        password = password.Trim();

        // 1. Direct plain text match (supports plain text seeded passwords)
        if (string.Equals(password, storedValue.Trim(), StringComparison.Ordinal))
            return true;

        // 2. ASP.NET Core Identity V3 hash verification
        if (TryVerifyIdentityV3(password, storedValue, out var identityResult) && identityResult)
            return true;

        // 3. Byte comparison fallback
        return FixedTimeEquals(Encoding.UTF8.GetBytes(password), Encoding.UTF8.GetBytes(storedValue));
    }

    private static bool TryVerifyIdentityV3(string password, string storedValue, out bool result)
    {
        result = false;
        byte[] decoded;
        try { decoded = Convert.FromBase64String(storedValue); }
        catch (FormatException) { return false; }

        if (decoded.Length < 13 || decoded[0] != 0x01)
            return false;

        var prf = (KeyDerivationPrf)ReadNetworkByteOrder(decoded, 1);
        var iterations = ReadNetworkByteOrder(decoded, 5);
        var saltLength = ReadNetworkByteOrder(decoded, 9);
        if (iterations < 1 || saltLength < 16 || decoded.Length < 13 + saltLength + 16)
            return false;

        var salt = decoded[13..(13 + saltLength)];
        var expectedSubkey = decoded[(13 + saltLength)..];
        var hashAlgorithm = prf switch
        {
            KeyDerivationPrf.HMACSHA1 => HashAlgorithmName.SHA1,
            KeyDerivationPrf.HMACSHA256 => HashAlgorithmName.SHA256,
            KeyDerivationPrf.HMACSHA512 => HashAlgorithmName.SHA512,
            _ => default
        };
        if (hashAlgorithm == default)
            return false;

        var actualSubkey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, hashAlgorithm, expectedSubkey.Length);
        result = CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
        return true;
    }

    private static bool FixedTimeEquals(byte[] left, byte[] right) =>
        left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);

    private static int ReadNetworkByteOrder(byte[] buffer, int offset) =>
        (buffer[offset] << 24) | (buffer[offset + 1] << 16) | (buffer[offset + 2] << 8) | buffer[offset + 3];

    private static void WriteNetworkByteOrder(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }

    private enum KeyDerivationPrf
    {
        HMACSHA1 = 0,
        HMACSHA256 = 1,
        HMACSHA512 = 2
    }
}