using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using KristofferStrube.ActivityStreams;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

namespace Fediverse;

public static class WebApplicationBuilderExtensions
{
    public static void SetActorDispatcher(this WebApplication app, string pattern, Func<Context, string, Actor?> f)
    {
        var activity = app.Services.GetService(typeof(ActivityPub)) as ActivityPub;
        if (activity == null)
        {
            return;
        }

        activity.SetProfileProvider(f);

        app.MapGet("/.well-known/webfinger", async ([FromQuery] string resource) =>
        {
            var result = await activity.Webfinger(resource);
            
        }).WithName(RoutingNames.Webfinger);

        app.MapGet(pattern, async (string identifier) =>
        {
            return await activity.Profile(identifier);
        }).Produces(200, null, "application/activity+json").WithName(RoutingNames.Profile);

    }

    public static void SetKeyPairsDispatcher(this WebApplication app, Func<Context, string, Tuple<RsaSecurityKey, RsaSecurityKey>> f)
    {
        var activity = app.Services.GetService(typeof(ActivityPub)) as ActivityPub;
        if (activity == null)
        {
            return;
        }
        activity.setKeypairsProvider(f);
    }

    public static ActivityHandlerBuilder? SetInboxListener(this WebApplication app, string pattern)
    {
        var activity = app.Services.GetService(typeof(ActivityPub)) as ActivityPub;
        if (activity == null)
        {
            return null;
        }

        app.MapPost(pattern, (string identifier, [FromBody] JsonDocument message) =>
        {
            activity.Inbox(message);
            return Results.Accepted();
        }).WithName(RoutingNames.Inbox);

        return app.Services.GetService(typeof(ActivityHandlerBuilder)) as ActivityHandlerBuilder;
    }

    public static void SetFollowingDispatcher(this WebApplication app, string pattern, Func<Context, string, string?, Collection> f)
    {
        var activity = app.Services.GetService(typeof(ActivityPub)) as ActivityPub;
        if (activity == null)
        {
            return;
        }

        activity.setFollowingDispatcher(f);

        app.MapGet(pattern, (string identifier, string? cursor = null) =>
        {
            return  Results.Json(activity.Following(identifier, cursor), new JsonSerializerOptions() { }, "application/activity+json", 200);
        }).Produces(200, null, "application/activity+json").WithName(RoutingNames.Following);
    }

    public static void SetFollowersDispatcher(this WebApplication app, string pattern, Func<Context, string, string?, Collection> f)
    {
        var activity = app.Services.GetService(typeof(ActivityPub)) as ActivityPub;
        if (activity == null)
        {
            return;
        }

        activity.setCollectionDispatcher(CollectionDispatcherTypes.Followers, f);

        app.MapGet(pattern, (string identifier, string? cursor = null) =>
        {
            return Results.Json(activity.GetCollection(CollectionDispatcherTypes.Followers, identifier, cursor), new JsonSerializerOptions() { }, "application/activity+json", 200);
        }).WithName(RoutingNames.Followers);
    }

    public static void SetOutboxDispatcher(this WebApplication app, string pattern, Func<Context, string, string?, Collection> f)
    {
        var activity = app.Services.GetService(typeof(ActivityPub)) as ActivityPub;
        if (activity == null)
        {
            return;
        }

        activity.setCollectionDispatcher(CollectionDispatcherTypes.Outbox, f);

        app.MapGet(pattern, (string identifier, string? cursor = null) =>
        {
            return  Results.Json(activity.GetCollection(CollectionDispatcherTypes.Outbox, identifier, cursor), new JsonSerializerOptions() { }, "application/activity+json", 200);
        }).Produces(200, null, "application/activity+json").WithName(RoutingNames.Outbox);
    }
}