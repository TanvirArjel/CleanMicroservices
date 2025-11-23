using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanHr.AuthApi.Features.User.Endpoints;

[Authorize]
[Route("api/v{version:apiVersion}/user")]
[ApiController]
[ApiExplorerSettings(GroupName = "User Endpoints")]
public abstract class UserEndpointBase : ControllerBase
{
}
