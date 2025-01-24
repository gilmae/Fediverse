using System.Text.Json.Serialization;
using KristofferStrube.ActivityStreams;
public class CryptographicKey {

    /// <summary>
    /// The owner of the key
    /// </summary>
    /// <remarks>This is only available as a part of ActivityPub.</remarks>
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Id { get; set; }

    /// <summary>
    /// The owner of the key
    /// </summary>
    /// <remarks>This is only available as a part of ActivityPub.</remarks>
    [JsonPropertyName("owner")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Owner { get; set; }
    
    /// <summary>
    /// The public key in PEM format
    /// </summary>
    /// <remarks>This is only available as a part of ActivityPub.</remarks>
    [JsonPropertyName("publicKeyPem")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string PublicKeyPem { get; set; }
}

