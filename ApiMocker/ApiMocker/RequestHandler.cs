using System.IO.Pipelines;
using System.Text;
using ApiMocker.Models;
using Microsoft.AspNetCore.WebUtilities;

namespace ApiMocker;

public sealed class RequestHandler
{
    private readonly MockConfiguration configuration;

    public RequestHandler(MockConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public async Task Handle(HttpContext context, MatchResult.SuccessResult result)
    {
        var mock = result.Mock;
        context.Response.StatusCode = mock.StatusCode;
        var headers = mock.Headers.Union(
            configuration.Server.Headers.Where(sh =>
                !mock.Headers.Any(mh => string.Equals(sh.Key, mh.Key, StringComparison.OrdinalIgnoreCase)))
        );
        foreach (var (key, value) in headers)
        {
            context.Response.Headers.Add(key, value);
        }

        if (!CanWriteResponseBody(mock.StatusCode) && !(string.IsNullOrEmpty(mock.Body) && string.IsNullOrEmpty(mock.File)))
        {
            context.Response.ContentLength = 0;
            context.Response.Headers.Add("x-api-mocker", "Body is not allowed for status codes: 204, 205, or 304");
            await context.Response.CompleteAsync();
            return;
        }

        if (mock.Body is not null)
        {
            var body = mock.Body;
            foreach (var (key, value) in result.PathCaptureGroups)
            {
                var searchArg = $"${{{key}}}";
                if (body.Contains(searchArg))
                {
                    body = body.Replace(searchArg, value);
                }
            }
            context.Response.ContentLength = Encoding.UTF8.GetByteCount(body);
            await using var httpWriter = new HttpResponseStreamWriter(context.Response.Body, Encoding.UTF8);
            await httpWriter.WriteAsync(body);
            await httpWriter.FlushAsync();
        }
        else if (mock.File is not null)
        {
            await using var fileStream = File.Open(mock.File, FileMode.Open, FileAccess.Read);
            context.Response.ContentLength = fileStream.Length;
            // await fileStream.CopyToAsync(context.Response.Body);
            await fileStream.CopyToAsync(context.Response.BodyWriter);
        }

        await context.Response.CompleteAsync();
    }

    // https://source.dot.net/#Microsoft.AspNetCore.Server.Kestrel.Core/Internal/Http/HttpProtocol.cs,1281
    private bool CanWriteResponseBody(int statusCode) => statusCode != StatusCodes.Status204NoContent &&
                                                         statusCode != StatusCodes.Status205ResetContent &&
                                                         statusCode != StatusCodes.Status304NotModified;

    public async Task ErrorHandle(HttpContext context, string message)
    {
        context.Response.ContentLength = 0;
        context.Response.Headers.Add("x-api-mocker", message);
        await context.Response.CompleteAsync();
    }
}
