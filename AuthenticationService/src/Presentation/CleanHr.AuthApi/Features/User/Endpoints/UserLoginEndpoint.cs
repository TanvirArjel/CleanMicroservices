using CleanHr.AuthApi.Features.User.Models;
using CleanHr.AuthApi.Application.Commands;
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
        LoginUserCommand command = new(loginModel.EmailOrUserName, loginModel.Password);
        Result<AuthenticationResult> result = await _mediator.Send(command);

        if (result.IsSuccess == false)
        {
            return ValidationProblem(result.Errors);
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
}
