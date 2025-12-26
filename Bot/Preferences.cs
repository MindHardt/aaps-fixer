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
        var contentJson = CryptoUtil.Decrypt(masterKey, salt, content.ToString());
        if (contentJson is null)
        {
            throw new InvalidOperationException("Could not decode the content");
        }
        prefs["content"] = JsonNode.Parse(contentJson);
    }
    
    public static void Fix(JsonObject obj)
    {
        foreach (var prop in obj.Select(kvp => kvp.Key).ToArray())
        {
            if (prop.StartsWith("ConfigBuilder_PUMP_"))
            {
                obj[prop] = "false";
            }

            if (prop.StartsWith("ExamTask_"))
            {
                obj[prop] = "true";
            }

            if (prop.StartsWith("apex_"))
            {
                obj.Remove(prop);
            }
        }

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
}