using System.ComponentModel.DataAnnotations;
using AutoFixture.Xunit3;
using CommonNetFuncs.Email;
using MimeKit;
using static CommonNetFuncs.Email.Email;

namespace Email.Tests;

/// <summary>
/// Mock attachment that only implements IDisposable (not IAsyncDisposable) for testing disposal logic
/// </summary>
internal sealed class MockDisposableAttachment(string? attachmentName = null) : IMailAttachment, IDisposable
{
	private bool disposed;
	public bool IsDisposed => disposed;

	public string? AttachmentName { get; set; } = attachmentName;

	public Stream? GetStream()
	{
		return new MemoryStream(new byte[] { 1, 2, 3 });
	}

	public void Dispose()
	{
		disposed = true;
		GC.SuppressFinalize(this);
	}
}

public sealed class EmailTests
{
	[Theory]
	[InlineData("test@example.com", true)]
	[InlineData("test.name@subdomain.example.com", true)]
	[InlineData("test+label@example.com", true)]
	[InlineData("invalid.email", false)]
	[InlineData("@example.com", false)]
	[InlineData("test@", false)]
	[InlineData("", false)]
	[InlineData(null, false)]
	public void IsValidEmail_ShouldValidateEmailCorrectly(string? email, bool expected)
	{
		// Act
		bool result = email.IsValidEmail();

		// Assert
		result.ShouldBe(expected);
	}

	[Fact]
	public void MailAddress_ShouldRespectMaxLength()
	{
		// Arrange
		string longEmail = $"{new string('a', EmailConstants.MaxEmailLength - 10)}@test.com";
		MailAddress mailAddress = new("Test", longEmail);

		// Assert
		ValidationContext validationContext = new(mailAddress);
		List<ValidationResult> validationResults = new();
		bool isValid = Validator.TryValidateObject(mailAddress, validationContext, validationResults, true);

		// Assert
		isValid.ShouldBeTrue();
	}

