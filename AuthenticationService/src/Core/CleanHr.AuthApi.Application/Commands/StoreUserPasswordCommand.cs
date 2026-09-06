using CleanHr.AuthApi.Domain.Models;
using CleanHr.AuthApi.Application.Metrics;
using CleanHr.AuthApi.Common.Telemetry;
using System.Diagnostics;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TanvirArjel.ArgumentChecker;
using TanvirArjel.EFCore.GenericRepository;

namespace CleanHr.AuthApi.Application.Commands;

public sealed class StoreUserPasswordCommand(ApplicationUser user, string password) : IRequest
{
    public ApplicationUser User { get; } = user.ThrowIfNull(nameof(user));

    public string Password { get; } = password.ThrowIfNullOrEmpty(nameof(password));

    private class StoreUserPasswordCommandHandler : IRequestHandler<StoreUserPasswordCommand>
    {
        private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
        private readonly IRepository _repository;
        private readonly IApplicationMetrics _applicationMetrics;
        private readonly ILogger<StoreUserPasswordCommandHandler> _logger;

        public StoreUserPasswordCommandHandler(
                IPasswordHasher<ApplicationUser> passwordHasher,
                IRepository repository,
                IApplicationMetrics applicationMetrics,
                ILogger<StoreUserPasswordCommandHandler> logger)
        {
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _applicationMetrics = applicationMetrics ?? throw new ArgumentNullException(nameof(applicationMetrics));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Handle(StoreUserPasswordCommand request, CancellationToken cancellationToken)
        {
            string operationName = "StoreUserPassword";
            using var activity = Tracing.Source.StartActivity("StoreUserPassword", ActivityKind.Internal);
            activity?.SetTag("password_storage.user_id", request?.User.Id);

            using var _ = _applicationMetrics.TrackOperation(operationName);
            using var loggerScope = _logger.BeginScope(new Dictionary<string, object> { { "UserId", request?.User.Id } });

            try
            {
                _logger.LogInformation("Received request to store user password history");
                request.ThrowIfNull(nameof(request));

                string passwordHash = _passwordHasher.HashPassword(request.User, request.Password);

                UserOldPassword userOldPassword = new()
                {
                    UserId = request.User.Id,
                    PasswordHash = passwordHash,
                    SetAtUtc = DateTime.UtcNow
                };

                _repository.Add(userOldPassword);
                await _repository.SaveChangesAsync(cancellationToken);
                activity?.SetStatus(ActivityStatusCode.Ok, "User password history stored");
                _applicationMetrics.RecordSuccessOperation(operationName);
                _logger.LogInformation("User password history stored successfully");
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _applicationMetrics.RecordFailureOperation(operationName, ex.GetType().Name);
                _logger.LogError(ex, "Unexpected error while storing user password history");
                throw;
            }
        }
    }
}
