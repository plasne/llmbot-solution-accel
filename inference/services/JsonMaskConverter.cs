using Newtonsoft.Json;
using System;

namespace Inference;

public class JsonMaskConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) => objectType == typeof(string);

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) => reader.Value;

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (!string.IsNullOrEmpty(value as string))
        {
            writer.WriteValue("(set)");
        }
        else
        {
            writer.WriteValue("(not set)");
        }
    }
}