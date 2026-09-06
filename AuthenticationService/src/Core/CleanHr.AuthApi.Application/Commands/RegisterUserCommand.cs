using System.Diagnostics;
using System.Linq;
using CleanHr.AuthApi.Common.Telemetry;
using CleanHr.AuthApi.Domain;
using CleanHr.AuthApi.Domain.Models;
using CleanHr.AuthApi.Domain.Repositories;
using CleanHr.AuthApi.Application.Metrics;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TanvirArjel.ArgumentChecker;

namespace CleanHr.AuthApi.Application.Commands;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string ConfirmPassword) : IRequest<Result<Guid>>
{
    private class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<Guid>>
    {
        private readonly IApplicationUserRepository _userRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IApplicationMetrics _applicationMetrics;
        private readonly ILogger<RegisterUserCommandHandler> _logger;

        public RegisterUserCommandHandler(
            IApplicationUserRepository userRepository,
            UserManager<ApplicationUser> userManager,
            IApplicationMetrics applicationMetrics,
            ILogger<RegisterUserCommandHandler> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _applicationMetrics = applicationMetrics ?? throw new ArgumentNullException(nameof(applicationMetrics));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<Guid>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            string operationName = "RegisterUser";
            using var activity = Tracing.Source.StartActivity(operationName, ActivityKind.Internal);
            activity?.SetTag("registration.identifier", request?.Email);

            using var _ = _applicationMetrics.TrackOperation(operationName);
            var loggerContext = new Dictionary<string, object>
            {
                { "Email", request?.Email }
            };
            using var loggerScope = _logger.BeginScope(loggerContext);

            try
            {
                _logger.LogInformation("Received request to handle user registration");
                request.ThrowIfNull(nameof(request));

                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    _applicationMetrics.RecordFailureOperation(operationName, "missing_email");
                    _logger.LogWarning("Registration failed: Missing email");

                    return Result<Guid>.Failure("Email", "The email is required.");
                }

                if (request.Password != request.ConfirmPassword)
                {
                    _applicationMetrics.RecordFailureOperation(operationName, "password_mismatch");
                    _logger.LogWarning("Registration failed: Passwords do not match");

                    return Result<Guid>.Failure("ConfirmPassword", "The password and confirmation password do not match.");
                }

                _logger.LogInformation("Creating user through the domain factory");
                Result<ApplicationUser> result = await ApplicationUser.CreateAsync(
                    _userRepository,
                    request.Email,
                    request.Password,
                    request.Email);

                if (result.IsSuccess == false)
                {
                    _applicationMetrics.RecordFailureOperation(operationName, "domain_validation");
                    loggerContext.Add("Errors", result.Errors);
                    _logger.LogWarning("Registration failed during domain validation");

                    return Result<Guid>.Failure(result.Errors);
                }

                _logger.LogInformation("Creating user through ASP.NET Core Identity");
                IdentityResult identityResult = await _userManager.CreateAsync(result.Value, request.Password);

                if (identityResult.Succeeded == false)
                {
                    _applicationMetrics.RecordFailureOperation(operationName, "identity_creation");
                    var errors = identityResult.Errors.ToDictionary(
                        e => e.Code,
                        e => e.Description);
                    loggerContext.Add("Errors", errors);
                    _logger.LogWarning("Registration failed during Identity user creation");

                    return Result<Guid>.Failure(errors);
                }

                activity?.SetStatus(ActivityStatusCode.Ok, "Registration successful");
                _applicationMetrics.RecordSuccessOperation(operationName);
                loggerContext.Add("UserId", result.Value.Id);
                _logger.LogInformation("User registration successful");

                return Result<Guid>.Success(result.Value.Id);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _applicationMetrics.RecordFailureOperation(operationName, ex.GetType().Name);
                _logger.LogCritical(ex, "An unexpected error occurred during the registration process");

                return Result<Guid>.Failure("Exception", "An error occurred while processing the registration.");
            }
        }
    }
}
