using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CleanHr.AuthApi.Application.Extensions;
using CleanHr.AuthApi.Application.Infrastructures;
using CleanHr.AuthApi.Infrastructure.Services.Configs;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace CleanHr.AuthApi.Infrastructure.Services;

public sealed class EmailSender : IEmailSender
{
    private readonly SendGridConfig _sendGridConfig;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(SendGridConfig sendGridConfig, ILogger<EmailSender> logger)
    {
        _sendGridConfig = sendGridConfig ?? throw new ArgumentNullException(nameof(sendGridConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private SendGridClient SendGridClient => new(_sendGridConfig.ApiKey);

    public async Task SendAsync(EmailMessage emailMessage)
    {
        using var _loggerScope = _logger.BeginScope(new Dictionary<string, object>
        {
            { "ReceiverEmail", emailMessage?.ReceiverEmail },
            { "ReceiverName", emailMessage?.ReceiverName },
            { "Subject", emailMessage?.Subject }
        });

        try
        {
            ArgumentNullException.ThrowIfNull(emailMessage);

            SendGridMessage message = new()
            {
                Subject = emailMessage.Subject,
                HtmlContent = emailMessage.MailBody,
            };

            message.AddTo(new EmailAddress(emailMessage.ReceiverEmail, emailMessage.ReceiverName));

            if (!string.IsNullOrWhiteSpace(emailMessage.SenderEmail))
            {
                message.From = new EmailAddress(emailMessage.SenderEmail, emailMessage.SenderName);
                message.ReplyTo = new EmailAddress(emailMessage.SenderEmail, emailMessage.SenderName);
            }

            Response response = await SendGridClient.SendEmailAsync(message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error sending email to {ReceiverEmail}", emailMessage?.ReceiverEmail);
        }
    }
}
