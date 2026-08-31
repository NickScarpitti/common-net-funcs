# CommonNetFuncs.Email

[![License](https://img.shields.io/github/license/NickScarpitti/common-net-funcs.svg)](http://opensource.org/licenses/MIT)
[![NuGet Version](https://img.shields.io/nuget/v/CommonNetFuncs.Email)](https://www.nuget.org/packages/CommonNetFuncs.Email/)
[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.Email)](https://www.nuget.org/packages/CommonNetFuncs.Email/)

This contains helper methods for sending emails in .NET applications. It includes a simple interface for sending emails as well as an implementation that can be used directly or consumed as a service.

## Contents

- [CommonNetFuncs.Email](#commonnetfuncsemail)
  - [Contents](#contents)
  - [Email](#email)
    - [Email Usage Examples](#email-usage-examples)
      - [SendEmail](#sendemail)
  - [Installation](#installation)
  - [License](#license)

---

## Email

Helper class for sending emails

### Email Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### SendEmail

Sends an email with the specified parameters. Can be consumed as a service using the IEmailService interface and EmailService implementation of that service.

```cs
SmtpSettings smtpSettings = new("smtp.server.address", 25);

EmailAddresses emailAddresses = new(
    fromAddress: new MailAddress("Nick", "NickEmail@test.com"),
    toAddresses: [new MailAddress("Chris", "ChrisEmail@test.com")]);

await using FileStream attachmentStream = new(@"C:\Documents\Important Attachment.txt", FileMode.Open, FileAccess.Read);

// Attachment data is compressed in memory (Gzip, CompressionLevel.Optimal by default) and transparently decompressed when the email is sent
MailAttachment attachment = new("Important Attachment.txt", attachmentStream);

EmailContent emailContent = new(
    subject: "Subject Line",
    body: "Mail Body",
    attachments: [attachment],
    zipAttachments: true); // Sends email with zipped attachment

bool success = await SendEmail(new SendEmailConfig(smtpSettings, emailAddresses, emailContent));
```

`MailAttachment` and `MailAttachmentBytes` both accept an optional `CompressionLevel` parameter to control how the attachment is compressed while held in memory (defaults to `CompressionLevel.Optimal`). `MailAttachmentBytes` stores the attachment as a `byte[]` instead of a `Stream`, making it serialization-friendly for scenarios such as Hangfire background jobs - use it along with `EmailContentBytes`/`SendEmailConfigBytes` in those cases.

</details>

## Installation

Install via NuGet:

```bash
dotnet add package CommonNetFuncs.Email
```

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/NickScarpitti/common-net-funcs/blob/main/LICENSE) file for details.
