using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.WebUtilities;
using System.Reflection.Metadata.Ecma335;

namespace Fediverse;

// Call this Artless, as opposedto the subtle library Fedify uses.
public class KeyPairs {
    public static Tuple<RsaSecurityKey, RsaSecurityKey> GenerateCryptoKeyPair(string type) 
    {
        using (var key = RSA.Create()) {
            key.KeySize = 2048;
            RSAParameters parameters = key.ExportParameters(false);
            
            
            return new Tuple<RsaSecurityKey, RsaSecurityKey> (new RsaSecurityKey(key.ExportParameters(true)), new RsaSecurityKey(key.ExportParameters(false)));
        }
    }

    public static JsonWebKey ExportJwk(RsaSecurityKey key) {
        return JsonWebKeyConverter.ConvertFromRSASecurityKey(key);
    }

    public static RsaSecurityKey ImportJwk(JsonWebKey key)
    {
        RSAParameters rsap;
        if (key.HasPrivateKey)
        {
            rsap = new RSAParameters
            {
                Modulus = WebEncoders.Base64UrlDecode(key.N),
                Exponent = WebEncoders.Base64UrlDecode(key.E),
                D = WebEncoders.Base64UrlDecode(key.D),
                P = WebEncoders.Base64UrlDecode(key.P),
                Q = WebEncoders.Base64UrlDecode(key.Q),
                DP = WebEncoders.Base64UrlDecode(key.DP),
                DQ = WebEncoders.Base64UrlDecode(key.DQ),
                InverseQ = WebEncoders.Base64UrlDecode(key.QI)
            };
        }
        else {
            rsap = new RSAParameters
            {
                Modulus = WebEncoders.Base64UrlDecode(key.N),
                Exponent = WebEncoders.Base64UrlDecode(key.E)
            };
        }
        
        RSA rsa = RSA.Create();
        rsa.ImportParameters(rsap);
        return new RsaSecurityKey(rsa);
    }
}