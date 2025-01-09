namespace Fediverse;

public class ActivityHandlerBuilder{
    private readonly ActivityPub _activityPub;

    public ActivityHandlerBuilder(ActivityPub activityPub) {
        _activityPub = activityPub;
    }

    public ActivityHandlerBuilder On(string type, Action<Context, object> action) {
        _activityPub.RegisterHandler(type, action);
        return this;
    }
}