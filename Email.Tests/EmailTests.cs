using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using AutoFixture.Xunit3;
using CommonNetFuncs.Email;
using MimeKit;
using static CommonNetFuncs.Compression.Streams;
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
		byte[] originalBytes = new byte[] { 1, 2, 3, 4, 5 };
		MemoryStream stream = new(originalBytes);
		MailAttachment attachment = new("test.txt", stream, CompressionLevel.NoCompression);

		// Act
		Stream? result = attachment.GetStream();

		// Assert
		result.ShouldNotBeNull();
		using MemoryStream resultCopy = new();
		result.CopyTo(resultCopy);
		resultCopy.ToArray().ShouldBe(originalBytes);
	}

	[Theory]
	[InlineData(CompressionLevel.NoCompression)]
	[InlineData(CompressionLevel.Fastest)]
	[InlineData(CompressionLevel.Optimal)]
	[InlineData(CompressionLevel.SmallestSize)]
	public void MailAttachment_StreamConstructor_GetStream_ShouldRoundTripForAllCompressionLevels(CompressionLevel compressionLevel)
	{
		// Arrange
		byte[] originalBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
		MemoryStream sourceStream = new(originalBytes);
		MailAttachment attachment = new("test.txt", sourceStream, compressionLevel);

		// Act
		Stream? result = attachment.GetStream();

		// Assert
		result.ShouldNotBeNull();
		using MemoryStream resultCopy = new();
		result.CopyTo(resultCopy);
		resultCopy.ToArray().ShouldBe(originalBytes);
	}

	[Theory]
	[InlineData(CompressionLevel.NoCompression)]
	[InlineData(CompressionLevel.Fastest)]
	[InlineData(CompressionLevel.Optimal)]
	[InlineData(CompressionLevel.SmallestSize)]
	public void MailAttachment_ByteArrayConstructor_GetStream_ShouldRoundTripForAllCompressionLevels(CompressionLevel compressionLevel)
	{
		// Arrange
		byte[] originalBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
		MailAttachment attachment = new("test.txt", originalBytes, compressionLevel);

		// Act
		Stream? result = attachment.GetStream();

		// Assert
		result.ShouldNotBeNull();
		using MemoryStream resultCopy = new();
		result.CopyTo(resultCopy);
		resultCopy.ToArray().ShouldBe(originalBytes);
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
		MailAttachment attachment = new("test.txt", bytes, CompressionLevel.NoCompression);

		// Assert - AttachmentStream holds Gzip-compressed data, so it will be larger than the original and must be decompressed to verify
		attachment.AttachmentStream.ShouldNotBeNull();
		attachment.AttachmentStream!.Position.ShouldBe(0);
		using MemoryStream compressedCopy = new();
		attachment.AttachmentStream.CopyTo(compressedCopy);
		compressedCopy.ToArray().Decompress(ECompressionType.Gzip, cancellationToken: Current.CancellationToken).ShouldBe(bytes);
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
		MailAttachmentBytes attachment = new("test.txt", stream, CompressionLevel.NoCompression);

		// Assert - AttachmentBytes holds Gzip-compressed data, so it must be decompressed to verify against the original
		attachment.AttachmentBytes.ShouldNotBeNull();
		attachment.AttachmentBytes.Decompress(ECompressionType.Gzip, cancellationToken: Current.CancellationToken).ShouldBe(originalBytes);
	}

	[Theory]
	[InlineData(CompressionLevel.NoCompression)]
	[InlineData(CompressionLevel.Fastest)]
	[InlineData(CompressionLevel.Optimal)]
	[InlineData(CompressionLevel.SmallestSize)]
	public void MailAttachmentBytes_StreamConstructor_GetStream_ShouldRoundTripForAllCompressionLevels(CompressionLevel compressionLevel)
	{
		// Arrange
		byte[] originalBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
		MemoryStream sourceStream = new(originalBytes);
		MailAttachmentBytes attachment = new("test.txt", sourceStream, compressionLevel);

		// Act
		Stream? result = attachment.GetStream();

		// Assert
		result.ShouldNotBeNull();
		using MemoryStream resultCopy = new();
		result.CopyTo(resultCopy);
		resultCopy.ToArray().ShouldBe(originalBytes);
	}

	[Theory]
	[InlineData(CompressionLevel.NoCompression)]
	[InlineData(CompressionLevel.Fastest)]
	[InlineData(CompressionLevel.Optimal)]
	[InlineData(CompressionLevel.SmallestSize)]
	public void MailAttachmentBytes_ByteArrayConstructor_GetStream_ShouldRoundTripForAllCompressionLevels(CompressionLevel compressionLevel)
	{
		// Arrange
		byte[] originalBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
		MailAttachmentBytes attachment = new("test.txt", originalBytes, compressionLevel);

		// Act
		Stream? result = attachment.GetStream();

		// Assert
		result.ShouldNotBeNull();
		using MemoryStream resultCopy = new();
		result.CopyTo(resultCopy);
		resultCopy.ToArray().ShouldBe(originalBytes);
	}

	[Theory]
	[InlineData(CompressionLevel.NoCompression)]
	[InlineData(CompressionLevel.Fastest)]
	[InlineData(CompressionLevel.Optimal)]
	[InlineData(CompressionLevel.SmallestSize)]
	public void MailAttachmentBytes_ReconstructedFromAlreadyCompressedBytes_ShouldNotDoubleCompress(CompressionLevel compressionLevel)
	{
		// Arrange - simulates a deserializer (e.g., Hangfire/Newtonsoft) reconstructing the object by invoking
		// the byte[] constructor again with the already-compressed AttachmentBytes from a previously constructed instance
		byte[] originalBytes = System.Text.Encoding.UTF8.GetBytes("This is the content of an important attachment file.");
		MailAttachmentBytes original = new("test.xlsx", originalBytes, compressionLevel);

		// Act
		MailAttachmentBytes reconstructed = new("test.xlsx", original.AttachmentBytes);
		Stream? result = reconstructed.GetStream();

		// Assert
		result.ShouldNotBeNull();
		using MemoryStream resultCopy = new();
		result.CopyTo(resultCopy);
		resultCopy.ToArray().ShouldBe(originalBytes);
	}

