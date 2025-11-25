using CleanHr.AuthApi.Domain;
using CleanHr.AuthApi.Domain.Aggregates;
using MediatR;
using Microsoft.Extensions.Logging;
using TanvirArjel.ArgumentChecker;
using TanvirArjel.EFCore.GenericRepository;

namespace CleanHr.AuthApi.Application.Commands;

public sealed class UpdateRefreshTokenCommand(Guid userId, string oldToken, string newToken) : IRequest<Result<RefreshToken>>
{
    public Guid UserId { get; } = userId.ThrowIfEmpty(nameof(userId));

    public string OldToken { get; } = oldToken.ThrowIfNullOrEmpty(nameof(oldToken));

    public string NewToken { get; } = newToken.ThrowIfNullOrEmpty(nameof(newToken));
}

internal class UpdateRefreshTokenCommandHandler(IRepository repository, ILogger<UpdateRefreshTokenCommandHandler> logger) : IRequestHandler<UpdateRefreshTokenCommand, Result<RefreshToken>>
{
    private readonly IRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly ILogger<UpdateRefreshTokenCommandHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<Result<RefreshToken>> Handle(UpdateRefreshTokenCommand request, CancellationToken cancellationToken)
    {
        request.ThrowIfNull(nameof(request));

        // Find the old refresh token
        RefreshToken oldRefreshToken = await _repository.GetAsync<RefreshToken>(
            rt => rt.UserId == request.UserId && rt.Token == request.OldToken,
            cancellationToken);

        if (oldRefreshToken == null)
        {
            return Result<RefreshToken>.Failure($"The RefreshToken does not exist for user: {request.UserId}.");
        }

        // SECURITY: Detect token reuse attack
        // If token has already been used, this is a security breach - revoke ALL user tokens
        if (oldRefreshToken.HasBeenUsed())
        {
            _logger.LogWarning(
                "SECURITY ALERT: Refresh token reuse detected for user {UserId}. Token {TokenId} was already used at {UsedAt}. Revoking all tokens.",
                request.UserId,
                oldRefreshToken.Id,
                oldRefreshToken.UsedAtUtc);

            // Revoke all tokens for this user
            List<RefreshToken> allUserTokens = await _repository.GetListAsync<RefreshToken>(
                rt => rt.UserId == request.UserId && !rt.IsRevoked,
                cancellationToken);

            foreach (RefreshToken token in allUserTokens)
            {
                token.Revoke();
                _repository.Update(token);
            }

            await _repository.SaveChangesAsync(cancellationToken);

            return Result<RefreshToken>.Failure("Token reuse detected. All tokens have been revoked for security. Please login again.");
        }

        // Mark old token as used (one-time use enforcement)
        oldRefreshToken.MarkAsUsed();

        // Also revoke it for extra safety
        oldRefreshToken.Revoke();
        _repository.Update(oldRefreshToken);

        // Create new refresh token
        Result<RefreshToken> createResult = await RefreshToken.CreateAsync(request.UserId, request.NewToken);

        if (createResult.IsSuccess == false)
        {
            return createResult;
        }

        RefreshToken newRefreshToken = createResult.Value;
        _repository.Add(newRefreshToken);

        await _repository.SaveChangesAsync(cancellationToken);

        return Result<RefreshToken>.Success(newRefreshToken);
    }
}
