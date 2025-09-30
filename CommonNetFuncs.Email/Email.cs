using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using System.Text.RegularExpressions;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
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
public sealed class MailAddress(string? Name = null, string? Email = null)
{
  public string? Name { get; set; } = Name;

  [MaxLength(MaxEmailLength, ErrorMessage = "Invalid email format")]
  [RegularExpression(EmailRegex, ErrorMessage = "Invalid email format")]
  [EmailAddress(ErrorMessage = "Invalid email format")]
  public string? Email { get; set; } = Email;
}

public sealed class MailAttachment(string? AttachmentName = null, Stream? AttachmentStream = null)
{
  public string? AttachmentName { get; set; } = AttachmentName;

  public Stream? AttachmentStream { get; set; } = AttachmentStream;
}

public sealed class SendEmailConfig(SmtpSettings? smtpSettings = null, EmailAddresses? emailAddresses = null, EmailContent? emailContent = null, bool readReceipt = false, string? readReceiptEmail = null)
{
  /// <summary>
  /// Gets or sets the values to use for the SMTP server conncetion.
  /// </summary>
  public SmtpSettings SmtpSettings { get; set; } = smtpSettings ?? new();

  /// <summary>
  /// Gets or sets the email addresses used in the email, including From, To, CC, and BCC.
  /// </summary>
  public EmailAddresses EmailAddresses { get; set; } = emailAddresses ?? new();

  /// <summary>
  /// Gets or sets a value indicating whether a read receipt request should be added to the email. ReadReceiptEmail must have a value for the read receipt to work.
  /// </summary>
  public bool ReadReceipt { get; set; } = readReceipt;

  /// <summary>
  /// Gets or sets the email address to which read receipts should be sent when ReadReceipt is true.
  /// </summary>
  public string? ReadReceiptEmail { get; set; } = readReceiptEmail;

  /// <summary>
  /// Gets or sets the email content to be sent, including subject, body, and attachments.
  /// </summary>
  public EmailContent EmailContent { get; set; } = emailContent ?? new();
}

public sealed class SmtpSettings(string? smtpServer = null, int smtpPort = default, string? smtpUser = null, string? smtpPassword = null)
{
  /// <summary>
  /// Gets or sets the SMTP server address used for sending emails.
  /// </summary>
  public string? SmtpServer { get; set; } = smtpServer;

  /// <summary>
  /// Gets or sets the port number used for the SMTP server connection.
  /// </summary>
  /// <remarks>The port number must match the configuration of the SMTP server being used. Incorrect values may result in connection failures.</remarks>
  public int SmtpPort { get; set; } = smtpPort;

  /// <summary>
  /// Gets or sets the username used for authenticating with the SMTP server.
  /// </summary>
  public string? SmtpUser { get; set; } = smtpUser;

  /// <summary>
  /// Gets or sets the password for the SMTP server, if required.
  /// </summary>
  public string? SmtpPassword { get; set; } = smtpPassword;
}

public sealed class  EmailAddresses(MailAddress? fromAddress = null, IEnumerable<MailAddress>? toAddresses = null, IEnumerable<MailAddress>? ccAddresses = null, IEnumerable<MailAddress>? bccAddresses = null)
{
  /// <summary>
  /// Gets or sets the sender's email address for the outgoing mail message.
  /// </summary>
  public MailAddress FromAddress { get; set; } = fromAddress ?? new();

  /// <summary>
  /// Gets or sets the collection of recipient email addresses for the message.
  /// </summary>
  public IEnumerable<MailAddress> ToAddresses { get; set; } = toAddresses ?? [];

  /// <summary>
  /// Gets or sets the collection of email addresses to be included as CC (carbon copy) recipients.
  /// </summary>
  public IEnumerable<MailAddress> CcAddresses { get; set; } = ccAddresses ?? [];

  /// <summary>
  /// Gets or sets the collection of email addresses to be included as blind carbon copy (BCC) recipients.
  /// </summary>
  public IEnumerable<MailAddress> BccAddresses { get; set; } = bccAddresses ?? [];
}

public sealed class EmailContent(string? subject = null, string? body = null, bool bodyIsHtml = false, IEnumerable<MailAttachment>? attachments = null, bool autoDisposeAttachments = true, bool zipAttachments = false)
{
  /// <summary>
  /// Gets or sets the subject of the message.
  /// </summary>
  public string? Subject { get; set; } = subject;

  /// <summary>
  /// Gets or sets the body content of the message.
  /// </summary>
  public string? Body { get; set; } = body;

