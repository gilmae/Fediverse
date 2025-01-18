using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using KristofferStrube.ActivityStreams;
using Microsoft.IdentityModel.Tokens;

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
            return await activity.Webfinger(resource);
        });

        app.MapGet(pattern, async (string identifier) =>
        {
            return await activity.Profile(identifier);
        }).WithName("actorProfile");

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

        app.MapPost(pattern, (string identifier) =>
        {
            return Results.Accepted();
        }).WithName("inboxEndpoint");

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
            return activity.Following(identifier, cursor);
        }).WithName("followingEndpoint");
    }
}