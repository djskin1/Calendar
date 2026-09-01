using Calendar.Models;
using System.IO;
using System.Text.Json;

namespace Calendar.Services
{
    public static class UserSettingService
    {
        private static readonly string SettingsFolder =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData), "Calendar");

        private static readonly string SettingsFile =
               Path.Combine(
                   SettingsFolder, "user-settings.json");

        public static UserPreferences Load()
        {
            try
            {
                if (!File.Exists(SettingsFile))
                {
                    return new UserPreferences();
                }

                string json =
                    File.ReadAllText(SettingsFile);

                UserPreferences? preferences =
                    JsonSerializer.Deserialize<UserPreferences>(
                        json);

                return preferences
                    ?? new UserPreferences();
            }
            catch
            {
                return new UserPreferences();
            }
        }


        public static void Save(
            UserPreferences preferences)
        {
            Directory.CreateDirectory(
                SettingsFolder);

            string json =
                JsonSerializer.Serialize(
                    preferences,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

            File.WriteAllText(
                SettingsFile,
                json);
        }
    }
}