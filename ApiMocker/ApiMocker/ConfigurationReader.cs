using ApiMocker.Config;
using ApiMocker.Models;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Server = ApiMocker.Models.Server;

namespace ApiMocker;

public sealed class ConfigurationReader
{
    private readonly IDeserializer deserializer;
    private readonly string configPath;

    public ConfigurationReader()
    {
        var path = Environment.GetEnvironmentVariable("API_MOCKER_CONFIG") ?? "./config.yaml";
        configPath = Path.GetFullPath(path);
        deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    public Server? Read()
    {
        try
        {
            var config = File.ReadAllText(configPath);
            var configuration = deserializer.Deserialize<Configuration>(config);
            return BuildServer(configuration, configPath);
        }
        catch (YamlException e)
        {
            Console.WriteLine($"Unable to deserialize the configuration. Problem location: ({e.Start}) - ({e.End})");
        }
        return null;
    }

    private Server BuildServer(Configuration configuration, string configPath)
    {
        var server = new Server
        {
            Url = configuration.Server.Url,
        };
        var requests = new List<RequestMock>();
        var context = new RequestsContext(new Headers(configuration.Server.Headers), string.Empty, configPath);
        foreach (var collection in configuration.Server.Collections)
        {
            CollectRequests(collection, requests, context);
        }
        server.Mocks = requests;
        return server;
    }

    private void CollectRequests(Collection collection, List<RequestMock> bucket, RequestsContext context)
    {
        if (!string.IsNullOrEmpty(collection.Include))
        {
            var result = LoadCollection(context.FilePath, collection.Include);
            if (!result.HasValue)
            {
                return;
            }
            var (col, path) = result.Value;
            collection = col;
            context = context with { FilePath = path };
        }

        context = context.ForCollection(collection);
        foreach (var request in collection.Requests)
        {
            var requestMock = BuildRequest(request, context);
            bucket.Add(requestMock);
        }

        foreach (var subCollection in collection.Collections)
        {
            CollectRequests(subCollection, bucket, context);
        }
    }

    private RequestMock BuildRequest(Request request, RequestsContext context)
    {
        string path = request.Path is null
            ? context.Path
            : request.Path.StartsWith("/")
                ? request.Path // absolute path override current
                : string.Concat(context.Path, "/", request.Path);
        var headers = context.Headers.Merge(request.Headers);
        var file = request.File is null
            ? null
            : Path.GetFullPath(request.File, Path.GetDirectoryName(context.FilePath)!);
        return new RequestMock
        (
            path,
            request.Method ?? "GET",
            request.StatusCode ?? StatusCodes.Status200OK,
            headers,
            request.Body,
            file
        );
    }

    private (Collection collection, string path)? LoadCollection(string sourcePath, string collectionPath)
    {
        var path = Path.GetFullPath(collectionPath, Path.GetDirectoryName(sourcePath)!);
        try
        {
            var collection = deserializer.Deserialize<CollectionWrapper>(File.ReadAllText(path)).Collection;
            if (!string.IsNullOrEmpty(collection.Include))
            {
                return LoadCollection(collection.Include, path);
            }

            return (collection, path);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Unable to load collection from path: {path}. Message: {e.Message}");
        }
        return null;
    }
}


record RequestsContext(Headers Headers, string Path, string FilePath)
{
    public RequestsContext ForCollection(Collection collection)
    {
        string path = collection.Path.StartsWith("/")
            ? collection.Path // absolute path override current
            : string.Concat(Path, "/", collection.Path.TrimEnd('/')); // append colleciton path to the current path.
        var headers = Headers.Merge(collection.Headers);

        return new RequestsContext(headers, path, FilePath);
    }
}
