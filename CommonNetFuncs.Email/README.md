# CommonNetFuncs.Email

[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.Email)](https://www.nuget.org/packages/CommonNetFuncs.Email/)

This lightweight project contains helper methods for several common functions required by applications.

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
