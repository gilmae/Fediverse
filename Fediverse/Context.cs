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
    private IServiceProvider _serviceProvider;
    private HttpClient _httpClient;
    private ActivityPub _activityPub;
    private readonly ILogger<Context> _logger;

    public Context(ILogger<Context> logger, ActivityPub activityPub, IServiceProvider serviceProvider, IHttpClientFactory httpClientFactory)
    {
        _serviceProvider = serviceProvider;
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
        var responseStream  = await response.Content.ReadAsStreamAsync();
        using StreamReader reader = new StreamReader(responseStream);
        var responseMessage = reader.ReadToEnd();

        _logger.LogInformation(
            JsonSerializer.Serialize(
                new { response.StatusCode, Response = responseMessage }
            )
        );

        response.EnsureSuccessStatusCode();
    }

    public string? GetActorUri(string identifier)
    {
        return GetLink(RoutingNames.Profile, new { identifier }).ToString();
    }
    
    public string? GetThingUri(string user, string identifier)
    {
        return GetLink(RoutingNames.Thing, new { user, identifier }).ToString();
    }

    public string? GetActivityUri(string user, string identifier)
    {
        return GetLink(RoutingNames.Activity, new { user, identifier }).ToString();
    }

    public string? GetFollowersUri(string identifier)
    {
        return GetLink(RoutingNames.Followers, new { identifier }).ToString();
    }

    public string? GetFollowingUri(string identifier) {
      return GetLink(RoutingNames.Following, new { identifier }).ToString(); 
    }

    public string? GetInboxUri(string identifier) {
      return GetLink(RoutingNames.Inbox, new { identifier }).ToString(); 
    }

    public string? GetOutboxUri(string identifier) {
      return GetLink(RoutingNames.Outbox, new { identifier }).ToString(); 
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

    private Uri GetLink(string routeName, object? routeValues) {
        LinkGenerator? linkGenerator = _serviceProvider.GetService(typeof(LinkGenerator)) as LinkGenerator;
        if (linkGenerator == null) {
            throw new ArgumentNullException(nameof(linkGenerator));
        }

        string? uri = linkGenerator.GetUriByRouteValues(routeName, routeValues, "https", new HostString(_activityPub.GetHost()));

        if (string.IsNullOrEmpty(uri)) {
            throw new ArgumentException(nameof(uri));
        }

        return new Uri(uri);        
    }

}