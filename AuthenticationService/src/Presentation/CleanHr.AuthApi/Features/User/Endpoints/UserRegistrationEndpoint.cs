using CleanHr.AuthApi.Features.User.Models;
using CleanHr.AuthApi.Application.Commands;
using CleanHr.AuthApi.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;

namespace CleanHr.AuthApi.Features.User.Endpoints;

[ApiVersion("1.0")]
public class UserRegistrationEndpoint : UserEndpointBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UserRegistrationEndpoint> _logger;

    public UserRegistrationEndpoint(
        IMediator mediator,
        ILogger<UserRegistrationEndpoint> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("registration")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesDefaultResponseType]
    [SwaggerOperation(Summary = "Create or register new user by posting the required data.")]
    public async Task<ActionResult> Post(RegistrationModel model)
    {
        RegisterUserCommand command = new(
            model.Email,
            model.Password,
            model.ConfirmPassword);

        Result<Guid> result = await _mediator.Send(command);

        if (result.IsSuccess == false)
        {
            return ValidationProblem(result.Errors);
        }

        return Ok();
    }
}
