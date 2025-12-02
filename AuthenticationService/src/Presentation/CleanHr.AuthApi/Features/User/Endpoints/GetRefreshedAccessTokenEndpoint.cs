using CleanHr.AuthApi.Features.User.Models;
using CleanHr.AuthApi.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CleanHr.AuthApi.Features.User.Endpoints;

[ApiVersion("1.0")]
public class GetRefreshedAccessTokenEndpoint(
    JwtTokenManager tokenManager) : UserEndpointBase
{
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesDefaultResponseType]
    [SwaggerOperation(Summary = "Get a new access token for user by posting user's expired access token and refresh token.")]
    public async Task<ActionResult<AuthenticationResponse>> Post(TokenRefreshModel model)
    {
        var result = await tokenManager.GetTokenAsync(model.AccessToken, model.RefreshToken);

        if (result.IsSuccess == false)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Value);
    }
}
