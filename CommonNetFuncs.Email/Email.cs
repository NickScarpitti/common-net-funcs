using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using MailKit.Net.Proxy;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Newtonsoft.Json;
using static CommonNetFuncs.Compression.Files;
using static CommonNetFuncs.Email.EmailConstants;

namespace CommonNetFuncs.Email;

public static class EmailConstants
{
	public const string EmailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
	public const int MaxEmailLength = 320;
}

/// <summary>
/// Interface for email attachments
/// </summary>
public interface IMailAttachment
{
	string? AttachmentName { get; set; }
	Stream? GetStream();
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

/// <summary>
/// Represents an email attachment with a name and stream. This class takes ownership of the stream and will dispose it when disposed.
/// </summary>
public sealed class MailAttachment : IMailAttachment, IAsyncDisposable, IDisposable
{
	private bool disposed;

	public MailAttachment(string? AttachmentName = null, byte[]? AttachmentBytes = null)
	{
		this.AttachmentName = AttachmentName;
		if (AttachmentBytes != null)
		{
			AttachmentStream = new MemoryStream();
			AttachmentStream.Write(AttachmentBytes, 0, AttachmentBytes.Length);
			AttachmentStream.Position = 0;
		}
	}

	public MailAttachment(string? AttachmentName = null, Stream? AttachmentStream = null)
	{
		this.AttachmentName = AttachmentName;
		this.AttachmentStream = AttachmentStream;
	}

	public string? AttachmentName { get; set; }

	public Stream? AttachmentStream { get; set; }

	public Stream? GetStream()
	{
		return AttachmentStream;
	}

	public async ValueTask DisposeAsync()
	{
		if (!disposed)
		{
			if (AttachmentStream != null)
			{
				await AttachmentStream.DisposeAsync().ConfigureAwait(false);
			}
			disposed = true;
		}
		GC.SuppressFinalize(this);
	}

	public void Dispose()
	{
		if (!disposed)
		{
			AttachmentStream?.Dispose();
			disposed = true;
		}
		GC.SuppressFinalize(this);
	}
}

public sealed class MailAttachmentBytes : IMailAttachment
{
	[JsonConstructor] // Required for Hangfire serialization
	public MailAttachmentBytes(string? AttachmentName = null, byte[]? AttachmentBytes = null)
	{
		this.AttachmentName = AttachmentName;
		this.AttachmentBytes = AttachmentBytes;
	}

	public MailAttachmentBytes(string? AttachmentName = null, Stream? AttachmentStream = null)
	{
		using MemoryStream memoryStream = new();
		AttachmentStream?.CopyTo(memoryStream);

		this.AttachmentName = AttachmentName;
		AttachmentBytes = memoryStream.ToArray();
	}

	public string? AttachmentName { get; set; }

	public byte[]? AttachmentBytes { get; set; }

	public Stream? GetStream()
	{
		if (AttachmentBytes == null)
		{
			return null;
		}

		MemoryStream stream = new();
		stream.Write(AttachmentBytes, 0, AttachmentBytes.Length);
		stream.Position = 0;
		return stream;
	}
}

public sealed class SendEmailConfig(SmtpSettings? smtpSettings = null, EmailAddresses? emailAddresses = null, EmailContent? emailContent = null, bool readReceipt = false, string? readReceiptEmail = null)
{
	/// <summary>
	/// Gets or sets the values to use for the SMTP server connection.
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

public sealed class SmtpSettings
{
	public SmtpSettings()
	{
		ConnectionTimeout = TimeSpan.FromSeconds(5);
	}

	public SmtpSettings(string? smtpServer, int smtpPort, string? smtpUser = null, string? smtpPassword = null, TimeSpan? connectionTimeout = null)
	{
		SmtpServer = smtpServer;
		SmtpPort = smtpPort;
		SmtpUser = smtpUser;
		SmtpPassword = smtpPassword;
		ConnectionTimeout = connectionTimeout ?? TimeSpan.FromSeconds(5);
	}


	/// <summary>
	/// Gets or sets the SMTP server address used for sending emails.
	/// </summary>
	public string? SmtpServer { get; set; }

	/// <summary>
	/// Gets or sets the port number used for the SMTP server connection.
	/// </summary>
	/// <remarks>The port number must match the configuration of the SMTP server being used. Incorrect values may result in connection failures.</remarks>
	public int SmtpPort { get; set; }

	/// <summary>
	/// Gets or sets the username used for authenticating with the SMTP server.
	/// </summary>
	public string? SmtpUser { get; set; }

	/// <summary>
	/// Gets or sets the password for the SMTP server, if required.
	/// </summary>
	public string? SmtpPassword { get; set; }

	/// <summary>
	/// Gets or sets the duration to wait before a connection attempt times out.
	/// </summary>
	public TimeSpan ConnectionTimeout { get; set; }