#if CORE_NATIVE_BUILD
	[Theory]
	[InlineData(CompressionLevel.NoCompression)]
	[InlineData(CompressionLevel.Fastest)]
	[InlineData(CompressionLevel.Optimal)]
	[InlineData(CompressionLevel.SmallestSize)]
	public void MailAttachmentBytes_NewtonsoftJsonRoundTrip_ShouldNotCorruptAttachment(CompressionLevel compressionLevel)
	{
		// Arrange - mirrors how Hangfire serializes/deserializes job arguments (Newtonsoft.Json with TypeNameHandling for the IMailAttachment interface array)
		byte[] originalBytes = System.Text.Encoding.UTF8.GetBytes("This simulates the binary content of an Excel attachment.");
		IMailAttachment[] attachments = new IMailAttachment[] { new MailAttachmentBytes("Report.xlsx", originalBytes, compressionLevel) };

		Newtonsoft.Json.JsonSerializerSettings settings = new() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto };

		// Act
		string json = Newtonsoft.Json.JsonConvert.SerializeObject(attachments, settings);
		IMailAttachment[]? deserializedAttachments = Newtonsoft.Json.JsonConvert.DeserializeObject<IMailAttachment[]>(json, settings);

		// Assert
		deserializedAttachments.ShouldNotBeNull();
		deserializedAttachments.Length.ShouldBe(1);
		Stream? result = deserializedAttachments[0].GetStream();
		result.ShouldNotBeNull();
		using MemoryStream resultCopy = new();
		result.CopyTo(resultCopy);
		resultCopy.ToArray().ShouldBe(originalBytes);
	}
