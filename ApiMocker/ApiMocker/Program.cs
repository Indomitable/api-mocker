using System.IO.Pipelines;
using System.Text;
using ApiMocker.Models;
using Microsoft.AspNetCore.WebUtilities;
using YamlDotNet.Core;
using YamlDotNet.Serialization.NamingConventions;

var config = File.ReadAllText("config.yaml");
var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
    .WithNamingConvention(CamelCaseNamingConvention.Instance)
    .Build();

MockConfiguration mockConfiguration;
try
{
    mockConfiguration = deserializer.Deserialize<MockConfiguration>(config);
}
catch (YamlException e)
{
    Console.WriteLine($"Unable to deserialize the configuration. Problem location: ({e.Start}) - ({e.End})");
    return;
}
if (!mockConfiguration.Verify())
{
    return;
}

var builder = WebApplication.CreateBuilder(args);

builder.WebHost
    .UseKestrel(k =>
        {
            k.AddServerHeader = false;
        }
    )
    .UseUrls(mockConfiguration.Server.Url);

var app = builder.Build();

foreach (var mock in mockConfiguration.Server.Mocks)
{
    app.MapMethods(mock.Path, new[] {mock.Method}, async context =>
    {
        context.Response.StatusCode = mock.StatusCode;
        var headers = mock.Headers.Union(
                mockConfiguration.Server.Headers.Where(sh =>
                    !mock.Headers.Any(mh => string.Equals(sh.Key, mh.Key, StringComparison.OrdinalIgnoreCase)))
            );
        foreach (var (key, value) in headers)
        {
            context.Response.Headers.Add(key, value);
        }

        if (mock.Body is not null)
        {
            context.Response.ContentLength = Encoding.UTF8.GetByteCount(mock.Body);
            await using var httpWriter = new HttpResponseStreamWriter(context.Response.Body, Encoding.UTF8);
            await httpWriter.WriteAsync(mock.Body);
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
    });
}

app.MapWhen(context =>
{
    var paths = mockConfiguration.Server.Mocks.Select(m => m.Path);
    return paths.All(p => !context.Request.Path.Equals(p));
}, applicationBuilder =>
{
    applicationBuilder.Use(async (context, next) =>
    {
        const string text = "Path is not mocked";
        context.Response.ContentLength = Encoding.UTF8.GetByteCount(text);
        var httpWriter = new HttpResponseStreamWriter(context.Response.Body, Encoding.UTF8);
        await httpWriter.WriteAsync(text);
        await httpWriter.FlushAsync();
        await next(context);
    });
});

await app.RunAsync();
