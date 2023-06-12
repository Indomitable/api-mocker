using ApiMocker.Config;
using ApiMocker.Models;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Server = ApiMocker.Models.Server;

namespace ApiMocker;

public sealed class ConfigurationReader: IDisposable
{
    private readonly IDeserializer deserializer;
    private readonly string configPath;
    private readonly FileSystemWatcher mainConfigWatcher;
    private readonly List<FileSystemWatcher> collectionsWatchers = new();

    public ConfigurationReader()
    {
        var path = Environment.GetEnvironmentVariable("API_MOCKER_CONFIG") ?? "./config.yaml";
        configPath = Path.GetFullPath(path);
        deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        try
        {
            var config = File.ReadAllText(configPath);
            var configuration = deserializer.Deserialize<Configuration>(config);
            mainConfigWatcher = CreateFileWatcher(configPath);
            Server = new Server
            {
                Url = configuration.Server.Url,
            };
            UpdateServer(configuration, Server);
        }
        catch (YamlException e)
        {
            throw new Exception($"Unable to deserialize the configuration.\nMessage: {e.Message}\nProblem location: ({e.Start}) - ({e.End})");
        }
    }

    public Server Server { get; }

    private void UpdateServerOnChange()
    {
        try
        {
            var config = File.ReadAllText(configPath);
            var configuration = deserializer.Deserialize<Configuration>(config);
            UpdateServer(configuration, Server);
        }
        catch (YamlException e)
        {
            Console.WriteLine($"Unable to deserialize the configuration. Problem location: ({e.Start}) - ({e.End})");
        }
    }

    private void UpdateServer(Configuration configuration, Server server)
    {
        var requests = new List<RequestMock>();
        var context = new RequestsContext(new Headers(configuration.Server.Headers), string.Empty, configPath);
        foreach (var collection in configuration.Server.Collections)
        {
            CollectRequests(collection, requests, context);
        }
        server.Mocks = requests;
        LogMocks(server);
    }

    private void LogMocks(Server server)
    {
        Console.WriteLine("Mocking requests");
        foreach (var mock in server.Mocks)
        {
            Console.WriteLine($"{mock.Method} {mock.Path}");
        }
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
            var data = File.ReadAllText(path);
            var collection = deserializer.Deserialize<CollectionWrapper>(data).Collection;
            collectionsWatchers.Add(CreateFileWatcher(path));
            return string.IsNullOrEmpty(collection.Include)
                ? (collection, path)
                : LoadCollection(collection.Include, path);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Unable to load collection from path: {path}. Message: {e.Message}");
        }
        return null;
    }

    private FileSystemWatcher CreateFileWatcher(string path)
    {
        var directory = Path.GetDirectoryName(path)!;
        var file = Path.GetFileName(path);
        var watcher = new FileSystemWatcher(directory, file);
        watcher.IncludeSubdirectories = false;
        watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite;
        watcher.EnableRaisingEvents = true;
        watcher.Changed += OnConfigFileChanged;
        return watcher;
    }

    private void OnConfigFileChanged(object sender, FileSystemEventArgs args)
    {
        DisposeCollectionWatchers(); // If some collection includes are removed or some are added.
        UpdateServerOnChange();
    }

    private void DisposeCollectionWatchers()
    {
        foreach (var watcher in collectionsWatchers)
        {
            watcher.Changed -= OnConfigFileChanged;
            watcher.Dispose();
        }
        collectionsWatchers.Clear();
    }

    public void Dispose()
    {
        mainConfigWatcher.Changed -= OnConfigFileChanged;
        mainConfigWatcher.Dispose();
        DisposeCollectionWatchers();
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
