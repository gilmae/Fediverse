using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Fediverse;

public static class ServicesExtensions {
    public static void AddActivityPub(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
        services.AddSingleton(typeof(ActivityPub));
        services.AddTransient(typeof(ActivityPubBuilder));
        services.AddTransient(typeof(Context));
        services.AddTransient(typeof(ActivityHandlerBuilder));
    }
}