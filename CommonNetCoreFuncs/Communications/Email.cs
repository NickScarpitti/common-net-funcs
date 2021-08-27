using MailKit.Net.Smtp;
using MimeKit;
using System;
using System.IO;

namespace CommonNetCoreFuncs.Communications
{
    public static class Email
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public static bool SendEmail(string fromName, string fromEmail, string toName, string toEmail, string subject, string body, string attachmentName = null, MemoryStream fileData = null)
        {
            bool success = true;
            try
            {
                MimeMessage email = new();
                email.From.Add(new MailboxAddress(fromName, fromEmail));
                email.To.Add(new MailboxAddress(toName, toEmail));
                email.Subject = subject;

                BodyBuilder bodyBuilder = new();
                bodyBuilder.TextBody = body;
                if (!string.IsNullOrEmpty(attachmentName) && fileData != null)
                {
                    bodyBuilder.Attachments.Add(attachmentName, fileData);
                }

                email.Body = bodyBuilder.ToMessageBody();

                using SmtpClient smtpClient = new();
                smtpClient.Connect("SMTPGTW1.HAM.AM.HONDA.COM", 25, true);
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
