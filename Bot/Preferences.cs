using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bot;

public static class Preferences
{
    public static void Decode(JsonObject prefs, string masterKey)
    {
        var content = prefs["content"]!;
        if (content.GetValueKind() is JsonValueKind.Object)
        {
            return;
        }

        var salt = Convert.FromHexString(prefs["security"]!["salt"]!.ToString());
        var contentJson = CryptoUtil.Decrypt(masterKey, salt, content.ToString())!;
        prefs["content"] = JsonNode.Parse(contentJson);
    }
    
    public static void Fix(JsonObject obj)
    {
        obj["apex_alarm_length"] = "Short";
        obj["apex_battery_type"] = "Custom";
        obj["apex_calc_battery_percentage"] = "true";
        obj["apex_fw_ver"] = "AUTO";
        obj["apex_hide_serial"] = "true";
        obj["apex_high_batt_vtg"] = "1.5";
        obj["apex_log_battery_change"] = "true";
        obj["apex_log_insulin_change"] = "true";
        obj["apex_low_batt_vtg"] = "1.2";
        obj["apex_max_basal"] = "0.0";
        obj["apex_max_bolus"] = "0.0";
        obj["apex_serial_number"] = "";

        var pumpProps = obj
            .Select(x => x.Key)
            .Where(prop => prop.StartsWith("ConfigBuilder_PUMP_"))
            .ToArray();
        foreach (var prop in pumpProps)
        {
            obj[prop] = "false";
        }

        obj["ConfigBuilder_PUMP_VirtualPumpPlugin_Enabled"] = "true";
        obj["ConfigBuilder_PUMP_VirtualPumpPlugin_Visible"] = "true";
    }

    public static void Encode(JsonObject prefs, string masterKey)
    {
        var jsonSalt = prefs["security"]!["salt"]?.GetValue<string?>();
        var saltBytes = jsonSalt is null 
            ? RandomNumberGenerator.GetBytes(32)
            : Convert.FromHexString(jsonSalt);
        var saltString = Convert.ToHexString(saltBytes).ToLowerInvariant();

        prefs["format"] = "aaps_encrypted";
        prefs["security"]!["algorithm"] = "v1";
        prefs["security"]!["salt"] = saltString;
        var prefsContentJson = JsonSerializer.Serialize(prefs["content"], JsonOptions.Compact)
            .Replace("\r\n", "\n");

        prefs["security"]!["content_hash"] = CryptoUtil.Sha256(prefsContentJson).ToLowerInvariant();
        prefs["content"] = CryptoUtil.Encrypt(masterKey, saltBytes, prefsContentJson)!;
    }
    
    private static class JsonOptions
    {
        public static JsonSerializerOptions Intended { get; } = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        
        public static JsonSerializerOptions Compact { get; } = new()
        {
            WriteIndented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }
}