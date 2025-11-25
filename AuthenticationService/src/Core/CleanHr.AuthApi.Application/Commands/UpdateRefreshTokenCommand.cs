using CleanHr.AuthApi.Domain;
using CleanHr.AuthApi.Domain.Aggregates;
using MediatR;
using TanvirArjel.ArgumentChecker;
using TanvirArjel.EFCore.GenericRepository;

namespace CleanHr.AuthApi.Application.Commands;

public sealed class UpdateRefreshTokenCommand(Guid userId, string oldToken, string newToken) : IRequest<Result<RefreshToken>>
{
    public Guid UserId { get; } = userId.ThrowIfEmpty(nameof(userId));

    public string OldToken { get; } = oldToken.ThrowIfNullOrEmpty(nameof(oldToken));

    public string NewToken { get; } = newToken.ThrowIfNullOrEmpty(nameof(newToken));
}

internal class UpdateRefreshTokenCommandHandler(IRepository repository) : IRequestHandler<UpdateRefreshTokenCommand, Result<RefreshToken>>
{
    private readonly IRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

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

        // Revoke the old token
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
