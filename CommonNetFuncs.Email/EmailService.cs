﻿namespace CommonNetFuncs.Email;

//TODO:: Consolidate settings into class to simplify signature
/// <summary>
/// Interface for use with dependency injection
/// </summary>
public interface IEmailService
{
    Task<bool> SendEmail(string? smtpServer, int smtpPort, MailAddress fromAddress, IEnumerable<MailAddress> toAddresses, string? subject, string? body, bool bodyIsHtml = false,
        IEnumerable<MailAddress>? ccAddresses = null, IEnumerable<MailAddress>? bccAddresses = null, IEnumerable<MailAttachment>? attachments = null, bool readReceipt = false, string? readReceiptEmail = null,
        string? smtpUser = null, string? smtpPassword = null, bool zipAttachments = false, bool autoDisposeAttachments = true, CancellationToken cancellationToken = default);

    Task<bool> SendEmail(string? smtpServer, int smtpPort, MailAddress fromAddress, MailAddress toAddress, string? subject, string? body, bool bodyIsHtml = false,
        IEnumerable<MailAddress>? ccAddresses = null, IEnumerable<MailAddress>? bccAddresses = null, IEnumerable<MailAttachment>? attachments = null, bool readReceipt = false, string? readReceiptEmail = null,
        string? smtpUser = null, string? smtpPassword = null, bool zipAttachments = false, bool autoDisposeAttachments = true, CancellationToken cancellationToken = default);

    Task<bool> SendEmail(string? smtpServer, int smtpPort, string fromAddress, string toAddress, string? subject, string? body, bool bodyIsHtml = false,
        IEnumerable<MailAddress>? ccAddresses = null, IEnumerable<MailAddress>? bccAddresses = null, IEnumerable<MailAttachment>? attachments = null, bool readReceipt = false, string? readReceiptEmail = null,
        string? smtpUser = null, string? smtpPassword = null, bool zipAttachments = false, bool autoDisposeAttachments = true, CancellationToken cancellationToken = default);

    Task<bool> SendEmail(string? smtpServer, int smtpPort, string fromAddress, IEnumerable<string> toAddress, string? subject, string? body, bool bodyIsHtml = false,
        IEnumerable<MailAddress>? ccAddresses = null, IEnumerable<MailAddress>? bccAddresses = null, IEnumerable<MailAttachment>? attachments = null, bool readReceipt = false, string? readReceiptEmail = null,
        string? smtpUser = null, string? smtpPassword = null, bool zipAttachments = false, bool autoDisposeAttachments = true, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of IEmailService that can be used with dependency injection in order to speed up sending multiple
/// emails
/// </summary>
public sealed class EmailService : IEmailService
{
    public Task<bool> SendEmail(string? smtpServer, int smtpPort, MailAddress fromAddress, IEnumerable<MailAddress> toAddresses, string? subject, string? body, bool bodyIsHtml = false,
        IEnumerable<MailAddress>? ccAddresses = null, IEnumerable<MailAddress>? bccAddresses = null, IEnumerable<MailAttachment>? attachments = null, bool readReceipt = false, string? readReceiptEmail = null,
        string? smtpUser = null, string? smtpPassword = null, bool zipAttachments = false, bool autoDisposeAttachments = true, CancellationToken cancellationToken = default)
    {
        return Email.SendEmail(smtpServer, smtpPort, fromAddress, toAddresses, subject, body, bodyIsHtml, ccAddresses, bccAddresses, attachments, readReceipt, readReceiptEmail, smtpUser, smtpPassword, zipAttachments, autoDisposeAttachments, cancellationToken);
    }

    public Task<bool> SendEmail(string? smtpServer, int smtpPort, MailAddress fromAddress, MailAddress toAddress, string? subject, string? body, bool bodyIsHtml = false, IEnumerable<MailAddress>? ccAddresses = null,
        IEnumerable<MailAddress>? bccAddresses = null, IEnumerable<MailAttachment>? attachments = null, bool readReceipt = false, string? readReceiptEmail = null, string? smtpUser = null,
        string? smtpPassword = null, bool zipAttachments = false, bool autoDisposeAttachments = true, CancellationToken cancellationToken = default)
    {
        IEnumerable<MailAddress> toAddresses = [new MailAddress(toAddress.Name, toAddress.Email)];
        return Email.SendEmail(smtpServer, smtpPort, fromAddress, toAddresses, subject, body, bodyIsHtml, ccAddresses, bccAddresses, attachments, readReceipt, readReceiptEmail, smtpUser, smtpPassword, zipAttachments, autoDisposeAttachments, cancellationToken);
    }

    public Task<bool> SendEmail(string? smtpServer, int smtpPort, string fromAddress, string toAddress, string? subject, string? body, bool bodyIsHtml = false, IEnumerable<MailAddress>? ccAddresses = null,
        IEnumerable<MailAddress>? bccAddresses = null, IEnumerable<MailAttachment>? attachments = null, bool readReceipt = false, string? readReceiptEmail = null, string? smtpUser = null,
        string? smtpPassword = null, bool zipAttachments = false, bool autoDisposeAttachments = true, CancellationToken cancellationToken = default)
    {
        IEnumerable<MailAddress> toAddresses = [new MailAddress(toAddress, toAddress)];
        MailAddress fromMailAddress = new(fromAddress, fromAddress);
        return Email.SendEmail(smtpServer, smtpPort, fromMailAddress, toAddresses, subject, body, bodyIsHtml, ccAddresses, bccAddresses, attachments, readReceipt, readReceiptEmail, smtpUser, smtpPassword, zipAttachments, autoDisposeAttachments, cancellationToken);
    }

    public Task<bool> SendEmail(string? smtpServer, int smtpPort, string fromAddress, IEnumerable<string> toAddress, string? subject, string? body, bool bodyIsHtml = false, IEnumerable<MailAddress>? ccAddresses = null,
        IEnumerable<MailAddress>? bccAddresses = null, IEnumerable<MailAttachment>? attachments = null, bool readReceipt = false, string? readReceiptEmail = null, string? smtpUser = null,
        string? smtpPassword = null, bool zipAttachments = false, bool autoDisposeAttachments = true, CancellationToken cancellationToken = default)
    {
        IEnumerable<MailAddress> toAddresses = toAddress.Select(x => new MailAddress(x, x));
        MailAddress fromMailAddress = new(fromAddress, fromAddress);
        return Email.SendEmail(smtpServer, smtpPort, fromMailAddress, toAddresses, subject, body, bodyIsHtml, ccAddresses, bccAddresses, attachments, readReceipt, readReceiptEmail, smtpUser, smtpPassword, zipAttachments, autoDisposeAttachments, cancellationToken);
    }
}
