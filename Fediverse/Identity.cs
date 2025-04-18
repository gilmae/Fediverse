namespace Fediverse;

public class Identity
{
    public string? PreferredUsername { get; set; }
    public string? Name { get; set; }
    public string? Summary { get; set; }
    public string Url { get; set; }
    public Uri? Icon { get; set; }
    public DateTime? JoinDate { get; set; }
}