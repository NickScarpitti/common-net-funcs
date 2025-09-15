# CommonNetFuncs.Email

[![License](https://img.shields.io/github/license/NickScarpitti/common-net-funcs.svg)](http://opensource.org/licenses/MIT)
[![Build](https://github.com/NickScarpitti/common-net-funcs/actions/workflows/dotnet.yml/badge.svg)](https://github.com/NickScarpitti/common-net-funcs/actions/workflows/dotnet.yml)
[![NuGet Version](https://img.shields.io/nuget/v/CommonNetFuncs.Email)](https://www.nuget.org/packages/CommonNetFuncs.Email/)
[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.Email)](https://www.nuget.org/packages/CommonNetFuncs.Email/)

This  contains helper methods for sending emails in .NET applications. It includes a simple interface for sending emails as well as an implementation that can be used directly or consumed as a service.

## Contents

- [CommonNetFuncs.Email](#commonnetfuncsemail)
  - [Contents](#contents)
  - [Email](#email)
    - [Email Usage Examples](#email-usage-examples)
      - [SendEmail](#sendemail)

---

## Email

Helper class for sending emails

### Email Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### SendEmail

Sends an email with the specified parameters. Can be consumed as a service using the IEmailService interface and EmailService implementation of that service.

```cs
MailAddress fromAddress = new()
{
  Name = "Nick",
  Email = "NickEmail@test.com"
};

MailAddress toAddress = new()
{
  Name = "Chris",
  Email = "ChrisEmail@test.com"
};

MailAttachment attachment = new()
{
  AttachmentName = "Important Attachment.txt",
  AttachmentStream = new FileStream(@"C:\Documents\Important Attachment.txt")
}

Email.SendEmail("smtp.server.address", 25, fromAddress, toAddress, "Subject Line", "Mail Body", attachments: [attachment], zipAttachments: true); // Sends email with zipped attachment
```

</details>
