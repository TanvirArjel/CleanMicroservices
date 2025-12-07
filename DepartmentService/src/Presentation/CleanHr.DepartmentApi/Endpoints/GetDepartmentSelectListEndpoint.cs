using CleanHr.DepartmentApi.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Swashbuckle.AspNetCore.Annotations;

namespace CleanHr.DepartmentApi.Endpoints;

public sealed class GetDepartmentSelectListEndpoint : DepartmentEndpointBase
{
    private readonly IMediator _mediator;

    public GetDepartmentSelectListEndpoint(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpGet("select-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesDefaultResponseType]
    [SwaggerOperation(Summary = "Get the department select list.")]
    public async Task<ActionResult<SelectList>> Get(Guid? selectedDepartment)
    {
        if (selectedDepartment == Guid.Empty)
        {
            ModelState.AddModelError(nameof(selectedDepartment), $"The value of {nameof(selectedDepartment)} can't be empty.");
            return ValidationProblem(ModelState);
        }

        GetDepartmentListQuery departmentListQuery = new();
        var departmentDtosResult = await _mediator.Send(departmentListQuery, HttpContext.RequestAborted);

        if (departmentDtosResult.IsSuccess)
        {
            SelectList selectList = new(departmentDtosResult.Value, "Id", "Name", selectedDepartment);
            return selectList;
        }

        return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the department select list.");
    }
}
