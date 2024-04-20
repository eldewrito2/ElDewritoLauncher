using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InstallerLib.Install
{
    public class InstallationStepStepJsonConverter : JsonConverter<IInstallStep>
    {
        private int DebugStepCounter = 0;
        public Dictionary<string, Type> RegisteredTypes = new Dictionary<string, Type>();

        public InstallationStepStepJsonConverter()
        {
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(IInstallStep).IsAssignableFrom(t)))
            {
                RegisteredTypes.Add(type.Name, type);
            }
        }

        public void RegisterType<T>()
        {
            RegisteredTypes.Add(typeof(T).Name, typeof(T));
        }

        public override IInstallStep? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
            {
                var root = doc.RootElement;
                if (!root.TryGetProperty("$Type", out var typeProp))
                {
                    throw new JsonException("Unknown type");
                }

                if (RegisteredTypes.TryGetValue(typeProp.GetString()!, out Type? type))
                {
                    return (IInstallStep?)JsonSerializer.Deserialize(root.GetRawText(), type, options);
                }
                else
                {
                    throw new JsonException("Unknown type");
                }
            }
        }

        public override void Write(Utf8JsonWriter writer, IInstallStep value, JsonSerializerOptions options)
        {
            var type = value.GetType();

            writer.WriteStartObject();
            writer.WriteNumber("$Step", DebugStepCounter);
            writer.WriteString("$Type", type.Name);

            DebugStepCounter++;

            // Write the properties of the original object
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