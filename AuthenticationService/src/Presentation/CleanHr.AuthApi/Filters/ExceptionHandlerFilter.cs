using System.Text;
using CleanHr.AuthApi.Application.Extensions;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using TanvirArjel.ArgumentChecker;

namespace CleanHr.AuthApi.Filters;

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
        try
        {
            httpRequest.Body.Seek(0, SeekOrigin.Begin);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Can't rewind body stream. " + ex.Message);
        }

        using StreamReader streamReader = new(httpRequest.Body, Encoding.UTF8);
        string requestBody = await streamReader.ReadToEndAsync();
        Dictionary<string, object> fields = new()
        {
            { "RequestPath", requestPath },
            { "RequestBody", requestBody }
        };

        _logger.LogException(context.Exception, $"Error occurred while processing request to {requestPath}", fields);

        context.Result = new StatusCodeResult(500);
    }
}
