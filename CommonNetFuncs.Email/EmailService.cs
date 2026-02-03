namespace CommonNetFuncs.Email;

/// <summary>
/// Interface for use with dependency injection
/// </summary>
public interface IEmailService
{
	Task<bool> SendEmail(SmtpSettings smtpSettings, EmailAddresses emailAddresses, EmailContent emailContent, bool readReceipt, string readReceiptEmail, CancellationToken cancellationToken = default);

	Task<bool> SendEmail(SmtpSettings smtpSettings, EmailAddresses emailAddresses, EmailContent emailContent, CancellationToken cancellationToken = default);

	Task<bool> SendEmail(SmtpSettings smtpSettings, EmailAddresses emailAddresses, EmailContentBytes emailContent, bool readReceipt, string readReceiptEmail, CancellationToken cancellationToken = default);

	Task<bool> SendEmail(SmtpSettings smtpSettings, EmailAddresses emailAddresses, EmailContentBytes emailContent, CancellationToken cancellationToken = default);

	Task<bool> SendEmail(SendEmailConfig sendEmailConfig, CancellationToken cancellationToken = default);

	Task<bool> SendEmail(SendEmailConfigBytes sendEmailConfig, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of IEmailService that can be used with dependency injection in order to speed up sending multiple emails
/// </summary>
public sealed class EmailService : IEmailService
{
	public Task<bool> SendEmail(SmtpSettings smtpSettings, EmailAddresses emailAddresses, EmailContent emailContent, bool readReceipt, string readReceiptEmail, CancellationToken cancellationToken = default)
	{
		return Email.SendEmail(new SendEmailConfig()
		{
			SmtpSettings = smtpSettings,
			EmailAddresses = emailAddresses,
			EmailContent = emailContent,
			ReadReceipt = readReceipt,
			ReadReceiptEmail = readReceiptEmail
		}, cancellationToken);
	}

	public Task<bool> SendEmail(SmtpSettings smtpSettings, EmailAddresses emailAddresses, EmailContent emailContent, CancellationToken cancellationToken = default)
	{
		return Email.SendEmail(new SendEmailConfig()
		{
			SmtpSettings = smtpSettings,
			EmailAddresses = emailAddresses,
			EmailContent = emailContent,
		}, cancellationToken);
	}
	public Task<bool> SendEmail(SmtpSettings smtpSettings, EmailAddresses emailAddresses, EmailContentBytes emailContent, bool readReceipt, string readReceiptEmail, CancellationToken cancellationToken = default)
	{
		return Email.SendEmail(new SendEmailConfigBytes()
		{
			SmtpSettings = smtpSettings,
			EmailAddresses = emailAddresses,
			EmailContent = emailContent,
			ReadReceipt = readReceipt,
			ReadReceiptEmail = readReceiptEmail
		}, cancellationToken);
	}

	public Task<bool> SendEmail(SmtpSettings smtpSettings, EmailAddresses emailAddresses, EmailContentBytes emailContent, CancellationToken cancellationToken = default)
	{
		return Email.SendEmail(new SendEmailConfigBytes()
		{
			SmtpSettings = smtpSettings,
			EmailAddresses = emailAddresses,
			EmailContent = emailContent,
		}, cancellationToken);
	}

	public Task<bool> SendEmail(SendEmailConfig sendEmailConfig, CancellationToken cancellationToken = default)
	{
		return Email.SendEmail(sendEmailConfig, cancellationToken);
	}

	public Task<bool> SendEmail(SendEmailConfigBytes sendEmailConfig, CancellationToken cancellationToken = default)
	{
		return Email.SendEmail(sendEmailConfig, cancellationToken);
	}
}
