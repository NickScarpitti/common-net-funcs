using System.Text.RegularExpressions;
using MailKit.Net.Smtp;
using MimeKit;

namespace CommonNetCoreFuncs.Communications;

/// <summary>
/// Class that stores both fields of a Mail Address
/// </summary>
public class MailAddress
{
    public string? Name { get; set; }
    public string? Email { get; set; }
}

public static class Email
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Sends an email using the SMTP server specified in the parameters
    /// </summary>
    /// <param name="smtpServer"></param>
    /// <param name="smtpPort"></param>
    /// <param name="from"></param>
    /// <param name="toAddresses"></param>
    /// <param name="subject"></param>
    /// <param name="body"></param>
    /// <param name="bodyIsHtml">Will render body as HTML if true</param>
    /// <param name="ccAddresses"></param>
    /// <param name="bccAddresses"></param>
    /// <param name="attachmentName"></param>
    /// <param name="fileData">Stream of file data you want to attach to the email</param>
    /// <returns>Email sent success bool</returns>
    public static async Task<bool> SendEmail(string? smtpServer, int smtpPort, MailAddress from, List<MailAddress> toAddresses, string? subject, string? body, bool bodyIsHtml = false, List<MailAddress>? ccAddresses = null, List<MailAddress>? bccAddresses = null, string? attachmentName = null, Stream? fileData = null)
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

            if (success && bccAddresses != null)
            {
                if (bccAddresses.Any())
                {
                    foreach (MailAddress mailAddress in bccAddresses)
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
                email.From.Add(new MailboxAddress(from?.Name, from?.Email));
                email.To.AddRange(toAddresses.Select(x => new MailboxAddress(x.Name, x.Email)).ToList());
                if (ccAddresses != null && ccAddresses.Any())
                {
                    email.Cc.AddRange(ccAddresses.Select(x => new MailboxAddress(x.Name, x.Email)).ToList());
                }
                if (bccAddresses != null && bccAddresses.Any())
                {
                    email.Bcc.AddRange(bccAddresses.Select(x => new MailboxAddress(x.Name, x.Email)).ToList());
                }
                email.Subject = subject;

                BodyBuilder bodyBuilder = new();
                if (bodyIsHtml) { bodyBuilder.HtmlBody = body; }
                else { bodyBuilder.TextBody = body; }

                if (!string.IsNullOrEmpty(attachmentName) && fileData != null)
                {
                    fileData.Position = 0; //Must have this to prevent errors writing data to the attachment
                    bodyBuilder.Attachments.Add(attachmentName, fileData);
                }

                email.Body = bodyBuilder.ToMessageBody();

                for (int i = 0; i < 8; i++)
                {
                    try
                    {
                        using SmtpClient smtpClient = new();
                        await smtpClient.ConnectAsync(smtpServer, smtpPort, MailKit.Security.SecureSocketOptions.None);
                        await smtpClient.SendAsync(email);
                        await smtpClient.DisconnectAsync(true);
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex, "SendEmail Error");
                        if (i == 7) { success = false; } //Sets success to false when the email send fails on the last attempt
                    }
                    Thread.Sleep(500);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "SendEmail Error");
            success = false;
        }
        return success;
    }

    /// <summary>
    /// Checks email string with simple regex to confirm that it is a properly formatted address
    /// </summary>
    /// <param name="email"></param>
    /// <returns>True if email is valid</returns>
    public static bool ConfirmValidEmail(string email)
    {
        bool isValid = false;
        try
        {
            isValid = Regex.IsMatch(email ?? "", @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
        }
        catch (Exception ex)
        {
            logger.Error(ex, "ConfirmValidEmail Error");
        }
        return isValid;
    }
}