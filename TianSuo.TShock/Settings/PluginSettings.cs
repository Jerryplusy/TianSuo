using System.IO;
using Newtonsoft.Json;

namespace TianSuo.Settings
{
    public sealed class PluginSettings
    {
        public bool Enabled { get; set; } = true;
        public string ServerName { get; set; } = "TianSuo";
        public string AccessToken { get; set; } = "";
        public string WebSocketHost { get; set; } = "127.0.0.1";
        public int WebSocketPort { get; set; } = 8080;
        public bool SubscribePlayerJoin { get; set; } = true;
        public bool SubscribePlayerQuit { get; set; } = true;
        public bool SubscribePlayerChat { get; set; } = true;
        public bool SubscribePlayerDeath { get; set; } = true;
    }

    public static class SettingsStore
    {
        private const string FileName = "tiansuo.json";

        public static PluginSettings Load(string directory)
        {
            var path = Path.Combine(directory, FileName);
            if (File.Exists(path))
            {
                try
                {
                    return JsonConvert.DeserializeObject<PluginSettings>(File.ReadAllText(path)) ?? new PluginSettings();
                }
                catch
                {
                }
            }

            var settings = new PluginSettings();
            Save(directory, settings);
            return settings;
        }

        public static void Save(string directory, PluginSettings settings)
        {
            var path = Path.Combine(directory, FileName);
            File.WriteAllText(path, JsonConvert.SerializeObject(settings, Formatting.Indented));
        }
    }
}
