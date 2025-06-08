using KristofferStrube.ActivityStreams;

namespace Fediverse;

internal record CollectionDispatcherSet
{
    internal Func<Context, string, string?, (IEnumerable<IObjectOrLink>?, string?, int?)> Dispatcher { get; set; } = default!;
    internal Func<Context, string, string> FirstCursor { get; set; } = default!;
    internal Func<Context, string, string> LastCursor { get; set; } = default!;
}
