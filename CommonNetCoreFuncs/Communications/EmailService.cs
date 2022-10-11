namespace CommonNetCoreFuncs.Communications;

/// <summary>
/// Interface for use with dependency injection
/// </summary>
public interface IEmailService
{
    Task<bool> SendEmail(string? smtpServer, int smtpPort, MailAddress fromAddress, IEnumerable<MailAddress> toAddresses, string? subject, string? body, bool bodyIsHtml = false, 
        IEnumerable<MailAddress>? ccAddresses = null, IEnumerable<MailAddress>? bccAddresses = null, string? attachmentName = null, Stream? fileData = null, bool readReceipt = false, string? readReceiptEmail = null);
    Task<bool> SendEmail(string? smtpServer, int smtpPort, MailAddress fromAddress, MailAddress toAddress, string? subject, string? body, bool bodyIsHtml = false, 
        IEnumerable<MailAddress>? ccAddresses = null, IEnumerable<MailAddress>? bccAddresses = null, string? attachmentName = null, Stream? fileData = null, bool readReceipt = false, string? readReceiptEmail = null);
    Task<bool> SendEmail(string? smtpServer, int smtpPort, string fromAddress, string toAddress, string? subject, string? body, bool bodyIsHtml = false, 
        IEnumerable<MailAddress>? ccAddresses = null, IEnumerable<MailAddress>? bccAddresses = null, string? attachmentName = null, Stream? fileData = null, bool readReceipt = false, string? readReceiptEmail = null);
}

/// <summary>
/// Implementation of IEmailService that can be used with dependency injection in order to speed up sending multiple emails
/// </summary>
public class EmailService : IEmailService
{
    public async Task<bool> SendEmail(string? smtpServer, int smtpPort, MailAddress fromAddress, IEnumerable<MailAddress> toAddresses, string? subject, string? body, bool bodyIsHtml = false, IEnumerable<MailAddress>? ccAddresses = null, IEnumerable<MailAddress>? bccAddresses = null, string? attachmentName = null, Stream? fileData = null, bool readReceipt = false, string? readReceiptEmail = null)
    {
        return await Email.SendEmail(smtpServer, smtpPort, fromAddress, toAddresses, subject, body, bodyIsHtml, ccAddresses, bccAddresses, attachmentName, fileData, readReceipt, readReceiptEmail, smtpUser, smtpPassword);
    }

    public async Task<bool> SendEmail(string? smtpServer, int smtpPort, MailAddress fromAddress, MailAddress toAddress, string? subject, string? body, bool bodyIsHtml = false, IEnumerable<MailAddress>? ccAddresses = null, IEnumerable<MailAddress>? bccAddresses = null, string? attachmentName = null, Stream? fileData = null, bool readReceipt = false, string? readReceiptEmail = null)
    {
        IEnumerable<MailAddress> toAddresses = new List<MailAddress>() { new MailAddress { Name = toAddress.Name, Email = toAddress.Email } };
        return await Email.SendEmail(smtpServer, smtpPort, fromAddress, toAddresses, subject, body, bodyIsHtml, ccAddresses, bccAddresses, attachmentName, fileData);
    }

    public async Task<bool> SendEmail(string? smtpServer, int smtpPort, string fromAddress, string toAddress, string? subject, string? body, bool bodyIsHtml = false, IEnumerable<MailAddress>? ccAddresses = null, IEnumerable<MailAddress>? bccAddresses = null, string? attachmentName = null, Stream? fileData = null, bool readReceipt = false, string? readReceiptEmail = null)
    {
        IEnumerable<MailAddress> toAddresses = new List<MailAddress>() { new MailAddress { Name = toAddress, Email = toAddress } };
        MailAddress fromMailAddress = new() { Name = fromAddress, Email = fromAddress };
        return await Email.SendEmail(smtpServer, smtpPort, fromMailAddress, toAddresses, subject, body, bodyIsHtml, ccAddresses, bccAddresses, attachmentName, fileData, readReceipt, readReceiptEmail, smtpUser, smtpPassword);
    }
}