  /// <summary>
  /// Gets or sets a value indicating whether the body of the message is formatted as HTML.
  /// </summary>
  public bool BodyIsHtml { get; set; } = bodyIsHtml;

  /// <summary>
  /// Gets or sets the collection of attachments associated with the mail message.
  /// </summary>
  public IEnumerable<MailAttachment>? Attachments { get; set; } = attachments;

  /// <summary>
  /// Gets or sets a value indicating whether attachments should be automatically disposed when they are no longer needed.
  /// </summary>
  /// <remarks>When this property is set to <see langword="true"/>, any attachments associated with the object will be disposed of automatically to free up resources.
  /// Set this property to <see langword="false"/> if you want to manage the disposal of attachments manually.</remarks>
  public bool AutoDisposeAttachments { get; set; } = autoDisposeAttachments;

  /// <summary>
  /// Gets or sets a value indicating whether email attachments should be compressed into a ZIP archive.
  /// </summary>
  public bool ZipAttachments { get; set; } = zipAttachments;
}

public static class Email
{
  private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

  /// <summary>
  /// Sends an email using the SMTP server specified in the parameters.
  /// </summary>
  /// <param name="sendEmailConfig">Configuration options for sending the email.</param>
  /// <param name="cancellationToken">Cancellation token for this operation.</param>
  /// <returns><see langword="true"/> if email was sent successfully, otherwise <see langword="false"/></returns>
  public static async Task<bool> SendEmail(SendEmailConfig sendEmailConfig, CancellationToken cancellationToken = default)
  {
    bool success = true;
    try
    {
      // Confirm emails
      if (!sendEmailConfig.EmailAddresses.FromAddress.Email.IsValidEmail())
      {
        success = false;
      }

      if (success && sendEmailConfig.EmailAddresses.ToAddresses.Any())
      {
        foreach (MailAddress mailAddress in sendEmailConfig.EmailAddresses.ToAddresses)
        {
          if (!mailAddress.Email.IsValidEmail())
          {
            success = false;
            break;
          }
        }
      }

      if (success && (sendEmailConfig.EmailAddresses.CcAddresses != null))
      {
        if (sendEmailConfig.EmailAddresses.CcAddresses.Any())
        {
          foreach (MailAddress mailAddress in sendEmailConfig.EmailAddresses.CcAddresses)
          {
            if (!mailAddress.Email.IsValidEmail())
            {
              success = false;
              break;
            }
          }
        }
      }

      if (success && (sendEmailConfig.EmailAddresses.BccAddresses != null))
      {
        if (sendEmailConfig.EmailAddresses.BccAddresses.Any())
        {
          foreach (MailAddress mailAddress in sendEmailConfig.EmailAddresses.BccAddresses)
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
        email.From.Add(new MailboxAddress(sendEmailConfig.EmailAddresses.FromAddress?.Name, sendEmailConfig.EmailAddresses.FromAddress?.Email));
        email.To.AddRange(sendEmailConfig.EmailAddresses.ToAddresses.Select(x => new MailboxAddress(x.Name, x.Email)).ToList());
        if (sendEmailConfig.EmailAddresses.CcAddresses?.Any() == true)
        {
          email.Cc.AddRange(sendEmailConfig.EmailAddresses.CcAddresses.Select(x => new MailboxAddress(x.Name, x.Email)).ToList());
        }
        if (sendEmailConfig.EmailAddresses.BccAddresses?.Any() == true)
        {
          email.Bcc.AddRange(sendEmailConfig.EmailAddresses.BccAddresses.Select(x => new MailboxAddress(x.Name, x.Email)).ToList());
        }
        email.Subject = sendEmailConfig.EmailContent.Subject;

        BodyBuilder bodyBuilder = new();
        if (sendEmailConfig.EmailContent.BodyIsHtml)
        {
          bodyBuilder.HtmlBody = sendEmailConfig.EmailContent.Body;
        }
        else
        {
          bodyBuilder.TextBody = sendEmailConfig.EmailContent.Body;
        }

        await AddAttachments(sendEmailConfig.EmailContent.Attachments, bodyBuilder, sendEmailConfig.EmailContent.ZipAttachments, cancellationToken).ConfigureAwait(false);

        email.Body = bodyBuilder.ToMessageBody();

        if (sendEmailConfig.ReadReceipt && !string.IsNullOrWhiteSpace(sendEmailConfig.ReadReceiptEmail))
        {
          email.Headers[HeaderId.DispositionNotificationTo] = sendEmailConfig.ReadReceiptEmail;
        }

        for (int i = 0; i < 8; i++)
        {
          try
          {
            using SmtpClient smtpClient = new();
            if (!string.IsNullOrWhiteSpace(sendEmailConfig.SmtpSettings.SmtpUser) && !string.IsNullOrWhiteSpace(sendEmailConfig.SmtpSettings.SmtpPassword))
            {
              await smtpClient.ConnectAsync(sendEmailConfig.SmtpSettings.SmtpServer, sendEmailConfig.SmtpSettings.SmtpPort, SecureSocketOptions.StartTls, cancellationToken).ConfigureAwait(false);
              await smtpClient.AuthenticateAsync(sendEmailConfig.SmtpSettings.SmtpUser, sendEmailConfig.SmtpSettings.SmtpPassword, cancellationToken).ConfigureAwait(false);
            }
            else
            {
              await smtpClient.ConnectAsync(sendEmailConfig.SmtpSettings.SmtpServer, sendEmailConfig.SmtpSettings.SmtpPort, SecureSocketOptions.None, cancellationToken).ConfigureAwait(false);
            }
            await smtpClient.SendAsync(email, cancellationToken).ConfigureAwait(false);
            await smtpClient.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
            break;
          }
          catch (Exception ex)
          {
            logger.Warn(ex, "{msg}", $"{nameof(Email)}.{nameof(SendEmail)} Error");
            if (i == 7)
            {
              logger.Error("{msg}", $"{nameof(Email)}.{nameof(SendEmail)} Error\nFailed to send email.\nSMTP Server: {sendEmailConfig.SmtpSettings.SmtpServer} | SMTP Port: {sendEmailConfig.SmtpSettings.SmtpPort} | SMTP User: {sendEmailConfig.SmtpSettings.SmtpUser}");
              success = false; //Sets success to false when the email send fails on the last attempt
            }
          }
          Thread.Sleep(500);
        }
      }
    }
    catch (Exception ex)
    {
      logger.Error(ex, "{msg}", $"{nameof(Email)}.{nameof(SendEmail)} Error\nFailed to send email.\nSMTP Server: {sendEmailConfig.SmtpSettings.SmtpServer} | SMTP Port: {sendEmailConfig.SmtpSettings.SmtpPort} | SMTP User: {sendEmailConfig.SmtpSettings.SmtpUser}");
      success = false;
    }

    if (sendEmailConfig.EmailContent.AutoDisposeAttachments)
    {
      foreach (MailAttachment attachment in sendEmailConfig.EmailContent.Attachments?.Where(x => x.AttachmentStream != null) ?? [])
      {
        await attachment.AttachmentStream!.DisposeAsync().ConfigureAwait(false);
      }
    }

    return success;
  }

  /// <summary>
  /// Checks email string with simple regex to confirm that it is a properly formatted address
  /// </summary>
  /// <param name="email">Email address to validate</param>
  /// <returns><see langword="true"/> if email is valid</returns>
  public static bool IsValidEmail(this string? email)
  {
    bool isValid = false;
    try
    {
      isValid = Regex.IsMatch(email ?? string.Empty, EmailRegex, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
    }
    catch (Exception ex)
    {
      logger.Error(ex, "{msg}", $"{nameof(Email)}.{nameof(IsValidEmail)} Error");
    }
    return isValid;
  }

  /// <summary>
  /// Adds attachments to email
  /// </summary>
  /// <param name="attachments">Attachments to add to the email</param>
  /// <param name="bodyBuilder">Builder for the email to add attachments to</param>
  /// <param name="zipAttachments">If <see langword="true"/>, will perform zip compression on the attachment files before adding them to the email</param>
  public static async Task AddAttachments(IEnumerable<MailAttachment>? attachments, BodyBuilder bodyBuilder, bool zipAttachments, CancellationToken cancellationToken = default)
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
              tasks.Add(bodyBuilder.Attachments.AddAsync(attachment.AttachmentName ?? $"File {i}", attachment.AttachmentStream, cancellationToken));
              i++;
            }
          }
          await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        else
        {
          await using MemoryStream memoryStream = new();
          using ZipArchive archive = new(memoryStream, ZipArchiveMode.Create, true);

          await attachments.Where(x => !string.IsNullOrWhiteSpace(x.AttachmentName)).Select(x => (x.AttachmentStream, x.AttachmentName!)).AddFilesToZip(archive, CompressionLevel.SmallestSize, cancellationToken).ConfigureAwait(false);

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
          await bodyBuilder.Attachments.AddAsync("Files.zip", memoryStream, cancellationToken).ConfigureAwait(false);
        }
      }
    }
    catch (Exception ex)
    {
      logger.Error(ex, "{msg}", $"{nameof(Email)}.{nameof(AddAttachments)} Error");
    }
  }
}
