using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;
namespace TelegramBotWebhook;
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        // Log request
        context.Request.EnableBuffering();
        var requestBody = await ReadStreamAsync(context.Request.Body);
        _logger.LogInformation($"Incoming Request: {context.Request.Method} {context.Request.Path} {requestBody}");

        // Replace the request stream with a new stream
        var requestStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        context.Request.Body = requestStream;

        // Create a new memory stream to capture the response
        var originalResponseBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        await _next(context);

        // Log response
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        _logger.LogInformation($"Outgoing Response: {context.Response.StatusCode} {responseBody}");

        // Copy the response stream to the original stream
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        await responseBodyStream.CopyToAsync(originalResponseBodyStream);
    }

    private async Task<string> ReadStreamAsync(Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        var text = await new StreamReader(stream).ReadToEndAsync();
        stream.Seek(0, SeekOrigin.Begin);
        return text;
    }
}
