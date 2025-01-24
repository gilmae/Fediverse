using AS = KristofferStrube.ActivityStreams;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Text.Json;
using KristofferStrube.ActivityStreams;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace Fediverse;

public class Context
{
    private LinkGenerator _linkGenerator;
    private IServiceProvider _serviceProvider;
    private HttpClient _httpClient;
    private ActivityPub _activityPub;
    private readonly ILogger<Context> _logger;

    public Context(ILogger<Context> logger, ActivityPub activityPub, IServiceProvider serviceProvider, LinkGenerator linkGenerator, IHttpClientFactory httpClientFactory)
    {
        _serviceProvider = serviceProvider;
        _linkGenerator = linkGenerator;
        _httpClient = httpClientFactory.CreateClient("activityPub");
        _activityPub = activityPub;
        _logger = logger;
    }
    public Uri Url() 
    {
        var httpContextAccessor = _serviceProvider.GetService(typeof(IHttpContextAccessor)) as IHttpContextAccessor;
        if (httpContextAccessor == null)
        {
            return null;
        }

        if (httpContextAccessor.HttpContext == null)
        {
            return null;
        }
        return new Uri(httpContextAccessor.HttpContext.Request.Host.ToString());
    }

    public async void SendActivity(IObjectOrLink sender, IObjectOrLink recipient, Activity activity)
    {
        _logger.LogInformation($"Sending activity to {JsonSerializer.Serialize(recipient)}");
        string serialisedData = JsonSerializer.Serialize(activity);
        _logger.LogInformation(serialisedData);

        using HttpContent body = new StringContent(serialisedData, encoding: Encoding.UTF8, mediaType: "application/activity+json");

        string? endpoint = (await GetObject<Actor>(recipient))?.Inbox?.Id;
        if (endpoint == null) {
            return; //false
        }
            
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = body,
        };

        request.Headers.Add("Accept", "application/activity+json");

        // if (signatureContext != null) {
        //     signatureContext.SignRequest(request);
        // }

        using HttpResponseMessage response = await _httpClient.SendAsync(request);
        // var responseStream  = await response.Content.ReadAsStreamAsync();
        // using StreamReader reader = new StreamReader(responseStream);
        // var responseMessage = reader.ReadToEnd();

        response.EnsureSuccessStatusCode();
    }

    public CryptographicKey? GetActorKeyPairs(string identifier) {
        string? owner = GetActorUri(identifier);
        
        if (string.IsNullOrEmpty(owner)) {
            return null;
        }

        var pair = _activityPub.GetKeyPairsFromIdentifier(this, identifier);

        if (pair == null) {
            return null;
        }

        using (RSA publicKey = RSA.Create())
        {
            publicKey.ImportParameters(pair.Item2.Parameters);
            return new CryptographicKey
            {
                Id = owner + "#main-key",
                Owner = new Link { Href = new Uri(owner), JsonLDContext = null },
                PublicKeyPem = publicKey.ExportRSAPublicKeyPem()
            };
        }
    }

    public string? GetActorUri(string identifier)
    {
        var httpContextAccessor = _serviceProvider.GetService(typeof(IHttpContextAccessor)) as IHttpContextAccessor;
        if (httpContextAccessor == null)
        {
            return null;
        }

        if (httpContextAccessor.HttpContext == null)
        {
            return null;
        }

        return _linkGenerator.GetUriByRouteValues(httpContextAccessor.HttpContext, RoutingNames.Profile, new { identifier });
    }

    public string? GetFollowersUri(string identifier) {
       var httpContextAccessor = _serviceProvider.GetService(typeof(IHttpContextAccessor)) as IHttpContextAccessor;
        if (httpContextAccessor == null)
        {
            return null;
        }

        if (httpContextAccessor.HttpContext == null)
        {
            return null;
        }

        return _linkGenerator.GetUriByRouteValues(httpContextAccessor.HttpContext, RoutingNames.Followers, new { identifier });
    }

    public string? GetFollowingUri(string identifier) {
       var httpContextAccessor = _serviceProvider.GetService(typeof(IHttpContextAccessor)) as IHttpContextAccessor;
        if (httpContextAccessor == null)
        {
            return null;
        }

        if (httpContextAccessor.HttpContext == null)
        {
            return null;
        }

        return _linkGenerator.GetUriByRouteValues(httpContextAccessor.HttpContext, RoutingNames.Following, new { identifier });
    }

    public string? GetInboxUri(string identifier) {
        var httpContextAccessor = _serviceProvider.GetService(typeof(IHttpContextAccessor)) as IHttpContextAccessor;
        if (httpContextAccessor == null)
        {
            return null;
        }

        if (httpContextAccessor.HttpContext == null)
        {
            return null;
        }

        return _linkGenerator.GetUriByRouteValues(httpContextAccessor.HttpContext, RoutingNames.Inbox, new { identifier });
    }

    public string? GetOutboxUri(string identifier) {
        var httpContextAccessor = _serviceProvider.GetService(typeof(IHttpContextAccessor)) as IHttpContextAccessor;
        if (httpContextAccessor == null)
        {
            return null;
        }

        if (httpContextAccessor.HttpContext == null)
        {
            return null;
        }

        return _linkGenerator.GetUriByRouteValues(httpContextAccessor.HttpContext, RoutingNames.Outbox, new { identifier });
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