	/// <summary>
	/// Gets or sets the SecureSocketOptions to use with the SMTP connection.
	/// </summary>
	/// <remarks>Defaults to StartTls when SmtpUser and SmtpPassword are used, otherwise, defaults to None.</remarks>
	public SecureSocketOptions? SecureSocketOptions { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether Transport Layer Security (TLS) is required for secure communication.
	/// </summary>
	/// <remarks>
	/// If set to <see langword="true"/>, all communications must use TLS to ensure data security.
	/// If set to <see langword="false"/>, non-TLS communications are allowed.
	/// A <see langword="null"/> value indicates that the requirement is not specified.
	/// </remarks>
	public bool? RequireTLS { get; set; }

	/// <summary>
	/// Gets or sets the collection of client certificates used to authenticate the client to a server.
	/// </summary>
	/// <remarks>
	/// Add one or more valid client certificates to this collection to enable client authentication during secure connections.
	/// Ensure that each certificate is properly configured and trusted by the target server.
	/// The collection may be null if no client certificates are required.
	/// </remarks>
	public X509CertificateCollection? ClientCertificates { get; set; }

	/// <summary>
	/// Gets or sets the proxy client used to route network requests through a proxy server.
	/// </summary>
	/// <remarks>
	/// Assigning a proxy client enables network operations to be performed via the specified proxy.
	/// If this property is null, network requests are made directly without using a proxy.
	/// </remarks>
	public IProxyClient? ProxyClient { get; set; }

	/// <summary>
	/// Gets or sets the local network endpoint for the connection, including the IP address and port number.
	/// </summary>
	/// <remarks>
	/// This property is useful for determining which local address and port are being used for the connection.
	/// The value may be null if the local endpoint has not been assigned.
	/// </remarks>
	public IPEndPoint? LocalEndPoint { get; set; }

	/// <summary>
	/// Gets or sets the local domain name associated with the application.
	/// </summary>
	/// <remarks>
	/// This property can be null if the local domain is not set.
	/// It is typically used to identify the domain in which the application is running if required by network configurations.
	/// </remarks>
	public string? LocalDomain { get; set; }
}

public sealed class EmailAddresses(MailAddress? fromAddress = null, IEnumerable<MailAddress>? toAddresses = null, IEnumerable<MailAddress>? ccAddresses = null, IEnumerable<MailAddress>? bccAddresses = null)
{
	/// <summary>
	/// Gets or sets the sender's email address for the outgoing mail message.
	/// </summary>
	public MailAddress FromAddress { get; set; } = fromAddress ?? new();

	/// <summary>
	/// Gets or sets the collection of recipient email addresses for the message.
	/// </summary>
	public MailAddress[] ToAddresses { get; set; } = toAddresses?.ToArray() ?? Array.Empty<MailAddress>();

	/// <summary>
	/// Gets or sets the collection of email addresses to be included as CC (carbon copy) recipients.
	/// </summary>
	public MailAddress[] CcAddresses { get; set; } = ccAddresses?.ToArray() ?? Array.Empty<MailAddress>();

	/// <summary>
	/// Gets or sets the collection of email addresses to be included as blind carbon copy (BCC) recipients.
	/// </summary>
	public MailAddress[] BccAddresses { get; set; } = bccAddresses?.ToArray() ?? Array.Empty<MailAddress>();
}

public sealed class EmailContent(string? subject = null, string? body = null, bool bodyIsHtml = false, IEnumerable<IMailAttachment>? attachments = null, bool autoDisposeAttachments = true, bool zipAttachments = false)
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
	public IMailAttachment[]? Attachments { get; set; } = attachments?.ToArray();

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

/// <summary>
/// Email content configuration using MailAttachmentBytes for serialization-friendly scenarios (e.g., Hangfire background jobs).
/// </summary>
public sealed class EmailContentBytes(string? subject = null, string? body = null, bool bodyIsHtml = false, IEnumerable<MailAttachmentBytes>? attachments = null, bool zipAttachments = false)
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
	/// Gets or sets the collection of byte-based attachments associated with the mail message.
	/// </summary>
	public MailAttachmentBytes[]? Attachments { get; set; } = attachments?.ToArray();

	/// <summary>
	/// Gets or sets a value indicating whether email attachments should be compressed into a ZIP archive.
	/// </summary>
	public bool ZipAttachments { get; set; } = zipAttachments;
}

/// <summary>
/// Send email configuration using EmailContentBytes for serialization-friendly scenarios (e.g., Hangfire background jobs).
/// </summary>
public sealed class SendEmailConfigBytes(SmtpSettings? smtpSettings = null, EmailAddresses? emailAddresses = null, EmailContentBytes? emailContent = null, bool readReceipt = false, string? readReceiptEmail = null)
{
	/// <summary>
	/// Gets or sets the values to use for the SMTP server connection.
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
	public EmailContentBytes EmailContent { get; set; } = emailContent ?? new();
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

