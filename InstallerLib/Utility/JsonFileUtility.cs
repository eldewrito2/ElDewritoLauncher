using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace InstallerLib.Utility
{
    public class JsonFileUtility
    {
        public static T? Load<T>(string filePath)
        {
            if (!File.Exists(filePath))
            {
                // File doesn't exist, return default value for the type
                return default;
            }

            string jsonContent = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<T?>(jsonContent);
        }

        public static void Store<T>(string filePath, T obj)
        {
            string jsonContent = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, jsonContent);
        }

        public static async Task<T?> LoadAsync<T>(string filePath)
        {
            if (!File.Exists(filePath))
            {
                // File doesn't exist, return default value for the type
                return default;
            }

            string jsonContent = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
            return JsonSerializer.Deserialize<T?>(jsonContent);
        }

        public static async Task StoreAsync<T>(string filePath, T obj)
        {
            string jsonContent = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, jsonContent).ConfigureAwait(false);
        }
    }
}
