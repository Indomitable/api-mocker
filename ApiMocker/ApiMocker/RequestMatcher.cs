using System.Text.RegularExpressions;
using ApiMocker.Models;

namespace ApiMocker;

public sealed class RequestMatcher
{
    private readonly MockConfiguration configuration;

    public RequestMatcher(MockConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public MatchResult TryMatch(HttpContext context)
    {
        var request = context.Request;
        if (!request.Path.HasValue)
        {
            var emptyPathMocks = configuration.Server.Mocks.Where(m =>
                m.Path == "/" && string.Equals(m.Method, request.Method, StringComparison.OrdinalIgnoreCase)).ToList();
            return emptyPathMocks switch
            {
                { Count: 1 } => new MatchResult.SuccessResult(emptyPathMocks[0]),
                { Count: > 1} => new MatchResult.AmbiguousMocks(emptyPathMocks[0].Method, emptyPathMocks[0].Path),
                _ => new MatchResult.NoMatch()
            };
        }

        // filter first by http method.
        var sameMethodMocks = configuration.Server.Mocks.Where(m => string.Equals(m.Method, request.Method, StringComparison.OrdinalIgnoreCase)).ToList();
        var samePathMocks = sameMethodMocks
            .Where(m => string.Equals(m.Path, request.Path, StringComparison.OrdinalIgnoreCase)).ToList();
        switch (samePathMocks.Count)
        {
            case 1:
                return new MatchResult.SuccessResult(samePathMocks[0]);
            case > 1:
                return new MatchResult.AmbiguousMocks(samePathMocks[0].Method, sameMethodMocks[0].Path);
        }

        foreach (var mock in sameMethodMocks)
        {
            var regEx = new Regex(mock.Path);
            var match = regEx.Match(request.Path);
            if (match.Success && match.Length == request.Path.Value.Length)
            {
                // find path mock which matches to the full path.
                return new MatchResult.SuccessResult(mock);
            }
        }
        return new MatchResult.NoMatch();
    }
}

public abstract record MatchResult
{
    public record SuccessResult(RequestMock Mock) : MatchResult;

    public record AmbiguousMocks(string Method, string Path) : MatchResult;

    public record NoMatch : MatchResult;
}
