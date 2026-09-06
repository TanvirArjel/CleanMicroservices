using System.Data;
using System.Diagnostics;
using CleanHr.AuthApi.Application.Metrics;
using CleanHr.AuthApi.Common.Telemetry;
using CleanHr.AuthApi.Domain;
using CleanHr.AuthApi.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using TanvirArjel.ArgumentChecker;
using TanvirArjel.EFCore.GenericRepository;

namespace CleanHr.AuthApi.Application.Commands;

public sealed class VerifyUserEmailCommand(string email, string code) : IRequest<Result>
{
    public string Email { get; } = email.ThrowIfNotValidEmail(nameof(email));

    public string Code { get; } = code.ThrowIfNullOrEmpty(nameof(code));

    private class VerifyUserEmailCommandHandler : IRequestHandler<VerifyUserEmailCommand, Result>
    {
        private readonly IRepository _repository;
        private readonly IApplicationMetrics _applicationMetrics;
        private readonly ILogger<VerifyUserEmailCommandHandler> _logger;

        public VerifyUserEmailCommandHandler(
            IRepository repository,
            IApplicationMetrics applicationMetrics,
            ILogger<VerifyUserEmailCommandHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _applicationMetrics = applicationMetrics ?? throw new ArgumentNullException(nameof(applicationMetrics));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result> Handle(VerifyUserEmailCommand request, CancellationToken cancellationToken)
        {
            string operationName = "VerifyUserEmail";
            using var activity = Tracing.Source.StartActivity("VerifyUserEmail", ActivityKind.Internal);
            activity?.SetTag("email_verification.identifier", request?.Email);

            using var _ = _applicationMetrics.TrackOperation(operationName);
            using var loggerScope = _logger.BeginScope(new Dictionary<string, object> { { "Email", request?.Email } });

            try
            {
                _logger.LogInformation("Received request to verify user email");
                request.ThrowIfNull(nameof(request));

                IDbContextTransaction dbContextTransaction = await _repository
                .BeginTransactionAsync(IsolationLevel.Unspecified, cancellationToken);

                try
                {
                    EmailVerificationCode emailVerificationCode = await _repository
                .GetAsync<EmailVerificationCode>(evc => evc.Email == request.Email && evc.Code == request.Code && evc.UsedAtUtc == null, cancellationToken);

                    if (emailVerificationCode == null)
                    {
                        await dbContextTransaction.RollbackAsync(cancellationToken);
                        _applicationMetrics.RecordFailureOperation(operationName, "invalid_code");
                        _logger.LogWarning("Email verification failed: Invalid code");
                        return Result.Failure("Either email or password reset code is incorrect.");
                    }

                    if (DateTime.UtcNow > emailVerificationCode.SentAtUtc.AddMinutes(5))
                    {
                        await dbContextTransaction.RollbackAsync(cancellationToken);
                        _applicationMetrics.RecordFailureOperation(operationName, "expired_code");
                        _logger.LogWarning("Email verification failed: Code expired");
                        return Result.Failure("The code is expired.");
                    }

                    ApplicationUser applicationUser = await _repository.GetAsync<ApplicationUser>(au => au.Email == request.Email, cancellationToken);

                    if (applicationUser == null)
                    {
                        await dbContextTransaction.RollbackAsync(cancellationToken);
                        _applicationMetrics.RecordFailureOperation(operationName, "user_not_found");
                        _logger.LogWarning("Email verification failed: User not found");
                        return Result.Failure("The provided email is not related to any account.");
                    }

                    applicationUser.EmailConfirmed = true;
                    _repository.Update(applicationUser);

                    emailVerificationCode.MarkAsUsed();
                    _repository.Update(emailVerificationCode);

                    await dbContextTransaction.CommitAsync(cancellationToken);
                    activity?.SetStatus(ActivityStatusCode.Ok, "Email verification successful");
                    _applicationMetrics.RecordSuccessOperation(operationName);
                    _logger.LogInformation("Email verification successful");

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
                _logger.LogError(ex, "Unexpected error while verifying user email");
                throw;
            }
        }
    }
}
