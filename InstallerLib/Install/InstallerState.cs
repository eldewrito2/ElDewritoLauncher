using InstallerLib.Events;
using InstallerLib.Utility;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InstallerLib.Install
{
    public record InstallerState
    {
        public InstallOperation Operation { get; init; }
        public string InstallerVersion { get; init; } = "";
        public string InstallDirectory { get; init; } = "";
        public int CurrentStep { get; init; }
        public List<IInstallStep> Steps { get; init; } = new List<IInstallStep>();
        public int FailureCount { get; init; }

        public bool IsCompleted => (Steps.Count == 0 || CurrentStep == Steps.Count) && FailureCount == 0;


        // TODO: clean this up

        public static InstallerState Load(string filePath)
        {
            string json = File.ReadAllText(filePath);
            var state = JsonSerializer.Deserialize<InstallerState>(json, CreateJsonSerializerOptions());
            return state;
        }

        public static void Save(string filePath, InstallerState state)
        {
            string json = JsonSerializer.Serialize(state, CreateJsonSerializerOptions());
            File.WriteAllText(filePath, json);
        }

        private static JsonSerializerOptions CreateJsonSerializerOptions()
        {
            var options = new JsonSerializerOptions() { WriteIndented = true };
            options.Converters.Add(new InstallationStepStepJsonConverter());
            options.Converters.Add(new JsonStringEnumConverter());
            options.Converters.Add(new DerivedTypeJsonConverter<IInstallerEvent>(
                Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(IInstallerEvent).IsAssignableFrom(t))));
            return options;
        }
    }
}