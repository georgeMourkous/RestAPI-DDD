using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;

namespace RestApiDdd.Api.Middleware;

public sealed class RequestResponseLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestResponseLoggingMiddleware> logger,
    IOptions<RequestResponseLoggingOptions> options)
{
    private static readonly string[] TextualContentTypeMarkers =
    [
        "application/json",
        "application/problem+json",
        "application/xml",
        "application/x-www-form-urlencoded",
        "text/",
        "+json",
        "+xml"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        if (!options.Value.Enabled)
        {
            await next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var requestBody = await ReadRequestBodyAsync(context.Request, options.Value.MaxBodyLength);

        var originalResponseBody = context.Response.Body;
        await using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();

            var responseBody = await ReadResponseBodyAsync(context.Response, responseBuffer, options.Value.MaxBodyLength);
            responseBuffer.Position = 0;
            await responseBuffer.CopyToAsync(originalResponseBody);
            context.Response.Body = originalResponseBody;

            logger.LogInformation(
                "HTTP {RequestMethod} {RequestPath}{RequestQueryString} responded {StatusCode} in {ElapsedMs:0.0000} ms with request body {RequestBody} and response body {ResponseBody}",
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString.Value,
                context.Response.StatusCode,
                stopwatch.Elapsed.TotalMilliseconds,
                requestBody,
                responseBody);
        }
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request, int maxBodyLength)
    {
        if (!CanCaptureBody(request.ContentType))
        {
            return "[omitted]";
        }

        request.EnableBuffering();
        request.Body.Position = 0;

        using var reader = new StreamReader(request.Body, GetEncoding(request.ContentType), detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        return NormalizeBody(body, maxBodyLength);
    }

    private static async Task<string> ReadResponseBodyAsync(HttpResponse response, MemoryStream responseBuffer, int maxBodyLength)
    {
        if (!CanCaptureBody(response.ContentType))
        {
            return "[omitted]";
        }

        responseBuffer.Position = 0;

        using var reader = new StreamReader(responseBuffer, GetEncoding(response.ContentType), detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        responseBuffer.Position = 0;

        return NormalizeBody(body, maxBodyLength);
    }

    private static bool CanCaptureBody(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        return TextualContentTypeMarkers.Any(marker => contentType.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static Encoding GetEncoding(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return Encoding.UTF8;
        }

        const string charsetPrefix = "charset=";
        var charsetIndex = contentType.IndexOf(charsetPrefix, StringComparison.OrdinalIgnoreCase);
        if (charsetIndex < 0)
        {
            return Encoding.UTF8;
        }

        var charset = contentType[(charsetIndex + charsetPrefix.Length)..].Trim().TrimEnd(';');

        try
        {
            return Encoding.GetEncoding(charset);
        }
        catch (ArgumentException)
        {
            return Encoding.UTF8;
        }
    }

    private static string NormalizeBody(string body, int maxBodyLength)
    {
        if (string.IsNullOrEmpty(body))
        {
            return "[empty]";
        }

        if (body.Length <= maxBodyLength)
        {
            return body;
        }

        return $"{body[..maxBodyLength]}... [truncated]";
    }
}
