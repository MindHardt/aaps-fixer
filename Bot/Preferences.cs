using System.Security.Cryptography;
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
            .Where(prop => prop.Contains("PUMP"))
            .ToArray();
        foreach (var prop in pumpProps)
        {
            obj[prop] = "false";
        }

        obj["ConfigBuilder_PUMP_VirtualPumpPlugin_Enabled"] = "true";
        obj["ConfigBuilder_PUMP_VirtualPumpPlugin_Visible"] = "true";
    }
}