using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Fediverse;

public class Context {
    private LinkGenerator _linkGenerator;
    private IServiceProvider _serviceProvider;

    public Context(IServiceProvider serviceProvider, LinkGenerator linkGenerator) {
        _serviceProvider = serviceProvider;
        _linkGenerator = linkGenerator;
    }

    public string? GetActorUri(string identifier) 
    {
        var httpContextAccessor = _serviceProvider.GetService(typeof(IHttpContextAccessor)) as IHttpContextAccessor;
        if (httpContextAccessor == null){ 
            return null;
        }

        if (httpContextAccessor.HttpContext == null)
        {
            return null;
        }

        return _linkGenerator.GetUriByRouteValues(httpContextAccessor.HttpContext, "actorProfile", new { identifier });
    }
}