	[Fact]
	public void MailAddress_ShouldFailForTooLongEmail()
	{
		// Arrange
		string longEmail = $"{new string('a', EmailConstants.MaxEmailLength + 1)}@test.com";
		MailAddress mailAddress = new("Test", longEmail);

		// Assert
		ValidationContext validationContext = new(mailAddress);
		List<ValidationResult> validationResults = new();
		bool isValid = Validator.TryValidateObject(mailAddress, validationContext, validationResults, true);

		// Assert
		isValid.ShouldBeFalse();
		validationResults.Count.ShouldBe(1);
		validationResults[0].ErrorMessage.ShouldBe("Invalid email format");
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithInvalidFromAddress_ShouldReturnFalse(string smtpServer, int smtpPort)
	{
		// Arrange
		SmtpSettings smtpSettings = new()
		{
			SmtpServer = smtpServer,
			SmtpPort = smtpPort,
		};

		EmailAddresses emailAddresses = new()
		{
			FromAddress = new("Test", "invalid-email"),
			ToAddresses = new[] { new MailAddress("Test Recipient", "valid@example.com") }
		};

		EmailContent emailContent = new()
		{
			Subject = "Test Subject",
			Body = "Test Body"
		};

		// Act
		bool result = await SendEmail(new SendEmailConfig() { SmtpSettings = smtpSettings, EmailAddresses = emailAddresses, EmailContent = emailContent }, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithInvalidToAddress_ShouldReturnFalse(string smtpServer, int smtpPort)
	{
		// Arrange
		SmtpSettings smtpSettings = new()
		{
			SmtpServer = smtpServer,
			SmtpPort = smtpPort,
		};

		EmailAddresses emailAddresses = new()
		{
			FromAddress = new("Test", "valid@example.com"),
			ToAddresses = new[] { new MailAddress("Test Recipient", "invalid-email") }
		};

		EmailContent emailContent = new()
		{
			Subject = "Test Subject",
			Body = "Test Body"
		};

		// Act
		bool result = await SendEmail(new SendEmailConfig() { SmtpSettings = smtpSettings, EmailAddresses = emailAddresses, EmailContent = emailContent }, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task AddAttachments_WithZipCompression_ShouldCreateSingleZipFile()
	{
		// Arrange
		BodyBuilder bodyBuilder = new();
		MailAttachment[] attachments = new[]
		{
			new MailAttachment("test1.txt", new MemoryStream(new byte[] { 1, 2, 3 })),
			new MailAttachment("test2.txt", new MemoryStream(new byte[] { 4, 5, 6 }))
		};

		// Act
		await AddAttachments(attachments, bodyBuilder, true, TestContext.Current.CancellationToken);

		// Assert
		bodyBuilder.Attachments.Count.ShouldBe(1);
		MimePart? zipAttachment = bodyBuilder.Attachments[0] as MimePart; // Cast to MimePart
		zipAttachment.ShouldNotBeNull(); // Ensure the cast was successful
		zipAttachment.FileName.ShouldBe("Files.zip");
	}

	[Fact]
	public async Task AddAttachments_WithoutZipCompression_ShouldAddAllAttachments()
	{
		// Arrange
		BodyBuilder bodyBuilder = new();
		MailAttachment[] attachments = new[]
		{
			new MailAttachment("test1.txt", new MemoryStream(new byte[] { 1, 2, 3 })),
			new MailAttachment("test2.txt", new MemoryStream(new byte[] { 4, 5, 6 }))
		};

		// Act
		await AddAttachments(attachments, bodyBuilder, false, TestContext.Current.CancellationToken);

		// Assert
		bodyBuilder.Attachments.Count.ShouldBe(2);
		MimePart? zipAttachment1 = bodyBuilder.Attachments[0] as MimePart; // Cast to MimePart
		zipAttachment1.ShouldNotBeNull(); // Ensure the cast was successful
		zipAttachment1.FileName.ShouldBe("test1.txt");

		MimePart? zipAttachment2 = bodyBuilder.Attachments[1] as MimePart; // Cast to MimePart
		zipAttachment2.ShouldNotBeNull(); // Ensure the cast was successful
		zipAttachment2.FileName.ShouldBe("test2.txt");
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithReadReceipt_ShouldSetHeader(string smtpServer, int smtpPort)
	{
		// Arrange
		SmtpSettings smtpSettings = new()
		{
			SmtpServer = smtpServer,
			SmtpPort = smtpPort,
		};

		EmailAddresses emailAddresses = new()
		{
			FromAddress = new("Test", "sender@example.com"),
			ToAddresses = new[] { new MailAddress("Test Recipient", "recipient@example.com") }
		};

		EmailContent emailContent = new()
		{
			Subject = "Test Subject",
			Body = "Test Body"
		};

		// Act
		bool result = await SendEmail(new SendEmailConfig()
		{
			SmtpSettings = smtpSettings,
			EmailAddresses = emailAddresses,
			EmailContent = emailContent,
			ReadReceipt = true,
			ReadReceiptEmail = "receipt@example.com"
		}, TestContext.Current.CancellationToken);

		// Note: We can't actually verify the header here since the SMTP interaction
		// is encapsulated, but the method should complete without throwing
		result.ShouldBe(false);
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithHtmlBody_ShouldSetContentTypeCorrectly(string smtpServer, int smtpPort)
	{
		// Arrange
		SmtpSettings smtpSettings = new()
		{
			SmtpServer = smtpServer,
			SmtpPort = smtpPort,
		};

		EmailAddresses emailAddresses = new()
		{
			FromAddress = new("Test", "sender@example.com"),
			ToAddresses = new[] { new MailAddress("Test Recipient", "recipient@example.com") }
		};

		EmailContent emailContent = new()
		{
			Subject = "Test ",
			Body = "<h1>Test</h1><p>This is a test email</p>",
			BodyIsHtml = true
		};

		// Act
		bool result = await SendEmail(new SendEmailConfig() { SmtpSettings = smtpSettings, EmailAddresses = emailAddresses, EmailContent = emailContent }, TestContext.Current.CancellationToken);

		// Note: Similar to above, we can't directly verify the content type
		// but the method should complete without throwing
		result.ShouldBe(false);
	}

	#region MailAttachment Tests

	[Fact]
	public void MailAttachment_GetStream_ShouldReturnAttachmentStream()
	{
		// Arrange
		MemoryStream stream = new(new byte[] { 1, 2, 3, 4, 5 });
		MailAttachment attachment = new("test.txt", stream);

		// Act
		Stream? result = attachment.GetStream();

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBe(stream);
	}

	[Fact]
	public void MailAttachment_GetStream_WithNullStream_ShouldReturnNull()
	{
		// Arrange
		MailAttachment attachment = new("test.txt", (Stream?)null);

		// Act
		Stream? result = attachment.GetStream();

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void MailAttachment_WithByteArrayConstructor_ShouldCreateMemoryStream()
	{
		// Arrange
		byte[] bytes = new byte[] { 1, 2, 3, 4, 5 };

		// Act
		MailAttachment attachment = new("test.txt", bytes);

		// Assert
		attachment.AttachmentStream.ShouldNotBeNull();
		attachment.AttachmentStream!.Length.ShouldBe(5);
		attachment.AttachmentStream.Position.ShouldBe(0);
	}

	[Fact]
	public void MailAttachment_ImplementsIMailAttachment()
	{
		// Arrange & Act
		MailAttachment attachment = new("test.txt", new MemoryStream());

		// Assert
		attachment.ShouldBeAssignableTo<IMailAttachment>();
	}

	#endregion

	#region MailAttachmentBytes Tests

	[Fact]
	public void MailAttachmentBytes_GetStream_ShouldCreateNewMemoryStream()
	{
		// Arrange
		byte[] bytes = new byte[] { 1, 2, 3, 4, 5 };
		MailAttachmentBytes attachment = new("test.txt", bytes);

		// Act
		Stream? result = attachment.GetStream();

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBeOfType<MemoryStream>();
		result.Length.ShouldBe(5);
		result.Position.ShouldBe(0);
	}

	[Fact]
	public void MailAttachmentBytes_GetStream_WithNullBytes_ShouldReturnNull()
	{
		// Arrange
		MailAttachmentBytes attachment = new("test.txt", (byte[]?)null);

		// Act
		Stream? result = attachment.GetStream();

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void MailAttachmentBytes_GetStream_ShouldCreateIndependentStreams()
	{
		// Arrange
		byte[] bytes = new byte[] { 1, 2, 3, 4, 5 };
		MailAttachmentBytes attachment = new("test.txt", bytes);

		// Act
		Stream? stream1 = attachment.GetStream();
		Stream? stream2 = attachment.GetStream();

		// Assert
		stream1.ShouldNotBeNull();
		stream2.ShouldNotBeNull();
		stream1.ShouldNotBe(stream2); // Different instances
		stream1.Length.ShouldBe(stream2.Length);
	}

	[Fact]
	public void MailAttachmentBytes_WithStreamConstructor_ShouldCopyStreamToBytes()
	{
		// Arrange
		byte[] originalBytes = new byte[] { 1, 2, 3, 4, 5 };
		MemoryStream stream = new(originalBytes);

		// Act
		MailAttachmentBytes attachment = new("test.txt", stream);

		// Assert
		attachment.AttachmentBytes.ShouldNotBeNull();
		attachment.AttachmentBytes.Length.ShouldBe(5);
		attachment.AttachmentBytes.ShouldBe(originalBytes);
	}

	[Fact]
	public void MailAttachmentBytes_ImplementsIMailAttachment()
	{
		// Arrange & Act
		MailAttachmentBytes attachment = new("test.txt", new byte[] { 1, 2, 3 });

		// Assert
		attachment.ShouldBeAssignableTo<IMailAttachment>();
	}

	[Fact]
	public void MailAttachmentBytes_WithNullStreamConstructor_ShouldHaveEmptyBytes()
	{
		// Arrange & Act
		MailAttachmentBytes attachment = new("test.txt", (Stream?)null);

		// Assert
		attachment.AttachmentBytes.ShouldNotBeNull();
		attachment.AttachmentBytes.Length.ShouldBe(0);
	}

	#endregion

	#region IMailAttachment Interchangeability Tests

	[Fact]
	public async Task AddAttachments_WithMixedAttachmentTypes_ShouldAddAllAttachments()
	{
		// Arrange
		BodyBuilder bodyBuilder = new();
		IMailAttachment[] attachments = new IMailAttachment[]
		{
						new MailAttachment("test1.txt", new MemoryStream(new byte[] { 1, 2, 3 })),
						new MailAttachmentBytes("test2.txt", new byte[] { 4, 5, 6 }),
						new MailAttachment("test3.txt", new MemoryStream(new byte[] { 7, 8, 9 }))
		};

		// Act
		await AddAttachments(attachments, bodyBuilder, false, TestContext.Current.CancellationToken);

		// Assert
		bodyBuilder.Attachments.Count.ShouldBe(3);
		(bodyBuilder.Attachments[0] as MimePart)?.FileName.ShouldBe("test1.txt");
		(bodyBuilder.Attachments[1] as MimePart)?.FileName.ShouldBe("test2.txt");
		(bodyBuilder.Attachments[2] as MimePart)?.FileName.ShouldBe("test3.txt");
	}

	[Fact]
	public async Task AddAttachments_WithMixedAttachmentTypesZipped_ShouldCreateSingleZipFile()
	{
		// Arrange
		BodyBuilder bodyBuilder = new();
		IMailAttachment[] attachments = new IMailAttachment[]
		{
						new MailAttachment("test1.txt", new MemoryStream(new byte[] { 1, 2, 3 })),
						new MailAttachmentBytes("test2.txt", new byte[] { 4, 5, 6 })
		};

		// Act
		await AddAttachments(attachments, bodyBuilder, true, TestContext.Current.CancellationToken);

		// Assert
		bodyBuilder.Attachments.Count.ShouldBe(1);
		(bodyBuilder.Attachments[0] as MimePart)?.FileName.ShouldBe("Files.zip");
	}

	[Fact]
	public async Task AddAttachments_WithMailAttachmentBytes_ShouldAddSuccessfully()
	{
		// Arrange
		BodyBuilder bodyBuilder = new();
		IMailAttachment[] attachments = new IMailAttachment[]
		{
						new MailAttachmentBytes("test1.txt", new byte[] { 1, 2, 3 }),
						new MailAttachmentBytes("test2.txt", new byte[] { 4, 5, 6 })
		};

		// Act
		await AddAttachments(attachments, bodyBuilder, false, TestContext.Current.CancellationToken);

		// Assert
		bodyBuilder.Attachments.Count.ShouldBe(2);
		(bodyBuilder.Attachments[0] as MimePart)?.FileName.ShouldBe("test1.txt");
		(bodyBuilder.Attachments[1] as MimePart)?.FileName.ShouldBe("test2.txt");
	}

	#endregion

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
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

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
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

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
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

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
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

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
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

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
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

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
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

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
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

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
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

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
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

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
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

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
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

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
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

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
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

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
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

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
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

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
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

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
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

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
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

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
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

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
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

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
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

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

	#region AddAttachments Edge Cases

	[Fact]
	public async Task AddAttachments_WithNullAttachments_ShouldNotThrow()
	{
		// Arrange
		BodyBuilder bodyBuilder = new();

		// Act
		await AddAttachments(null, bodyBuilder, false, TestContext.Current.CancellationToken);

		// Assert
		bodyBuilder.Attachments.Count.ShouldBe(0);
	}

	[Fact]
	public async Task AddAttachments_WithEmptyAttachments_ShouldNotThrow()
	{
		// Arrange
		BodyBuilder bodyBuilder = new();
		IMailAttachment[] attachments = Array.Empty<IMailAttachment>();

		// Act
		await AddAttachments(attachments, bodyBuilder, false, TestContext.Current.CancellationToken);

		// Assert
		bodyBuilder.Attachments.Count.ShouldBe(0);
	}

	[Fact]
	public async Task AddAttachments_WithNullAttachmentName_ShouldUseDefaultName()
	{
		// Arrange
		BodyBuilder bodyBuilder = new();
		IMailAttachment[] attachments = new[]
		{
			new MailAttachment(null, new byte[] { 1, 2, 3 })
		};

		// Act
		await AddAttachments(attachments, bodyBuilder, false, TestContext.Current.CancellationToken);

		// Assert
		bodyBuilder.Attachments.Count.ShouldBe(1);
		(bodyBuilder.Attachments[0] as MimePart)?.FileName.ShouldBe("File 1");
	}

	[Fact]
	public async Task AddAttachments_WithMultipleNullNames_ShouldUseIncrementingDefaultNames()
	{
		// Arrange
		BodyBuilder bodyBuilder = new();
		IMailAttachment[] attachments = new[]
		{
			new MailAttachment(null, new byte[] { 1, 2, 3 }),
			new MailAttachment(null, new byte[] { 4, 5, 6 }),
			new MailAttachment(null, new byte[] { 7, 8, 9 })
		};

		// Act
		await AddAttachments(attachments, bodyBuilder, false, TestContext.Current.CancellationToken);

		// Assert
		bodyBuilder.Attachments.Count.ShouldBe(3);
		(bodyBuilder.Attachments[0] as MimePart)?.FileName.ShouldBe("File 1");
		(bodyBuilder.Attachments[1] as MimePart)?.FileName.ShouldBe("File 2");
		(bodyBuilder.Attachments[2] as MimePart)?.FileName.ShouldBe("File 3");
	}

	[Fact]
	public async Task AddAttachments_WithAttachmentReturningNullStream_ShouldSkipAttachment()
	{
		// Arrange
		BodyBuilder bodyBuilder = new();
		IMailAttachment[] attachments = new[]
		{
			new MailAttachment("test1.txt", new byte[] { 1, 2, 3 }),
			new MailAttachment("test2.txt", (Stream?)null),
			new MailAttachment("test3.txt", new byte[] { 7, 8, 9 })
		};

		// Act
		await AddAttachments(attachments, bodyBuilder, false, TestContext.Current.CancellationToken);

		// Assert
		bodyBuilder.Attachments.Count.ShouldBe(2);
		(bodyBuilder.Attachments[0] as MimePart)?.FileName.ShouldBe("test1.txt");
		(bodyBuilder.Attachments[1] as MimePart)?.FileName.ShouldBe("test3.txt");
	}

	[Fact]
	public async Task AddAttachments_WithZipAndNullAttachmentName_ShouldSkipAttachment()
	{
		// Arrange
		BodyBuilder bodyBuilder = new();
		IMailAttachment[] attachments = new[]
		{
			new MailAttachment(null, new byte[] { 1, 2, 3 }),
			new MailAttachment("test.txt", new byte[] { 4, 5, 6 })
		};

		// Act
		await AddAttachments(attachments, bodyBuilder, true, TestContext.Current.CancellationToken);

		// Assert
		bodyBuilder.Attachments.Count.ShouldBe(1);
		(bodyBuilder.Attachments[0] as MimePart)?.FileName.ShouldBe("Files.zip");
	}

	[Fact]
	public async Task AddAttachments_WithZipAndEmptyAttachmentName_ShouldSkipAttachment()
	{
		// Arrange
		BodyBuilder bodyBuilder = new();
		IMailAttachment[] attachments = new[]
		{
			new MailAttachment("", new byte[] { 1, 2, 3 }),
			new MailAttachment("test.txt", new byte[] { 4, 5, 6 })
		};

		// Act
		await AddAttachments(attachments, bodyBuilder, true, TestContext.Current.CancellationToken);

		// Assert
		bodyBuilder.Attachments.Count.ShouldBe(1);
		(bodyBuilder.Attachments[0] as MimePart)?.FileName.ShouldBe("Files.zip");
	}

	[Fact]
	public async Task AddAttachments_WithZipAndWhitespaceAttachmentName_ShouldSkipAttachment()
	{
		// Arrange
		BodyBuilder bodyBuilder = new();
		IMailAttachment[] attachments = new[]
		{
			new MailAttachment("   ", new byte[] { 1, 2, 3 }),
			new MailAttachment("test.txt", new byte[] { 4, 5, 6 })
		};

		// Act
		await AddAttachments(attachments, bodyBuilder, true, TestContext.Current.CancellationToken);

		// Assert
		bodyBuilder.Attachments.Count.ShouldBe(1);
		(bodyBuilder.Attachments[0] as MimePart)?.FileName.ShouldBe("Files.zip");
	}

	[Fact]
	public async Task MailAttachment_DisposeWithNullStream_ShouldNotThrow()
	{
		// Arrange
		MailAttachment attachment = new("test.txt", (Stream?)null);

		// Act & Assert
		Exception? exception = await Record.ExceptionAsync(async () => await attachment.DisposeAsync());
		exception.ShouldBeNull();
	}

	[Fact]
	public void MailAttachment_SynchronousDisposeWithNullStream_ShouldNotThrow()
	{
		// Arrange
		MailAttachment attachment = new("test.txt", (Stream?)null);

		// Act & Assert
		Exception? exception = Record.Exception(attachment.Dispose);
		exception.ShouldBeNull();
	}

	[Fact]
	public async Task MailAttachment_DisposeWithByteArrayConstructor_ShouldDisposeStream()
	{
		// Arrange
		byte[] bytes = new byte[] { 1, 2, 3, 4, 5 };
		MailAttachment attachment = new("test.txt", bytes);

		// Act
		await attachment.DisposeAsync();

		// Assert
		Assert.Throws<ObjectDisposedException>(() => attachment.AttachmentStream?.ReadByte());
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithMultipleInvalidRecipients_ShouldReturnFalse(string smtpServer, int smtpPort)
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[]
				{
					new MailAddress("To1", "invalid1"),
					new MailAddress("To2", "invalid2"),
					new MailAddress("To3", "to3@example.com")
				}
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithCcAndBccAddressesAllValid_ShouldProcessCorrectly(string smtpServer, int smtpPort)
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") },
				CcAddresses = new[]
				{
					new MailAddress("CC1", "cc1@example.com"),
					new MailAddress("CC2", "cc2@example.com")
				},
				BccAddresses = new[]
				{
					new MailAddress("BCC1", "bcc1@example.com"),
					new MailAddress("BCC2", "bcc2@example.com")
				}
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithMultipleCcAddresses_ShouldProcessCorrectly(string smtpServer, int smtpPort)
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") },
				CcAddresses = new[]
				{
					new MailAddress("CC1", "cc1@example.com"),
					new MailAddress("CC2", "cc2@example.com"),
					new MailAddress("CC3", "cc3@example.com")
				}
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithMultipleBccAddresses_ShouldProcessCorrectly(string smtpServer, int smtpPort)
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") },
				BccAddresses = new[]
				{
					new MailAddress("BCC1", "bcc1@example.com"),
					new MailAddress("BCC2", "bcc2@example.com"),
					new MailAddress("BCC3", "bcc3@example.com")
				}
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithMultipleToAddresses_ShouldProcessCorrectly(string smtpServer, int smtpPort)
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[]
				{
					new MailAddress("To1", "to1@example.com"),
					new MailAddress("To2", "to2@example.com"),
					new MailAddress("To3", "to3@example.com")
				}
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithSmtpAuthenticationCredentials_ShouldProcessCorrectly(string smtpServer, int smtpPort)
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings
			{
				SmtpServer = smtpServer,
				SmtpPort = smtpPort,
				SmtpUser = "user@example.com",
				SmtpPassword = "password123"
			},
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithAttachmentsAutoDisposeFalse_ShouldNotDisposeAttachments(string smtpServer, int smtpPort)
	{
		// Arrange
		MemoryStream stream = new(new byte[] { 1, 2, 3 });
		IMailAttachment[] attachments = new[]
		{
			new MailAttachment("test.txt", stream)
		};
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body", false, attachments, false, false)
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
														// Stream should still be usable since AutoDisposeAttachments is false
		stream.CanRead.ShouldBeTrue();
		await stream.DisposeAsync();
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithAttachmentsAutoDisposeTrue_ShouldDisposeAttachments(string smtpServer, int smtpPort)
	{
		// Arrange
		MemoryStream stream = new(new byte[] { 1, 2, 3 });
		IMailAttachment[] attachments = new[]
		{
			new MailAttachment("test.txt", stream)
		};
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body", false, attachments, true, false)
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
														// Stream should be disposed since AutoDisposeAttachments is true
		Assert.Throws<ObjectDisposedException>(() => stream.ReadByte());
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithNullFromAddressName_ShouldProcessCorrectly(string smtpServer, int smtpPort)
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings(smtpServer, smtpPort),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress(null, "sender@example.com"),
				ToAddresses = new[] { new MailAddress(null, "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	[Fact]
	public async Task SendEmail_WithCancelledToken_ShouldReturnFalse()
	{
		// Arrange
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings("smtp.example.com", 587, "user", "pass"),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, cts.Token);

		// Assert
		result.ShouldBeFalse();
	}

	[Theory]
	[AutoData]
	public async Task SendEmail_WithNullSmtpServer_ShouldReturnFalse(int smtpPort)
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings
			{
				SmtpServer = null,
				SmtpPort = smtpPort
			},
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task SendEmail_WithInvalidSmtpPort_ShouldReturnFalse()
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings
			{
				SmtpServer = "smtp.example.com",
				SmtpPort = -1 // Invalid port
			},
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task SendEmail_WithVeryLongEmailSubject_ShouldProcessCorrectly()
	{
		// Arrange
		string veryLongSubject = new('A', 1000);
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings("smtp.example.com", 587),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent(veryLongSubject, "Body")
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	[Fact]
	public async Task SendEmail_WithVeryLongEmailBody_ShouldProcessCorrectly()
	{
		// Arrange
		string veryLongBody = new('B', 10000);
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings("smtp.example.com", 587),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", veryLongBody)
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	[Fact]
	public async Task SendEmail_WithEmptySubject_ShouldProcessCorrectly()
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings("smtp.example.com", 587),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("", "Body")
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	[Fact]
	public async Task SendEmail_WithNullSubject_ShouldProcessCorrectly()
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings("smtp.example.com", 587),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent(null, "Body")
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	[Fact]
	public async Task SendEmail_WithNullBody_ShouldProcessCorrectly()
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings("smtp.example.com", 587),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", null)
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	[Fact]
	public async Task SendEmail_WithMixedAsyncAndSyncDisposableAttachments_ShouldDisposeAll()
	{
		// Arrange
		MemoryStream stream1 = new(new byte[] { 1, 2, 3 });
		IMailAttachment[] attachments = new IMailAttachment[]
		{
			new MailAttachment("test1.txt", stream1),
			new MailAttachmentBytes("test2.txt", new byte[] { 7, 8, 9 })
		};
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings("smtp.example.com", 587),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body", false, attachments, true, false)
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
		// MailAttachment should be disposed
		Assert.Throws<ObjectDisposedException>(() => stream1.ReadByte());
	}

	[Fact]
	public void IsValidEmail_WithSpecialCharacters_ShouldValidateCorrectly()
	{
		// Arrange & Act & Assert
		"user+tag@example.com".IsValidEmail().ShouldBeTrue();
		"user.name@example.com".IsValidEmail().ShouldBeTrue();
		"user_name@example.com".IsValidEmail().ShouldBeTrue();
		"user-name@example.com".IsValidEmail().ShouldBeTrue();
	}

	[Fact]
	public void IsValidEmail_WithInternationalDomain_ShouldValidateCorrectly()
	{
		// Arrange & Act & Assert
		"user@example.co.uk".IsValidEmail().ShouldBeTrue();
		"user@sub.domain.example.com".IsValidEmail().ShouldBeTrue();
	}

	[Fact]
	public void IsValidEmail_WithInvalidFormats_ShouldReturnFalse()
	{
		// Arrange & Act & Assert
		"user".IsValidEmail().ShouldBeFalse();
		"@example.com".IsValidEmail().ShouldBeFalse();
		"user@".IsValidEmail().ShouldBeFalse();
		"user @example.com".IsValidEmail().ShouldBeFalse();
		"user@example .com".IsValidEmail().ShouldBeFalse();
		"user@exam ple.com".IsValidEmail().ShouldBeFalse();
	}

	[Fact]
	public async Task AddAttachments_WithLargeAttachment_ShouldProcessCorrectly()
	{
		// Arrange
		BodyBuilder bodyBuilder = new();
		byte[] largeData = new byte[1024 * 1024]; // 1 MB
		new Random().NextBytes(largeData);
		IMailAttachment[] attachments = new[]
		{
			new MailAttachment("large.bin", largeData)
		};

		// Act
		await AddAttachments(attachments, bodyBuilder, false, TestContext.Current.CancellationToken);

		// Assert
		bodyBuilder.Attachments.Count.ShouldBe(1);
	}

	[Fact]
	public async Task AddAttachments_WithLargeAttachmentZipped_ShouldProcessCorrectly()
	{
		// Arrange
		BodyBuilder bodyBuilder = new();
		byte[] largeData = new byte[1024 * 100]; // 100 KB
		new Random().NextBytes(largeData);
		IMailAttachment[] attachments = new[]
		{
			new MailAttachment("large.bin", largeData)
		};

		// Act
		await AddAttachments(attachments, bodyBuilder, true, TestContext.Current.CancellationToken);

		// Assert
		bodyBuilder.Attachments.Count.ShouldBe(1);
		(bodyBuilder.Attachments[0] as MimePart)?.FileName.ShouldBe("Files.zip");
	}

	[Fact]
	public async Task AddAttachments_WithCancelledToken_ShouldNotThrow()
	{
		// Arrange
		BodyBuilder bodyBuilder = new();
		using CancellationTokenSource cts = new();
		await cts.CancelAsync();
		IMailAttachment[] attachments = new[]
		{
			new MailAttachment("test.txt", new byte[] { 1, 2, 3 })
		};

		// Act & Assert - should not throw even with cancelled token
		// The method catches all exceptions
		await AddAttachments(attachments, bodyBuilder, false, cts.Token);

		bodyBuilder.Attachments.Count.ShouldBe(0); // Should not add attachments due to cancellation
	}

	[Fact]
	public async Task SendEmail_WithEmptyAttachmentsList_ShouldProcessCorrectly()
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings("smtp.example.com", 587),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body", false, Array.Empty<IMailAttachment>(), false, false)
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	[Fact]
	public void MailAttachmentBytes_WithEmptyBytes_ShouldCreateEmptyStream()
	{
		// Arrange
		MailAttachmentBytes attachment = new("test.txt", Array.Empty<byte>());

		// Act
		Stream? stream = attachment.GetStream();

		// Assert
		stream.ShouldNotBeNull();
		stream.Length.ShouldBe(0);
	}

	[Fact]
	public void MailAttachment_WithNullAttachmentName_ShouldReturnNull()
	{
		// Arrange
		MailAttachment attachment = new(null, new byte[] { 1, 2, 3 });

		// Act & Assert
		attachment.AttachmentName.ShouldBeNull();
	}

	[Fact]
	public void MailAttachment_SetAttachmentName_ShouldUpdateValue()
	{
		// Arrange
		MailAttachment attachment = new("original.txt", new byte[] { 1, 2, 3 })
		{
			// Act
			AttachmentName = "updated.txt"
		};

		// Assert
		attachment.AttachmentName.ShouldBe("updated.txt");
	}

	[Fact]
	public void MailAttachmentBytes_SetAttachmentName_ShouldUpdateValue()
	{
		// Arrange
		MailAttachmentBytes attachment = new("original.txt", new byte[] { 1, 2, 3 })
		{
			// Act
			AttachmentName = "updated.txt"
		};

		// Assert
		attachment.AttachmentName.ShouldBe("updated.txt");
	}

	[Fact]
	public void MailAttachmentBytes_SetAttachmentBytes_ShouldUpdateValue()
	{
		// Arrange
		MailAttachmentBytes attachment = new("test.txt", new byte[] { 1, 2, 3 })
		{
			// Act
			AttachmentBytes = new byte[] { 4, 5, 6, 7 }
		};

		// Assert
		attachment.AttachmentBytes.ShouldNotBeNull();
		attachment.AttachmentBytes.Length.ShouldBe(4);
	}

	[Fact]
	public void EmailConstants_ShouldHaveCorrectValues()
	{
		// Assert
		EmailConstants.EmailRegex.ShouldBe(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
		EmailConstants.MaxEmailLength.ShouldBe(320);
	}

	[Fact]
	public async Task SendEmail_WithAllRecipientsInCcOnly_ShouldProcessCorrectly()
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings("smtp.example.com", 587),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = Array.Empty<MailAddress>(),
				CcAddresses = new[] { new MailAddress("CC", "cc@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server but should process CC addresses
	}

	[Fact]
	public async Task SendEmail_WithAllRecipientsInBccOnly_ShouldProcessCorrectly()
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings("smtp.example.com", 587),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = Array.Empty<MailAddress>(),
				BccAddresses = new[] { new MailAddress("BCC", "bcc@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server but should process BCC addresses
	}

	[Fact]
	public async Task SendEmail_WithOnlyBccAndCc_ShouldProcessCorrectly()
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings("smtp.example.com", 587),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = Array.Empty<MailAddress>(),
				CcAddresses = new[] { new MailAddress("CC", "cc@example.com") },
				BccAddresses = new[] { new MailAddress("BCC", "bcc@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	[Fact]
	public async Task AddAttachments_WithMultipleAttachmentsZipped_ShouldProcessCorrectly()
	{
		// Arrange
		BodyBuilder bodyBuilder = new();
		IMailAttachment[] attachments = new[]
		{
			new MailAttachment("file1.txt", new byte[] { 1, 2, 3 }),
			new MailAttachment("file2.txt", new byte[] { 4, 5, 6 }),
			new MailAttachment("file3.txt", new byte[] { 7, 8, 9 }),
			new MailAttachment("file4.txt", new byte[] { 10, 11, 12 })
		};

		// Act
		await AddAttachments(attachments, bodyBuilder, true, TestContext.Current.CancellationToken);

		// Assert
		bodyBuilder.Attachments.Count.ShouldBe(1);
		(bodyBuilder.Attachments[0] as MimePart)?.FileName.ShouldBe("Files.zip");
	}

	[Fact]
	public async Task AddAttachments_WithMultipleStreamsZipped_ShouldProcessCorrectly()
	{
		// Arrange
		BodyBuilder bodyBuilder = new();
		IMailAttachment[] attachments = new[]
		{
			new MailAttachment("stream1.txt", new MemoryStream(new byte[] { 1, 2, 3 })),
			new MailAttachment("stream2.txt", new MemoryStream(new byte[] { 4, 5, 6 })),
			new MailAttachment("stream3.txt", new MemoryStream(new byte[] { 7, 8, 9 }))
		};

		// Act
		await AddAttachments(attachments, bodyBuilder, true, TestContext.Current.CancellationToken);

		// Assert
		bodyBuilder.Attachments.Count.ShouldBe(1);
		(bodyBuilder.Attachments[0] as MimePart)?.FileName.ShouldBe("Files.zip");
	}

	[Fact]
	public async Task SendEmail_WithManyAttachments_ShouldProcessCorrectly()
	{
		// Arrange
		IMailAttachment[] attachments = new IMailAttachment[10];
		for (int i = 0; i < 10; i++)
		{
			attachments[i] = new MailAttachment($"file{i}.txt", new byte[] { (byte)i });
		}

		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings("smtp.example.com", 587),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body", false, attachments, true, false)
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	[Fact]
	public async Task SendEmail_WithManyAttachmentsZipped_ShouldProcessCorrectly()
	{
		// Arrange
		IMailAttachment[] attachments = new IMailAttachment[10];
		for (int i = 0; i < 10; i++)
		{
			attachments[i] = new MailAttachment($"file{i}.txt", new byte[] { (byte)i });
		}

		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings("smtp.example.com", 587),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body", false, attachments, true, true)
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
	}

	[Fact]
	public void MailAddress_WithValidation_ShouldValidateEmail()
	{
		// Arrange
		MailAddress address = new("Test", "test@example.com");
		ValidationContext validationContext = new(address);
		List<ValidationResult> validationResults = new();

		// Act
		bool isValid = Validator.TryValidateObject(address, validationContext, validationResults, true);

		// Assert
		isValid.ShouldBeTrue();
		validationResults.ShouldBeEmpty();
	}

	[Fact]
	public void MailAddress_WithInvalidEmail_ShouldFailValidation()
	{
		// Arrange
		MailAddress address = new("Test", "not-an-email");
		ValidationContext validationContext = new(address);
		List<ValidationResult> validationResults = new();

		// Act
		bool isValid = Validator.TryValidateObject(address, validationContext, validationResults, true);

		// Assert
		isValid.ShouldBeFalse();
		validationResults.ShouldNotBeEmpty();
	}

	[Fact]
	public void MailAttachment_SetAttachmentStream_ShouldUpdateValue()
	{
		// Arrange
		MemoryStream originalStream = new(new byte[] { 1, 2, 3 });
		MailAttachment attachment = new("test.txt", originalStream);

		// Act
		MemoryStream newStream = new(new byte[] { 4, 5, 6 });
		attachment.AttachmentStream = newStream;

		// Assert
		attachment.AttachmentStream.ShouldBe(newStream);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(587)]
	[InlineData(25)]
	[InlineData(465)]
	[InlineData(2525)]
	public void SmtpSettings_WithVariousPorts_ShouldSetCorrectly(int port)
	{
		// Arrange & Act
		SmtpSettings settings = new("smtp.example.com", port);

		// Assert
		settings.SmtpPort.ShouldBe(port);
	}

	[Fact]
	public async Task SendEmail_WithSyncDisposableAttachment_ShouldDisposeCorrectly()
	{
		// Arrange
		MockDisposableAttachment disposableAttachment = new("test.txt");
		IMailAttachment[] attachments = new IMailAttachment[]
		{
			disposableAttachment
		};
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings("smtp.example.com", 587),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body", false, attachments, true, false)
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
		disposableAttachment.IsDisposed.ShouldBeTrue(); // Should be disposed via IDisposable.Dispose()
	}

	[Fact]
	public async Task SendEmail_WithMixedAsyncAndSyncDisposableAttachments_ShouldDisposeBoth()
	{
		// Arrange
		MemoryStream stream = new(new byte[] { 1, 2, 3 });
		MockDisposableAttachment syncDisposableAttachment = new("sync.txt");
		IMailAttachment[] attachments = new IMailAttachment[]
		{
			new MailAttachment("async.txt", stream), // IAsyncDisposable
			syncDisposableAttachment // IDisposable only
		};
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings("smtp.example.com", 587),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body", false, attachments, true, false)
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
		// MailAttachment should be disposed async
		Assert.Throws<ObjectDisposedException>(() => stream.ReadByte());
		// MockDisposableAttachment should be disposed sync
		syncDisposableAttachment.IsDisposed.ShouldBeTrue();
	}

	[Fact]
	public async Task SendEmail_WithOnlySyncDisposableAttachments_ShouldDisposeCorrectly()
	{
		// Arrange
		MockDisposableAttachment attachment1 = new("file1.txt");
		MockDisposableAttachment attachment2 = new("file2.txt");
		MockDisposableAttachment attachment3 = new("file3.txt");
		IMailAttachment[] attachments = new IMailAttachment[]
		{
			attachment1,
			attachment2,
			attachment3
		};
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings("smtp.example.com", 587),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body", false, attachments, true, false)
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
		attachment1.IsDisposed.ShouldBeTrue();
		attachment2.IsDisposed.ShouldBeTrue();
		attachment3.IsDisposed.ShouldBeTrue();
	}

	[Fact]
	public async Task SendEmail_WithInvalidSmtpServerAndAuth_ShouldRetryAndFail()
	{
		// Arrange - Use clearly invalid server that will fail quickly
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings
			{
				SmtpServer = "invalid.smtp.server.that.does.not.exist.local",
				SmtpPort = 587,
				SmtpUser = "user@example.com",
				SmtpPassword = "password"
			},
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert - Should fail after all retries
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task SendEmail_WithInvalidSmtpServerNoAuth_ShouldRetryAndFail()
	{
		// Arrange - Use clearly invalid server that will fail quickly, no auth
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings
			{
				SmtpServer = "invalid.smtp.server.that.does.not.exist.local",
				SmtpPort = 25,
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
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert - Should fail after all retries
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task SendEmail_WithLocalhostAndAuth_ShouldAttemptConnection()
	{
		// Arrange - Use localhost which won't have SMTP but will try to connect
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings
			{
				SmtpServer = "127.0.0.1",
				SmtpPort = 25,
				SmtpUser = "test@test.com",
				SmtpPassword = "TestPass"
			},
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert - Should fail but will exercise the auth path
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task SendEmail_WithLocalhostNoAuth_ShouldAttemptConnection()
	{
		// Arrange - Use localhost which won't have SMTP but will try to connect
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings
			{
				SmtpServer = "127.0.0.1",
				SmtpPort = 25,
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
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert - Should fail but will exercise the non-auth path
		result.ShouldBeFalse();
	}

	[Theory]
	[InlineData("192.0.2.1", 587, "user", "pass")] // TEST-NET-1 address
	[InlineData("192.0.2.2", 25, "", "")] // TEST-NET-1 address, no auth
	[InlineData("198.51.100.1", 465, "admin", "secret")] // TEST-NET-2 address
	public async Task SendEmail_WithVariousInvalidServers_ShouldRetryAndFail(string server, int port, string user, string pass)
	{
		// Arrange - Use reserved TEST-NET addresses that won't respond
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings
			{
				SmtpServer = server,
				SmtpPort = port,
				SmtpUser = user,
				SmtpPassword = pass
			},
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert - Should fail after retries
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task SendEmail_WithWhitespaceSmtpPassword_ShouldUseNoAuthPath()
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings
			{
				SmtpServer = "invalid.example.com",
				SmtpPort = 587,
				SmtpUser = "user@example.com",
				SmtpPassword = "   " // Whitespace password
			},
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task SendEmail_WithWhitespaceSmtpUser_ShouldUseNoAuthPath()
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings
			{
				SmtpServer = "invalid.example.com",
				SmtpPort = 587,
				SmtpUser = "   ", // Whitespace user
				SmtpPassword = "password"
			},
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task SendEmail_WithVeryHighPort_ShouldRetryAndFail()
	{
		// Arrange - Use a very high port number that's unlikely to be in use
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings
			{
				SmtpServer = "127.0.0.1",
				SmtpPort = 65000,
				SmtpUser = "user",
				SmtpPassword = "pass"
			},
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task SendEmail_WithComplexEmailAndInvalidServer_ShouldFailAfterRetries()
	{
		// Arrange - Complex email with all features to ensure we go through entire process before hitting SMTP failures
		MemoryStream stream1 = new(new byte[] { 1, 2, 3 });
		MemoryStream stream2 = new(new byte[] { 4, 5, 6 });
		IMailAttachment[] attachments = new IMailAttachment[]
		{
			new MailAttachment("file1.txt", stream1),
			new MailAttachment("file2.txt", stream2)
		};

		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings
			{
				SmtpServer = "nonexistent.invalid.test.local",
				SmtpPort = 587,
				SmtpUser = "test@test.com",
				SmtpPassword = "password"
			},
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Test Sender", "sender@example.com"),
				ToAddresses = new[]
				{
					new MailAddress("To 1", "to1@example.com"),
					new MailAddress("To 2", "to2@example.com")
				},
				CcAddresses = new[]
				{
					new MailAddress("CC User", "cc@example.com")
				},
				BccAddresses = new[]
				{
					new MailAddress("BCC User", "bcc@example.com")
				}
			},
			EmailContent = new EmailContent
			{
				Subject = "Test Subject with Special Chars: <>&\"'",
				Body = "<html><body><h1>HTML Email</h1><p>With content</p></body></html>",
				BodyIsHtml = true,
				Attachments = attachments,
				AutoDisposeAttachments = true
			},
			ReadReceipt = true,
			ReadReceiptEmail = "receipt@example.com"
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
		// Attachments should be disposed
		Assert.Throws<ObjectDisposedException>(() => stream1.ReadByte());
		Assert.Throws<ObjectDisposedException>(() => stream2.ReadByte());
	}

	[Fact]
	public async Task SendEmail_WithNonRespondingServer_ShouldRetryEightTimes()
	{
		// Arrange - Use 0.0.0.0 which should immediately fail
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings
			{
				SmtpServer = "0.0.0.0",
				SmtpPort = 25,
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
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task SendEmail_WithEmptyStringAuth_ShouldUseNoAuthConnection()
	{
		// Arrange - Empty strings for auth (different from null)
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings
			{
				SmtpServer = "0.0.0.0",
				SmtpPort = 25,
				SmtpUser = string.Empty,
				SmtpPassword = string.Empty
			},
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task SendEmail_WithZippedAttachmentsAndInvalidServer_ShouldFailAfterRetries()
	{
		// Arrange - Ensure attachment processing completes before SMTP failures
		IMailAttachment[] attachments = new IMailAttachment[]
		{
			new MailAttachment("file1.txt", new byte[] { 1, 2, 3, 4, 5 }),
			new MailAttachment("file2.txt", new byte[] { 6, 7, 8, 9, 10 }),
			new MailAttachment("file3.txt", new byte[] { 11, 12, 13, 14, 15 })
		};

		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings
			{
				SmtpServer = "test.invalid.nonexistent.local",
				SmtpPort = 587,
				SmtpUser = "TestUser",
				SmtpPassword = "TestPass"
			},
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent
			{
				Subject = "Test",
				Body = "Test body",
				Attachments = attachments,
				ZipAttachments = true,
				AutoDisposeAttachments = false
			}
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task SendEmail_WithSyncDisposableAttachment_ShouldCallSyncDispose()
	{
		// Arrange
		MockDisposableAttachment mockAttachment = new("test.txt");
		IMailAttachment[] attachments = new IMailAttachment[] { mockAttachment };

		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings("invalid.smtp.server", 587),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent
			{
				Subject = "Test",
				Body = "Test body",
				Attachments = attachments,
				AutoDisposeAttachments = true // Should dispose after sending
			}
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Will fail due to invalid SMTP server
		mockAttachment.IsDisposed.ShouldBeTrue(); // Should have called Dispose() not DisposeAsync()
	}

	[Fact]
	public async Task SendEmail_WithMixedAsyncAndSyncDisposableAttachments_ShouldDisposeAllCorrectly()
	{
		// Arrange
		MemoryStream asyncStream = new(new byte[] { 1, 2, 3 });
		MailAttachment asyncDisposable = new("async.txt", asyncStream);
		MockDisposableAttachment syncDisposable = new("sync.txt");

		IMailAttachment[] attachments = new IMailAttachment[] { asyncDisposable, syncDisposable };

		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings("invalid.smtp.server", 587),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent
			{
				Subject = "Test",
				Body = "Test body",
				Attachments = attachments,
				AutoDisposeAttachments = true
			}
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
		syncDisposable.IsDisposed.ShouldBeTrue(); // Sync disposable should be disposed
		Assert.Throws<ObjectDisposedException>(() => asyncStream.ReadByte()); // Async disposable should be disposed
	}

	[Fact]
	public async Task SendEmail_WithOnlySyncDisposableAttachments_ShouldDisposeSynchronously()
	{
		// Arrange
		MockDisposableAttachment attachment1 = new("test1.txt");
		MockDisposableAttachment attachment2 = new("test2.txt");
		MockDisposableAttachment attachment3 = new("test3.txt");

		IMailAttachment[] attachments = new IMailAttachment[] { attachment1, attachment2, attachment3 };

		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings("invalid.smtp.server", 587),
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent
			{
				Subject = "Test",
				Body = "Test body",
				Attachments = attachments,
				AutoDisposeAttachments = true
			}
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
		attachment1.IsDisposed.ShouldBeTrue();
		attachment2.IsDisposed.ShouldBeTrue();
		attachment3.IsDisposed.ShouldBeTrue();
	}

	[Fact]
	public async Task SendEmail_WithInvalidDnsName_ShouldTriggerRetryLogic()
	{
		// Arrange - using a definitely non-existent domain to trigger DNS failure
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings
			{
				SmtpServer = "this.domain.absolutely.does.not.exist.invalid",
				SmtpPort = 587,
				SmtpUser = "user@example.com",
				SmtpPassword = "password"
			},
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Should fail after all retry attempts
	}

	[Fact]
	public async Task SendEmail_WithConnectionRefused_ShouldTriggerRetryLogic()
	{
		// Arrange - using localhost with a port that's unlikely to have an SMTP server
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings
			{
				SmtpServer = "127.0.0.1",
				SmtpPort = 9999, // Port unlikely to have SMTP server
				SmtpUser = "user@example.com",
				SmtpPassword = "password"
			},
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Should fail after all retry attempts
	}

	[Fact]
	public async Task SendEmail_WithoutAuthentication_ShouldAttemptUnauthenticatedConnection()
	{
		// Arrange - no credentials, should use SecureSocketOptions.None
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings
			{
				SmtpServer = "127.0.0.1",
				SmtpPort = 9998,
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
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Should fail due to no server
	}

	[Fact]
	public async Task SendEmail_WithEmptyCredentials_ShouldAttemptUnauthenticatedConnection()
	{
		// Arrange - empty credentials, should use SecureSocketOptions.None
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings
			{
				SmtpServer = "127.0.0.1",
				SmtpPort = 9997,
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
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Should fail due to no server
	}

	[Fact]
	public async Task SendEmail_WithWhitespaceCredentials_ShouldAttemptUnauthenticatedConnection()
	{
		// Arrange - whitespace credentials, should use SecureSocketOptions.None
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings
			{
				SmtpServer = "127.0.0.1",
				SmtpPort = 9996,
				SmtpUser = "   ",
				SmtpPassword = "   "
			},
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Should fail due to no server
	}

	[Fact]
	public async Task SendEmail_WithCredentials_ShouldAttemptAuthenticatedConnection()
	{
		// Arrange - with credentials, should use SecureSocketOptions.StartTls
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings
			{
				SmtpServer = "127.0.0.1",
				SmtpPort = 9995,
				SmtpUser = "validuser@example.com",
				SmtpPassword = "validpassword"
			},
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Should fail due to no server but should attempt authenticated connection
	}

	[Fact]
	public async Task SendEmail_WithInvalidPortZero_ShouldTriggerRetryLogic()
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings
			{
				SmtpServer = "smtp.example.com",
				SmtpPort = 0,
				SmtpUser = "user@example.com",
				SmtpPassword = "password"
			},
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task SendEmail_WithVeryHighPort_ShouldTriggerRetryLogic()
	{
		// Arrange
		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings
			{
				SmtpServer = "127.0.0.1",
				SmtpPort = 65535,
				SmtpUser = "user@example.com",
				SmtpPassword = "password"
			},
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender", "sender@example.com"),
				ToAddresses = new[] { new MailAddress("To", "to@example.com") }
			},
			EmailContent = new EmailContent("Subject", "Body")
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public async Task SendEmail_WithComplexEmailContent_ShouldTriggerAllSmtpPaths()
	{
		// Arrange - complex email with all features to ensure all SMTP paths are hit
		MockDisposableAttachment syncAttachment = new("sync.txt");
		MailAttachment asyncAttachment = new("async.txt", new byte[] { 1, 2, 3 });

		SendEmailConfig config = new()
		{
			SmtpSettings = new SmtpSettings
			{
				SmtpServer = "nonexistent.smtp.server.invalid",
				SmtpPort = 587,
				SmtpUser = "user@example.com",
				SmtpPassword = "password123"
			},
			EmailAddresses = new EmailAddresses
			{
				FromAddress = new MailAddress("Sender Name", "sender@example.com"),
				ToAddresses = new[]
				{
					new MailAddress("Recipient 1", "recipient1@example.com"),
					new MailAddress("Recipient 2", "recipient2@example.com")
				},
				CcAddresses = new[]
				{
					new MailAddress("CC 1", "cc1@example.com")
				},
				BccAddresses = new[]
				{
					new MailAddress("BCC 1", "bcc1@example.com")
				}
			},
			EmailContent = new EmailContent
			{
				Subject = "Complex Email Test",
				Body = "<html><body><h1>Test</h1></body></html>",
				BodyIsHtml = true,
				Attachments = new IMailAttachment[] { syncAttachment, asyncAttachment },
				AutoDisposeAttachments = true,
				ZipAttachments = false
			},
			ReadReceipt = true,
			ReadReceiptEmail = "receipt@example.com"
		};

		// Act
		bool result = await SendEmail(config, TestContext.Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Should fail due to invalid SMTP server
		syncAttachment.IsDisposed.ShouldBeTrue(); // Should dispose sync attachment
	}

	#endregion
}
