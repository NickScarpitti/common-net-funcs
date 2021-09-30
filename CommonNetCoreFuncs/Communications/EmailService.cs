using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CommonNetCoreFuncs.Communications
{
    public interface IEmailService
    {
        Task<bool> SendEmail(string smtpServer, int smtpPort, MailAddress from, List<MailAddress> toAddresses, string subject, string body, bool bodyIsHtml = false, List<MailAddress> ccAddresses = null, string attachmentName = null, Stream fileData = null);
    }

    public class EmailService : IEmailService
    {
        public async Task<bool> SendEmail(string smtpServer, int smtpPort, MailAddress from, List<MailAddress> toAddresses, string subject, string body, bool bodyIsHtml = false, List<MailAddress> ccAddresses = null, string attachmentName = null, Stream fileData = null)
        {
            return await Email.SendEmail(smtpServer, smtpPort, from, toAddresses, subject, body, bodyIsHtml, ccAddresses, attachmentName, fileData);
        }
    }
}
