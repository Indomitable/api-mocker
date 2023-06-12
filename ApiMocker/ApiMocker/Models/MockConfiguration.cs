using HttpMethod = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod;

namespace ApiMocker.Models;

public sealed class MockConfiguration
{
    public Server Server { get; set; }

    public bool Verify()
    {
        if (!Uri.TryCreate(Server.Url, UriKind.Absolute, out _))
        {
            Console.WriteLine("Provided server url is not valid URI");
            return false;
        }

        foreach (var mock in Server.Mocks)
        {
            if (string.IsNullOrEmpty(mock.Path) || !mock.Path.StartsWith("/"))
            {
                Console.WriteLine($"Request paths must start with '/' and not to be empty. Path: {mock.Path}");
                return false;
            }

            if (!Enum.TryParse<HttpMethod>(mock.Method, true, out _))
            {
                Console.WriteLine($"Invalid http method. Method: {mock.Method}");
                return false;
            }

            if (mock.StatusCode is < 100 or > 599)
            {
                Console.WriteLine($"Request status code should be in range [100..599]. Method: {mock.StatusCode}");
                return false;
            }

            if (!string.IsNullOrEmpty(mock.Body) && !string.IsNullOrEmpty(mock.File))
            {
                Console.WriteLine("Both body and file property have value. Unable to determinate the response body source.");
                return false;
            }

            if (!string.IsNullOrEmpty(mock.File) && !File.Exists(mock.File))
            {
                Console.WriteLine("File source not found.");
                return false;
            }
        }
        return true;
    }
}
