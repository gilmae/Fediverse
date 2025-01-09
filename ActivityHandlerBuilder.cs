using KristofferStrube.ActivityStreams;

namespace Fediverse;

public class ActivityHandlerBuilder{
    private readonly ActivityPub _activityPub;

    public ActivityHandlerBuilder(ActivityPub activityPub) {
        _activityPub = activityPub;
    }

    public ActivityHandlerBuilder On(ActivityType type, Action<Context, Activity> action) {
        _activityPub.RegisterHandler(type, action);
        return this;
    }
}