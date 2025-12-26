using System.Text.Encodings.Web;
using System.Text.Json;

namespace Bot;

public static class JsonOptions
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