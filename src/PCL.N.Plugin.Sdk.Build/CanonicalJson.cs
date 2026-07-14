using System.Text.Encodings.Web;
using System.Text.Json;

internal static class CanonicalJson
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static byte[] Serialize<T>(T value) => Normalize(JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions));

    public static byte[] Normalize(ReadOnlySpan<byte> json)
    {
        using JsonDocument document = JsonDocument.Parse(json.ToArray());
        using MemoryStream stream = new();
        RejectDuplicateProperties(document.RootElement, "$", 0);
        using (Utf8JsonWriter writer = new(stream, new JsonWriterOptions
               {
                   Indented = false,
                   Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
               }))
            Write(writer, document.RootElement);
        return stream.ToArray();
    }

    private static void Write(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (JsonProperty property in element.EnumerateObject().OrderBy(static item => item.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    Write(writer, property.Value);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (JsonElement item in element.EnumerateArray()) Write(writer, item);
                writer.WriteEndArray();
                break;
            case JsonValueKind.String: writer.WriteStringValue(element.GetString()); break;
            case JsonValueKind.Number:
                if (element.TryGetInt64(out long signed)) writer.WriteNumberValue(signed);
                else if (element.TryGetUInt64(out ulong unsigned)) writer.WriteNumberValue(unsigned);
                else writer.WriteNumberValue(element.GetDouble());
                break;
            case JsonValueKind.True: writer.WriteBooleanValue(true); break;
            case JsonValueKind.False: writer.WriteBooleanValue(false); break;
            case JsonValueKind.Null: writer.WriteNullValue(); break;
            default: throw new InvalidOperationException("Unsupported JSON token.");
        }
    }

    private static void RejectDuplicateProperties(JsonElement element, string path, int depth)
    {
        if (depth > 64) throw new InvalidOperationException("JSON nesting exceeds the RFC 8785 limit.");
        if (element.ValueKind == JsonValueKind.Object)
        {
            HashSet<string> names = new(StringComparer.Ordinal);
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                    throw new InvalidOperationException($"JSON contains duplicate property {path}.{property.Name}.");
                RejectDuplicateProperties(property.Value, path + "." + property.Name, depth + 1);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            int index = 0;
            foreach (JsonElement item in element.EnumerateArray())
                RejectDuplicateProperties(item, $"{path}[{index++}]", depth + 1);
        }
    }
}
