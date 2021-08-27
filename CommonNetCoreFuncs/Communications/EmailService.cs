using System.Collections.Generic;
using System.IO;

namespace CommonNetCoreFuncs.Communications
{
    public interface IEmailService
    {
        bool SendEmail(MailAddress from, List<MailAddress> to, string toName, string toEmail, string subject, string body, bool bodyIsHtml, List<MailAddress> cc = null, string attachmentName = null, FileStream fileData = null);
    }

    public class EmailService : IEmailService
    {
        public bool SendEmail(MailAddress from, List<MailAddress> to, string toName, string toEmail, string subject, string body, bool bodyIsHtml, List<MailAddress> cc = null, string attachmentName = null, FileStream fileData = null)
        {
            return Email.SendEmail(from, to, subject, body, bodyIsHtml, cc, attachmentName, fileData);
        }
    }
}
