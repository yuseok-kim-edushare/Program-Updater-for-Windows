using System;
using System.IO;
using System.Xml.Serialization;
using ProgramUpdater.ConfigManager.Models;

namespace ProgramUpdater.ConfigManager.Services
{
    public class SettingsService
    {
        private readonly string _settingsPath;
        private readonly XmlSerializer _serializer;

        public SettingsService(string settingsPath = "settings.xml")
        {
            _settingsPath = settingsPath;
            _serializer = new XmlSerializer(typeof(Settings));
        }

        public Settings LoadSettings()
        {
            if (!File.Exists(_settingsPath))
            {
                return new Settings();
            }

            using var reader = new StreamReader(_settingsPath);
            return (Settings)_serializer.Deserialize(reader);
        }

        public void SaveSettings(Settings settings)
        {
            using var writer = new StreamWriter(_settingsPath);
            _serializer.Serialize(writer, settings);
        }
    }
} 