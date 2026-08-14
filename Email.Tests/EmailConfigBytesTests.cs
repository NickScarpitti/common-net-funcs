using AutoFixture.Xunit3;
using CommonNetFuncs.Email;
using static CommonNetFuncs.Email.Email;
using static Xunit.TestContext;

namespace Email.Tests;

public sealed class EmailConfigBytesTests
{
	#region EmailContentBytes Tests

	[Fact]
	public void EmailContentBytes_Initialization_ShouldSetPropertiesCorrectly()
	{
		// Arrange
		const string subject = "Test Subject";
		const string body = "Test Body";
		MailAttachmentBytes[] attachments = new[]
		{
						new MailAttachmentBytes("test.txt", new byte[] { 1, 2, 3 })
				};

		// Act
		EmailContentBytes content = new(subject, body, true, attachments, true);

		// Assert
		content.Subject.ShouldBe(subject);
		content.Body.ShouldBe(body);
		content.BodyIsHtml.ShouldBeTrue();
		content.Attachments.ShouldBe(attachments);
		content.ZipAttachments.ShouldBeTrue();
	}

	[Fact]
	public void EmailContentBytes_DefaultInitialization_ShouldHaveDefaultValues()
	{
		// Act
		EmailContentBytes content = new();

		// Assert
		content.Subject.ShouldBeNull();
		content.Body.ShouldBeNull();
		content.BodyIsHtml.ShouldBeFalse();
		content.Attachments.ShouldBeNull();
		content.ZipAttachments.ShouldBeFalse();
	}

	#endregion

	#region SendEmailConfigBytes Tests

	[Fact]
	public void SendEmailConfigBytes_Initialization_ShouldSetPropertiesCorrectly()
	{
		// Arrange
		SmtpSettings smtpSettings = new("smtp.example.com", 587, "user", "password");
		EmailAddresses emailAddresses = new(
				new MailAddress("Sender", "sender@example.com"),
				new[] { new MailAddress("Recipient", "recipient@example.com") }
		);
		EmailContentBytes emailContent = new("Subject", "Body");

		// Act
		SendEmailConfigBytes config = new(smtpSettings, emailAddresses, emailContent, true, "receipt@example.com");

		// Assert
		config.SmtpSettings.ShouldBe(smtpSettings);
		config.EmailAddresses.ShouldBe(emailAddresses);
		config.EmailContent.ShouldBe(emailContent);
		config.ReadReceipt.ShouldBeTrue();
		config.ReadReceiptEmail.ShouldBe("receipt@example.com");
	}

	[Fact]
	public void SendEmailConfigBytes_DefaultInitialization_ShouldCreateDefaultInstances()
	{
		// Act
		SendEmailConfigBytes config = new();

		// Assert
		config.SmtpSettings.ShouldNotBeNull();
		config.EmailAddresses.ShouldNotBeNull();
		config.EmailContent.ShouldNotBeNull();
		config.ReadReceipt.ShouldBeFalse();
		config.ReadReceiptEmail.ShouldBeNull();
	}

	#endregion

	#region SendEmail with SendEmailConfigBytes Tests

