using CleanHr.AuthApi.Domain;
using CleanHr.AuthApi.Domain.Models;
using CleanHr.AuthApi.Application.Metrics;
using CleanHr.AuthApi.Common.Telemetry;
using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using TanvirArjel.ArgumentChecker;
using TanvirArjel.EFCore.GenericRepository;

namespace CleanHr.AuthApi.Application.Commands;

public sealed class RevokeRefreshTokenFamilyCommand(Guid userId, string refreshToken) : IRequest<Result>
{
    public Guid UserId { get; } = userId.ThrowIfEmpty(nameof(userId));

    public string RefreshToken { get; } = refreshToken.ThrowIfNullOrEmpty(nameof(refreshToken));

    private class RevokeRefreshTokenFamilyCommandHandler : IRequestHandler<RevokeRefreshTokenFamilyCommand, Result>
    {
        private readonly IRepository _repository;
        private readonly IApplicationMetrics _applicationMetrics;
        private readonly ILogger<RevokeRefreshTokenFamilyCommandHandler> _logger;

        public RevokeRefreshTokenFamilyCommandHandler(
            IRepository repository,
            IApplicationMetrics applicationMetrics,
            ILogger<RevokeRefreshTokenFamilyCommandHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _applicationMetrics = applicationMetrics ?? throw new ArgumentNullException(nameof(applicationMetrics));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result> Handle(RevokeRefreshTokenFamilyCommand request, CancellationToken cancellationToken)
        {
            string operationName = "RevokeRefreshTokenFamily";
            using var activity = Tracing.Source.StartActivity(operationName, ActivityKind.Internal);
            activity?.SetTag("token_revocation.user_id", request?.UserId);

            using var _ = _applicationMetrics.TrackOperation(operationName);
            using var loggerScope = _logger.BeginScope(new Dictionary<string, object> { { "UserId", request?.UserId } });

            try
            {
                _logger.LogInformation("Received request to revoke refresh token family");
                request.ThrowIfNull(nameof(request));

                RefreshToken refreshToken = await _repository.GetAsync<RefreshToken>(
                    rt => rt.UserId == request.UserId && rt.Token == request.RefreshToken,
                    cancellationToken);

                if (refreshToken == null)
                {
                    _applicationMetrics.RecordFailureOperation(operationName, "token_not_found");
                    _logger.LogWarning("Refresh token revocation failed: Token not found");
                    return Result.Failure("Refresh token not found.");
                }

                List<RefreshToken> familyTokens = await _repository.GetListAsync<RefreshToken>(
                    rt => rt.TokenFamilyId == refreshToken.TokenFamilyId && !rt.RevokedAtUtc.HasValue,
                    cancellationToken);

                foreach (RefreshToken token in familyTokens)
                {
                    token.Revoke();
                    _repository.Update(token);
                }

                await _repository.SaveChangesAsync(cancellationToken);

                activity?.SetStatus(ActivityStatusCode.Ok, "Refresh token family revoked");
                _applicationMetrics.RecordSuccessOperation(operationName);
                _logger.LogInformation("Refresh token family revoked successfully");
                return Result.Success();
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _applicationMetrics.RecordFailureOperation(operationName, ex.GetType().Name);
                _logger.LogError(ex, "Unexpected error while revoking refresh token family");
                throw;
            }
        }
    }
}
