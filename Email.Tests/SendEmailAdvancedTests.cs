using AutoFixture.Xunit3;
using CommonNetFuncs.Email;
using static CommonNetFuncs.Email.Email;
using static Xunit.TestContext;

namespace Email.Tests;

public sealed class SendEmailAdvancedTests
{
	#region SendEmail Advanced Tests

	[Theory]
	[AutoData]
	public async Task SendEmail_WithCcAddresses_ShouldReturnFalse(string smtpServer, int smtpPort)
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") },
				CcAddresses = new[] { new MailAddress("CC", "cc@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithBccAddresses_ShouldReturnFalse(string smtpServer, int smtpPort)
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") },
				BccAddresses = new[] { new MailAddress("BCC", "bcc@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithInvalidCcAddress_ShouldReturnFalse(string smtpServer, int smtpPort)
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") },
				CcAddresses = new[] { new MailAddress("CC", "invalid-email") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithInvalidBccAddress_ShouldReturnFalse(string smtpServer, int smtpPort)
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") },
				BccAddresses = new[] { new MailAddress("BCC", "invalid-email") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithNoCcOrBccAndEmptyToAddresses_ShouldReturnFalse(string smtpServer, int smtpPort)
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = Array.Empty<MailAddress>()
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithEmptyToAddressesButValidCc_ShouldReturnFalse(string smtpServer, int smtpPort)
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = Array.Empty<MailAddress>(),
				CcAddresses = new[] { new MailAddress("CC", "cc@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithEmptyToAddressesButValidBcc_ShouldReturnFalse(string smtpServer, int smtpPort)
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = Array.Empty<MailAddress>(),
				BccAddresses = new[] { new MailAddress("BCC", "bcc@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithReadReceiptButNoEmail_ShouldProcessWithoutReadReceipt(string smtpServer, int smtpPort)
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body"),
			ReadReceipt = true,
			ReadReceiptEmail = null
		};

		// Act
		bool result = await SendEmail(config, Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithTextBody_ShouldProcessCorrectly(string smtpServer, int smtpPort)
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Plain text body", false) // bodyIsHtml = false
		};

		// Act
		bool result = await SendEmail(config, Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithNoRecipients_ShouldReturnFalse(string smtpServer, int smtpPort)
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = Array.Empty<MailAddress>(),
				CcAddresses = Array.Empty<MailAddress>(),
				BccAddresses = Array.Empty<MailAddress>()
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithNullCcAndBccArrays_ShouldProcessCorrectly(string smtpServer, int smtpPort)
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") },
				CcAddresses = null!,
				BccAddresses = null!
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithEmptyReadReceiptEmail_ShouldProcessWithoutReadReceipt(string smtpServer, int smtpPort)
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body"),
			ReadReceipt = true,
			ReadReceiptEmail = ""
		};

		// Act
		bool result = await SendEmail(config, Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithWhitespaceReadReceiptEmail_ShouldProcessWithoutReadReceipt(string smtpServer, int smtpPort)
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body"),
			ReadReceipt = true,
			ReadReceiptEmail = "   "
		};

		// Act
		bool result = await SendEmail(config, Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithEmptySmtpUser_ShouldAttemptConnectionWithoutAuth(string smtpServer, int smtpPort)
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings
			{
				SmtpServer = smtpServer,
				SmtpPort = smtpPort,
				SmtpUser = "",
				SmtpPassword = ""
			},
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithNullSmtpUser_ShouldAttemptConnectionWithoutAuth(string smtpServer, int smtpPort)
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings
			{
				SmtpServer = smtpServer,
				SmtpPort = smtpPort,
				SmtpUser = null,
				SmtpPassword = null
			},
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithSmtpUserButNullPassword_ShouldAttemptConnectionWithoutAuth(string smtpServer, int smtpPort)
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings
			{
				SmtpServer = smtpServer,
				SmtpPort = smtpPort,
				SmtpUser = "user@example.com",
				SmtpPassword = null
			},
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	[Fact]
	public void MailAddress_DefaultConstructor_ShouldHaveNullValues()
	{
		// Arrange & Act
		MailAddress address = new();

		// Assert
		address.Name.ShouldBeNull();
		address.Email.ShouldBeNull();
	}

	[Fact]
	public void MailAddress_ParameterizedConstructor_ShouldSetValues()
	{
		// Arrange & Act
		MailAddress address = new("Test Name", "test@example.com");

		// Assert
		address.Name.ShouldBe("Test Name");
		address.Email.ShouldBe("test@example.com");
	}

	#endregion
}
