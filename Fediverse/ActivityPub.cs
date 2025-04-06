using System.Runtime.CompilerServices;
using System.Text.Json;
using KristofferStrube.ActivityStreams;
using KristofferStrube.ActivityStreams.JsonLD;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using AS = KristofferStrube.ActivityStreams;

namespace Fediverse;

public class ActivityPub
{
    private string _host = default!;
    private Func<Context, string, AS.Actor?>? _profileProvider;
    private Dictionary<CollectionDispatcherTypes, CollectionDispatcherSet> _collectionDispatchers;

    private Func<Context, string, Tuple<RsaSecurityKey, RsaSecurityKey>>? _keyPairsProvider;
    private readonly IDictionary<ActivityType, Action<Context, AS.Activity>> _activityHandlers = new Dictionary<ActivityType, Action<Context, AS.Activity>>();

    private readonly ILogger<ActivityPub> _logger;

    private IServiceProvider _services;

    public ActivityPub(IServiceProvider services, ILogger<ActivityPub> logger)
    {
        _services = services;
        _profileProvider = null;
        _collectionDispatchers = new Dictionary<CollectionDispatcherTypes, CollectionDispatcherSet>();
        _logger = logger;
    }

    internal void Configure(string host)
    {
        _host = host;
    }

    internal void SetProfileProvider(Func<Context, string, AS.Actor?> profileProvider)
    {
        _profileProvider = profileProvider;
    }

    internal void setKeypairsProvider(Func<Context, string, Tuple<RsaSecurityKey, RsaSecurityKey>> keypairsProvider)
    {
        _keyPairsProvider = keypairsProvider;
    }

    internal void setCollectionDispatcher(CollectionDispatcherTypes type, Func<Context, string, string?, (IEnumerable<IObjectOrLink>?, string?)> f)
    {
        CollectionDispatcherSet collectionDispatcher;
        if (_collectionDispatchers.ContainsKey(type))
        {
            collectionDispatcher = _collectionDispatchers[type];
        }
        else
        {
            collectionDispatcher = new CollectionDispatcherSet();
        }
        collectionDispatcher.Dispatcher = f;
        _collectionDispatchers[type] = collectionDispatcher;
    }

    internal void setCollectionFirstCursorDispatcher(CollectionDispatcherTypes type, Func<Context, string, string> f)
    {
        CollectionDispatcherSet collectionDispatcher;
        if (_collectionDispatchers.ContainsKey(type))
        {
            collectionDispatcher = _collectionDispatchers[type];
        }
        else
        {
            collectionDispatcher = new CollectionDispatcherSet();
        }
        collectionDispatcher.FirstCursor = f;
        _collectionDispatchers[type] = collectionDispatcher;
    }

    internal void setCollectionLastCursorDispatcher(CollectionDispatcherTypes type, Func<Context, string, string> f)
    {
        CollectionDispatcherSet collectionDispatcher;
        if (_collectionDispatchers.ContainsKey(type))
        {
            collectionDispatcher = _collectionDispatchers[type];
        }
        else
        {
            collectionDispatcher = new CollectionDispatcherSet();
        }
        collectionDispatcher.LastCursor = f;
        _collectionDispatchers[type] = collectionDispatcher;
    }

    internal string? GetHost()
    {
        return _host;
    }

    private Uri GetLink(string routeName, object? routeValues)
    {
        LinkGenerator? linkGenerator = _services.GetService(typeof(LinkGenerator)) as LinkGenerator;
        if (linkGenerator == null)
        {
            throw new ArgumentNullException(nameof(linkGenerator));
        }

        string? uri = linkGenerator.GetUriByRouteValues(routeName, routeValues, "https", new HostString(_host));

        if (string.IsNullOrEmpty(uri))
        {
            throw new ArgumentException(nameof(uri));
        }

        return new Uri(uri);
    }

    private string? ActorProfileLink(string identifier)
    {
        return GetLink(RoutingNames.Profile, new { identifier }).ToString();
    }

    internal void RegisterHandler(ActivityType type, Action<Context, AS.Activity> handler)
    {
        _activityHandlers[type] = handler;
    }

