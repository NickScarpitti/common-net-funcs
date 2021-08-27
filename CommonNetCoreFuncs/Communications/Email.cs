using MailKit.Net.Smtp;
using MimeKit;
using System;
using System.IO;

namespace CommonNetCoreFuncs.Communications
{
    public class MailAddress
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }
    public static class Email
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public static bool SendEmail(MailAddress from, MailAddress to, string subject, string body, MailAddress cc = null, string attachmentName = null, FileStream fileData = null)
        {
            bool success = true;
            try
            {
                MimeMessage email = new();
                email.From.Add(new MailboxAddress(from.Name, from.Email));
                email.To.Add(new MailboxAddress(to.Name, to.Email));
                if (cc != null)
                {
                    email.Cc.Add(new MailboxAddress(cc.Name, cc.Email));
                }
                email.Subject = subject;

                BodyBuilder bodyBuilder = new();
                bodyBuilder.TextBody = body;
                if (!string.IsNullOrEmpty(attachmentName) && fileData != null)
                {
                    bodyBuilder.Attachments.Add(attachmentName, fileData);
                }

                email.Body = bodyBuilder.ToMessageBody();

                using SmtpClient smtpClient = new();
                smtpClient.Connect("smtpgtw1.ham.am.honda.com", 25, MailKit.Security.SecureSocketOptions.None);
                //smtpClient.Authenticate("user", "password");
                smtpClient.Send(email);
                smtpClient.Disconnect(true);
            }
            catch (Exception ex)
            {
                logger.Error(ex, (ex.InnerException ?? new()).ToString());
                success = false;
            }
            return success;
        }
    }
}
