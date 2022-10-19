using System.IO.Compression;
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

public class MailAttachment
{
    public string? AttachmentName { get; set; }
    public Stream? AtttachmentStream { get; set; }
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
    /// <param name="attachmentNames">Enumerable of names for the attachment files. Should be ordered in the same way as the fileData. If missing will use default file name "File #"</param>
    /// <param name="fileData">Enumerable of Streams of file data you want to attach to the email. Should be ordered in the same way as attachmentNames</param>
    /// <param name="readReceipt">Whether or not to add a read receipt request to the email</param>
    /// <param name="readReceiptEmail">Email to send the read receipt to</param>
    /// <param name="smtpUser">User name for the SMTP server, if required. Requires smtpPassword to be set to use</param>
    /// <param name="smtpPassword">Password for the SMTP server, if required. Requires smtpUser to be set to use</param>
    /// <returns>Email sent success bool</returns>
    public static async Task<bool> SendEmail(string? smtpServer, int smtpPort, MailAddress from, IEnumerable<MailAddress> toAddresses, string? subject, string? body, bool bodyIsHtml = false, 
        IEnumerable<MailAddress>? ccAddresses = null, IEnumerable<MailAddress>? bccAddresses = null, IEnumerable<MailAttachment>? attachments = null, bool readReceipt = false, 
        string? readReceiptEmail = null, string? smtpUser = null, string? smtpPassword = null, bool zipAttachments = false)
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

                await AddAttachments(attachments, bodyBuilder, zipAttachments);

                email.Body = bodyBuilder.ToMessageBody();

                if (readReceipt && !string.IsNullOrEmpty(readReceiptEmail))
                {
                    email.Headers[HeaderId.DispositionNotificationTo] = readReceiptEmail;
                }

                for (int i = 0; i < 8; i++)
                {
                    try
                    {
                        using SmtpClient smtpClient = new();
                        if (!string.IsNullOrWhiteSpace(smtpUser) && !string.IsNullOrWhiteSpace(smtpPassword))
                        {
                            await smtpClient.ConnectAsync(smtpServer, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                            await smtpClient.AuthenticateAsync(smtpUser, smtpPassword);
                        }
                        else
                        {
                            await smtpClient.ConnectAsync(smtpServer, smtpPort, MailKit.Security.SecureSocketOptions.None);
                        }
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

    private static async Task AddAttachments(IEnumerable<MailAttachment>? attachments, BodyBuilder bodyBuilder, bool zipAttachments)
    {
        try
        {
            if (attachments != null && attachments.Any())
            {
                if (!zipAttachments)
                {
                    List<Task> tasks = new();
                    int i = 1;
                    foreach (MailAttachment attachment in attachments)
                    {
                        if (attachment.AtttachmentStream != null)
                        {
                            attachment.AtttachmentStream.Position = 0; //Must have this to prevent errors writing data to the attachment
                            tasks.Add(bodyBuilder.Attachments.AddAsync(attachment.AttachmentName ?? $"File {i}", attachment.AtttachmentStream));
                            i++;
                        }
                    }
                    await Task.WhenAll(tasks);
                }
                else
                {
                    int i = 1;
                    using MemoryStream memoryStream = new();
                    using ZipArchive archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true);
                    foreach (MailAttachment attachment in attachments)
                    {
                        if (attachment.AtttachmentStream != null)
                        {
                            attachment.AtttachmentStream.Position = 0; //Must have this to prevent errors writing data to the attachment
                            ZipArchiveEntry entry = archive.CreateEntry(attachment.AttachmentName ?? $"File {i}", CompressionLevel.SmallestSize);
                            using Stream entryStream = entry.Open();
                            await attachment.AtttachmentStream.CopyToAsync(entryStream);
                            await entryStream.FlushAsync();
                            i++;
                        }
                    }
                    archive.Dispose();
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    await bodyBuilder.Attachments.AddAsync("Files.zip", memoryStream);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "AddAttachments Error");
        }
    }
}
