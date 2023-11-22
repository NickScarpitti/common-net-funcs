namespace Common_Net_Funcs.Communications;

/// <summary>
/// Interface for use with dependency injection
/// </summary>
public interface IEmailService
{
    Task<bool> SendEmail(string? smtpServer, int smtpPort, MailAddress fromAddress, IEnumerable<MailAddress> toAddresses, string? subject, string? body, bool bodyIsHtml = false,
        IEnumerable<MailAddress>? ccAddresses = null, IEnumerable<MailAddress>? bccAddresses = null, IEnumerable<MailAttachment>? attachments = null, bool readReceipt = false, string? readReceiptEmail = null,
        string? smtpUser = null, string? smtpPassword = null, bool zipAttachments = false);
    Task<bool> SendEmail(string? smtpServer, int smtpPort, MailAddress fromAddress, MailAddress toAddress, string? subject, string? body, bool bodyIsHtml = false,
        IEnumerable<MailAddress>? ccAddresses = null, IEnumerable<MailAddress>? bccAddresses = null, IEnumerable<MailAttachment>? attachments = null, bool readReceipt = false, string? readReceiptEmail = null,
        string? smtpUser = null, string? smtpPassword = null, bool zipAttachments = false);
    Task<bool> SendEmail(string? smtpServer, int smtpPort, string fromAddress, string toAddress, string? subject, string? body, bool bodyIsHtml = false,
        IEnumerable<MailAddress>? ccAddresses = null, IEnumerable<MailAddress>? bccAddresses = null, IEnumerable<MailAttachment>? attachments = null, bool readReceipt = false, string? readReceiptEmail = null,
        string? smtpUser = null, string? smtpPassword = null, bool zipAttachments = false);
}

/// <summary>
/// Implementation of IEmailService that can be used with dependency injection in order to speed up sending multiple emails
/// </summary>
public class EmailService : IEmailService
{
    public Task<bool> SendEmail(string? smtpServer, int smtpPort, MailAddress fromAddress, IEnumerable<MailAddress> toAddresses, string? subject, string? body, bool bodyIsHtml = false,
        IEnumerable<MailAddress>? ccAddresses = null, IEnumerable<MailAddress>? bccAddresses = null, IEnumerable<MailAttachment>? attachments = null, bool readReceipt = false, string? readReceiptEmail = null,
        string? smtpUser = null, string? smtpPassword = null, bool zipAttachments = false)
    {
        return Email.SendEmail(smtpServer, smtpPort, fromAddress, toAddresses, subject, body, bodyIsHtml, ccAddresses, bccAddresses, attachments, readReceipt, readReceiptEmail, smtpUser, smtpPassword, zipAttachments);
    }

    public Task<bool> SendEmail(string? smtpServer, int smtpPort, MailAddress fromAddress, MailAddress toAddress, string? subject, string? body, bool bodyIsHtml = false, IEnumerable<MailAddress>? ccAddresses = null,
        IEnumerable<MailAddress>? bccAddresses = null, IEnumerable<MailAttachment>? attachments = null, bool readReceipt = false, string? readReceiptEmail = null, string? smtpUser = null,
        string? smtpPassword = null, bool zipAttachments = false)
    {
        IEnumerable<MailAddress> toAddresses = new List<MailAddress>() { new() { Name = toAddress.Name, Email = toAddress.Email } };
        return Email.SendEmail(smtpServer, smtpPort, fromAddress, toAddresses, subject, body, bodyIsHtml, ccAddresses, bccAddresses, attachments, readReceipt, readReceiptEmail, smtpUser, smtpPassword, zipAttachments);
    }

    public Task<bool> SendEmail(string? smtpServer, int smtpPort, string fromAddress, string toAddress, string? subject, string? body, bool bodyIsHtml = false, IEnumerable<MailAddress>? ccAddresses = null,
        IEnumerable<MailAddress>? bccAddresses = null, IEnumerable<MailAttachment>? attachments = null, bool readReceipt = false, string? readReceiptEmail = null, string? smtpUser = null, string? smtpPassword = null, bool zipAttachments = false)
    {
        IEnumerable<MailAddress> toAddresses = new List<MailAddress>() { new() { Name = toAddress, Email = toAddress } };
        MailAddress fromMailAddress = new() { Name = fromAddress, Email = fromAddress };
        return Email.SendEmail(smtpServer, smtpPort, fromMailAddress, toAddresses, subject, body, bodyIsHtml, ccAddresses, bccAddresses, attachments, readReceipt, readReceiptEmail, smtpUser, smtpPassword, zipAttachments);
    }
}