    internal async Task<IResult> Webfinger(string resource)
    {
        string username = resource;

        if (resource.StartsWith("acct:"))
        {
            username = resource.Substring(5);
        }

        username = username.Split("@")[0];
        var actor = ActorProfileLink(username);
        if (actor == null)
        {
            return Results.NotFound();
        }
        var profile = new
        {
            subject = $"{username}@{GetHost()}",
            aliases = new[] { actor },
            links = new[] {
                    new {
                        rel="self",
                        href=actor,
                        type="application/activity+json"
                    }
                }
        };
        return Results.Json(profile);
    }

    internal async Task<IResult> Profile(string resource)
    {
        Context ctx = _services.GetService(typeof(Context)) as Context;
        if (ctx == null)
        {
            return Results.StatusCode(500);
        }
        if (_profileProvider == null)
        {
            return Results.StatusCode(500);
        }

        var profile = _profileProvider.Invoke(ctx, resource);
        profile.JsonLDContext = new List<ReferenceTermDefinition> {
            new(new("https://www.w3.org/ns/activitystreams")),
            new(new("https://w3id.org/security/v1"))
        };

        return Results.Json(profile, new JsonSerializerOptions() { }, "application/activity+json", 200);
    }

    internal IResult Inbox(JsonDocument message)
    {
        _logger.LogInformation("Received inbox message");
        _logger.LogInformation(JsonSerializer.Serialize(message));

        if (message == null)
        {
            return Results.BadRequest();
        }

        Context? ctx = _services.GetService(typeof(Context)) as Context;
        if (ctx == null)
        {
            return Results.BadRequest();
        }

        JsonElement? activityType = message.RootElement.GetProperty("type");
        // Try the @type property instead
        if (activityType == null)
        {
            activityType = message.RootElement.GetProperty("@type");
        }

        if (activityType == null)
        {
            return Results.BadRequest();
        }

        IEnumerable<string> activityTypeValues = activityType.Value.ValueKind switch
        {
            JsonValueKind.Array => activityType.Value.EnumerateArray().Select(i => i.GetString() ?? ""),
            JsonValueKind.String => [activityType.Value.GetString() ?? ""],
            _ => [""]
        };

        foreach (string type in activityTypeValues)
        {
            ActivityType parsed;
            if (Enum.TryParse<ActivityType>(type, out parsed))
            {
                Type? activityMessageType = Type.GetType($"KristofferStrube.ActivityStreams.{type}, KristofferStrube.ActivityStreams", false, true);
                if (activityMessageType == null)
                {
                    continue;
                }
                _activityHandlers[parsed].Invoke(ctx, (AS.Activity)JsonSerializer.Deserialize(message, activityMessageType));
            }
        }

        return Results.Ok();
    }

    internal Tuple<RsaSecurityKey, RsaSecurityKey>? GetKeyPairsFromIdentifier(Context ctx, string identifier)
    {
        if (_keyPairsProvider == null)
        {
            return null;
        }
        return _keyPairsProvider.Invoke(ctx, identifier);
    }

    internal Collection? GetCollection(CollectionDispatcherTypes type, string identifier, string? cursor = null)
    {
        Context? ctx = _services.GetService(typeof(Context)) as Context;

        if (ctx == null)
        {
            return null;
        }
        CollectionDispatcherSet? collectionDispatcher = null;
        if (!_collectionDispatchers.TryGetValue(type, out  collectionDispatcher))
        {
            return null;
        }
         
        (IEnumerable<IObjectOrLink>? data, string? nextPage) = collectionDispatcher.Dispatcher.Invoke(ctx, identifier, cursor);

        if (data == null)
        {
            var orderedCollection = new OrderedCollection();
            if (collectionDispatcher.FirstCursor != null)
            {
                string firstCursor = collectionDispatcher.FirstCursor.Invoke(ctx, identifier);
                orderedCollection.First = new Link { Href = new Uri(GetLink(type.GetRoutingName(), new { identifier, cursor=firstCursor }).ToString()) };
            }
            else
            {
                orderedCollection.Items = [];
            }
            return orderedCollection;
        }

        var collection = new CollectionPage
        {
            Items = data,

        };
        if (data.Count() > 0)
        {
            collection.Next = new Link { Href = new Uri(GetLink(type.GetRoutingName(), new { identifier, cursor }).ToString()) };
        }
        return collection;
    }
}
