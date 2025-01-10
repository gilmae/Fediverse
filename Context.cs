using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Fediverse;

public class Context {
    private LinkGenerator _linkGenerator;
    private IServiceProvider _serviceProvider;
    private HttpClient _httpClient;

    public Context(IServiceProvider serviceProvider, LinkGenerator linkGenerator) {
        _serviceProvider = serviceProvider;
        _linkGenerator = linkGenerator;
        _httpClient = httpClientFactory.CreateClient("activityPub");
    }

    public string? GetActorUri(string identifier) 
    {
        var httpContextAccessor = _serviceProvider.GetService(typeof(IHttpContextAccessor)) as IHttpContextAccessor;
        if (httpContextAccessor == null){ 
            return null;
        }

        if (httpContextAccessor.HttpContext == null)
        {
            return null;
        }

        return _linkGenerator.GetUriByRouteValues(httpContextAccessor.HttpContext, "actorProfile", new { identifier });
    }
    public string GetInboxUri(string identifier) {
        var httpContextAccessor = _serviceProvider.GetService(typeof(IHttpContextAccessor)) as IHttpContextAccessor;
        if (httpContextAccessor == null)
        {
            return null;
        }

        if (httpContextAccessor.HttpContext == null)
        {
            return null;
        }

        return _linkGenerator.GetUriByRouteValues(httpContextAccessor.HttpContext, "inboxEndpoint", new { identifier });
    }
    public async Task<T?> GetObject<T>(AS.IObjectOrLink o)
    {
        T? obj = o switch
        {
            T => (T)o,
            AS.Object => (T)o,
            AS.Link => await GetObject<T>((o as AS.Link)?.Href?.ToString() ?? ""),
            _ => default
        };
        return obj;

    }

    private async Task<T?> GetObject<T>(string iri)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, iri);
        request.Headers.Add("Accept", "application/activity+json");
        using HttpResponseMessage response = await _httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        string body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(body))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(body);
    }
}