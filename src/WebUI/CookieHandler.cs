using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace WebUI;

public class CookieHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        // This is required for ASP.NET Core Identity Cookie Auth as cookie headers aren't sent otherwise
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        return await base.SendAsync(request, cancellationToken);
    }
}
