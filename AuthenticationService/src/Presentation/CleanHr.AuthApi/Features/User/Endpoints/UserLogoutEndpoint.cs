using System.Security.Claims;
using CleanHr.AuthApi.Application.Commands;
using CleanHr.AuthApi.Domain;
using CleanHr.AuthApi.Features.User.Models;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CleanHr.AuthApi.Features.User.Endpoints;

[ApiVersion("1.0")]
[Authorize]
public class UserLogoutEndpoint(IMediator mediator) : UserEndpointBase
{
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesDefaultResponseType]
    [SwaggerOperation(Summary = "Logout user and revoke refresh token family for this device/session.")]
    public async Task<ActionResult> Post([FromBody] LogoutModel model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.RefreshToken))
        {
            return BadRequest("Refresh token is required.");
        }

        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Revoke the token family (all tokens for this device/session)
        RevokeRefreshTokenFamilyCommand command = new(Guid.Parse(userId), model.RefreshToken);
        Result result = await mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { message = "Logged out successfully. This device's session has been revoked." });
    }
}
