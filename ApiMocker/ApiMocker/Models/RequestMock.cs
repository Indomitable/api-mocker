namespace ApiMocker.Models;

public sealed class RequestMock
{
    public string Path { get; set; } = "/";
    public string Method { get; set; } = "GET";
    public int StatusCode { get; set; } = StatusCodes.Status200OK;
    public Dictionary<string, string> Headers { get; set; } = new ();
    public string? Body { get; set; }
    public string? File { get; set; }
}
