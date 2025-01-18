namespace Fediverse;

[Obsolete]
public class ActivityPubBuilder{
    private readonly ActivityPub _activity;

    public ActivityPubBuilder (ActivityPub activity) {
        _activity = activity;
    }
}