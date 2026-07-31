using System.ComponentModel.DataAnnotations;
using AutoFixture.Xunit3;
using CommonNetFuncs.Email;
using MailKit.Security;
using MimeKit;
using static CommonNetFuncs.Email.Email;

namespace Email.Tests;

public sealed class AttachmentsEdgeCasesTests1
{
	#region AddAttachments Edge Cases (Part 1)

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
		IMailAttachment[] attachments = new[] { new MailAttachment("test.txt", stream) };
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

	#endregion
}
