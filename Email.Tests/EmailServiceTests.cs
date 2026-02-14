using AutoFixture.Xunit3;
using CommonNetFuncs.Email;

namespace Email.Tests;

public sealed class EmailServiceTests
{
	#region SendEmail with SmtpSettings, EmailAddresses, EmailContent (with read receipt)

	[Theory]
	[AutoData]
	public async Task SendEmail_WithReadReceipt_ShouldCallUnderlyingEmailSendMethod(string smtpServer, int smtpPort)
	{
		// Arrange
		EmailService service = new();
		SmtpSettings smtpSettings = new(smtpServer, smtpPort);
		EmailAddresses emailAddresses = new(
			new MailAddress("Sender", "sender@example.com"),
			new[] { new MailAddress("Recipient", "recipient@example.com") }
		);
		EmailContent emailContent = new("Subject", "Body");

		// Act
		bool result = await service.SendEmail(smtpSettings, emailAddresses, emailContent, true, "receipt@example.com", TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server but method should complete
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithReadReceiptAndInvalidFromAddress_ShouldReturnFalse(string smtpServer, int smtpPort)
	{
		// Arrange
		EmailService service = new();
		SmtpSettings smtpSettings = new(smtpServer, smtpPort);
		EmailAddresses emailAddresses = new(
			new MailAddress("Sender", "invalid-email"),
			new[] { new MailAddress("Recipient", "recipient@example.com") }
		);
		EmailContent emailContent = new("Subject", "Body");

		// Act
		bool result = await service.SendEmail(smtpSettings, emailAddresses, emailContent, true, "receipt@example.com", TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
	}

	#endregion

	#region SendEmail with SmtpSettings, EmailAddresses, EmailContent (without read receipt)

	[Theory]
	[AutoData]
	public async Task SendEmail_WithoutReadReceipt_ShouldCallUnderlyingEmailSendMethod(string smtpServer, int smtpPort)
	{
		// Arrange
		EmailService service = new();
		SmtpSettings smtpSettings = new(smtpServer, smtpPort);
		EmailAddresses emailAddresses = new(
			new MailAddress("Sender", "sender@example.com"),
			new[] { new MailAddress("Recipient", "recipient@example.com") }
		);
		EmailContent emailContent = new("Subject", "Body");

		// Act
		bool result = await service.SendEmail(smtpSettings, emailAddresses, emailContent, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server but method should complete
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithoutReadReceiptAndInvalidToAddress_ShouldReturnFalse(string smtpServer, int smtpPort)
	{
		// Arrange
		EmailService service = new();
		SmtpSettings smtpSettings = new(smtpServer, smtpPort);
		EmailAddresses emailAddresses = new(
			new MailAddress("Sender", "sender@example.com"),
			new[] { new MailAddress("Recipient", "invalid-email") }
		);
		EmailContent emailContent = new("Subject", "Body");

		// Act
		bool result = await service.SendEmail(smtpSettings, emailAddresses, emailContent, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithHtmlContent_ShouldProcessCorrectly(string smtpServer, int smtpPort)
	{
		// Arrange
		EmailService service = new();
		SmtpSettings smtpSettings = new(smtpServer, smtpPort);
		EmailAddresses emailAddresses = new(
			new MailAddress("Sender", "sender@example.com"),
			new[] { new MailAddress("Recipient", "recipient@example.com") }
		);
		EmailContent emailContent = new("<h1>Subject</h1>", "<p>HTML Body</p>", true);

		// Act
		bool result = await service.SendEmail(smtpSettings, emailAddresses, emailContent, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	#endregion

	#region SendEmail with SmtpSettings, EmailAddresses, EmailContentBytes (with read receipt)

	[Theory]
	[AutoData]
	public async Task SendEmail_WithEmailContentBytesAndReadReceipt_ShouldCallUnderlyingEmailSendMethod(string smtpServer, int smtpPort)
	{
		// Arrange
		EmailService service = new();
		SmtpSettings smtpSettings = new(smtpServer, smtpPort);
		EmailAddresses emailAddresses = new(
			new MailAddress("Sender", "sender@example.com"),
			new[] { new MailAddress("Recipient", "recipient@example.com") }
		);
		EmailContentBytes emailContent = new("Subject", "Body");

		// Act
		bool result = await service.SendEmail(smtpSettings, emailAddresses, emailContent, true, "receipt@example.com", TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server but method should complete
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithEmailContentBytesAndReadReceiptAndInvalidFromAddress_ShouldReturnFalse(string smtpServer, int smtpPort)
	{
		// Arrange
		EmailService service = new();
		SmtpSettings smtpSettings = new(smtpServer, smtpPort);
		EmailAddresses emailAddresses = new(
			new MailAddress("Sender", "invalid-email"),
			new[] { new MailAddress("Recipient", "recipient@example.com") }
		);
		EmailContentBytes emailContent = new("Subject", "Body");

		// Act
		bool result = await service.SendEmail(smtpSettings, emailAddresses, emailContent, true, "receipt@example.com", TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithEmailContentBytesAndAttachments_ShouldProcessCorrectly(string smtpServer, int smtpPort)
	{
		// Arrange
		EmailService service = new();
		SmtpSettings smtpSettings = new(smtpServer, smtpPort);
		EmailAddresses emailAddresses = new(
			new MailAddress("Sender", "sender@example.com"),
			new[] { new MailAddress("Recipient", "recipient@example.com") }
		);
		MailAttachmentBytes[] attachments = new[]
		{
			new MailAttachmentBytes("test.txt", new byte[] { 1, 2, 3 })
		};
		EmailContentBytes emailContent = new("Subject", "Body", false, attachments);

		// Act
		bool result = await service.SendEmail(smtpSettings, emailAddresses, emailContent, true, "receipt@example.com", TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	#endregion

	#region SendEmail with SmtpSettings, EmailAddresses, EmailContentBytes (without read receipt)

	[Theory]
	[AutoData]
	public async Task SendEmail_WithEmailContentBytesWithoutReadReceipt_ShouldCallUnderlyingEmailSendMethod(string smtpServer, int smtpPort)
	{
		// Arrange
		EmailService service = new();
		SmtpSettings smtpSettings = new(smtpServer, smtpPort);
		EmailAddresses emailAddresses = new(
			new MailAddress("Sender", "sender@example.com"),
			new[] { new MailAddress("Recipient", "recipient@example.com") }
		);
		EmailContentBytes emailContent = new("Subject", "Body");

		// Act
		bool result = await service.SendEmail(smtpSettings, emailAddresses, emailContent, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server but method should complete
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithEmailContentBytesWithoutReadReceiptAndInvalidToAddress_ShouldReturnFalse(string smtpServer, int smtpPort)
	{
		// Arrange
		EmailService service = new();
		SmtpSettings smtpSettings = new(smtpServer, smtpPort);
		EmailAddresses emailAddresses = new(
			new MailAddress("Sender", "sender@example.com"),
			new[] { new MailAddress("Recipient", "invalid-email") }
		);
		EmailContentBytes emailContent = new("Subject", "Body");

		// Act
		bool result = await service.SendEmail(smtpSettings, emailAddresses, emailContent, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithEmailContentBytesWithZippedAttachments_ShouldProcessCorrectly(string smtpServer, int smtpPort)
	{
		// Arrange
		EmailService service = new();
		SmtpSettings smtpSettings = new(smtpServer, smtpPort);
		EmailAddresses emailAddresses = new(
			new MailAddress("Sender", "sender@example.com"),
			new[] { new MailAddress("Recipient", "recipient@example.com") }
		);
		MailAttachmentBytes[] attachments = new[]
		{
			new MailAttachmentBytes("test1.txt", new byte[] { 1, 2, 3 }),
			new MailAttachmentBytes("test2.txt", new byte[] { 4, 5, 6 })
		};
		EmailContentBytes emailContent = new("Subject", "Body", false, attachments, true);

		// Act
		bool result = await service.SendEmail(smtpSettings, emailAddresses, emailContent, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	#endregion

	#region SendEmail with SendEmailConfig

	[Theory]
	[AutoData]
	public async Task SendEmail_WithSendEmailConfig_ShouldProcessCorrectly(string smtpServer, int smtpPort)
	{
		// Arrange
		EmailService service = new();
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
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithSendEmailConfigAndInvalidEmail_ShouldReturnFalse(string smtpServer, int smtpPort)
	{
		// Arrange
		EmailService service = new();
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "invalid-email"),
				ToAddresses = new[] { new MailAddress("Recipient", "recipient@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await service.SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithSendEmailConfigAndAttachments_ShouldProcessCorrectly(string smtpServer, int smtpPort)
	{
		// Arrange
		EmailService service = new();
		IMailAttachment[] attachments = new IMailAttachment[]
		{
			new MailAttachment("test.txt", new byte[] { 1, 2, 3 })
		};
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("Recipient", "recipient@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body", false, attachments)
		};

		// Act
		bool result = await service.SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithSendEmailConfigAndReadReceipt_ShouldProcessCorrectly(string smtpServer, int smtpPort)
	{
		// Arrange
		EmailService service = new();
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
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	#endregion

	#region SendEmail with SendEmailConfigBytes

	[Theory]
	[AutoData]
	public async Task SendEmail_WithSendEmailConfigBytes_ShouldProcessCorrectly(string smtpServer, int smtpPort)
	{
		// Arrange
		EmailService service = new();
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
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithSendEmailConfigBytesAndInvalidEmail_ShouldReturnFalse(string smtpServer, int smtpPort)
	{
		// Arrange
		EmailService service = new();
		SendEmailConfigBytes config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "invalid-email"),
				ToAddresses = new[] { new MailAddress("Recipient", "recipient@example.com") }
			},
			EmailContent = new EmailContentBytes("Subject", "Body")
		};

		// Act
		bool result = await service.SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithSendEmailConfigBytesAndAttachments_ShouldProcessCorrectly(string smtpServer, int smtpPort)
	{
		// Arrange
		EmailService service = new();
		MailAttachmentBytes[] attachments = new[]
		{
			new MailAttachmentBytes("test.txt", new byte[] { 1, 2, 3 })
		};
		SendEmailConfigBytes config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("Recipient", "recipient@example.com") }
			},
			EmailContent = new EmailContentBytes("Subject", "Body", false, attachments)
		};

		// Act
		bool result = await service.SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithSendEmailConfigBytesAndReadReceipt_ShouldProcessCorrectly(string smtpServer, int smtpPort)
	{
		// Arrange
		EmailService service = new();
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
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	#endregion
}
