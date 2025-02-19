using System.Xml.Serialization;

namespace ProgramUpdater.ConfigManager.Models
{
    [XmlRoot("Settings")]
    public class Settings
    {
        [XmlElement("UI")]
        public UISettings UI { get; set; } = new UISettings();

        [XmlElement("Configuration")]
        public ConfigurationSettings Configuration { get; set; } = new ConfigurationSettings();
    }

    public class UISettings
    {
        [XmlElement("WindowTitle")]
        public string WindowTitle { get; set; } = "Program Updater";

        [XmlElement("TitleText")]
        public string TitleText { get; set; } = "Program Update in Progress";
    }

    public class ConfigurationSettings
    {
        [XmlElement("ConfigurationFilePath")]
        public string ConfigurationFilePath { get; set; } = string.Empty;
    }
} 