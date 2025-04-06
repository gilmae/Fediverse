using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using KristofferStrube.ActivityStreams;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Runtime.CompilerServices;

namespace Fediverse;

public static class WebApplicationBuilderExtensions
{
    public static void UseActivityPub(this WebApplication app, Action<ActivityPubBuilder> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }
        ActivityPubBuilder? activityPubBuilder = app.Services.GetRequiredService(typeof(ActivityPubBuilder)) as ActivityPubBuilder;
        if (activityPubBuilder == null) {
            throw new ArgumentNullException(nameof(activityPubBuilder));    
        }

        configure(activityPubBuilder);
    }

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
            return await activity.Webfinger(resource);
            
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

    public static CollectionPaginationBuilder? SetFollowingDispatcher(this WebApplication app, string pattern, Func<Context, string, string?, (IEnumerable<IObjectOrLink>?, string?)> f)
    {
        var activity = app.Services.GetService(typeof(ActivityPub)) as ActivityPub;
        if (activity == null)
        {
            return null;
        }

        activity.setCollectionDispatcher(CollectionDispatcherTypes.Following, f);

        app.MapGet(pattern, (string identifier, string? cursor = null) =>
        {
            return Results.Json(activity.GetCollection(CollectionDispatcherTypes.Following, identifier, cursor), new JsonSerializerOptions() { }, "application/activity+json", 200);
        }).Produces(200, null, "application/activity+json").WithName(RoutingNames.Following);

        CollectionPaginationBuilder? builder = app.Services.GetService(typeof(CollectionPaginationBuilder)) as CollectionPaginationBuilder;
        if (builder != null)
        {
            builder.SetCollectionDispatcherType(CollectionDispatcherTypes.Following);
        }
        return builder;
    }

    public static CollectionPaginationBuilder? SetFollowersDispatcher(this WebApplication app, string pattern, Func<Context, string, string?, (IEnumerable<IObjectOrLink>?, string?)> f)
    {
        var activity = app.Services.GetService(typeof(ActivityPub)) as ActivityPub;
        if (activity == null)
        {
            return null;
        }

        activity.setCollectionDispatcher(CollectionDispatcherTypes.Followers, f);

        app.MapGet(pattern, (string identifier, string? cursor = null) =>
        {
            return Results.Json(activity.GetCollection(CollectionDispatcherTypes.Followers, identifier, cursor), new JsonSerializerOptions() { }, "application/activity+json", 200);
        }).WithName(RoutingNames.Followers);

                 CollectionPaginationBuilder? builder = app.Services.GetService(typeof(CollectionPaginationBuilder)) as CollectionPaginationBuilder;
        if (builder != null)
        {
            builder.SetCollectionDispatcherType(CollectionDispatcherTypes.Outbox);
        }
        return builder;
    }

    public static CollectionPaginationBuilder? SetOutboxDispatcher(this WebApplication app, string pattern, Func<Context, string, string?, (IEnumerable<IObjectOrLink>?, string?)> f)
    {
        var activity = app.Services.GetService(typeof(ActivityPub)) as ActivityPub;
        if (activity == null)
        {
            return null;
        }

        activity.setCollectionDispatcher(CollectionDispatcherTypes.Outbox, f);

        app.MapGet(pattern, (string identifier, string? cursor = null) =>
        {
            return Results.Json(activity.GetCollection(CollectionDispatcherTypes.Outbox, identifier, cursor), new JsonSerializerOptions() { }, "application/activity+json", 200);
        }).Produces(200, null, "application/activity+json").WithName(RoutingNames.Outbox);
        
         CollectionPaginationBuilder? builder = app.Services.GetService(typeof(CollectionPaginationBuilder)) as CollectionPaginationBuilder;
        if (builder != null)
        {
            builder.SetCollectionDispatcherType(CollectionDispatcherTypes.Outbox);
        }
        return builder;
    }
}