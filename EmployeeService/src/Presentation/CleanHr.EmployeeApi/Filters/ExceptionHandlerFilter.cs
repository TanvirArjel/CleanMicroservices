using System.Text;
using CleanHr.EmployeeApi.Domain.Exceptions;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using TanvirArjel.ArgumentChecker;

namespace CleanHr.EmployeeApi.Filters;

internal sealed class ExceptionHandlerFilter : IAsyncExceptionFilter
{
    private readonly ILogger<ExceptionHandlerFilter> _logger;

    public ExceptionHandlerFilter(ILogger<ExceptionHandlerFilter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task OnExceptionAsync(ExceptionContext context)
    {
        context.ThrowIfNull(nameof(context));

        // DomainValidationException should be treated as a validation error.
        if (context.Exception is DomainValidationException)
        {
            context.ModelState.AddModelError(string.Empty, context.Exception.Message);
            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState));
            return;
        }

        // EntityNotFoundException should be treated as a validation error.
        if (context.Exception is EntityNotFoundException)
        {
            context.ModelState.AddModelError(string.Empty, context.Exception.Message);
            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState));
            return;
        }

        HttpRequest httpRequest = context.HttpContext.Request;
        string requestPath = httpRequest.GetEncodedUrl();

        Dictionary<string, object> loggerContext = new()
        {
            { "RequestPath", requestPath },
        };
        using var _ = _logger.BeginScope(loggerContext);

        try
        {
            httpRequest.Body.Seek(0, SeekOrigin.Begin);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Can't rewind body stream.");
        }

        using StreamReader streamReader = new(httpRequest.Body, Encoding.UTF8);
        string requestBoy = await streamReader.ReadToEndAsync();

        loggerContext.Add("RequestBody", requestBoy);
        _logger.LogCritical(context.Exception, "Error occurred while processing request");

        context.Result = new StatusCodeResult(500);
    }
}
