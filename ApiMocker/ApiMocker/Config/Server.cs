namespace ApiMocker.Config;

public sealed class Server
{
    public string Url { get; set; } = string.Empty;

    public Dictionary<string, string> Headers { get; set; } = new();

    public List<Collection> Collections { get; set; } = new();
}
