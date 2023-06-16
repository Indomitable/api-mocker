using ApiMocker;
using ApiMocker.Logging;
using Microsoft.AspNetCore.Http.Extensions;

var builder = WebApplication.CreateBuilder();
builder.Logging
    .AddSimpleConsole(o => o.SingleLine = true)
    .SetMinimumLevel(LogLevel.Information)
    .AddFilter((context, level) => context is null || !context.StartsWith("Microsoft"));
builder.WebHost
    .UseKestrel(k => { k.AddServerHeader = false; });

builder.Services.RegisterServices();

var app = builder.Build();
using var configuration = app.Services.GetRequiredService<IConfigurationReader>();
var matcher = app.Services.GetRequiredService<IRequestMatcher>();
var handler = app.Services.GetRequiredService<IRequestHandler>();
var requestsLogger = app.Services.GetRequiredService<IHttpRequestsLogger>();
app.Run(async context =>
{
    var request = context.Request;
    await requestsLogger.LogRequest(request);
    switch (matcher.TryMatch(context))
    {
        case MatchResult.SuccessResult result:
            await handler.Handle(context, result);
            break;
        case MatchResult.AmbiguousMocks ambiguousMocks:
        {
            var message = $"More than one mocks can handle this request! Method: {request.Method}, Request: {request.Path}, Mock Path: {ambiguousMocks.Path}";
            await handler.ErrorHandle(context, message);
            break;
        }
        case MatchResult.NoMatch:
        {
            var message = $"No mocks configured for this request. Method: {request.Method}, Request: {request.GetDisplayUrl()}";
            await handler.ErrorHandle(context, message);
            break;
        }
    }
});

var url = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? configuration.Server.Url;
await app.RunAsync(url);
