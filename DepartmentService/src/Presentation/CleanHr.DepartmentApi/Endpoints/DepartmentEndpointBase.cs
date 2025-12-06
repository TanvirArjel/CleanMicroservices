using Microsoft.AspNetCore.Mvc;

namespace CleanHr.DepartmentApi.Endpoints;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/departments")]
[ApiController]
////[Authorize]
[ApiExplorerSettings(GroupName = "Department Endpoints")]
public abstract class DepartmentEndpointBase : ControllerBase
{
    protected void AddModelErrorsToModelState(Dictionary<string, string> errors)
    {
        foreach (KeyValuePair<string, string> error in errors)
        {
            ModelState.AddModelError(error.Key, error.Value);
        }
    }
}
