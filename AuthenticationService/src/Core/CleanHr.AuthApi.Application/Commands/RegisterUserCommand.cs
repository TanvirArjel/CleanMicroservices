using System.Linq;
using CleanHr.AuthApi.Domain;
using CleanHr.AuthApi.Domain.Models;
using CleanHr.AuthApi.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Identity;
using TanvirArjel.ArgumentChecker;

namespace CleanHr.AuthApi.Application.Commands;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string ConfirmPassword) : IRequest<Result<Guid>>
{
    private class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<Guid>>
    {
        private readonly IApplicationUserRepository _userRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public RegisterUserCommandHandler(
            IApplicationUserRepository userRepository,
            UserManager<ApplicationUser> userManager)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public async Task<Result<Guid>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            request.ThrowIfNull(nameof(request));

            if (request.Password != request.ConfirmPassword)
            {
                return Result<Guid>.Failure("ConfirmPassword", "The password and confirmation password do not match.");
            }

            // Create user through domain factory (with password validation)
            Result<ApplicationUser> result = await ApplicationUser.CreateAsync(
                _userRepository,
                request.Email,
                request.Password,
                request.Email); // Will default to email

            if (result.IsSuccess == false)
            {
                return Result<Guid>.Failure(result.Errors);
            }

            // Use UserManager to handle password hashing and persistence
            IdentityResult identityResult = await _userManager.CreateAsync(result.Value, request.Password);

            if (identityResult.Succeeded == false)
            {
                Dictionary<string, string> errors = identityResult.Errors.ToDictionary(
                    e => e.Code,
                    e => e.Description);
                return Result<Guid>.Failure(errors);
            }

            return Result<Guid>.Success(result.Value.Id);
        }
    }
}
