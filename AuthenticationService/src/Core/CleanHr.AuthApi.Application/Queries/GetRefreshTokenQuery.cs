using CleanHr.AuthApi.Domain.Aggregates;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TanvirArjel.ArgumentChecker;
using TanvirArjel.EFCore.GenericRepository;

namespace CleanHr.AuthApi.Application.Queries;

public sealed class GetRefreshTokenQuery(Guid userId, string token) : IRequest<RefreshToken>
{
    public Guid UserId { get; } = userId.ThrowIfEmpty(nameof(userId));

    public string Token { get; } = token.ThrowIfNullOrEmpty(nameof(token));

    private class GetRefreshTokenQueryHanlder(IRepository repository) : IRequestHandler<GetRefreshTokenQuery, RefreshToken>
    {
        public async Task<RefreshToken> Handle(GetRefreshTokenQuery request, CancellationToken cancellationToken)
        {
            request.ThrowIfNull(nameof(request));

            RefreshToken refreshToken = await repository.GetAsync<RefreshToken>(
                rt => rt.UserId == request.UserId && rt.Token == request.Token,
                cancellationToken);

            return refreshToken;
        }
    }
}
