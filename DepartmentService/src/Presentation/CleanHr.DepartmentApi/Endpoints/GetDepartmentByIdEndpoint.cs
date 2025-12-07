using CleanHr.DepartmentApi.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CleanHr.DepartmentApi.Endpoints;

public sealed class GetDepartmentByIdEndpoint : DepartmentEndpointBase
{
    private readonly IMediator _mediator;

    public GetDepartmentByIdEndpoint(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpGet("{departmentId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesDefaultResponseType]
    [SwaggerOperation(Summary = "Get the details of a department by department id.")]
    public async Task<ActionResult<DepartmentDetailsDto>> GetDepartment([FromRoute] Guid departmentId)
    {
        if (departmentId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(departmentId), $"The value of {nameof(departmentId)} can't be empty.");
            return ValidationProblem(ModelState);
        }

        GetDepartmentByIdQuery query = new(departmentId);

        var departmentDetailsResult = await _mediator.Send(query, HttpContext.RequestAborted);

        if (departmentDetailsResult.IsException)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, departmentDetailsResult.Errors);
        }

        if (departmentDetailsResult.IsSuccess == false)
        {
            AddModelErrorsToModelState(departmentDetailsResult.Errors);
            return ValidationProblem(ModelState);
        }

        if (departmentDetailsResult.Value == null)
        {
            return NotFound();
        }

        return Ok(departmentDetailsResult.Value);
    }
}
