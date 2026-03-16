using System;
using System.IO;
using Newtonsoft.Json;

namespace BelSync
{
    public class AppSettings
    {
        public string Host      { get; set; } = "10.180.27.52";
        public string Port      { get; set; } = "1521";
        public string Service   { get; set; } = "orcl";
        public string AdminUser { get; set; } = "PRM";
        public string Theme     { get; set; } = "Light";
        public string Language  { get; set; } = "English";

        // Last used selections
        public string LastSchema { get; set; } = "";
        public string LastTable  { get; set; } = "";
        public string LastKeyCol { get; set; } = "";

        private static readonly string SettingsPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "BelSync", "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                    return JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(SettingsPath))
                           ?? new AppSettings();
            }
            catch { }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
                File.WriteAllText(SettingsPath, JsonConvert.SerializeObject(this, Formatting.Indented));
            }
            catch { }
        }
    }

    public class Preset
    {
        public string Name   { get; set; }
        public string Schema { get; set; }
        public string Table  { get; set; }
        public string KeyCol { get; set; }
    }
}
