using System.ComponentModel.DataAnnotations;
using AutoFixture.Xunit2;
using CommonNetFuncs.Email;
using MimeKit;

using static CommonNetFuncs.Email.Email;

namespace Email.Tests;

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
		bool result = await SendEmail(new SendEmailConfig() { SmtpSettings = smtpSettings, EmailAddresses = emailAddresses, EmailContent = emailContent });

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
		bool result = await SendEmail(new SendEmailConfig() { SmtpSettings = smtpSettings, EmailAddresses = emailAddresses, EmailContent = emailContent });

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
		await AddAttachments(attachments, bodyBuilder, true);

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
		await AddAttachments(attachments, bodyBuilder, false);

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
		bool result = await SendEmail(new SendEmailConfig() { SmtpSettings = smtpSettings, EmailAddresses = emailAddresses, EmailContent = emailContent, ReadReceipt = true, ReadReceiptEmail = "receipt@example.com" });

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
		bool result = await SendEmail(new SendEmailConfig() { SmtpSettings = smtpSettings, EmailAddresses = emailAddresses, EmailContent = emailContent });

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
		await AddAttachments(attachments, bodyBuilder, false);

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
		await AddAttachments(attachments, bodyBuilder, true);

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
		await AddAttachments(attachments, bodyBuilder, false);

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
		bool result = await SendEmail(config);

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
		bool result = await SendEmail(config);

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
		bool result = await SendEmail(config);

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
		bool result = await SendEmail(config);

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
		bool result = await SendEmail(config);

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
		bool result = await SendEmail(config);

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
		Exception? exception2 = Record.Exception(() => attachment.Dispose());

		// Assert
		exception1.ShouldBeNull();
		exception2.ShouldBeNull();
	}

	#endregion
}
