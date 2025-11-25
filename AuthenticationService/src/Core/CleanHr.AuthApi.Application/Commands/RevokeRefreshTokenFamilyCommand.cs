using CleanHr.AuthApi.Domain;
using CleanHr.AuthApi.Domain.Aggregates;
using MediatR;
using TanvirArjel.ArgumentChecker;
using TanvirArjel.EFCore.GenericRepository;

namespace CleanHr.AuthApi.Application.Commands;

public sealed class RevokeRefreshTokenFamilyCommand(Guid userId, string refreshToken) : IRequest<Result>
{
    public Guid UserId { get; } = userId.ThrowIfEmpty(nameof(userId));

    public string RefreshToken { get; } = refreshToken.ThrowIfNullOrEmpty(nameof(refreshToken));
}

internal class RevokeRefreshTokenFamilyCommandHandler(IRepository repository) : IRequestHandler<RevokeRefreshTokenFamilyCommand, Result>
{
    private readonly IRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<Result> Handle(RevokeRefreshTokenFamilyCommand request, CancellationToken cancellationToken)
    {
        request.ThrowIfNull(nameof(request));

        // Find the refresh token to get its family ID
        RefreshToken refreshToken = await _repository.GetAsync<RefreshToken>(
            rt => rt.UserId == request.UserId && rt.Token == request.RefreshToken,
            cancellationToken);

        if (refreshToken == null)
        {
            return Result.Failure("Refresh token not found.");
        }

        // Revoke all tokens in the same family (device/session)
        List<RefreshToken> familyTokens = await _repository.GetListAsync<RefreshToken>(
            rt => rt.TokenFamilyId == refreshToken.TokenFamilyId && !rt.IsRevoked,
            cancellationToken);

        foreach (RefreshToken token in familyTokens)
        {
            token.Revoke();
            _repository.Update(token);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