			// Check that there is at least one recipient
			success = (success && sendEmailConfig.EmailAddresses.ToAddresses.Length != 0) || sendEmailConfig.EmailAddresses.CcAddresses?.Length > 0 || sendEmailConfig.EmailAddresses.BccAddresses?.Length > 0;

			// Validate all recipient email addresses
			success = success && sendEmailConfig.EmailAddresses.ToAddresses.All(mailAddress => mailAddress.Email.IsValidEmail());
			success = success && (sendEmailConfig.EmailAddresses.CcAddresses?.All(mailAddress => mailAddress.Email.IsValidEmail()) != false);
			success = success && (sendEmailConfig.EmailAddresses.BccAddresses?.All(mailAddress => mailAddress.Email.IsValidEmail()) != false);

			if (success)
			{
				MimeMessage email = new();
				email.From.Add(new MailboxAddress(sendEmailConfig.EmailAddresses.FromAddress?.Name, sendEmailConfig.EmailAddresses.FromAddress?.Email ?? string.Empty));
				email.To.AddRange(sendEmailConfig.EmailAddresses.ToAddresses.Select(x => new MailboxAddress(x.Name, x.Email ?? string.Empty)).ToList());
				if (sendEmailConfig.EmailAddresses.CcAddresses?.Length > 0)
				{
					email.Cc.AddRange(sendEmailConfig.EmailAddresses.CcAddresses.Select(x => new MailboxAddress(x.Name, x.Email ?? string.Empty)).ToList());
				}
				if (sendEmailConfig.EmailAddresses.BccAddresses?.Length > 0)
				{
					email.Bcc.AddRange(sendEmailConfig.EmailAddresses.BccAddresses.Select(x => new MailboxAddress(x.Name, x.Email ?? string.Empty)).ToList());
				}
				email.Subject = sendEmailConfig.EmailContent.Subject ?? string.Empty;

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
						using SmtpClient smtpClient = new()
						{
							Timeout = (int)sendEmailConfig.SmtpSettings.ConnectionTimeout.TotalMilliseconds
						};

						if (sendEmailConfig.SmtpSettings.RequireTLS != null)
						{
							smtpClient.RequireTLS = (bool)sendEmailConfig.SmtpSettings.RequireTLS;
						}

						if (sendEmailConfig.SmtpSettings.ClientCertificates != null)
						{
							smtpClient.ClientCertificates = sendEmailConfig.SmtpSettings.ClientCertificates;
						}

						if (sendEmailConfig.SmtpSettings.ProxyClient != null)
						{
							smtpClient.ProxyClient = sendEmailConfig.SmtpSettings.ProxyClient;
						}

						if (sendEmailConfig.SmtpSettings.LocalEndPoint != null)
						{
							smtpClient.LocalEndPoint = sendEmailConfig.SmtpSettings.LocalEndPoint;
						}

						if (sendEmailConfig.SmtpSettings.LocalDomain != null)
						{
							smtpClient.LocalDomain = sendEmailConfig.SmtpSettings.LocalDomain;
						}

						if (!string.IsNullOrWhiteSpace(sendEmailConfig.SmtpSettings.SmtpUser) && !string.IsNullOrWhiteSpace(sendEmailConfig.SmtpSettings.SmtpPassword))
						{
							await smtpClient.ConnectAsync(sendEmailConfig.SmtpSettings.SmtpServer, sendEmailConfig.SmtpSettings.SmtpPort,
								sendEmailConfig.SmtpSettings.SecureSocketOptions ?? SecureSocketOptions.StartTls, cancellationToken).ConfigureAwait(false);
							await smtpClient.AuthenticateAsync(sendEmailConfig.SmtpSettings.SmtpUser, sendEmailConfig.SmtpSettings.SmtpPassword, cancellationToken).ConfigureAwait(false);
						}
						else
						{
							await smtpClient.ConnectAsync(sendEmailConfig.SmtpSettings.SmtpServer, sendEmailConfig.SmtpSettings.SmtpPort,
								sendEmailConfig.SmtpSettings.SecureSocketOptions ?? SecureSocketOptions.None, cancellationToken).ConfigureAwait(false);
						}
						await smtpClient.SendAsync(email, cancellationToken).ConfigureAwait(false);
						await smtpClient.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
						break;
					}
					catch (Exception ex)
					{
						logger.Warn(ex, "{Class}.{Method} Error", nameof(Email), nameof(SendEmail));
						if (i == 7)
						{
							logger.Error("{Class}.{Method} Error\nFailed to send email.\nSMTP Server: {SmtpServer} | SMTP Port: {SmtpPort} | SMTP User: {SmtpUser}", nameof(Email), nameof(SendEmail), sendEmailConfig.SmtpSettings.SmtpServer, sendEmailConfig.SmtpSettings.SmtpPort, sendEmailConfig.SmtpSettings.SmtpUser);
							success = false; //Sets success to false when the email send fails on the last attempt
						}
					}
					await Task.Delay(500, cancellationToken).ConfigureAwait(false);
				}
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{Class}.{Method} Error\nFailed to send email.\nSMTP Server: {SmtpServer} | SMTP Port: {SmtpPort} | SMTP User: {SmtpUser}", nameof(Email), nameof(SendEmail), sendEmailConfig.SmtpSettings.SmtpServer, sendEmailConfig.SmtpSettings.SmtpPort, sendEmailConfig.SmtpSettings.SmtpUser);
			success = false;
		}

