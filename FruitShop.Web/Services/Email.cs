using System.Net;
using System.Net.Mail;

namespace FruitShop.Web.Services
{
    public class Email : INotification
    {
        public async Task<bool> Send(MessageNotification request)
        {
            try
            {
                using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.Credentials = new NetworkCredential("hotelluxurytrungduc@gmail.com", "ykbg blmo tqxy hrld");
                    smtp.EnableSsl = true;

                    using (MailMessage message = new MailMessage())
                    {
                        message.From = new MailAddress("hotelluxurytrungduc@gmail.com", "PTD Corporation");
                        message.To.Add(request.To);
                        message.Subject = request.subject;
                        message.Body = request.Content;
                        message.IsBodyHtml = true;

                        await smtp.SendMailAsync(message);
                    }
                }

                Console.WriteLine("Email sent successfully!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending email: " + ex.Message);
                return false;
            }
        }
    }
}
