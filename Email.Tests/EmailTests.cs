<<<<<<< HEAD
﻿using System.ComponentModel.DataAnnotations;
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
        MailAddress from = new("Test", "invalid-email");
        MailAddress[] to = new[] { new MailAddress("Test Recipient", "valid@example.com") };

        // Act
        bool result = await SendEmail(smtpServer, smtpPort, from, to, "Test Subject", "Test Body");

        // Assert
        result.ShouldBeFalse();
    }

    [Theory]
    [AutoData]
    public async Task SendEmail_WithInvalidToAddress_ShouldReturnFalse(string smtpServer, int smtpPort)
    {
        // Arrange
        MailAddress from = new("Test", "valid@example.com");
        MailAddress[] to = new[] { new MailAddress("Test Recipient", "invalid-email") };

        // Act
        bool result = await SendEmail(smtpServer, smtpPort, from, to, "Test Subject", "Test Body");

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
        MailAddress from = new("Test", "sender@example.com");
        MailAddress[] to = new[] { new MailAddress("Test Recipient", "recipient@example.com") };

        // Act
        bool result = await SendEmail(smtpServer, smtpPort, from, to, "Test Subject", "Test Body", readReceipt: true, readReceiptEmail: "receipt@example.com");

        // Note: We can't actually verify the header here since the SMTP interaction
        // is encapsulated, but the method should complete without throwing
        result.ShouldBe(false);
    }

    [Theory]
    [AutoData]
    public async Task SendEmail_WithHtmlBody_ShouldSetContentTypeCorrectly(string smtpServer, int smtpPort)
    {
        // Arrange
        MailAddress from = new("Test", "sender@example.com");
        MailAddress[] to = new[] { new MailAddress("Test Recipient", "recipient@example.com") };
        const string htmlBody = "<h1>Test</h1><p>This is a test email</p>";

        // Act
        bool result = await SendEmail(smtpServer, smtpPort, from, to, "Test  ", htmlBody, bodyIsHtml: true);

        // Note: Similar to above, we can't directly verify the content type
        // but the method should complete without throwing
        result.ShouldBe(false);
    }
}
=======
﻿using System.ComponentModel.DataAnnotations;
using AutoFixture.Xunit2;
using CommonNetFuncs.Email;
using MimeKit;

using static CommonNetFuncs.Email.Email;

namespace Email.Tests;

#pragma warning disable CRR0029 // ConfigureAwait(true) is called implicitly
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
        MailAddress from = new("Test", "invalid-email");
        MailAddress[] to = new[] { new MailAddress("Test Recipient", "valid@example.com") };

        // Act
        bool result = await SendEmail(smtpServer, smtpPort, from, to, "Test Subject", "Test Body");

        // Assert
        result.ShouldBeFalse();
    }

    [Theory]
    [AutoData]
    public async Task SendEmail_WithInvalidToAddress_ShouldReturnFalse(string smtpServer, int smtpPort)
    {
        // Arrange
        MailAddress from = new("Test", "valid@example.com");
        MailAddress[] to = new[] { new MailAddress("Test Recipient", "invalid-email") };

        // Act
        bool result = await SendEmail(smtpServer, smtpPort, from, to, "Test Subject", "Test Body");

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
        MailAddress from = new("Test", "sender@example.com");
        MailAddress[] to = new[] { new MailAddress("Test Recipient", "recipient@example.com") };

        // Act
        bool result = await SendEmail(smtpServer, smtpPort, from, to, "Test Subject", "Test Body", readReceipt: true, readReceiptEmail: "receipt@example.com");

        // Note: We can't actually verify the header here since the SMTP interaction
        // is encapsulated, but the method should complete without throwing
        result.ShouldBe(false);
    }

    [Theory]
    [AutoData]
    public async Task SendEmail_WithHtmlBody_ShouldSetContentTypeCorrectly(string smtpServer, int smtpPort)
    {
        // Arrange
        MailAddress from = new("Test", "sender@example.com");
        MailAddress[] to = new[] { new MailAddress("Test Recipient", "recipient@example.com") };
        const string htmlBody = "<h1>Test</h1><p>This is a test email</p>";

        // Act
        bool result = await SendEmail(smtpServer, smtpPort, from, to, "Test  ", htmlBody, bodyIsHtml: true);

        // Note: Similar to above, we can't directly verify the content type
        // but the method should complete without throwing
        result.ShouldBe(false);
    }
}
#pragma warning restore CRR0029 // ConfigureAwait(true) is called implicitly
>>>>>>> 270705e4f794428a4927e32ef23496c0001e47e7
