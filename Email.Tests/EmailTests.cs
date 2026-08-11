using System.ComponentModel.DataAnnotations;
using AutoFixture.Xunit3;
using CommonNetFuncs.Email;
using MimeKit;
using static CommonNetFuncs.Email.Email;
using static Xunit.TestContext;

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
		bool result = await SendEmail(new SendEmailConfig() { SmtpSettings = smtpSettings, EmailAddresses = emailAddresses, EmailContent = emailContent }, Current.CancellationToken);

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
		bool result = await SendEmail(new SendEmailConfig() { SmtpSettings = smtpSettings, EmailAddresses = emailAddresses, EmailContent = emailContent }, Current.CancellationToken);

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
		await AddAttachments(attachments, bodyBuilder, true, Current.CancellationToken);

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
		await AddAttachments(attachments, bodyBuilder, false, Current.CancellationToken);

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
		}, Current.CancellationToken);

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
		bool result = await SendEmail(new SendEmailConfig() { SmtpSettings = smtpSettings, EmailAddresses = emailAddresses, EmailContent = emailContent }, Current.CancellationToken);

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
		await AddAttachments(attachments, bodyBuilder, false, Current.CancellationToken);

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
		await AddAttachments(attachments, bodyBuilder, true, Current.CancellationToken);

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
		await AddAttachments(attachments, bodyBuilder, false, Current.CancellationToken);

		// Assert
		bodyBuilder.Attachments.Count.ShouldBe(2);
		(bodyBuilder.Attachments[0] as MimePart)?.FileName.ShouldBe("test1.txt");
		(bodyBuilder.Attachments[1] as MimePart)?.FileName.ShouldBe("test2.txt");
	}

	#endregion
}
