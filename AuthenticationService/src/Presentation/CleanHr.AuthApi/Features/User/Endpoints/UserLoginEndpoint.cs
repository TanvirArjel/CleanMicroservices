using CleanHr.AuthApi.Features.User.Models;
using CleanHr.AuthApi.Application.Commands;
using CleanHr.AuthApi.Application.Extensions;
using CleanHr.AuthApi.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using CleanHr.AuthApi.Application.Services;

namespace CleanHr.AuthApi.Features.User.Endpoints;

[ApiVersion("1.0")]
[ApiController]
public class UserLoginEndpoint : UserEndpointBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UserLoginEndpoint> _logger;

    public UserLoginEndpoint(
        IMediator mediator,
        ILogger<UserLoginEndpoint> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesDefaultResponseType]
    [SwaggerOperation(Summary = "Post the required credentials to get the access token for the login.")]
    public async Task<ActionResult<AuthenticationResponse>> Post([FromBody] LoginModel loginModel)
    {
        try
        {
            LoginUserCommand command = new(loginModel.EmailOrUserName, loginModel.Password);
            Result<AuthenticationResult> result = await _mediator.Send(command);

            if (result.IsSuccess == false)
            {
                foreach (KeyValuePair<string, string> error in result.Errors)
                {
                    ModelState.AddModelError(error.Key, error.Value);
                }

                return ValidationProblem(ModelState);
            }

            AuthenticationResponse response = new()
            {
                AccessToken = result.Value.AccessToken,
                RefreshToken = result.Value.RefreshToken,
                ExpiresIn = result.Value.ExpiresIn,
                TokenType = "Bearer"
            };

            return Ok(response);
        }
        catch (Exception exception)
        {
            loginModel.Password = null;
            Dictionary<string, object> fields = new()
            {
                { "LoginModel", loginModel }
            };
            _logger.LogException(exception, "An error occurred while processing the login for {@LoginModel}", fields);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
