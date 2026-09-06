using System.Globalization;
using System.Security.Cryptography;
using System.Diagnostics;
using CleanHr.AuthApi.Application.Infrastructures;
using CleanHr.AuthApi.Application.Metrics;
using CleanHr.AuthApi.Application.Services;
using CleanHr.AuthApi.Common.Telemetry;
using CleanHr.AuthApi.Domain;
using CleanHr.AuthApi.Domain.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using TanvirArjel.ArgumentChecker;
using TanvirArjel.EFCore.GenericRepository;

namespace CleanHr.AuthApi.Application.Commands;

public sealed class SendPasswordResetCodeCommand(string email) : IRequest<Result>
{
    public string Email { get; } = email.ThrowIfNotValidEmail(nameof(email));

    private class SendPasswordResetCodeCommandHandler : IRequestHandler<SendPasswordResetCodeCommand, Result>
    {
        private readonly IRepository _repository;
        private readonly ViewRenderService _viewRenderService;
        private readonly IEmailSender _emailSender;
        private readonly IApplicationMetrics _applicationMetrics;
        private readonly ILogger<SendPasswordResetCodeCommandHandler> _logger;

        public SendPasswordResetCodeCommandHandler(
                IRepository repository,
                ViewRenderService viewRenderService,
                IEmailSender emailSender,
                IApplicationMetrics applicationMetrics,
                ILogger<SendPasswordResetCodeCommandHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _viewRenderService = viewRenderService ?? throw new ArgumentNullException(nameof(viewRenderService));
            _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
            _applicationMetrics = applicationMetrics ?? throw new ArgumentNullException(nameof(applicationMetrics));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result> Handle(SendPasswordResetCodeCommand request, CancellationToken cancellationToken)
        {
            string operationName = "SendPasswordResetCode";
            using var activity = Tracing.Source.StartActivity(operationName, ActivityKind.Internal);
            activity?.SetTag("password_reset.identifier", request?.Email);

            using var _ = _applicationMetrics.TrackOperation(operationName);
            using var loggerScope = _logger.BeginScope(new Dictionary<string, object> { { "Email", request?.Email } });

            try
            {
                _logger.LogInformation("Received request to send password reset code");
                request.ThrowIfNull(nameof(request));

                ApplicationUser applicationUser = await _repository.GetAsync<ApplicationUser>(u => u.Email == request.Email, cancellationToken);

                if (applicationUser == null)
                {
                    _applicationMetrics.RecordFailureOperation(operationName, "user_not_found");
                    _logger.LogWarning("Password reset code was not sent: User not found");
                    return Result.Failure("The user does not exist with the provided email.");
                }

                int randomNumber = RandomNumberGenerator.GetInt32(0, 1000000);
                string verificationCode = randomNumber.ToString("D6", CultureInfo.InvariantCulture);

                Result<PasswordResetCode> result = await PasswordResetCode.CreateAsync(applicationUser.Id, request.Email, verificationCode);

                if (result.IsSuccess == false)
                {
                    _applicationMetrics.RecordFailureOperation(operationName, "code_creation");
                    _logger.LogWarning("Password reset code creation failed");
                    return Result.Failure(result.Errors);
                }

                PasswordResetCode passwordResetCode = result.Value;

                await _repository.AddAsync(passwordResetCode, cancellationToken);
                await _repository.SaveChangesAsync(cancellationToken);

                (string Email, string VerificationCode) model = (request.Email, verificationCode);
                string subject = "Reset Password";
                string senderEmail = "noreply@yourapp.com";
                string emailBody = await _viewRenderService.RenderViewToStringAsync("EmailTemplates/PasswordResetCodeTemplate", model);
                EmailMessage emailObject = new(request.Email, request.Email, senderEmail, senderEmail, subject, emailBody);
                await _emailSender.SendAsync(emailObject);

                activity?.SetStatus(ActivityStatusCode.Ok, "Password reset code sent");
                _applicationMetrics.RecordSuccessOperation(operationName);
                _logger.LogInformation("Password reset code sent successfully");
                return Result.Success();
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _applicationMetrics.RecordFailureOperation(operationName, ex.GetType().Name);
                _logger.LogError(ex, "Unexpected error while sending password reset code");
                throw;
            }
        }
    }
}