#endif

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

	#region Compression Level Coverage

	[Theory]
	[InlineData(CompressionLevel.NoCompression)]
	[InlineData(CompressionLevel.Fastest)]
	[InlineData(CompressionLevel.Optimal)]
	[InlineData(CompressionLevel.SmallestSize)]
	public async Task AddAttachments_WithoutZip_ShouldPreserveOriginalContent_ForAllCompressionLevels(CompressionLevel compressionLevel)
	{
		// Arrange
		byte[] originalBytes1 = new byte[] { 1, 2, 3, 4, 5 };
		byte[] originalBytes2 = new byte[] { 6, 7, 8, 9, 10 };
		BodyBuilder bodyBuilder = new();
		IMailAttachment[] attachments = new IMailAttachment[]
		{
			new MailAttachment("test1.txt", originalBytes1, compressionLevel),
			new MailAttachmentBytes("test2.txt", originalBytes2, compressionLevel)
		};

		// Act
		await AddAttachments(attachments, bodyBuilder, false, Current.CancellationToken);

		// Assert
		bodyBuilder.Attachments.Count.ShouldBe(2);

		MimePart? part1 = bodyBuilder.Attachments[0] as MimePart;
		part1.ShouldNotBeNull();
		part1.Content.ShouldNotBeNull();
		await using MemoryStream content1 = new();
		await part1.Content!.DecodeToAsync(content1, Current.CancellationToken);
		content1.ToArray().ShouldBe(originalBytes1);

		MimePart? part2 = bodyBuilder.Attachments[1] as MimePart;
		part2.ShouldNotBeNull();
		part2.Content.ShouldNotBeNull();
		await using MemoryStream content2 = new();
		await part2.Content!.DecodeToAsync(content2, Current.CancellationToken);
		content2.ToArray().ShouldBe(originalBytes2);
	}

	[Theory]
	[InlineData(CompressionLevel.NoCompression)]
	[InlineData(CompressionLevel.Fastest)]
	[InlineData(CompressionLevel.Optimal)]
	[InlineData(CompressionLevel.SmallestSize)]
	public async Task AddAttachments_WithZip_ShouldPreserveOriginalContent_ForAllCompressionLevels(CompressionLevel compressionLevel)
	{
		// Arrange
		byte[] originalBytes1 = new byte[] { 1, 2, 3, 4, 5 };
		byte[] originalBytes2 = new byte[] { 6, 7, 8, 9, 10 };
		BodyBuilder bodyBuilder = new();
		IMailAttachment[] attachments = new IMailAttachment[]
		{
			new MailAttachment("test1.txt", originalBytes1, compressionLevel),
			new MailAttachmentBytes("test2.txt", originalBytes2, compressionLevel)
		};

		// Act
		await AddAttachments(attachments, bodyBuilder, true, Current.CancellationToken);

		// Assert
		bodyBuilder.Attachments.Count.ShouldBe(1);
		MimePart? zipPart = bodyBuilder.Attachments[0] as MimePart;
		zipPart.ShouldNotBeNull();
		zipPart.FileName.ShouldBe("Files.zip");
		zipPart.Content.ShouldNotBeNull();

		await using MemoryStream zipContent = new();
		await zipPart.Content!.DecodeToAsync(zipContent, Current.CancellationToken);
		zipContent.Position = 0;

		using ZipArchive archive = new(zipContent, ZipArchiveMode.Read);

		using MemoryStream entry1Content = new();
		await using (Stream entry1Stream = archive.GetEntry("test1.txt")!.Open())
		{
			await entry1Stream.CopyToAsync(entry1Content, Current.CancellationToken);
		}
		entry1Content.ToArray().ShouldBe(originalBytes1);

		using MemoryStream entry2Content = new();
		await using (Stream entry2Stream = archive.GetEntry("test2.txt")!.Open())
		{
			await entry2Stream.CopyToAsync(entry2Content, Current.CancellationToken);
		}
		entry2Content.ToArray().ShouldBe(originalBytes2);
	}

	#endregion
}
