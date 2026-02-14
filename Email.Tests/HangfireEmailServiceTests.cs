using AutoFixture.Xunit3;
using CommonNetFuncs.Email;
using CommonNetFuncs.Hangfire;

namespace Email.Tests;

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
	#region SendEmail with SmtpSettings, EmailAddresses, EmailContent (with read receipt) - Success

	[Theory]
	[AutoData]
	public async Task SendEmail_WithReadReceipt_WhenUnderlyingServiceSucceeds_ShouldReturnTrue(string smtpServer, int smtpPort)
	{
		// Arrange
		MockEmailService mockService = new(true);
		HangfireEmailService service = new(mockService);
		SmtpSettings smtpSettings = new(smtpServer, smtpPort);
		EmailAddresses emailAddresses = new(
			new MailAddress("Sender", "sender@example.com"),
			new[] { new MailAddress("Recipient", "recipient@example.com") }
		);
		EmailContent emailContent = new("Subject", "Body");

		// Act
		bool result = await service.SendEmail(smtpSettings, emailAddresses, emailContent, true, "receipt@example.com", TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeTrue();
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithReadReceipt_WhenUnderlyingServiceFails_ShouldThrowHangfireJobException(string smtpServer, int smtpPort)
	{
		// Arrange
		MockEmailService mockService = new(false);
		HangfireEmailService service = new(mockService);
		SmtpSettings smtpSettings = new(smtpServer, smtpPort);
		EmailAddresses emailAddresses = new(
			new MailAddress("Sender", "sender@example.com"),
			new[] { new MailAddress("Recipient", "recipient@example.com") }
		);
		EmailContent emailContent = new("Subject", "Body");

		// Act & Assert
		HangfireJobException exception = await Should.ThrowAsync<HangfireJobException>(async () =>
			await service.SendEmail(smtpSettings, emailAddresses, emailContent, true, "receipt@example.com", TestContext.Current.CancellationToken));

		exception.Message.ShouldBe("Failed to send email");
	}

	#endregion

	#region SendEmail with SmtpSettings, EmailAddresses, EmailContent (without read receipt) - Success/Failure

	[Theory]
	[AutoData]
	public async Task SendEmail_WithoutReadReceipt_WhenUnderlyingServiceSucceeds_ShouldReturnTrue(string smtpServer, int smtpPort)
	{
		// Arrange
		MockEmailService mockService = new(true);
		HangfireEmailService service = new(mockService);
		SmtpSettings smtpSettings = new(smtpServer, smtpPort);
		EmailAddresses emailAddresses = new(
			new MailAddress("Sender", "sender@example.com"),
			new[] { new MailAddress("Recipient", "recipient@example.com") }
		);
		EmailContent emailContent = new("Subject", "Body");

		// Act
		bool result = await service.SendEmail(smtpSettings, emailAddresses, emailContent, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeTrue();
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithoutReadReceipt_WhenUnderlyingServiceFails_ShouldThrowHangfireJobException(string smtpServer, int smtpPort)
	{
		// Arrange
		MockEmailService mockService = new(false);
		HangfireEmailService service = new(mockService);
		SmtpSettings smtpSettings = new(smtpServer, smtpPort);
		EmailAddresses emailAddresses = new(
			new MailAddress("Sender", "sender@example.com"),
			new[] { new MailAddress("Recipient", "recipient@example.com") }
		);
		EmailContent emailContent = new("Subject", "Body");

		// Act & Assert
		HangfireJobException exception = await Should.ThrowAsync<HangfireJobException>(async () =>
			await service.SendEmail(smtpSettings, emailAddresses, emailContent, TestContext.Current.CancellationToken));

		exception.Message.ShouldBe("Failed to send email");
	}

	#endregion

	#region SendEmail with SmtpSettings, EmailAddresses, EmailContentBytes (with read receipt) - Success/Failure

	[Theory]
	[AutoData]
	public async Task SendEmail_WithEmailContentBytesAndReadReceipt_WhenUnderlyingServiceSucceeds_ShouldReturnTrue(string smtpServer, int smtpPort)
	{
		// Arrange
		MockEmailService mockService = new(true);
		HangfireEmailService service = new(mockService);
		SmtpSettings smtpSettings = new(smtpServer, smtpPort);
		EmailAddresses emailAddresses = new(
			new MailAddress("Sender", "sender@example.com"),
			new[] { new MailAddress("Recipient", "recipient@example.com") }
		);
		EmailContentBytes emailContent = new("Subject", "Body");

		// Act
		bool result = await service.SendEmail(smtpSettings, emailAddresses, emailContent, true, "receipt@example.com", TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeTrue();
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithEmailContentBytesAndReadReceipt_WhenUnderlyingServiceFails_ShouldThrowHangfireJobException(string smtpServer, int smtpPort)
	{
		// Arrange
		MockEmailService mockService = new(false);
		HangfireEmailService service = new(mockService);
		SmtpSettings smtpSettings = new(smtpServer, smtpPort);
		EmailAddresses emailAddresses = new(
			new MailAddress("Sender", "sender@example.com"),
			new[] { new MailAddress("Recipient", "recipient@example.com") }
		);
		EmailContentBytes emailContent = new("Subject", "Body");

		// Act & Assert
		HangfireJobException exception = await Should.ThrowAsync<HangfireJobException>(async () =>
			await service.SendEmail(smtpSettings, emailAddresses, emailContent, true, "receipt@example.com", TestContext.Current.CancellationToken));

		exception.Message.ShouldBe("Failed to send email");
	}

	#endregion

	#region SendEmail with SmtpSettings, EmailAddresses, EmailContentBytes (without read receipt) - Success/Failure

	[Theory]
	[AutoData]
	public async Task SendEmail_WithEmailContentBytesWithoutReadReceipt_WhenUnderlyingServiceSucceeds_ShouldReturnTrue(string smtpServer, int smtpPort)
	{
		// Arrange
		MockEmailService mockService = new(true);
		HangfireEmailService service = new(mockService);
		SmtpSettings smtpSettings = new(smtpServer, smtpPort);
		EmailAddresses emailAddresses = new(
			new MailAddress("Sender", "sender@example.com"),
			new[] { new MailAddress("Recipient", "recipient@example.com") }
		);
		EmailContentBytes emailContent = new("Subject", "Body");

		// Act
		bool result = await service.SendEmail(smtpSettings, emailAddresses, emailContent, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeTrue();
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithEmailContentBytesWithoutReadReceipt_WhenUnderlyingServiceFails_ShouldThrowHangfireJobException(string smtpServer, int smtpPort)
	{
		// Arrange
		MockEmailService mockService = new(false);
		HangfireEmailService service = new(mockService);
		SmtpSettings smtpSettings = new(smtpServer, smtpPort);
		EmailAddresses emailAddresses = new(
			new MailAddress("Sender", "sender@example.com"),
			new[] { new MailAddress("Recipient", "recipient@example.com") }
		);
		EmailContentBytes emailContent = new("Subject", "Body");

		// Act & Assert
		HangfireJobException exception = await Should.ThrowAsync<HangfireJobException>(async () =>
			await service.SendEmail(smtpSettings, emailAddresses, emailContent, TestContext.Current.CancellationToken));

		exception.Message.ShouldBe("Failed to send email");
	}

	#endregion

	#region SendEmail with SendEmailConfig - Success/Failure

	[Theory]
	[AutoData]
	public async Task SendEmail_WithSendEmailConfig_WhenUnderlyingServiceSucceeds_ShouldReturnTrue(string smtpServer, int smtpPort)
	{
		// Arrange
		MockEmailService mockService = new(true);
		HangfireEmailService service = new(mockService);
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("Recipient", "recipient@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await service.SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeTrue();
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithSendEmailConfig_WhenUnderlyingServiceFails_ShouldThrowHangfireJobException(string smtpServer, int smtpPort)
	{
		// Arrange
		MockEmailService mockService = new(false);
		HangfireEmailService service = new(mockService);
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("Recipient", "recipient@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act & Assert
		HangfireJobException exception = await Should.ThrowAsync<HangfireJobException>(async () =>
			await service.SendEmail(config, TestContext.Current.CancellationToken));

		exception.Message.ShouldBe("Failed to send email");
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithSendEmailConfigAndReadReceipt_WhenUnderlyingServiceSucceeds_ShouldReturnTrue(string smtpServer, int smtpPort)
	{
		// Arrange
		MockEmailService mockService = new(true);
		HangfireEmailService service = new(mockService);
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("Recipient", "recipient@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body"),
			ReadReceipt = true,
			ReadReceiptEmail = "receipt@example.com"
		};

		// Act
		bool result = await service.SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeTrue();
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithSendEmailConfigAndReadReceipt_WhenUnderlyingServiceFails_ShouldThrowHangfireJobException(string smtpServer, int smtpPort)
	{
		// Arrange
		MockEmailService mockService = new(false);
		HangfireEmailService service = new(mockService);
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("Recipient", "recipient@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body"),
			ReadReceipt = true,
			ReadReceiptEmail = "receipt@example.com"
		};

		// Act & Assert
		HangfireJobException exception = await Should.ThrowAsync<HangfireJobException>(async () =>
			await service.SendEmail(config, TestContext.Current.CancellationToken));

		exception.Message.ShouldBe("Failed to send email");
	}

	#endregion

	#region SendEmail with SendEmailConfigBytes - Success/Failure

	[Theory]
	[AutoData]
	public async Task SendEmail_WithSendEmailConfigBytes_WhenUnderlyingServiceSucceeds_ShouldReturnTrue(string smtpServer, int smtpPort)
	{
		// Arrange
		MockEmailService mockService = new(true);
		HangfireEmailService service = new(mockService);
		SendEmailConfigBytes config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("Recipient", "recipient@example.com") }
			},
			EmailContent = new EmailContentBytes("Subject", "Body")
		};

		// Act
		bool result = await service.SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeTrue();
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithSendEmailConfigBytes_WhenUnderlyingServiceFails_ShouldThrowHangfireJobException(string smtpServer, int smtpPort)
	{
		// Arrange
		MockEmailService mockService = new(false);
		HangfireEmailService service = new(mockService);
		SendEmailConfigBytes config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("Recipient", "recipient@example.com") }
			},
			EmailContent = new EmailContentBytes("Subject", "Body")
		};

		// Act & Assert
		HangfireJobException exception = await Should.ThrowAsync<HangfireJobException>(async () =>
			await service.SendEmail(config, TestContext.Current.CancellationToken));

		exception.Message.ShouldBe("Failed to send email");
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithSendEmailConfigBytesAndReadReceipt_WhenUnderlyingServiceSucceeds_ShouldReturnTrue(string smtpServer, int smtpPort)
	{
		// Arrange
		MockEmailService mockService = new(true);
		HangfireEmailService service = new(mockService);
		SendEmailConfigBytes config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("Recipient", "recipient@example.com") }
			},
			EmailContent = new EmailContentBytes("Subject", "Body"),
			ReadReceipt = true,
			ReadReceiptEmail = "receipt@example.com"
		};

		// Act
		bool result = await service.SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeTrue();
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithSendEmailConfigBytesAndReadReceipt_WhenUnderlyingServiceFails_ShouldThrowHangfireJobException(string smtpServer, int smtpPort)
	{
		// Arrange
		MockEmailService mockService = new(false);
		HangfireEmailService service = new(mockService);
		SendEmailConfigBytes config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("Recipient", "recipient@example.com") }
			},
			EmailContent = new EmailContentBytes("Subject", "Body"),
			ReadReceipt = true,
			ReadReceiptEmail = "receipt@example.com"
		};

		// Act & Assert
		HangfireJobException exception = await Should.ThrowAsync<HangfireJobException>(async () =>
			await service.SendEmail(config, TestContext.Current.CancellationToken));

		exception.Message.ShouldBe("Failed to send email");
	}

	#endregion

	#region Integration Tests with Real EmailService

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

	#endregion
}
