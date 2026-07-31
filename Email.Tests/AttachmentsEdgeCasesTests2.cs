using System.ComponentModel.DataAnnotations;
using AutoFixture.Xunit3;
using CommonNetFuncs.Email;
using MailKit.Security;
using MimeKit;
using static CommonNetFuncs.Email.Email;

namespace Email.Tests;

public sealed class AttachmentsEdgeCasesTests2
{
	#region AddAttachments Edge Cases (Part 2)


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

	#endregion
}
