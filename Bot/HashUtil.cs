using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Bot;

public static partial class HashUtil
{
    public static string CalculateAndSetFileHash(string jsonContent)
    {
        // source: https://github.com/nightscout/AndroidAPS/blob/master/plugins/configuration/src/main/kotlin/app/aaps/plugins/configuration/maintenance/formats/EncryptedPrefsFormat.kt
        const string keyConscience = "if you remove/change this, please make sure you know the consequences!";
        var hash = Hmac256(jsonContent, keyConscience);
        return ToBeCalculatedRegex()
            .Replace(jsonContent, match => $"{match.Groups[1].Value}{hash}{match.Groups[3].Value}");
    }

    private static string Hmac256(string str, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(str));
        return Convert.ToHexStringLower(hashBytes);
    }

    public static JsonObject NormalizeJsonStructure(JsonObject original)
    {
        var normalized = new JsonObject
        {
            ["format"] = original["format"]?.DeepClone(),
            ["metadata"] = new JsonObject
            {
                ["device_name"] = original["metadata"]?["device_name"]?.DeepClone(),
                ["created_at"] = original["metadata"]?["created_at"]?.DeepClone(),
                ["aaps_version"] = original["metadata"]?["aaps_version"]?.DeepClone(),
                ["aaps_flavour"] = original["metadata"]?["aaps_flavour"]?.DeepClone(),
                ["device_model"] = original["metadata"]?["device_model"]?.DeepClone()
            },
            ["security"] = new JsonObject
            {
                ["file_hash"] = "--to-be-calculated--",
                ["algorithm"] = original["security"]?["algorithm"]?.DeepClone()
            },
            ["content"] = original["content"]?.DeepClone().AsObject()!
        };

        return normalized;
    }

    [GeneratedRegex("(\"file_hash\"\\s*:\\s*\")(--to-be-calculated--)(\")")]
    private static partial Regex ToBeCalculatedRegex();
}