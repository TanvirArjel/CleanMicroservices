using CleanHr.AuthApi.Domain.Aggregates;
using MediatR;
using TanvirArjel.ArgumentChecker;
using TanvirArjel.EFCore.GenericRepository;

namespace CleanHr.AuthApi.Application.Queries;

public sealed class IsRefreshTokenValidQuery(Guid userId, string refreshToken) : IRequest<bool>
{
    public Guid UserId { get; } = userId;

    public string RefreshToken { get; } = refreshToken;
}

internal class IsRefreshTokenValidQueryHandler(IRepository repository) : IRequestHandler<IsRefreshTokenValidQuery, bool>
{
    private readonly IRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<bool> Handle(IsRefreshTokenValidQuery request, CancellationToken cancellationToken)
    {
        request.ThrowIfNull(nameof(request));

        RefreshToken refreshToken = await _repository.GetAsync<RefreshToken>(
            rt => rt.UserId == request.UserId && rt.Token == request.RefreshToken,
            cancellationToken);

        if (refreshToken == null)
        {
            return false;
        }

        // Check if token is valid (not expired and not revoked)
        bool isRefreshTokenValid = refreshToken.IsValid();

        return isRefreshTokenValid;
    }
}
