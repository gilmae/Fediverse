namespace Fediverse;

/// <summary>
/// Builder class for creating Collection Dispatchers
/// </summary>
public class CollectionPaginationBuilder
{
    private readonly ActivityPub _activityPub;

    private CollectionDispatcherTypes _type;

    internal void SetCollectionDispatcherType(CollectionDispatcherTypes type)
    {
        _type = type;
    }

    /// <summary>
    /// Constructs a new CollectionPaginationBuilder object
    /// </summary>
    /// <param name="activityPub"></param>
    public CollectionPaginationBuilder(ActivityPub activityPub)
    {
        _activityPub = activityPub;
    }

    /// <summary>
    /// Sets the function that provides the first cursor value
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    public CollectionPaginationBuilder SetFirstCursor(Func<Context, string, string> action)
    {
        _activityPub.setCollectionFirstCursorDispatcher(_type, action);
        return this;
    }

    /// <summary>
    /// Sets the function that provides the last cursor value
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    public CollectionPaginationBuilder SetLastCursor(Func<Context, string, string> action)
    {
        _activityPub.setCollectionLastCursorDispatcher(_type, action);
        return this;
    }
}