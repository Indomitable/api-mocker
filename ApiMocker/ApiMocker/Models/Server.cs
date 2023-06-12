namespace ApiMocker.Models;

public sealed class Server
{
    public string Url { get; set; }

    public Dictionary<string, string> Headers { get; set; } = new();

    public List<RequestMock> Mocks { get; set; }
}
