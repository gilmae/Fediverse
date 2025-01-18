using KristofferStrube.ActivityStreams;

namespace Fediverse;

/// <summary>
/// Holding class for collection data. Used to construct an ActivityStream collection
/// </summary>
public class CollectionData
{
    /// <summary>
    /// The items to return. Either a page or the full collection
    /// </summary>
    public IEnumerable<IObjectOrLink> Items { get; set; } = [];
    /// <summary>
    /// Holds the cursor for the next page. Null if there is no next page.
    /// </summary>
    public string? NextCursor { get; set; } = null;
    /// <summary>
    /// Holds the cursor for the previous page. Null if there is no previous page.
    /// </summary>
    public string? PrevCursor { get; set; } = null;
}