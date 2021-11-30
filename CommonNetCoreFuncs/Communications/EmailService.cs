using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CommonNetCoreFuncs.Communications
{
    /// <summary>
    /// Interface for use with dependancy injection
    /// </summary>
    public interface IEmailService
    {
        Task<bool> SendEmail(string smtpServer, int smtpPort, MailAddress from, List<MailAddress> toAddresses, string subject, string body, bool bodyIsHtml = false, List<MailAddress> ccAddresses = null, List<MailAddress> bccAddresses = null, string attachmentName = null, Stream fileData = null);
    }

    /// <summary>
    /// Implementation of IEmailService that can be used with dependancy injection in order to speed up sending multiple emails
    /// </summary>
    public class EmailService : IEmailService
    {
        public async Task<bool> SendEmail(string smtpServer, int smtpPort, MailAddress from, List<MailAddress> toAddresses, string subject, string body, bool bodyIsHtml = false, List<MailAddress> ccAddresses = null, List<MailAddress> bccAddresses = null, string attachmentName = null, Stream fileData = null)
        {
            return await Email.SendEmail(smtpServer, smtpPort, from, toAddresses, subject, body, bodyIsHtml, ccAddresses, bccAddresses, attachmentName, fileData);
        }
    }
}
