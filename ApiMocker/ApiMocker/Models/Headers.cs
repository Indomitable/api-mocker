namespace ApiMocker.Models;

public sealed class Headers: List<KeyValuePair<string, string>>
{
    public Headers(IEnumerable<KeyValuePair<string, string>> headers): base(headers)
    {
    }

    public Headers Merge(IDictionary<string, string> overrideHeaders)
    {
        var headers = overrideHeaders.Union(
            this.Where(sh =>
                !overrideHeaders.Any(mh => string.Equals(sh.Key, mh.Key, StringComparison.OrdinalIgnoreCase))));
        return new Headers(headers);
    }
}
