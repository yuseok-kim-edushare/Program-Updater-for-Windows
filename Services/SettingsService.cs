using System;
using System.IO;
using System.Xml.Linq;

namespace ProgramUpdater.Services
{
    public class SettingsService
    {
        private readonly XDocument _settings;

        public SettingsService()
        {
            try
            {
                _settings = XDocument.Load("settings.xml");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to load settings.xml file.", ex);
            }
        }

        public string WindowTitle => GetSettingValue("UI", "WindowTitle") ?? "Program Updater";
        public string TitleText => GetSettingValue("UI", "TitleText") ?? "Program Update in Progress";
        public string ConfigurationFilePath => GetSettingValue("Configuration", "ConfigurationFilePath") ?? 
            "https://raw.githubusercontent.com/yuseok-kim-edushare/Program-Updater-for-Windows/refs/heads/main/example.json";

        private string GetSettingValue(string section, string key)
        {
            try
            {
                return _settings.Root?
                    .Element("Settings")?
                    .Element(section)?
                    .Element(key)?
                    .Value;
            }
            catch
            {
                return null;
            }
        }
    }
} 