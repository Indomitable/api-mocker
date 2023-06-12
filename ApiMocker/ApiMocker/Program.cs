using ApiMocker;
using ApiMocker.Models;
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
builder.WebHost.UseKestrel(k => { k.AddServerHeader = false; });

var app = builder.Build();

var matcher = new RequestMatcher(mockConfiguration);
var handler = new RequestHandler(mockConfiguration);
app.Run(async context =>
{
    switch (matcher.TryMatch(context))
    {
        case MatchResult.SuccessResult result:
            await handler.Handle(context, result);
            break;
        case MatchResult.AmbiguousMocks ambiguousMocks:
        {
            var message = $"More than one mocks can handle this request! Method: {context.Request.Method}, Request: {context.Request.Path}, Mock Path: {ambiguousMocks.Path}";
            await handler.ErrorHandle(context, message);
            break;
        }
        case MatchResult.NoMatch:
        {
            var message = $"No mocks configured for this request. Method: {context.Request.Method}, Request: {context.Request.Path}";
            await handler.ErrorHandle(context, message);
            break;
        }
    }
});

await app.RunAsync(mockConfiguration.Server.Url);
