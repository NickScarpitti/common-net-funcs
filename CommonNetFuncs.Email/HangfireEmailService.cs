using CommonNetFuncs.Hangfire;

namespace CommonNetFuncs.Email;

/// <summary>
/// Implementation of IEmailService to send emails via Hangfire jobs that will throw exceptions on failure to ensure job failure is tracked and retried
/// </summary>
/// <param name="emailService">DI of the base email service implementation</param>
public sealed class HangfireEmailService(IEmailService emailService) : IEmailService
{
	private readonly IEmailService emailService = emailService;

	public async Task<bool> SendEmail(SmtpSettings smtpSettings, EmailAddresses emailAddresses, EmailContent emailContent, bool readReceipt, string readReceiptEmail, CancellationToken cancellationToken = default)
	{
		if (!await emailService.SendEmail(new SendEmailConfig() { SmtpSettings = smtpSettings, EmailAddresses = emailAddresses, EmailContent = emailContent, ReadReceipt = readReceipt, ReadReceiptEmail = readReceiptEmail }, cancellationToken))
		{
			throw new HangfireJobException("Failed to send email");
		}
		return true;
	}

	public async Task<bool> SendEmail(SmtpSettings smtpSettings, EmailAddresses emailAddresses, EmailContent emailContent, CancellationToken cancellationToken = default)
	{
		if (!await emailService.SendEmail(new SendEmailConfig() { SmtpSettings = smtpSettings, EmailAddresses = emailAddresses, EmailContent = emailContent, }, cancellationToken))
		{
			throw new HangfireJobException("Failed to send email");
		}
		return true;
	}

	public async Task<bool> SendEmail(SmtpSettings smtpSettings, EmailAddresses emailAddresses, EmailContentBytes emailContent, bool readReceipt, string readReceiptEmail, CancellationToken cancellationToken = default)
	{
		if (!await emailService.SendEmail(new SendEmailConfigBytes() { SmtpSettings = smtpSettings, EmailAddresses = emailAddresses, EmailContent = emailContent, ReadReceipt = readReceipt, ReadReceiptEmail = readReceiptEmail }, cancellationToken))
		{
			throw new HangfireJobException("Failed to send email");
		}
		return true;
	}

	public async Task<bool> SendEmail(SmtpSettings smtpSettings, EmailAddresses emailAddresses, EmailContentBytes emailContent, CancellationToken cancellationToken = default)
	{
		if (!await emailService.SendEmail(new SendEmailConfigBytes() { SmtpSettings = smtpSettings, EmailAddresses = emailAddresses, EmailContent = emailContent, }, cancellationToken))
		{
			throw new HangfireJobException("Failed to send email");
		}
		return true;
	}

	public async Task<bool> SendEmail(SendEmailConfig sendEmailConfig, CancellationToken cancellationToken = default)
	{
		if (!await emailService.SendEmail(sendEmailConfig, cancellationToken))
		{
			throw new HangfireJobException("Failed to send email");
		}
		return true;
	}

	public async Task<bool> SendEmail(SendEmailConfigBytes sendEmailConfig, CancellationToken cancellationToken = default)
	{
		if (!await emailService.SendEmail(sendEmailConfig, cancellationToken))
		{
			throw new HangfireJobException("Failed to send email");
		}
		return true;
	}
}
