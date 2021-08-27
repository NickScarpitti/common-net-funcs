using MailKit.Net.Smtp;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public static bool SendEmail(MailAddress from, List<MailAddress> toAddresses, string subject, string body, bool bodyIsHtml = false, List<MailAddress> ccAddresses = null, string attachmentName = null, FileStream fileData = null)
        {
            bool success = true;
            try
            {
                //Confirm emails
                if (!ConfirmValidEmail(from?.Email ?? ""))
                {
                    success = false;
                }

                if (success && toAddresses.Any())
                {
                    foreach (MailAddress mailAddress in toAddresses)
                    {
                        if (!ConfirmValidEmail(mailAddress?.Email ?? ""))
                        {
                            success = false;
                            break;
                        }
                    }
                }
                else
                {
                    success = false;
                }
                

                if (success && ccAddresses != null)
                {
                    if (ccAddresses.Any())
                    {
                        foreach (MailAddress mailAddress in ccAddresses)
                        {
                            if (!ConfirmValidEmail(mailAddress?.Email ?? ""))
                            {
                                success = false;
                                break;
                            }
                        }
                    }
                }

                if (success)
                {
                    MimeMessage email = new();
                    email.From.Add(new MailboxAddress(from.Name, from.Email));
                    email.To.AddRange(toAddresses.Select(x => new MailboxAddress(x.Name, x.Email)).ToList());
                    if (ccAddresses != null)
                    {
                        if (ccAddresses.Any())
                        {
                            email.Cc.AddRange(ccAddresses.Select(x => new MailboxAddress(x.Name, x.Email)).ToList());
                        }
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
