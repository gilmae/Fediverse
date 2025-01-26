namespace Fediverse;

public class ActivityPubBuilder{
    private readonly ActivityPub _activity;

    public ActivityPubBuilder (ActivityPub activity) {
        _activity = activity;
    }

    public ActivityPubBuilder SetHost(string host) {
        _activity.Configure(host);
        return this;
    }
}