namespace ApiMocker.Models;

public sealed class Server
{
    public string Url { get; set; } = "http://localhost:5000";

    public Dictionary<string, string> Headers { get; set; } = new();

    public List<RequestMock> Mocks { get; set; } = new();
}
