using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using System.Text.RegularExpressions;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using static CommonNetFuncs.Core.ExceptionLocation;
using static CommonNetFuncs.Compression.Files;
using static CommonNetFuncs.Email.EmailConstants;

namespace CommonNetFuncs.Email;

public static class EmailConstants
{
    public const string EmailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    public const int MaxEmailLength = 320;
}

/// <summary>
/// Class that stores both fields of a Mail Address
/// </summary>
public class MailAddress
{
    public string? Name { get; set; }

    [MaxLength(MaxEmailLength, ErrorMessage = "Invalid email format")]
    [RegularExpression(EmailRegex, ErrorMessage = "Invalid email format")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string? Email { get; set; }
}

public class MailAttachment
{
    public string? AttachmentName { get; set; }
    public Stream? AttachmentStream { get; set; }
}

public static class Email
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Sends an email using the SMTP server specified in the parameters
    /// </summary>
    /// <param name="smtpServer">The address of the SMTP server to use to sent the email</param>
    /// <param name="smtpPort">Port to use when connecting to the SMPT server</param>
    /// <param name="from">The MailAddress indicating the email address to use in the From field (does not need to be an actual email address)</param>
    /// <param name="toAddresses">List of MailAdresses that indicates who to add as direct recipients of the email</param>
    /// <param name="subject">Text to be used as the subject of the email</param>
    /// <param name="body">Text to be used for the body of the email. Can be HTML or plain text (see bodyIsHtml parameter)</param>
    /// <param name="bodyIsHtml">Will render body as HTML if true</param>
    /// <param name="ccAddresses">List of MailAdresses that indicates who to add as CC recipients of the email</param>
    /// <param name="bccAddresses">List of MailAdresses that indicates who to add as BCC recipients of the email</param>
    /// <param name="attachments">List of attachments with the name to give the file as well as the file data</param>
    /// <param name="readReceipt">Whether or not to add a read receipt request to the email</param>
    /// <param name="readReceiptEmail">Email to send the read receipt to</param>
    /// <param name="smtpUser">User name for the SMTP server, if required. Requires smtpPassword to be set to use</param>
    /// <param name="smtpPassword">Password for the SMTP server, if required. Requires smtpUser to be set to use</param>
    /// <param name="zipAttachments">Will zip all attachments if true</param>
    /// <returns>Email sent success bool</returns>
    public static async Task<bool> SendEmail(string? smtpServer, int smtpPort, MailAddress from, IEnumerable<MailAddress> toAddresses, string? subject, string? body, bool bodyIsHtml = false,
        IEnumerable<MailAddress>? ccAddresses = null, IEnumerable<MailAddress>? bccAddresses = null, IEnumerable<MailAttachment>? attachments = null, bool readReceipt = false,
        string? readReceiptEmail = null, string? smtpUser = null, string? smtpPassword = null, bool zipAttachments = false)
    {
        bool success = true;
        try
        {
            //Confirm emails
            if (!from.Email.IsValidEmail())
            {
                success = false;
            }

            if (success && toAddresses.Any())
            {
                foreach (MailAddress mailAddress in toAddresses)
                {
                    if (!mailAddress.Email.IsValidEmail())
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
                        if (!mailAddress.Email.IsValidEmail())
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
                        if (!mailAddress.Email.IsValidEmail())
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
                if (ccAddresses?.Any() == true)
                {
                    email.Cc.AddRange(ccAddresses.Select(x => new MailboxAddress(x.Name, x.Email)).ToList());
                }
                if (bccAddresses?.Any() == true)
                {
                    email.Bcc.AddRange(bccAddresses.Select(x => new MailboxAddress(x.Name, x.Email)).ToList());
                }
                email.Subject = subject;

                BodyBuilder bodyBuilder = new();
                if (bodyIsHtml) { bodyBuilder.HtmlBody = body; }
                else { bodyBuilder.TextBody = body; }

                await AddAttachments(attachments, bodyBuilder, zipAttachments).ConfigureAwait(false);

                email.Body = bodyBuilder.ToMessageBody();

                if (readReceipt && !string.IsNullOrWhiteSpace(readReceiptEmail))
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
                            await smtpClient.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls).ConfigureAwait(false);
                            await smtpClient.AuthenticateAsync(smtpUser, smtpPassword).ConfigureAwait(false);
                        }
                        else
                        {
                            await smtpClient.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.None).ConfigureAwait(false);
                        }
                        await smtpClient.SendAsync(email).ConfigureAwait(false);
                        await smtpClient.DisconnectAsync(true).ConfigureAwait(false);
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
                        if (i == 7)
                        {
                            logger.Error("{msg}", $"{ex.GetLocationOfException()} Error\nFailed to send email.\nSMTP Server: {smtpServer} | SMTP Port: {smtpPort} | SMTP User: {smtpUser}");
                            success = false; //Sets success to false when the email send fails on the last attempt
                        }
                    }
                    Thread.Sleep(500);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error\nFailed to send email.\nSMTP Server: {smtpServer} | SMTP Port: {smtpPort} | SMTP User: {smtpUser}");
            success = false;
        }
        return success;
    }

    /// <summary>
    /// Checks email string with simple regex to confirm that it is a properly formatted address
    /// </summary>
    /// <param name="email">Email address to validate</param>
    /// <returns>True if email is valid</returns>
    public static bool IsValidEmail(this string? email)
    {
        bool isValid = false;
        try
        {
            isValid = Regex.IsMatch(email ?? "", EmailRegex, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return isValid;
    }

    /// <summary>
    /// Adds attachments to email
    /// </summary>
    /// <param name="attachments">Attachments to add to the email</param>
    /// <param name="bodyBuilder">Builder for the email to add attachments to</param>
    /// <param name="zipAttachments">If true, will perform zip compression on the attachment files before adding them to the email</param>
    private static async Task AddAttachments(IEnumerable<MailAttachment>? attachments, BodyBuilder bodyBuilder, bool zipAttachments)
    {
        try
        {
            if (attachments?.Any() == true)
            {
                if (!zipAttachments)
                {
                    List<Task> tasks = [];
                    int i = 1;
                    foreach (MailAttachment attachment in attachments)
                    {
                        if (attachment.AttachmentStream != null)
                        {
                            attachment.AttachmentStream.Position = 0; //Must have this to prevent errors writing data to the attachment
                            tasks.Add(bodyBuilder.Attachments.AddAsync(attachment.AttachmentName ?? $"File {i}", attachment.AttachmentStream));
                            i++;
                        }
                    }
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                else
                {
                    await using MemoryStream memoryStream = new();
                    using ZipArchive archive = new(memoryStream, ZipArchiveMode.Create, true);

                    await attachments.Where(x => !string.IsNullOrWhiteSpace(x.AttachmentName)).Select(x => (x.AttachmentStream, x.AttachmentName!)).AddFilesToZip(archive, CompressionLevel.SmallestSize).ConfigureAwait(false);

                    //foreach (MailAttachment attachment in attachments)
                    //{
                    //    //await attachment.AttachmentStream.AddZipToArchive(archive, attachment.AttachmentName, CompressionLevel.SmallestSize);
                    //    if (attachment.AttachmentStream != null)
                    //    {
                    //        attachment.AttachmentStream.Position = 0; //Must have this to prevent errors writing data to the attachment
                    //        ZipArchiveEntry entry = archive.CreateEntry(attachment.AttachmentName ?? $"File {archive.Entries.Count}", CompressionLevel.SmallestSize);
                    //        await using Stream entryStream = entry.Open();
                    //        await attachment.AttachmentStream.CopyToAsync(entryStream);
                    //        await entryStream.FlushAsync();
                    //    }
                    //}
                    archive.Dispose();
                    memoryStream.Position = 0;
                    await bodyBuilder.Attachments.AddAsync("Files.zip", memoryStream).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
    }
}
