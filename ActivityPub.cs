using System.Text.Json;
using KristofferStrube.ActivityStreams;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using AS = KristofferStrube.ActivityStreams;

namespace Fediverse;

public class ActivityPub
{
    private Func<Context, string, AS.Actor?>? _profileProvider;
    private Func<Context, string, Tuple<RsaSecurityKey, RsaSecurityKey>>? _keyPairsProvider;
    private readonly IDictionary<ActivityType, Action<Context, AS.Activity>> _activityHandlers = new Dictionary<ActivityType, Action<Context, AS.Activity>>();

    private IServiceProvider _services;

    public ActivityPub(IHttpClientFactory httpClientFactory, IServiceProvider services)
    {
        _services = services;
        _profileProvider = null;
    }

    internal void SetProfileProvider(Func<Context, string, AS.Actor> profileProvider)
    {
        _profileProvider = profileProvider;
    }

    internal void setKeypairsProvider(Func<Context, string, Tuple<RsaSecurityKey,RsaSecurityKey>> keypairsProvider) {
        _keyPairsProvider = keypairsProvider;
    }

    internal void RegisterHandler(ActivityType type, Action<Context, AS.Activity> handler)
    {
        _activityHandlers[type] = handler;
    }

    internal async Task<IResult> Webfinger(string resource)
    {
        if (resource.StartsWith("acct:"))
        {
            return Results.Json(new
            {
                subject = resource,
                aliases = new[] { "" },
                links = new[] {
                    new {
                        rel="self",
                        href="",
                        type="application/activity+json"
                    }
                }
            });
        }
        throw new NotImplementedException();
    }

    internal async Task<IResult> Profile(string resource)
    {
        Context ctx = _services.GetService(typeof(Context)) as Context;
        if (ctx == null) {
            return Results.StatusCode(500);
        }
        if (_profileProvider == null) {
            return Results.StatusCode(500);
        }

        return Results.Json(_profileProvider.Invoke(ctx, resource));
    }

    internal async Task<IResult> Inbox(JsonDocument message)
    {
        if (message == null)
        {
            return Results.BadRequest();
        }
        
        Context ctx = _services.GetService(typeof(Context)) as Context;

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
                Type? activityMessageType = Type.GetType($"KristofferStrube.ActivityStreams.{type}", false, true);
                if (activityMessageType == null) {
                    continue;
                }
                _activityHandlers[parsed].Invoke(ctx, (AS.Activity)JsonSerializer.Deserialize(message, activityMessageType));
            }
        }

        return Results.Ok();
    }

    internal Tuple<RsaSecurityKey, RsaSecurityKey>? GetKeyPairsFromIdentifier(Context ctx, string identifier) {
        if (_keyPairsProvider == null) {
            return null;
        }
        return _keyPairsProvider.Invoke(ctx, identifier);
    }
}
