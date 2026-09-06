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
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TanvirArjel.ArgumentChecker;
using TanvirArjel.EFCore.GenericRepository;

namespace CleanHr.AuthApi.Application.Commands;

public sealed class SendEmailVerificationCodeCommand(string email) : IRequest<Result>
{
    public string Email { get; } = email.ThrowIfNotValidEmail(nameof(email));

    private class SendEmailVerificationCodeCommandHandler : IRequestHandler<SendEmailVerificationCodeCommand, Result>
    {
        private readonly IRepository _repository;
        private readonly ViewRenderService _viewRenderService;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IApplicationMetrics _applicationMetrics;
        private readonly ILogger<SendEmailVerificationCodeCommandHandler> _logger;

        public SendEmailVerificationCodeCommandHandler(
            IRepository repository,
            ViewRenderService viewRenderService,
            IEmailSender emailSender,
            UserManager<ApplicationUser> userManager,
            IApplicationMetrics applicationMetrics,
            ILogger<SendEmailVerificationCodeCommandHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _viewRenderService = viewRenderService ?? throw new ArgumentNullException(nameof(viewRenderService));
            _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _applicationMetrics = applicationMetrics ?? throw new ArgumentNullException(nameof(applicationMetrics));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result> Handle(SendEmailVerificationCodeCommand request, CancellationToken cancellationToken)
        {
            string operationName = "SendEmailVerificationCode";
            using var activity = Tracing.Source.StartActivity("SendEmailVerificationCode", ActivityKind.Internal);
            activity?.SetTag("email_verification.identifier", request?.Email);

            using var _ = _applicationMetrics.TrackOperation(operationName);
            using var loggerScope = _logger.BeginScope(new Dictionary<string, object> { { "Email", request?.Email } });

            try
            {
                _logger.LogInformation("Received request to send email verification code");
                request.ThrowIfNull(nameof(request));

                ApplicationUser applicationUser = await _userManager.FindByEmailAsync(request.Email);

                if (applicationUser == null)
                {
                    _applicationMetrics.RecordFailureOperation(operationName, "user_not_found");
                    _logger.LogWarning("Email verification code was not sent: User not found");
                    return Result.Failure("Email", "The provided email is not related to any account.");
                }

                if (applicationUser.EmailConfirmed)
                {
                    _applicationMetrics.RecordFailureOperation(operationName, "email_already_confirmed");
                    _logger.LogInformation("Email verification code was not sent: Email already confirmed");
                    return Result.Failure("Email", "The email is already confirmed.");
                }

                int randomNumber = RandomNumberGenerator.GetInt32(0, 1000000);
                string verificationCode = randomNumber.ToString("D6", CultureInfo.InvariantCulture);

                Result<EmailVerificationCode> result = await EmailVerificationCode.CreateAsync(_userManager, applicationUser.Id, request.Email, verificationCode);

                if (result.IsSuccess == false)
                {
                    _applicationMetrics.RecordFailureOperation(operationName, "code_creation");
                    _logger.LogWarning("Email verification code creation failed");
                    return Result.Failure(result.Errors);
                }

                EmailVerificationCode emailVerificationCode = result.Value;

                await _repository.AddAsync(emailVerificationCode, cancellationToken);
                await _repository.SaveChangesAsync(cancellationToken);

                (string Email, string VerificationCode) model = (request.Email, verificationCode);
                string emailBody = await _viewRenderService.RenderViewToStringAsync("EmailTemplates/ConfirmRegistrationCodeTemplate", model);

                string senderEmail = "noreply@yourapp.com";
                string subject = "User Registration";

                EmailMessage emailObject = new(request.Email, request.Email, senderEmail, senderEmail, subject, emailBody);

                await _emailSender.SendAsync(emailObject);

                activity?.SetStatus(ActivityStatusCode.Ok, "Email verification code sent");
                _applicationMetrics.RecordSuccessOperation(operationName);
                _logger.LogInformation("Email verification code sent successfully");
                return Result.Success();
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _applicationMetrics.RecordFailureOperation(operationName, ex.GetType().Name);
                _logger.LogError(ex, "Unexpected error while sending email verification code");
                throw;
            }
        }
    }
}
