namespace Fediverse;

internal static class CollectionDispatcherTypesExtensions
{
    internal static string GetRoutingName(this CollectionDispatcherTypes collectionDispatcherTypes)
    {
        switch (collectionDispatcherTypes)
        {
            case CollectionDispatcherTypes.Following:
                return RoutingNames.Following;
            case CollectionDispatcherTypes.Followers:
                return RoutingNames.Followers;
            case CollectionDispatcherTypes.Outbox:
                return RoutingNames.Outbox;
            default:
                return string.Empty;
        }
    }
}
