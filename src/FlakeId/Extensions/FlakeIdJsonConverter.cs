using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlakeId.Extensions
{
    public class FlakeIdJsonConverter : JsonConverter<Id>
    {
        public override Id Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out long value))
            {
                return new Id(value);
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Invalid JSON token for FlakeId.Id");
            }

            string stringValue = reader.GetString();
            if (stringValue != null && long.TryParse(stringValue, out long parsedValue))
            {
                return new Id(parsedValue);
            }

            throw new JsonException("Invalid JSON token for FlakeId.Id");
        }

        public override void Write(Utf8JsonWriter writer, Id value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }
}
