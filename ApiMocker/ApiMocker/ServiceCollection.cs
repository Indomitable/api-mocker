using ApiMocker.Logging;

namespace ApiMocker;

public static class ServiceCollection
{
    public static void RegisterServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IConfigurationReader, ConfigurationReader>();
        serviceCollection.AddSingleton<IHttpRequestsLogger, HttpRequestsLogger>();
        serviceCollection.AddSingleton<IRequestMatcher, RequestMatcher>();
        serviceCollection.AddSingleton<IRequestHandler, RequestHandler>();
    }
}