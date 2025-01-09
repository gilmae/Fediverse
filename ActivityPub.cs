using Microsoft.AspNetCore.Http;
using AS = KristofferStrube.ActivityStreams;

namespace Fediverse;

public class ActivityPub {

    private Func<Context, string, AS.Actor>? _profileProvider;
    private readonly IDictionary<ActivityType, Action<Context, AS.Activity>> _activityHandlers = new Dictionary<ActivityType, Action<Context, AS.Activity>>();

    private IServiceProvider _services;
    
    public ActivityPub(IServiceProvider services)
    {
        _services = services;
        _profileProvider = null;
    }
    
    internal void SetProfileProvider( Func<Context, string, AS.Actor> profileProvider) {
        _profileProvider = profileProvider;
    }

    internal void RegisterHandler(ActivityType type, Action<Context, AS.Activity> handler){
        _activityHandlers[type] = handler;
    }

    internal async Task<IResult> Webfinger(string resource) {
        if (resource.StartsWith("acct:")) {
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

    internal async Task<IResult> Profile(string resource) {
        Context ctx = _services.GetService(typeof(Context)) as Context;
        return Results.Json(_profileProvider.Invoke(ctx, resource));
    }
}
