using System.Text;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using TanvirArjel.ArgumentChecker;

namespace CleanHr.DepartmentApi.Filters;

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

        HttpRequest httpRequest = context.HttpContext.Request;
        string requestPath = httpRequest.GetEncodedUrl();

        string requestBoy = string.Empty;

        try
        {
            httpRequest.Body.Seek(0, SeekOrigin.Begin);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Can't rewind body stream. " + ex.Message);
        }

        using StreamReader streamReader = new(httpRequest.Body, Encoding.UTF8);
        requestBoy = await streamReader.ReadToEndAsync();

        Dictionary<string, object> fields = new()
        {
            { "RequestPath", requestPath },
            { "RequestBody", requestBoy }
        };

        _logger.LogError(context.Exception, "Error occurred while processing request to {RequestPath}", fields);

        context.Result = new StatusCodeResult(500);
    }
}
