namespace ApiMocker.Config;

public sealed class Request
{
    public string? Path { get; set; }
    public string? Method { get; set; }
    public int? StatusCode { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new ();
    public string? Body { get; set; }
    public string? File { get; set; }
}
