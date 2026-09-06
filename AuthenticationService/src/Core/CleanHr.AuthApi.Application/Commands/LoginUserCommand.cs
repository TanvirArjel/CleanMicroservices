using System.Diagnostics;
using CleanHr.AuthApi.Application.Services;
using CleanHr.AuthApi.Domain;
using CleanHr.AuthApi.Domain.Models;
using CleanHr.AuthApi.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TanvirArjel.ArgumentChecker;
using TanvirArjel.EFCore.GenericRepository;
using CleanHr.AuthApi.Common.Telemetry;
using CleanHr.AuthApi.Application.Metrics;

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
        private readonly IApplicationMetrics _applicationMetrics;
        private readonly ILogger<LoginUserCommandHandler> _logger;

        public LoginUserCommandHandler(
            UserManager<ApplicationUser> userManager,
            IRepository repository,
            JwtTokenManager jwtTokenManager,
            ILogger<LoginUserCommandHandler> logger,
            IApplicationUserRepository applicationUserRepository,
            IApplicationMetrics applicationMetrics)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _applicationUserRepository = applicationUserRepository ?? throw new ArgumentNullException(nameof(applicationUserRepository));
            _jwtTokenManager = jwtTokenManager ?? throw new ArgumentNullException(nameof(jwtTokenManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _applicationMetrics = applicationMetrics ?? throw new ArgumentNullException(nameof(applicationMetrics));
        }

        public async Task<Result<AuthenticationResult>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
            string operationName = "LoginUser";
            using var activity = Tracing.Source.StartActivity(operationName, ActivityKind.Internal);
            activity.SetTag("login.identifier", request.EmailOrUserName);

            using var _ = _applicationMetrics.TrackOperation(operationName);
            var loggerContext = new Dictionary<string, object>
            {
                { "EmailOrUserName", request.EmailOrUserName }
            };
            using var loggerScope = _logger.BeginScope(loggerContext);

            try
            {
                _logger.LogInformation("Received request to handle user login");
                request.ThrowIfNull(nameof(request));

                if (string.IsNullOrWhiteSpace(request.EmailOrUserName))
                {
                    _applicationMetrics.RecordFailureOperation(operationName, "missing_email_or_username");
                    _logger.LogWarning("Login failed: Missing email or username");

                    return Result<AuthenticationResult>.Failure("EmailOrUserName", "The email or username is required.");
                }

                if (string.IsNullOrWhiteSpace(request.Password))
                {
                    _applicationMetrics.RecordFailureOperation(operationName, "missing_password");
                    _logger.LogWarning("Login failed: password is null or empty");

                    return Result<AuthenticationResult>.Failure("Password", "The password is required.");
                }

                var findUserResult = await _applicationUserRepository.GetByEmailOrUserNameAsync(request.EmailOrUserName);

                if (findUserResult.IsSuccess == false)
                {
                    loggerContext.Add("Errors", findUserResult.Errors);
                    _logger.LogError("Error occurred while retrieving the user");

                    return Result<AuthenticationResult>.Failure("UserRetrieval", "An error occurred while retrieving the user.");
                }

                if (findUserResult.Value == null)
                {
                    _applicationMetrics.RecordFailureOperation(operationName, "user_not_found");
                    _logger.LogError("Login failed: User not found");

                    return Result<AuthenticationResult>.Failure("EmailOrUserName", "The email or username does not exist.");
                }

                loggerContext.Add("UserId", findUserResult.Value.Id);
                _logger.LogInformation("User found, proceeding with password validation");
                var isPasswordValid = await ValidatePasswordAsync(findUserResult.Value, request.Password);
                if (!isPasswordValid)
                {
                    _applicationMetrics.RecordFailureOperation(operationName, "invalid_password");
                    _logger.LogError("Login failed: Invalid password for the user");

                    return Result<AuthenticationResult>.Failure("Password", "The password is incorrect.");
                }

                _logger.LogInformation("Password validation successful, generating JWT tokens for the user");
                var authResult = await _jwtTokenManager.GetTokenAsync(findUserResult.Value);

                if (authResult.IsSuccess == false)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, "Failed to generate JWT tokens");
                    _applicationMetrics.RecordFailureOperation(operationName, "token_generation_failed");
                    _logger.LogError("Error occurred while generating JWT tokens for the user");

                    return Result<AuthenticationResult>.Failure("TokenGeneration", "Failed to generate authentication tokens.");
                }

                await RecordLoginAsync(findUserResult.Value, cancellationToken);
                activity?.SetStatus(ActivityStatusCode.Ok, "Login successful");
                _applicationMetrics.RecordSuccessOperation(operationName);
                _logger.LogInformation("Login successful, returning authentication result");

                return Result<AuthenticationResult>.Success(authResult.Value);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _applicationMetrics.RecordFailureOperation(operationName, ex.GetType().Name);
                _logger.LogCritical(ex, "An unexpected error occurred during the login process");

                return Result<AuthenticationResult>.Failure("Exception", "An error occurred while processing the login.");
            }
        }
        private async Task<bool> ValidatePasswordAsync(ApplicationUser user, string password)
        {
            using var activity = Tracing.Source.StartActivity("ValidatePassword", ActivityKind.Internal);
            activity?.SetTag("userId", user.Id.ToString());

            _logger.LogInformation("Validating password for the user");
            var isValid = await _userManager.CheckPasswordAsync(user, password);

            if (isValid)
            {
                activity?.SetStatus(ActivityStatusCode.Ok, "Password validation successful");
                _applicationMetrics.RecordSuccessOperation("ValidatePassword");
                _logger.LogInformation("Password validation successful for the user");
            }
            else
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Password validation failed");
                _applicationMetrics.RecordFailureOperation("ValidatePassword", "invalid_password");
                _logger.LogWarning("Password validation failed: Invalid password for the user");
            }

            return isValid;
        }

        private async Task RecordLoginAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            using var activity = Tracing.Source.StartActivity(
               "RecordLogin",
                ActivityKind.Internal);
            activity?.SetTag("userId", user?.Id.ToString());

            try
            {
                _logger.LogInformation("Recording login for the user");

                user.RecordLogin();
                _repository.Update(user);
                await _repository.SaveChangesAsync(cancellationToken);

                activity.SetStatus(ActivityStatusCode.Ok, "Login recorded successfully");
                _logger.LogInformation("Login recorded successfully for the user");
            }
            catch (Exception ex)
            {
                activity.SetStatus(ActivityStatusCode.Error, "Failed to record login");

                _logger.LogError(ex, "Failed to record login for the user");
            }
        }
    }
}
