using CleanHr.AuthApi.Application.Services;
using CleanHr.AuthApi.Domain;
using CleanHr.AuthApi.Domain.Aggregates;
using MediatR;
using Microsoft.AspNetCore.Identity;
using TanvirArjel.ArgumentChecker;
using TanvirArjel.EFCore.GenericRepository;

namespace CleanHr.AuthApi.Application.Commands;

public sealed class LoginUserCommand(string emailOrUserName, string password, bool rememberMe) : IRequest<Result<AuthenticationResult>>
{
    public string EmailOrUserName { get; } = emailOrUserName.ThrowIfNullOrEmpty(nameof(emailOrUserName));

    public string Password { get; } = password.ThrowIfNullOrEmpty(nameof(password));

    public bool RememberMe { get; } = rememberMe;
}

internal class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, Result<AuthenticationResult>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IRepository _repository;
    private readonly JwtTokenManager _jwtTokenManager;

    public LoginUserCommandHandler(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IRepository repository,
        JwtTokenManager jwtTokenManager)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _jwtTokenManager = jwtTokenManager ?? throw new ArgumentNullException(nameof(jwtTokenManager));
    }

    public async Task<Result<AuthenticationResult>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        request.ThrowIfNull(nameof(request));

        ApplicationUser applicationUser = await _userManager.FindByEmailAsync(request.EmailOrUserName);

        applicationUser ??= await _userManager.FindByNameAsync(request.EmailOrUserName);

        if (applicationUser == null)
        {
            return Result<AuthenticationResult>.Failure("EmailOrUserName", "The email or username does not exist.");
        }

        Microsoft.AspNetCore.Identity.SignInResult signInResult = await _signInManager.PasswordSignInAsync(
            request.EmailOrUserName,
            request.Password,
            isPersistent: request.RememberMe,
            lockoutOnFailure: false);

        if (signInResult.Succeeded)
        {
            // Record login using domain method
            applicationUser.RecordLogin();
            _repository.Update(applicationUser);
            await _repository.SaveChangesAsync(cancellationToken);

            // Generate JWT token
            AuthenticationResult authResult = await _jwtTokenManager.GetTokenAsync(applicationUser.Id.ToString());
            return Result<AuthenticationResult>.Success(authResult);
        }

        if (signInResult.IsNotAllowed)
        {
            if (!await _userManager.IsEmailConfirmedAsync(applicationUser))
            {
                return Result<AuthenticationResult>.Failure("EmailOrUserName", "The email is not confirmed yet.");
            }

            if (!await _userManager.IsPhoneNumberConfirmedAsync(applicationUser))
            {
                return Result<AuthenticationResult>.Failure(string.Empty, "The phone number is not confirmed yet.");
            }
        }
        else if (signInResult.IsLockedOut)
        {
            return Result<AuthenticationResult>.Failure(string.Empty, "The account is locked.");
        }
        else if (signInResult.RequiresTwoFactor)
        {
            return Result<AuthenticationResult>.Failure(string.Empty, "Require two factor authentication.");
        }
        else
        {
            return Result<AuthenticationResult>.Failure("Password", "Password is incorrect.");
        }

        return Result<AuthenticationResult>.Failure(string.Empty, "Login failed.");
    }
}
