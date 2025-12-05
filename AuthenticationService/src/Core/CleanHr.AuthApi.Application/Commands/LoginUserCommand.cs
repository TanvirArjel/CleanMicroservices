using System.Diagnostics;
using CleanHr.AuthApi.Application.Extensions;
using CleanHr.AuthApi.Application.Services;
using CleanHr.AuthApi.Application.Telemetry;
using CleanHr.AuthApi.Domain;
using CleanHr.AuthApi.Domain.Models;
using CleanHr.AuthApi.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TanvirArjel.ArgumentChecker;
using TanvirArjel.EFCore.GenericRepository;

namespace CleanHr.AuthApi.Application.Commands;

public sealed class LoginUserCommand(string emailOrUserName, string password) : IRequest<Result<AuthenticationResult>>
{
    public string EmailOrUserName { get; } = emailOrUserName;

    public string Password { get; } = password;

    private class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, Result<AuthenticationResult>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRepository _repository;
        private readonly IApplicationUserRepository _applicationUserRepository;
        private readonly JwtTokenManager _jwtTokenManager;
        private readonly ILogger<LoginUserCommandHandler> _logger;

        public LoginUserCommandHandler(
            UserManager<ApplicationUser> userManager,
            IRepository repository,
            JwtTokenManager jwtTokenManager,
            ILogger<LoginUserCommandHandler> logger,
            IApplicationUserRepository applicationUserRepository)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _applicationUserRepository = applicationUserRepository ?? throw new ArgumentNullException(nameof(applicationUserRepository));
            _jwtTokenManager = jwtTokenManager ?? throw new ArgumentNullException(nameof(jwtTokenManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<AuthenticationResult>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
            using var activity = ApplicationDiagnostics.ActivitySource.StartActivity("LoginUser", ActivityKind.Internal);
            activity.SetTag("login.identifier", request.EmailOrUserName);

            ApplicationMetrics.ActiveLogins.Add(1);

            try
            {
                _logger.LogInformation("Processing login for {EmailOrUserName}", request.EmailOrUserName);
                request.ThrowIfNull(nameof(request));

                if (string.IsNullOrWhiteSpace(request.EmailOrUserName))
                {
                    ApplicationMetrics.RecordLoginAttempt("validation_failed", "missing_email_or_username");
                    return Result<AuthenticationResult>.Failure("EmailOrUserName", "The email or username is required.");
                }

                if (string.IsNullOrWhiteSpace(request.Password))
                {
                    ApplicationMetrics.RecordLoginAttempt("validation_failed", "missing_password");
                    return Result<AuthenticationResult>.Failure("Password", "The password is required.");
                }

                ApplicationUser user = await _applicationUserRepository.GetByEmailOrUserNameAsync(request.EmailOrUserName);
                if (user == null)
                {
                    ApplicationMetrics.RecordLoginAttempt("failed", "user_not_found");
                    ApplicationMetrics.RecordUserLookup(found: false);
                    return Result<AuthenticationResult>.Failure("EmailOrUserName", "The email or username does not exist.");
                }

                ApplicationMetrics.RecordUserLookup(found: true);

                var isPasswordValid = await ValidatePasswordAsync(user, request.Password);
                if (!isPasswordValid)
                {
                    ApplicationMetrics.RecordLoginAttempt("failed", "invalid_password");
                    return Result<AuthenticationResult>.Failure("Password", "The password is incorrect.");
                }

                var authResult = await _jwtTokenManager.GetTokenAsync(user);

                if (authResult.IsSuccess == false)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, "Failed to generate JWT tokens");
                    ApplicationMetrics.RecordLoginAttempt("failed", "token_generation_failed");
                    _logger.LogError("Failed to generate JWT tokens for user {UserId}", user.Id);
                    return Result<AuthenticationResult>.Failure("TokenGeneration", "Failed to generate authentication tokens.");
                }

                await RecordLoginAsync(user, cancellationToken);

                activity?.SetStatus(ActivityStatusCode.Ok, "Login successful");
                ApplicationMetrics.RecordLoginAttempt("success", "none");
                _logger.LogInformation("Login successful for user {UserId}", user.Id);

                return Result<AuthenticationResult>.Success(authResult.Value);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                ApplicationMetrics.RecordLoginAttempt("error", ex.GetType().Name);

                var logFields = new Dictionary<string, object>
            {
                { "EmailOrUserName", request.EmailOrUserName }
            };

                _logger.LogException(ex, "Unhandled exception occurred while processing login for {EmailOrUserName}", logFields);
                return Result<AuthenticationResult>.Failure("Exception", "An error occurred while processing the login.");
            }
            finally
            {
                ApplicationMetrics.ActiveLogins.Add(-1);
            }
        }
        private async Task<bool> ValidatePasswordAsync(ApplicationUser user, string password)
        {
            using var activity = ApplicationDiagnostics.ActivitySource.StartActivity("ValidatePassword", ActivityKind.Internal);
            activity?.SetTag(ApplicationDiagnostics.Tags.UserId, user.Id.ToString());

            _logger.LogDebug("Validating password for user {User}", user);

            var isValid = await _userManager.CheckPasswordAsync(user, password); if (isValid)
            {
                activity?.SetStatus(ActivityStatusCode.Ok, "Password validation successful");
                ApplicationMetrics.RecordPasswordValidation(isValid: true);
                _logger.LogInformation("Password validation successful for user {UserId}", user.Id);
            }
            else
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Password validation failed");
                ApplicationMetrics.RecordPasswordValidation(isValid: false);
                _logger.LogWarning("Login failed: Invalid password for user {UserId}", user.Id);
            }

            return isValid;
        }

        private async Task RecordLoginAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            using var activity = ApplicationDiagnostics.ActivitySource.StartActivity(
               "RecordLogin",
                ActivityKind.Internal);

            activity?.SetTag(ApplicationDiagnostics.Tags.UserId, user?.Id.ToString());

            try
            {
                _logger.LogDebug("Recording login for user {User}", user);

                user.RecordLogin();
                _repository.Update(user);
                await _repository.SaveChangesAsync(cancellationToken);

                activity.SetStatus(ActivityStatusCode.Ok, "Login recorded successfully");
                _logger.LogInformation("Login recorded successfully for user {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                activity.SetStatus(ActivityStatusCode.Error, "Failed to record login");

                var fields = new Dictionary<string, object>
                {
                    { "UserId", user.Id }
                };

                _logger.LogException(ex, "Failed to record login for user {UserId}", fields);
            }
        }
    }
}
