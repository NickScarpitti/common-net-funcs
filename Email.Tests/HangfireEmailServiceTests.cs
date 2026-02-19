using AutoFixture.Xunit3;
using CommonNetFuncs.Email;
using CommonNetFuncs.Hangfire;

namespace Email.Tests;

/// <summary>
/// Type of email service overload to test
/// </summary>
public enum EmailServiceOverloadType
{
	SmtpSettingsWithEmailContentAndReadReceipt,
	SmtpSettingsWithEmailContentNoReadReceipt,
	SmtpSettingsWithEmailContentBytesAndReadReceipt,
	SmtpSettingsWithEmailContentBytesNoReadReceipt,
	SendEmailConfig,
	SendEmailConfigBytes
}

/// <summary>
/// Test outcome for Hangfire service
/// </summary>
public enum HangfireTestOutcome
{
	Success,
	ThrowsException
}

/// <summary>
/// Mock implementation of IEmailService for testing purposes
/// </summary>
internal sealed class MockEmailService(bool returnValue) : IEmailService
{
	private readonly bool returnValue = returnValue;

	public Task<bool> SendEmail(SmtpSettings smtpSettings, EmailAddresses emailAddresses, EmailContent emailContent, bool readReceipt, string readReceiptEmail, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(returnValue);
	}

	public Task<bool> SendEmail(SmtpSettings smtpSettings, EmailAddresses emailAddresses, EmailContent emailContent, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(returnValue);
	}

	public Task<bool> SendEmail(SmtpSettings smtpSettings, EmailAddresses emailAddresses, EmailContentBytes emailContent, bool readReceipt, string readReceiptEmail, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(returnValue);
	}

	public Task<bool> SendEmail(SmtpSettings smtpSettings, EmailAddresses emailAddresses, EmailContentBytes emailContent, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(returnValue);
	}

	public Task<bool> SendEmail(SendEmailConfig sendEmailConfig, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(returnValue);
	}

	public Task<bool> SendEmail(SendEmailConfigBytes sendEmailConfig, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(returnValue);
	}
}