		if (sendEmailConfig.EmailContent.AutoDisposeAttachments)
		{
			foreach (IMailAttachment attachment in sendEmailConfig.EmailContent.Attachments ?? [])
			{
				if (attachment is IAsyncDisposable asyncDisposable)
				{
					await asyncDisposable.DisposeAsync().ConfigureAwait(false);
				}
				else if (attachment is IDisposable disposable)
				{
					disposable.Dispose();
				}
			}
		}

		return success;
	}

	/// <summary>
	/// Sends an email using the SMTP server specified in the parameters. This overload uses MailAttachmentBytes for serialization-friendly scenarios (e.g., Hangfire background jobs).
	/// </summary>
	/// <param name="sendEmailConfig">Configuration options for sending the email with byte-based attachments.</param>
	/// <param name="cancellationToken">Cancellation token for this operation.</param>
	/// <returns><see langword="true"/> if email was sent successfully, otherwise <see langword="false"/></returns>
	public static Task<bool> SendEmail(SendEmailConfigBytes sendEmailConfig, CancellationToken cancellationToken = default)
	{
		// Convert to standard SendEmailConfig
		SendEmailConfig standardConfig = new(
			sendEmailConfig.SmtpSettings,
			sendEmailConfig.EmailAddresses,
			new EmailContent(
				sendEmailConfig.EmailContent.Subject,
				sendEmailConfig.EmailContent.Body,
				sendEmailConfig.EmailContent.BodyIsHtml,
				sendEmailConfig.EmailContent.Attachments?.Cast<IMailAttachment>(),
				autoDisposeAttachments: false, // Don't dispose byte-based attachments
				sendEmailConfig.EmailContent.ZipAttachments
			),
			sendEmailConfig.ReadReceipt,
			sendEmailConfig.ReadReceiptEmail
		);

		return SendEmail(standardConfig, cancellationToken);
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
			isValid = !string.IsNullOrWhiteSpace(email) && Regex.IsMatch(email ?? string.Empty, EmailRegex, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{Class}.{Method} Error", nameof(Email), nameof(IsValidEmail));
		}
		return isValid;
	}

	/// <summary>
	/// Adds attachments to email
	/// </summary>
	/// <param name="attachments">Attachments to add to the email</param>
	/// <param name="bodyBuilder">Builder for the email to add attachments to</param>
	/// <param name="zipAttachments">If <see langword="true"/>, will perform zip compression on the attachment files before adding them to the email</param>
	public static async Task AddAttachments(IEnumerable<IMailAttachment>? attachments, BodyBuilder bodyBuilder, bool zipAttachments, CancellationToken cancellationToken = default)
	{
		try
		{
			if (attachments?.Any() == true)
			{
				if (!zipAttachments)
				{
					List<Task> tasks = [];
					int i = 1;
					foreach (IMailAttachment attachment in attachments)
					{
						Stream? stream = attachment.GetStream();
						if (stream != null)
						{
							stream.Position = 0; //Must have this to prevent errors writing data to the attachment
							tasks.Add(bodyBuilder.Attachments.AddAsync(attachment.AttachmentName ?? $"File {i}", stream, cancellationToken));
							i++;
						}
					}
					await Task.WhenAll(tasks).ConfigureAwait(false);
				}
				else
				{
					await using MemoryStream memoryStream = new();
					await using ZipArchive archive = new(memoryStream, ZipArchiveMode.Create, true);

					await attachments.Where(x => !string.IsNullOrWhiteSpace(x.AttachmentName)).Select(x => (x.GetStream(), x.AttachmentName!)).AddFilesToZip(archive, CompressionLevel.SmallestSize, cancellationToken).ConfigureAwait(false);

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
					await archive.DisposeAsync();
					memoryStream.Position = 0;
					await bodyBuilder.Attachments.AddAsync("Files.zip", memoryStream, cancellationToken).ConfigureAwait(false);
				}
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{Class}.{Method} Error", nameof(Email), nameof(AddAttachments));
		}
	}
}
