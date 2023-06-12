using System.Text.RegularExpressions;

namespace ApiMocker.Models;

public sealed record RequestMock
(
    string Path,
    string Method,
    int StatusCode,
    Headers Headers,
    string? Body,
    string? File
)
{
    Regex pathRegex = new(Path);

    public Match Match(string requestPath)
    {
        return pathRegex.Match(requestPath);
    }
}
