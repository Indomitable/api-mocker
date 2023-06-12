using System.Text.RegularExpressions;
using ApiMocker.Models;

namespace ApiMocker;

public sealed class RequestMatcher
{
    private readonly Server server;

    public RequestMatcher(Server server)
    {
        this.server = server;
    }

    public MatchResult TryMatch(HttpContext context)
    {
        var request = context.Request;
        var mocks = server.Mocks;
        if (!request.Path.HasValue)
        {
            var emptyPathMocks = mocks.Where(m =>
                m.Path == "/" && string.Equals(m.Method, request.Method, StringComparison.OrdinalIgnoreCase)).ToList();
            return emptyPathMocks switch
            {
                { Count: 1 } => new MatchResult.SuccessResult(emptyPathMocks[0], new Dictionary<string, string>()),
                { Count: > 1} => new MatchResult.AmbiguousMocks(emptyPathMocks[0].Method, emptyPathMocks[0].Path),
                _ => new MatchResult.NoMatch()
            };
        }

        // filter first by http method.
        var sameMethodMocks = mocks.Where(m => string.Equals(m.Method, request.Method, StringComparison.OrdinalIgnoreCase)).ToList();
        var samePathMocks = sameMethodMocks
            .Where(m => string.Equals(m.Path, request.Path, StringComparison.OrdinalIgnoreCase)).ToList();
        switch (samePathMocks.Count)
        {
            case 1:
                return new MatchResult.SuccessResult(samePathMocks[0], new Dictionary<string, string>());
            case > 1:
                return new MatchResult.AmbiguousMocks(samePathMocks[0].Method, sameMethodMocks[0].Path);
        }

        foreach (var mock in sameMethodMocks)
        {
            var match = mock.Match(request.Path);
            if (match.Success && match.Length == request.Path.Value.Length)
            {
                // find path mock which matches to the full path.
                var captures = match.Groups.OfType<Group>()
                    .Skip(1) // first one is the Match
                    .ToDictionary(g => g.Name, g => g.Value);
                return new MatchResult.SuccessResult(mock, captures);
            }
        }
        return new MatchResult.NoMatch();
    }
}

public abstract record MatchResult
{
    public record SuccessResult(RequestMock Mock, Dictionary<string, string> Variables) : MatchResult;

    public record AmbiguousMocks(string Method, string Path) : MatchResult;

    public record NoMatch : MatchResult;
}
