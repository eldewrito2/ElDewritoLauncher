using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InstallerLib.Utility
{
    public class DerivedTypeJsonConverter<T> : JsonConverter<T>
    {
        private Dictionary<string, Type> _registeredTypes;

        public DerivedTypeJsonConverter(IEnumerable<Type> types)
        {
            _registeredTypes = types.ToDictionary(k => k.Name, v => v);
        }

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
            {
                var root = doc.RootElement;
                if (!root.TryGetProperty("$Type", out var typeProp))
                {
                    throw new JsonException("Unknown type");
                }

                string? typeName = typeProp.GetString();
                if (typeName == null)
                {
                    throw new JsonException($"Unknown type");
                }

                if (_registeredTypes.TryGetValue(typeName, out Type? type))
                {
                    return (T?)JsonSerializer.Deserialize(root.GetRawText(), type);
                }
                else
                {
                    throw new JsonException($"Unknown type {typeName}");
                }
            }
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                Type type = value.GetType();
                writer.WriteStartObject();
                writer.WriteString("$Type", type.Name);

                foreach (var property in type.GetProperties())
                {
                    var propertyValue = property.GetValue(value);
                    writer.WritePropertyName(property.Name);
                    JsonSerializer.Serialize(writer, propertyValue, property.PropertyType, options);
                }
                writer.WriteEndObject();
            }
        }
    }
}
