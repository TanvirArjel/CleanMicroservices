using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using CleanHr.AuthApi.Application.Infrastructures;
using CleanHr.AuthApi.Common.Metrics;
using CleanHr.AuthApi.Common.Telemetry;
using CleanHr.AuthApi.Infrastructure.Services.Configs;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace CleanHr.AuthApi.Infrastructure.Services;

public sealed class EmailSender : IEmailSender
{
    private readonly SendGridConfig _sendGridConfig;
    private readonly IApplicationMetrics _applicationMetrics;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(
        SendGridConfig sendGridConfig,
        IApplicationMetrics applicationMetrics,
        ILogger<EmailSender> logger)
    {
        _sendGridConfig = sendGridConfig ?? throw new ArgumentNullException(nameof(sendGridConfig));
        _applicationMetrics = applicationMetrics ?? throw new ArgumentNullException(nameof(applicationMetrics));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private SendGridClient SendGridClient => new(_sendGridConfig.ApiKey);

    public async Task SendAsync(EmailMessage emailMessage)
    {
        const string operationName = "SendEmail";
        using var activity = Tracing.Source.StartActivity(operationName, ActivityKind.Producer);
        activity?.SetTag("email.recipient", emailMessage?.ReceiverEmail);
        using var operationScope = _applicationMetrics.TrackOperation(operationName);
        var loggerContext = new Dictionary<string, object>
        {
            { "ReceiverEmail", emailMessage?.ReceiverEmail },
            { "ReceiverName", emailMessage?.ReceiverName },
            { "SenderEmail", emailMessage?.SenderEmail },
            { "SenderName", emailMessage?.SenderName },
            { "Subject", emailMessage?.Subject }
        };

        using var loggerScope = _logger.BeginScope(loggerContext);

        try
        {
            _logger.LogInformation("Received request to send email");
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

            if (!response.IsSuccessStatusCode)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Email provider rejected the message");
                _applicationMetrics.RecordFailureOperation(operationName, $"email_provider_http_{(int)response.StatusCode}");
                _logger.LogError("Email provider rejected the message with status code {StatusCode}", response.StatusCode);
                return;
            }

            activity?.SetStatus(ActivityStatusCode.Ok, "Email sent successfully");
            _applicationMetrics.RecordSuccessOperation(operationName);
            _logger.LogInformation("Email sent successfully");
        }
        catch (Exception exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            _applicationMetrics.RecordFailureOperation(operationName, $"email_provider_{exception.GetType().Name}");
            _logger.LogError(exception, "Exception occurred while sending email");
        }
    }
}
