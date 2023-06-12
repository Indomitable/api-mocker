using ApiMocker;

using var configuration = new ConfigurationReader();
var builder = WebApplication.CreateBuilder();
builder.WebHost.UseKestrel(k => { k.AddServerHeader = false; });

var app = builder.Build();

var matcher = new RequestMatcher(configuration.Server);
var handler = new RequestHandler(configuration.Server);
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

await app.RunAsync(configuration.Server.Url);
