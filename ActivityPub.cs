using Microsoft.AspNetCore.Http;

namespace Fediverse;

public class ActivityPub {

    private Func<Context, string, object>? _profileProvider;
    private readonly IDictionary<string, Action<Context, object>> _activityHandlers = new Dictionary<string, Action<Context, object>>();

    private IServiceProvider _services;
    
    public ActivityPub(IServiceProvider services)
    {
        _services = services;
        _profileProvider = null;
    }
    
    internal void SetProfileProvider( Func<Context, string, object> profileProvider) {
        _profileProvider = profileProvider;
    }

    internal void RegisterHandler(string type, Action<Context, object> handler){
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
