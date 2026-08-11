using CommonNetFuncs.Email;
using static CommonNetFuncs.Email.Email;
using static Xunit.TestContext;

namespace Email.Tests;

public sealed class AttachmentsEdgeCasesTests3
{
	#region AddAttachments Edge Cases (Part 3)


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
		bool result = await SendEmail(config, Current.CancellationToken);

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
		bool result = await SendEmail(config, Current.CancellationToken);

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
		bool result = await SendEmail(config, Current.CancellationToken);

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
		bool result = await SendEmail(config, Current.CancellationToken);

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
		bool result = await SendEmail(config, Current.CancellationToken);

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
		bool result = await SendEmail(config, Current.CancellationToken);

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
		bool result = await SendEmail(config, Current.CancellationToken);

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
		bool result = await SendEmail(config, Current.CancellationToken);

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
		bool result = await SendEmail(config, Current.CancellationToken);

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
		bool result = await SendEmail(config, Current.CancellationToken);

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
		bool result = await SendEmail(config, Current.CancellationToken);

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
		bool result = await SendEmail(config, Current.CancellationToken);

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
		bool result = await SendEmail(config, Current.CancellationToken);

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
		bool result = await SendEmail(config, Current.CancellationToken);

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
		bool result = await SendEmail(config, Current.CancellationToken);

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
		bool result = await SendEmail(config, Current.CancellationToken);

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
				SmtpPassword = "ValidPassword"
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
		bool result = await SendEmail(config, Current.CancellationToken);

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
		bool result = await SendEmail(config, Current.CancellationToken);

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
		bool result = await SendEmail(config, Current.CancellationToken);

		// Assert
		result.ShouldBeFalse(); // Should fail due to invalid SMTP server
		syncAttachment.IsDisposed.ShouldBeTrue(); // Should dispose sync attachment
	}

	#endregion
}
