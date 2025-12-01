using System.Diagnostics;
using System.Linq;
using CleanHr.AuthApi.Application.Extensions;
using CleanHr.AuthApi.Application.Services;
using CleanHr.AuthApi.Application.Telemetry;
using CleanHr.AuthApi.Domain;
using CleanHr.AuthApi.Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;
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
        private readonly JwtTokenManager _jwtTokenManager;
        private readonly ILogger<LoginUserCommandHandler> _logger;

        public LoginUserCommandHandler(
            UserManager<ApplicationUser> userManager,
            IRepository repository,
            JwtTokenManager jwtTokenManager,
            ILogger<LoginUserCommandHandler> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _jwtTokenManager = jwtTokenManager ?? throw new ArgumentNullException(nameof(jwtTokenManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<AuthenticationResult>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
            using var activity = ApplicationDiagnostics.ActivitySource.StartActivity(
                ApplicationDiagnostics.Activities.UserLogin,
                ActivityKind.Internal);

            activity?.SetTag(ApplicationDiagnostics.Tags.Operation, "user.login");
            activity?.SetTag("login.identifier", request.EmailOrUserName);

            EnrichLogContext(activity);

            try
            {
                request.ThrowIfNull(nameof(request));

                ValidateInput(request, activity);
                if (string.IsNullOrWhiteSpace(request.EmailOrUserName) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return Result<AuthenticationResult>.Failure(
                        string.IsNullOrWhiteSpace(request.EmailOrUserName) ? "EmailOrUserName" : "Password",
                        string.IsNullOrWhiteSpace(request.EmailOrUserName)
                            ? "The email or username is required."
                            : "The password is required.");
                }

                var user = await FindUserAsync(request.EmailOrUserName, cancellationToken);
                if (user == null)
                {
                    return Result<AuthenticationResult>.Failure("EmailOrUserName", "The email or username does not exist.");
                }

                activity?.SetTag(ApplicationDiagnostics.Tags.UserId, user.Id.ToString());
                activity?.SetTag(ApplicationDiagnostics.Tags.Email, user.Email);
                activity?.SetTag(ApplicationDiagnostics.Tags.UserName, user.UserName);

                var isPasswordValid = await ValidatePasswordAsync(user, request.Password);
                if (!isPasswordValid)
                {
                    return Result<AuthenticationResult>.Failure("Password", "The password is incorrect.");
                }

                await RecordLoginAsync(user, cancellationToken);

                var authResult = await GenerateTokenAsync(user.Id.ToString());

                activity?.SetTag(ApplicationDiagnostics.Tags.Result, "success");
                _logger.LogInformation("Login successful for user {UserId}", user.Id);

                return Result<AuthenticationResult>.Success(authResult);
            }
            catch (Exception ex)
            {
                return HandleException(ex, request.EmailOrUserName, activity);
            }
        }

        private void ValidateInput(LoginUserCommand request, Activity parentActivity)
        {
            using var activity = ApplicationDiagnostics.ActivitySource.StartActivity(
                "ValidateLoginInput",
                ActivityKind.Internal,
                parentActivity.Context);

            EnrichLogContext(activity);

            if (string.IsNullOrWhiteSpace(request.EmailOrUserName))
            {
                activity?.SetTag(ApplicationDiagnostics.Tags.Result, "validation_failed");
                activity?.SetTag(ApplicationDiagnostics.Tags.ErrorType, "ValidationError");
                activity?.AddEvent(new ActivityEvent("EmailOrUserName is required"));
                _logger.LogWarning("Login failed: EmailOrUserName is required");
                return;
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                activity?.SetTag(ApplicationDiagnostics.Tags.Result, "validation_failed");
                activity?.SetTag(ApplicationDiagnostics.Tags.ErrorType, "ValidationError");
                activity?.AddEvent(new ActivityEvent("Password is required"));
                _logger.LogWarning("Login failed for {EmailOrUserName}: Password is required", request.EmailOrUserName);
                return;
            }

            activity?.SetTag(ApplicationDiagnostics.Tags.Result, "success");
            _logger.LogDebug("Input validation successful");
        }

        private async Task<ApplicationUser> FindUserAsync(string emailOrUserName, CancellationToken cancellationToken)
        {
            using var activity = ApplicationDiagnostics.ActivitySource.StartActivity(
                "FindUser",
                ActivityKind.Internal);

            EnrichLogContext(activity);

            _logger.LogDebug("Looking up user with identifier: {EmailOrUserName}", emailOrUserName);

            string normalizedEmailOrUserName = emailOrUserName.ToUpperInvariant();
            var user = await _userManager.Users
                .Where(u => u.NormalizedEmail == normalizedEmailOrUserName || u.NormalizedUserName == normalizedEmailOrUserName)
                .FirstOrDefaultAsync(cancellationToken);

            if (user == null)
            {
                activity?.SetTag(ApplicationDiagnostics.Tags.Result, "user_not_found");
                activity?.AddEvent(new ActivityEvent("User not found"));
                _logger.LogWarning("Login failed: User not found for {EmailOrUserName}", emailOrUserName);
            }
            else
            {
                activity?.SetTag(ApplicationDiagnostics.Tags.Result, "success");
                activity?.SetTag(ApplicationDiagnostics.Tags.UserId, user.Id.ToString());
                activity?.AddEvent(new ActivityEvent("User found"));
                _logger.LogDebug("User found with Id: {UserId}", user.Id);
            }

            return user;
        }

        private async Task<bool> ValidatePasswordAsync(ApplicationUser user, string password)
        {
            using var activity = ApplicationDiagnostics.ActivitySource.StartActivity(
                "ValidatePassword",
                ActivityKind.Internal);

            activity?.SetTag(ApplicationDiagnostics.Tags.UserId, user.Id.ToString());
            EnrichLogContext(activity);

            _logger.LogDebug("Validating password for user {UserId}", user.Id);

            var isValid = await _userManager.CheckPasswordAsync(user, password);

            if (isValid)
            {
                activity?.SetTag(ApplicationDiagnostics.Tags.Result, "success");
                activity?.AddEvent(new ActivityEvent("Password validation successful"));
                _logger.LogInformation("Password validation successful for user {UserId}", user.Id);
            }
            else
            {
                activity?.SetTag(ApplicationDiagnostics.Tags.Result, "invalid_password");
                activity?.AddEvent(new ActivityEvent("Password validation failed"));
                _logger.LogWarning("Login failed: Invalid password for user {UserId}", user.Id);
            }

            return isValid;
        }

        private async Task RecordLoginAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            using var activity = ApplicationDiagnostics.ActivitySource.StartActivity(
                "RecordLogin",
                ActivityKind.Internal);

            activity?.SetTag(ApplicationDiagnostics.Tags.UserId, user.Id.ToString());
            EnrichLogContext(activity);

            _logger.LogDebug("Recording login for user {UserId}", user.Id);

            user.RecordLogin();
            _repository.Update(user);
            await _repository.SaveChangesAsync(cancellationToken);

            activity?.SetTag(ApplicationDiagnostics.Tags.Result, "success");
            activity?.AddEvent(new ActivityEvent("Login recorded successfully"));
            _logger.LogDebug("Login recorded successfully for user {UserId}", user.Id);
        }

        private async Task<AuthenticationResult> GenerateTokenAsync(string userId)
        {
            using var activity = ApplicationDiagnostics.ActivitySource.StartActivity(
                "GenerateJwtToken",
                ActivityKind.Internal);

            activity?.SetTag(ApplicationDiagnostics.Tags.UserId, userId);
            EnrichLogContext(activity);

            _logger.LogDebug("Generating JWT token for user {UserId}", userId);

            var authResult = await _jwtTokenManager.GetTokenAsync(userId);

            activity?.SetTag(ApplicationDiagnostics.Tags.Result, "success");
            activity?.AddEvent(new ActivityEvent("JWT token generated successfully"));
            _logger.LogDebug("JWT token generated for user {UserId}", userId);

            return authResult;
        }

        private Result<AuthenticationResult> HandleException(Exception ex, string emailOrUserName, Activity? activity)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag(ApplicationDiagnostics.Tags.Result, "error");
            activity?.SetTag(ApplicationDiagnostics.Tags.ErrorType, ex.GetType().Name);
            activity?.SetTag(ApplicationDiagnostics.Tags.ErrorMessage, ex.Message);

            var exceptionTags = new ActivityTagsCollection
            {
                { "exception.type", ex.GetType().FullName },
                { "exception.message", ex.Message },
                { "exception.stacktrace", ex.StackTrace }
            };
            activity?.AddEvent(new ActivityEvent("exception", tags: exceptionTags));

            var logFields = new Dictionary<string, object>
            {
                { "EmailOrUserName", emailOrUserName },
                { "ExceptionType", ex.GetType().Name },
                { "ExceptionMessage", ex.Message },
                { "StackTrace", ex.StackTrace ?? string.Empty }
            };

            _logger.LogException(ex, "Unhandled exception occurred while processing login for {EmailOrUserName}", logFields);
            return Result<AuthenticationResult>.Failure("Exception", "An error occurred while processing the login.");
        }

        private static void EnrichLogContext(Activity activity)
        {
            if (activity == null)
            {
                return;
            }

            LogContext.PushProperty("TraceId", activity.TraceId.ToString());
            LogContext.PushProperty("SpanId", activity.SpanId.ToString());
            LogContext.PushProperty("ParentId", activity.ParentSpanId.ToString());
        }
    }
}
