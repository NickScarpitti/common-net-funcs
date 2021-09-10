using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CommonNetCoreFuncs.Communications
{
    public interface IEmailService
    {
        Task<bool> SendEmail(MailAddress from, List<MailAddress> to, string toName, string toEmail, string subject, string body, bool bodyIsHtml, List<MailAddress> cc = null, string attachmentName = null, FileStream fileData = null);
    }

    public class EmailService : IEmailService
    {
        public async Task<bool> SendEmail(MailAddress from, List<MailAddress> to, string toName, string toEmail, string subject, string body, bool bodyIsHtml, List<MailAddress> cc = null, string attachmentName = null, FileStream fileData = null)
        {
            return await Email.SendEmail(from, to, subject, body, bodyIsHtml, cc, attachmentName, fileData);
        }
    }
}
