using System.Data;
using System.Diagnostics;
using CleanHr.AuthApi.Application.Metrics;
using CleanHr.AuthApi.Common.Telemetry;
using CleanHr.AuthApi.Domain;
using CleanHr.AuthApi.Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using TanvirArjel.ArgumentChecker;
using TanvirArjel.EFCore.GenericRepository;

namespace CleanHr.AuthApi.Application.Commands;

public sealed record ResetPasswordCommand(string Email, string Code, string NewPassword) : IRequest<Result>
{
    private class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
    {
        private readonly IRepository _repository;
        private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
        private readonly IApplicationMetrics _applicationMetrics;
        private readonly ILogger<ResetPasswordCommandHandler> _logger;

        public ResetPasswordCommandHandler(
            IRepository repository,
            IPasswordHasher<ApplicationUser> passwordHasher,
            IApplicationMetrics applicationMetrics,
            ILogger<ResetPasswordCommandHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
            _applicationMetrics = applicationMetrics ?? throw new ArgumentNullException(nameof(applicationMetrics));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            string operationName = "ResetPassword";
            using var activity = Tracing.Source.StartActivity(operationName, ActivityKind.Internal);
            activity?.SetTag("password_reset.identifier", request?.Email);

            using var _ = _applicationMetrics.TrackOperation(operationName);
            using var loggerScope = _logger.BeginScope(new Dictionary<string, object> { { "Email", request?.Email } });

            try
            {
                _logger.LogInformation("Received request to reset user password");
                request.ThrowIfNull(nameof(request));

                IDbContextTransaction dbContextTransaction = await _repository
                    .BeginTransactionAsync(IsolationLevel.Unspecified, cancellationToken);

                try
                {
                    PasswordResetCode passwordResetCode = await _repository
                    .GetAsync<PasswordResetCode>(evc => evc.Email == request.Email && evc.Code == request.Code && evc.UsedAtUtc == null, cancellationToken);

                    if (passwordResetCode == null)
                    {
                        await dbContextTransaction.RollbackAsync(cancellationToken);
                        _applicationMetrics.RecordFailureOperation(operationName, "invalid_code");
                        _logger.LogWarning("Password reset failed: Invalid code");
                        return Result.Failure("Either email or password reset code is incorrect.");
                    }

                    if (DateTime.UtcNow > passwordResetCode.SentAtUtc.AddMinutes(5))
                    {
                        await dbContextTransaction.RollbackAsync(cancellationToken);
                        _applicationMetrics.RecordFailureOperation(operationName, "expired_code");
                        _logger.LogWarning("Password reset failed: Code expired");
                        return Result.Failure("The code is expired.");
                    }

                    ApplicationUser applicationUser = await _repository.GetAsync<ApplicationUser>(au => au.Email == request.Email, cancellationToken);

                    if (applicationUser == null)
                    {
                        await dbContextTransaction.RollbackAsync(cancellationToken);
                        _applicationMetrics.RecordFailureOperation(operationName, "user_not_found");
                        _logger.LogWarning("Password reset failed: User not found");
                        return Result.Failure("The provided email is not related to any account.");
                    }

                    // Use domain method to set password (includes validation)
                    Result setPasswordResult = await applicationUser.SetPasswordAsync(request.NewPassword, _passwordHasher);

                    if (setPasswordResult.IsSuccess == false)
                    {
                        await dbContextTransaction.RollbackAsync(cancellationToken);
                        _applicationMetrics.RecordFailureOperation(operationName, "password_validation");
                        _logger.LogWarning("Password reset failed during password validation");
                        return setPasswordResult;
                    }

                    _repository.Update(applicationUser);

                    passwordResetCode.MarkAsUsed();
                    _repository.Update(passwordResetCode);

                    await dbContextTransaction.CommitAsync(cancellationToken);
                    activity?.SetStatus(ActivityStatusCode.Ok, "Password reset successful");
                    _applicationMetrics.RecordSuccessOperation(operationName);
                    _logger.LogInformation("Password reset successful");
                    return Result.Success();
                }
                catch
                {
                    await dbContextTransaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _applicationMetrics.RecordFailureOperation(operationName, ex.GetType().Name);
                _logger.LogError(ex, "Unexpected error while resetting password");
                throw;
            }
        }
    }
}
