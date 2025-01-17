using System.Text.Json.Serialization;
using KristofferStrube.ActivityStreams;
public class CryptographicKey : KristofferStrube.ActivityStreams.Object {
    public CryptographicKey()
    {
        Type = new List<string>() { "CryptographicKey" };
    }

    /// <summary>
    /// The owner of the key
    /// </summary>
    /// <remarks>This is only available as a part of ActivityPub.</remarks>
    [JsonPropertyName("owner")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public IObjectOrLink Owner { get; set; }
    
    /// <summary>
    /// The public key in PEM format
    /// </summary>
    /// <remarks>This is only available as a part of ActivityPub.</remarks>
    [JsonPropertyName("publicKeyPem")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string PublicKeyPem { get; set; }
}

