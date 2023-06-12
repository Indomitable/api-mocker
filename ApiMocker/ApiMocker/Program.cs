using System.Text;
using ApiMocker.Models;
using Microsoft.AspNetCore.WebUtilities;
using YamlDotNet.Serialization.NamingConventions;

var text = File.ReadAllText("config.yaml");
var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
    .WithNamingConvention(CamelCaseNamingConvention.Instance)
    .Build();

var mockConfiguration = deserializer.Deserialize<MockConfiguration>(text);

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
        foreach (var (key, value) in mockConfiguration.Server.Headers.Union(mock.Headers))
        {
            context.Response.Headers.Add(key, value);
        }

        context.Response.ContentLength = Encoding.UTF8.GetByteCount(mock.Body);
        var httpWriter = new HttpResponseStreamWriter(context.Response.Body, Encoding.UTF8);
        await httpWriter.WriteAsync(mock.Body);
        await httpWriter.FlushAsync();
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
