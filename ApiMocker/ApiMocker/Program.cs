using ApiMocker;

var server = new ConfigurationReader().Read();
if (server is null)
{
    return;
}

Console.WriteLine("Mocking requests");
foreach (var mock in server.Mocks)
{
    Console.WriteLine($"{mock.Method} {mock.Path}");
}

var builder = WebApplication.CreateBuilder();
builder.WebHost.UseKestrel(k => { k.AddServerHeader = false; });

var app = builder.Build();

var matcher = new RequestMatcher(server);
var handler = new RequestHandler(server);
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

await app.RunAsync(server.Url);