	[Theory]
	[AutoData]
	public async Task SendEmail_WithSendEmailConfigBytes_ShouldReturnFalseForInvalidEmail(string smtpServer, int smtpPort)
	{
		// Arrange
		SendEmailConfigBytes config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Test", "invalid-email"),
				ToAddresses = new[] { new MailAddress("Recipient", "recipient@example.com") }
			},
			EmailContent = new EmailContentBytes("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithSendEmailConfigBytesAndAttachments_ShouldProcessCorrectly(string smtpServer, int smtpPort)
	{
		// Arrange
		byte[] attachmentData = new byte[] { 1, 2, 3, 4, 5 };
		SendEmailConfigBytes config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("Recipient", "recipient@example.com") }
			},
			EmailContent = new EmailContentBytes
			{
				Subject = "Test Subject",
				Body = "Test Body",
				Attachments = new[] { new MailAttachmentBytes("test.txt", attachmentData) }
			}
		};

		// Act
		bool result = await SendEmail(config, Current.CancellationToken);

		// Assert
		// Should fail because SMTP server is invalid, but should not throw
		result.ShouldBe(false);
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithSendEmailConfigBytesAndZippedAttachments_ShouldProcessCorrectly(string smtpServer, int smtpPort)
	{
		// Arrange
		SendEmailConfigBytes config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("Recipient", "recipient@example.com") }
			},
			EmailContent = new EmailContentBytes
			{
				Subject = "Test Subject",
				Body = "Test Body",
				Attachments = new[]
						{
										new MailAttachmentBytes("test1.txt", new byte[] { 1, 2, 3 }),
										new MailAttachmentBytes("test2.txt", new byte[] { 4, 5, 6 })
								},
				ZipAttachments = true
			}
		};

		// Act
		bool result = await SendEmail(config, Current.CancellationToken);

		// Assert
		// Should fail because SMTP server is invalid, but should not throw
		result.ShouldBe(false);
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithSendEmailConfigBytesAndHtmlBody_ShouldProcessCorrectly(string smtpServer, int smtpPort)
	{
		// Arrange
		SendEmailConfigBytes config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("Recipient", "recipient@example.com") }
			},
			EmailContent = new EmailContentBytes
			{
				Subject = "Test Subject",
				Body = "<h1>Test</h1><p>HTML Body</p>",
				BodyIsHtml = true
			}
		};

		// Act
		bool result = await SendEmail(config, Current.CancellationToken);

		// Assert
		result.ShouldBe(false);
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithSendEmailConfigBytesAndReadReceipt_ShouldProcessCorrectly(string smtpServer, int smtpPort)
	{
		// Arrange
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
		bool result = await SendEmail(config, Current.CancellationToken);

		// Assert
		result.ShouldBe(false);
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithSendEmailConfigBytesNoRecipients_ShouldReturnFalse(string smtpServer, int smtpPort)
	{
		// Arrange
		SendEmailConfigBytes config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = Array.Empty<MailAddress>()
			},
			EmailContent = new EmailContentBytes("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
	}

	#endregion

	#region Stream Management Tests

	[Fact]
	public void MailAttachmentBytes_GetStream_StreamsAreIndependent()
	{
		// Arrange
		byte[] bytes = new byte[] { 1, 2, 3, 4, 5 };
		MailAttachmentBytes attachment = new("test.txt", bytes);

		// Act
		Stream? stream1 = attachment.GetStream();
		stream1!.ReadByte(); // Read first byte
		Stream? stream2 = attachment.GetStream();

		// Assert
		stream1.Position.ShouldBe(1);
		stream2!.Position.ShouldBe(0); // Second stream should start at beginning
	}

	[Fact]
	public async Task MailAttachment_DisposeShouldDisposeStream()
	{
		// Arrange
		MemoryStream stream = new(new byte[] { 1, 2, 3 });
		MailAttachment attachment = new("test.txt", stream);

		// Act
		await attachment.DisposeAsync();

		// Assert
		Assert.Throws<ObjectDisposedException>(() => stream.ReadByte());
	}

	[Fact]
	public void MailAttachment_SynchronousDisposeShouldDisposeStream()
	{
		// Arrange
		MemoryStream stream = new(new byte[] { 1, 2, 3 });
		MailAttachment attachment = new("test.txt", stream);

		// Act
		attachment.Dispose();

		// Assert
		Assert.Throws<ObjectDisposedException>(() => stream.ReadByte());
	}

	[Fact]
	public async Task MailAttachment_MultipleDisposeCalls_ShouldNotThrow()
	{
		// Arrange
		MemoryStream stream = new(new byte[] { 1, 2, 3 });
		MailAttachment attachment = new("test.txt", stream);

		// Act
		await attachment.DisposeAsync();
		Exception? exception1 = await Record.ExceptionAsync(async () => await attachment.DisposeAsync());
		Exception? exception2 = Record.Exception(attachment.Dispose);

		// Assert
		exception1.ShouldBeNull();
		exception2.ShouldBeNull();
	}

	#endregion

	#region Configuration Classes Tests

	[Fact]
	public void SmtpSettings_ConstructorWithParameters_ShouldSetPropertiesCorrectly()
	{
		// Arrange & Act
		SmtpSettings settings = new("smtp.example.com", 587, "user@example.com", "password");

		// Assert
		settings.SmtpServer.ShouldBe("smtp.example.com");
		settings.SmtpPort.ShouldBe(587);
		settings.SmtpUser.ShouldBe("user@example.com");
		settings.SmtpPassword.ShouldBe("password");
	}

	[Fact]
	public void SmtpSettings_DefaultConstructor_ShouldHaveNullValues()
	{
		// Arrange & Act
		SmtpSettings settings = new();

		// Assert
		settings.SmtpServer.ShouldBeNull();
		settings.SmtpPort.ShouldBe(0);
		settings.SmtpUser.ShouldBeNull();
		settings.SmtpPassword.ShouldBeNull();
	}

	[Fact]
	public void SmtpSettings_PropertySetters_ShouldWork()
	{
		// Arrange
		SmtpSettings settings = new()
		{
			SmtpServer = "smtp.test.com",
			SmtpPort = 465,
			SmtpUser = "test@test.com",
			SmtpPassword = "TestPass"
		};

		// Assert
		settings.SmtpServer.ShouldBe("smtp.test.com");
		settings.SmtpPort.ShouldBe(465);
		settings.SmtpUser.ShouldBe("test@test.com");
		settings.SmtpPassword.ShouldBe("TestPass");
	}

	[Fact]
	public void MailAddress_PropertySetters_ShouldWork()
	{
		// Arrange
		MailAddress address = new()
		{
			Name = "Test User",
			Email = "test@example.com"
		};

		// Assert
		address.Name.ShouldBe("Test User");
		address.Email.ShouldBe("test@example.com");
	}

	[Fact]
	public void EmailAddresses_WithCcAndBcc_ShouldSetPropertiesCorrectly()
	{
		// Arrange
		MailAddress from = new("Sender", "sender@example.com");
		MailAddress[] to = new[] { new MailAddress("To", "to@example.com") };
		MailAddress[] cc = new[] { new MailAddress("CC", "cc@example.com") };
		MailAddress[] bcc = new[] { new MailAddress("BCC", "bcc@example.com") };

		// Act
		EmailAddresses addresses = new(from, to, cc, bcc);

		// Assert
		addresses.FromAddress.ShouldBe(from);
		addresses.ToAddresses.Length.ShouldBe(1);
		addresses.CcAddresses.Length.ShouldBe(1);
		addresses.BccAddresses.Length.ShouldBe(1);
	}

	[Fact]
	public void EmailAddresses_DefaultConstructor_ShouldHaveEmptyArrays()
	{
		// Arrange & Act
		EmailAddresses addresses = new();

		// Assert
		addresses.FromAddress.ShouldNotBeNull();
		addresses.ToAddresses.ShouldBeEmpty();
		addresses.CcAddresses.ShouldBeEmpty();
		addresses.BccAddresses.ShouldBeEmpty();
	}

	[Fact]
	public void EmailContent_AllConstructorParameters_ShouldSetPropertiesCorrectly()
	{
		// Arrange
		IMailAttachment[] attachments = new[] { new MailAttachment("test.txt", new byte[] { 1, 2, 3 }) };

		// Act
		EmailContent content = new("Subject", "Body", true, attachments, false, true);

		// Assert
		content.Subject.ShouldBe("Subject");
		content.Body.ShouldBe("Body");
		content.BodyIsHtml.ShouldBeTrue();
		content.Attachments.ShouldBe(attachments);
		content.AutoDisposeAttachments.ShouldBeFalse();
		content.ZipAttachments.ShouldBeTrue();
	}

	[Fact]
	public void EmailContent_DefaultConstructor_ShouldHaveDefaultValues()
	{
		// Arrange & Act
		EmailContent content = new();

		// Assert
		content.Subject.ShouldBeNull();
		content.Body.ShouldBeNull();
		content.BodyIsHtml.ShouldBeFalse();
		content.Attachments.ShouldBeNull();
		content.AutoDisposeAttachments.ShouldBeTrue();
		content.ZipAttachments.ShouldBeFalse();
	}

	[Fact]
	public void SendEmailConfig_AllConstructorParameters_ShouldSetPropertiesCorrectly()
	{
		// Arrange
		SmtpSettings smtp = new("smtp.test.com", 587);
		EmailAddresses addresses = new(new MailAddress("Test", "test@example.com"));
		EmailContent content = new("Subject", "Body");

		// Act
		SendEmailConfig config = new(smtp, addresses, content, true, "receipt@example.com");

		// Assert
		config.SmtpSettings.ShouldBe(smtp);
		config.EmailAddresses.ShouldBe(addresses);
		config.EmailContent.ShouldBe(content);
		config.ReadReceipt.ShouldBeTrue();
		config.ReadReceiptEmail.ShouldBe("receipt@example.com");
	}

	[Fact]
	public void SendEmailConfig_DefaultConstructor_ShouldCreateDefaultInstances()
	{
		// Arrange & Act
		SendEmailConfig config = new();

		// Assert
		config.SmtpSettings.ShouldNotBeNull();
		config.EmailAddresses.ShouldNotBeNull();
		config.EmailContent.ShouldNotBeNull();
		config.ReadReceipt.ShouldBeFalse();
		config.ReadReceiptEmail.ShouldBeNull();
	}

	#endregion
}
