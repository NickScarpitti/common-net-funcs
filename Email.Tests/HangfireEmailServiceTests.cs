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
	[InlineData(EmailServiceOverloadType.SmtpSettingsWithEmailContentAndReadReceipt, HangfireTestOutcome.Success)]
	[InlineData(EmailServiceOverloadType.SmtpSettingsWithEmailContentAndReadReceipt, HangfireTestOutcome.ThrowsException)]
	[InlineData(EmailServiceOverloadType.SmtpSettingsWithEmailContentNoReadReceipt, HangfireTestOutcome.Success)]
	[InlineData(EmailServiceOverloadType.SmtpSettingsWithEmailContentNoReadReceipt, HangfireTestOutcome.ThrowsException)]
	[InlineData(EmailServiceOverloadType.SmtpSettingsWithEmailContentBytesAndReadReceipt, HangfireTestOutcome.Success)]
	[InlineData(EmailServiceOverloadType.SmtpSettingsWithEmailContentBytesAndReadReceipt, HangfireTestOutcome.ThrowsException)]
	[InlineData(EmailServiceOverloadType.SmtpSettingsWithEmailContentBytesNoReadReceipt, HangfireTestOutcome.Success)]
	[InlineData(EmailServiceOverloadType.SmtpSettingsWithEmailContentBytesNoReadReceipt, HangfireTestOutcome.ThrowsException)]
	[InlineData(EmailServiceOverloadType.SendEmailConfig, HangfireTestOutcome.Success)]
	[InlineData(EmailServiceOverloadType.SendEmailConfig, HangfireTestOutcome.ThrowsException)]
	[InlineData(EmailServiceOverloadType.SendEmailConfigBytes, HangfireTestOutcome.Success)]
	[InlineData(EmailServiceOverloadType.SendEmailConfigBytes, HangfireTestOutcome.ThrowsException)]
	public async Task SendEmail_WithAllOverloads_ShouldHandleSuccessAndFailure(
		EmailServiceOverloadType overloadType,
		HangfireTestOutcome expectedOutcome)
	{
		// Arrange
		bool mockReturnsSuccess = expectedOutcome == HangfireTestOutcome.Success;
		MockEmailService mockService = new(mockReturnsSuccess);
		HangfireEmailService service = new(mockService);
		SmtpSettings smtpSettings = new("smtp.test.com", 587);
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

	[Fact]
	public async Task SendEmail_WithRealEmailServiceAndInvalidAddress_ShouldThrowHangfireJobException()
	{
		// Arrange
		EmailService realEmailService = new();
		HangfireEmailService service = new(realEmailService);
		SmtpSettings smtpSettings = new("smtp.invalid.com", 587);
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
