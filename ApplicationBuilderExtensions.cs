using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using KristofferStrube.ActivityStreams;

namespace Fediverse;

public static class WebApplicationBuilderExtensions {
    public static void SetActorDispatcher(this WebApplication app, string pattern, Func<Context, string, Actor> func) {
        var activity = app.Services.GetService(typeof(ActivityPub)) as ActivityPub;
        if (activity == null) {
            return;
        }
        
        activity.SetProfileProvider(func);
        
        app.MapGet("/.well-known/webfinger", async ([FromQuery] string resource) =>
        {
            return await activity.Webfinger(resource);
        });

        app.MapGet(pattern, async (string identifier) =>
        {
            return await activity.Profile(identifier);
        }).WithName("actorProfile") ;
        
    }

    public static ActivityHandlerBuilder? SetInboxListener(this WebApplication app, string pattern)
    {
        var activity = app.Services.GetService(typeof(ActivityPub)) as ActivityPub;
        if (activity == null) {
            return null;
        }

        app.MapGet(pattern, async (string identifier) =>
        {
            return Results.Accepted();
        }).WithName("inboxEndpoint") ;

        return app.Services.GetService(typeof(ActivityHandlerBuilder)) as ActivityHandlerBuilder;
    }    
}