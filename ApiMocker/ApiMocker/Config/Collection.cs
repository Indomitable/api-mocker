namespace ApiMocker.Config;

public sealed class Collection
{
    public string Include { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public Dictionary<string, string> Headers { get; set; } = new();

    public List<Request> Requests = new();

    public List<Collection> Collections = new();
}
