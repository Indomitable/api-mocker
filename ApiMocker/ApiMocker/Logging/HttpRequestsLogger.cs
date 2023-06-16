using Microsoft.AspNetCore.Http.Extensions;

namespace ApiMocker.Logging;

public interface IHttpRequestsLogger
{
    Task LogRequest(HttpRequest request);
}

public class HttpRequestsLogger : IHttpRequestsLogger
{
    public async Task LogRequest(HttpRequest request)
    {
        var textWriter = Console.Out;  
        await textWriter.WriteLineAsync($"{request!.Method} {request.GetDisplayUrl()}");
        await WriteHeaders(textWriter, request);
        await WriteBody(textWriter, request);
    }

    private async Task WriteBody(TextWriter textWriter, HttpRequest request)
    {
        if (request.ContentLength is null or 0)
        {
            return;
        }
        
        if (IsPrintBodySupported(request))
        {
            using var reader = new StreamReader(request.Body);
            var str = await reader.ReadToEndAsync();
            await textWriter.WriteLineAsync(str);
        }
        else
        {
            await textWriter.WriteLineAsync($"Body: {request.ContentLength} bytes.");
        }
    }

    private static async Task WriteHeaders(TextWriter textWriter, HttpRequest request)
    {
        foreach (var header in request.Headers)
        {
            foreach (var value in header.Value)
            {
                await textWriter.WriteLineAsync($"{header.Key}: {value}");
            }
        }
    }

    private bool IsPrintBodySupported(HttpRequest request)
    {
        return request.ContentLength is > 0 and < 1024 &&
               !string.IsNullOrEmpty(request.ContentType) &&
               (request.ContentType.StartsWith("application/json") 
                || request.ContentType.StartsWith("application/xml")
                || request.ContentType.StartsWith("text/")
               );
    }
}
