using System.IO.Pipelines;
using System.Text;
using ApiMocker.Models;
using Microsoft.AspNetCore.WebUtilities;

namespace ApiMocker;

public sealed class RequestHandler
{
    private readonly Server server;

    public RequestHandler(Server server)
    {
        this.server = server;
    }

    public async Task Handle(HttpContext context, MatchResult.SuccessResult result)
    {
        var mock = result.Mock;
        context.Response.StatusCode = mock.StatusCode;
        foreach (var (key, value) in mock.Headers)
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
            var body = Interpolate(mock.Body, result.Variables);
            context.Response.ContentLength = Encoding.UTF8.GetByteCount(body);
            await using var httpWriter = new HttpResponseStreamWriter(context.Response.Body, Encoding.UTF8);
            await httpWriter.WriteAsync(body);
            await httpWriter.FlushAsync();
        }
        else if (mock.File is not null)
        {
            var file = Interpolate(mock.File, result.Variables);
            await using var fileStream = File.Open(file, FileMode.Open, FileAccess.Read);
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

    private string Interpolate(string template, Dictionary<string, string> variables)
    {
        foreach (var (key, value) in variables)
        {
            var searchArg = $"${{{key}}}";
            if (template.Contains(searchArg))
            {
                template = template.Replace(searchArg, value);
            }
        }
        return template;
    }

    public async Task ErrorHandle(HttpContext context, string message)
    {
        context.Response.ContentLength = 0;
        context.Response.Headers.Add("x-api-mocker", message);
        await context.Response.CompleteAsync();
    }
}
