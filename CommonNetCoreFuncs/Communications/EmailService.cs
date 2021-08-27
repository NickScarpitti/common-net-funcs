using System.IO;

namespace CommonNetCoreFuncs.Communications
{
    public interface IEmailService
    {
        bool SendEmail(MailAddress from, MailAddress to, string toName, string toEmail, string subject, string body, MailAddress cc = null, string attachmentName = null, MemoryStream fileData = null);
    }

    public class EmailService : IEmailService
    {
        public bool SendEmail(MailAddress from, MailAddress to, string toName, string toEmail, string subject, string body, MailAddress cc = null, string attachmentName = null, MemoryStream fileData = null)
        {
            return Email.SendEmail(from, to, subject, body, cc, attachmentName, fileData);
        }
    }
}