public sealed class HangfireEmailServiceTests
{
	[Theory]
	[AutoData]
	public async Task SendEmail_WithAllOverloads_ShouldHandleSuccessAndFailure(
		string smtpServer,
		int smtpPort,
		EmailServiceOverloadType overloadType,
		HangfireTestOutcome expectedOutcome)
	{
		// Arrange
		bool mockReturnsSuccess = expectedOutcome == HangfireTestOutcome.Success;
		MockEmailService mockService = new(mockReturnsSuccess);
		HangfireEmailService service = new(mockService);
		SmtpSettings smtpSettings = new(smtpServer, smtpPort);
		EmailAddresses emailAddresses = new(
			new MailAddress("Sender", "sender@example.com"),
			new[] { new MailAddress("Recipient", "recipient@example.com") }
		);

		// Act & Assert
		switch (overloadType)
		{
			case EmailServiceOverloadType.SmtpSettingsWithEmailContentAndReadReceipt:
				{
					EmailContent emailContent = new("Subject", "Body");
					if (expectedOutcome == HangfireTestOutcome.Success)
					{
						bool result = await service.SendEmail(smtpSettings, emailAddresses, emailContent, true, "receipt@example.com", TestContext.Current.CancellationToken);
						result.ShouldBeTrue();
					}
					else
					{
						HangfireJobException exception = await Should.ThrowAsync<HangfireJobException>(async () =>
							await service.SendEmail(smtpSettings, emailAddresses, emailContent, true, "receipt@example.com", TestContext.Current.CancellationToken));
						exception.Message.ShouldBe("Failed to send email");
					}
					break;
				}
			case EmailServiceOverloadType.SmtpSettingsWithEmailContentNoReadReceipt:
				{
					EmailContent emailContent = new("Subject", "Body");
					if (expectedOutcome == HangfireTestOutcome.Success)
					{
						bool result = await service.SendEmail(smtpSettings, emailAddresses, emailContent, TestContext.Current.CancellationToken);
						result.ShouldBeTrue();
					}
					else
					{
						HangfireJobException exception = await Should.ThrowAsync<HangfireJobException>(async () =>
							await service.SendEmail(smtpSettings, emailAddresses, emailContent, TestContext.Current.CancellationToken));
						exception.Message.ShouldBe("Failed to send email");
					}
					break;
				}
			case EmailServiceOverloadType.SmtpSettingsWithEmailContentBytesAndReadReceipt:
				{
					EmailContentBytes emailContent = new("Subject", "Body");
					if (expectedOutcome == HangfireTestOutcome.Success)
					{
						bool result = await service.SendEmail(smtpSettings, emailAddresses, emailContent, true, "receipt@example.com", TestContext.Current.CancellationToken);
						result.ShouldBeTrue();
					}
					else
					{
						HangfireJobException exception = await Should.ThrowAsync<HangfireJobException>(async () =>
							await service.SendEmail(smtpSettings, emailAddresses, emailContent, true, "receipt@example.com", TestContext.Current.CancellationToken));
						exception.Message.ShouldBe("Failed to send email");
					}
					break;
				}
			case EmailServiceOverloadType.SmtpSettingsWithEmailContentBytesNoReadReceipt:
				{
					EmailContentBytes emailContent = new("Subject", "Body");
					if (expectedOutcome == HangfireTestOutcome.Success)
					{
						bool result = await service.SendEmail(smtpSettings, emailAddresses, emailContent, TestContext.Current.CancellationToken);
						result.ShouldBeTrue();
					}
					else
					{
						HangfireJobException exception = await Should.ThrowAsync<HangfireJobException>(async () =>
							await service.SendEmail(smtpSettings, emailAddresses, emailContent, TestContext.Current.CancellationToken));
						exception.Message.ShouldBe("Failed to send email");
					}
					break;
				}
			case EmailServiceOverloadType.SendEmailConfig:
				{
					SendEmailConfig config = new()
					{
						SmtpSettings = smtpSettings,
						EmailAddresses = emailAddresses,
						EmailContent = new EmailContent("Subject", "Body")
					};
					if (expectedOutcome == HangfireTestOutcome.Success)
					{
						bool result = await service.SendEmail(config, TestContext.Current.CancellationToken);
						result.ShouldBeTrue();
					}
					else
					{
						HangfireJobException exception = await Should.ThrowAsync<HangfireJobException>(async () =>
							await service.SendEmail(config, TestContext.Current.CancellationToken));
						exception.Message.ShouldBe("Failed to send email");
					}
					break;
				}
			case EmailServiceOverloadType.SendEmailConfigBytes:
				{
					SendEmailConfigBytes config = new()
					{
						SmtpSettings = smtpSettings,
						EmailAddresses = emailAddresses,
						EmailContent = new EmailContentBytes("Subject", "Body")
					};
					if (expectedOutcome == HangfireTestOutcome.Success)
					{
						bool result = await service.SendEmail(config, TestContext.Current.CancellationToken);
						result.ShouldBeTrue();
					}
					else
					{
						HangfireJobException exception = await Should.ThrowAsync<HangfireJobException>(async () =>
							await service.SendEmail(config, TestContext.Current.CancellationToken));
						exception.Message.ShouldBe("Failed to send email");
					}
					break;
				}
		}
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithRealEmailServiceAndInvalidAddress_ShouldThrowHangfireJobException(string smtpServer, int smtpPort)
	{
		// Arrange
		EmailService realEmailService = new();
		HangfireEmailService service = new(realEmailService);
		SmtpSettings smtpSettings = new(smtpServer, smtpPort);
		EmailAddresses emailAddresses = new(
			new MailAddress("Sender", "invalid-email"),
			new[] { new MailAddress("Recipient", "recipient@example.com") }
		);
		EmailContent emailContent = new("Subject", "Body");

		// Act & Assert
		HangfireJobException exception = await Should.ThrowAsync<HangfireJobException>(async () =>
			await service.SendEmail(smtpSettings, emailAddresses, emailContent, TestContext.Current.CancellationToken));

		exception.Message.ShouldBe("Failed to send email");
	}
}
