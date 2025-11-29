using System.Linq;
using CleanHr.AuthApi.Application.Extensions;
using CleanHr.AuthApi.Application.Services;
using CleanHr.AuthApi.Domain;
using CleanHr.AuthApi.Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TanvirArjel.ArgumentChecker;
using TanvirArjel.EFCore.GenericRepository;

namespace CleanHr.AuthApi.Application.Commands;

public sealed class LoginUserCommand(string emailOrUserName, string password) : IRequest<Result<AuthenticationResult>>
{
    public string EmailOrUserName { get; } = emailOrUserName;

    public string Password { get; } = password;

    private class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, Result<AuthenticationResult>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRepository _repository;
        private readonly JwtTokenManager _jwtTokenManager;
        private readonly ILogger<LoginUserCommandHandler> _logger;

        public LoginUserCommandHandler(
            UserManager<ApplicationUser> userManager,
            IRepository repository,
            JwtTokenManager jwtTokenManager,
            ILogger<LoginUserCommandHandler> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _jwtTokenManager = jwtTokenManager ?? throw new ArgumentNullException(nameof(jwtTokenManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<AuthenticationResult>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                request.ThrowIfNull(nameof(request));

                if (string.IsNullOrWhiteSpace(request.EmailOrUserName))
                {
                    return Result<AuthenticationResult>.Failure("EmailOrUserName", "The email or username is required.");
                }

                if (string.IsNullOrWhiteSpace(request.Password))
                {
                    return Result<AuthenticationResult>.Failure("Password", "The password is required.");
                }

                string normalizedEmailOrUserName = request.EmailOrUserName.ToUpperInvariant();
                ApplicationUser applicationUser = await _userManager.Users
                                                    .Where(u => u.NormalizedEmail == normalizedEmailOrUserName || u.NormalizedUserName == normalizedEmailOrUserName)
                                                    .FirstOrDefaultAsync(cancellationToken);

                if (applicationUser == null)
                {
                    return Result<AuthenticationResult>.Failure("EmailOrUserName", "The email or username does not exist.");
                }

                bool isPasswordValid = await _userManager.CheckPasswordAsync(applicationUser, request.Password);

                if (isPasswordValid)
                {
                    // Record login using domain method
                    applicationUser.RecordLogin();
                    _repository.Update(applicationUser);
                    await _repository.SaveChangesAsync(cancellationToken);

                    // Generate JWT token
                    AuthenticationResult authResult = await _jwtTokenManager.GetTokenAsync(applicationUser.Id.ToString());
                    return Result<AuthenticationResult>.Success(authResult);
                }

                return Result<AuthenticationResult>.Failure("Password", "The password is incorrect.");
            }
            catch (Exception ex)
            {
                var logFields = new Dictionary<string, object>
                {
                    { "EmailOrUserName", request.EmailOrUserName },
                    { "Exception", ex.Message }
                };

                _logger.LogException(ex, "Exception occurred while processing login", logFields);
                return Result<AuthenticationResult>.Failure("Exception", "An error occurred while processing the login.");
            }
        }
    }
}
