using System.IO;

namespace CommonNetCoreFuncs.Communications
{
    public interface IEmailService
    {
        bool SendEmail(string fromName, string fromEmail, string toName, string toEmail, string subject, string body, string attachmentName = null, MemoryStream fileData = null);
    }

    public class EmailService : IEmailService
    {
        public bool SendEmail(string fromName, string fromEmail, string toName, string toEmail, string subject, string body, string attachmentName = null, MemoryStream fileData = null)
        {
            return Email.SendEmail(fromName, fromEmail, toName, toEmail, subject, body, attachmentName, fileData);
        }
    }
}
