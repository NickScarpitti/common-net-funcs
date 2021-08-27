using MailKit.Net.Smtp;
using MimeKit;
using System;
using System.IO;
using System.Text.RegularExpressions;

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
        public static bool SendEmail(MailAddress from, MailAddress to, string subject, string body, bool bodyIsHtml = false, MailAddress cc = null, string attachmentName = null, FileStream fileData = null)
        {
            bool success = true;
            try
            {
                //Confirm emails
                if (!ConfirmValidEmail(from?.Email ?? "") || !ConfirmValidEmail(to?.Email ?? ""))
                {
                    success = false;
                }

                if (cc != null)
                {
                    if (!ConfirmValidEmail(cc?.Email ?? ""))
                    {
                        success = false;
                    }
                }

                if (success)
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
                    if (bodyIsHtml) { bodyBuilder.HtmlBody = body; }
                    else { bodyBuilder.TextBody = body; }

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

        public static bool ConfirmValidEmail(string email)
        {
            bool isValid = false;
            try
            {
                isValid = Regex.IsMatch(email ?? "", @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (Exception ex)
            {
                logger.Error(ex, (ex.InnerException ?? new()).ToString());
            }
            return isValid;
        }
    }
}
