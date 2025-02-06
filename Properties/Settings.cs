using System;
using System.Configuration;
using System.IO;
using System.Xml.Linq;

namespace ProgramUpdater.Properties
{
    public class Settings : ApplicationSettingsBase
    {
        private static Settings defaultInstance = ((Settings)(ApplicationSettingsBase.Synchronized(new Settings())));
        private static XDocument userSettings;

        static Settings()
        {
            try
            {
                string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.xml");
                if (File.Exists(settingsPath))
                {
                    userSettings = XDocument.Load(settingsPath);
                }
            }
            catch (Exception)
            {
                userSettings = null;
            }
        }

        public static Settings Default
        {
            get
            {
                return defaultInstance;
            }
        }

        public string WindowTitle
        {
            get
            {
                try
                {
                    if (userSettings != null)
                    {
                        return userSettings.Root
                            ?.Element("UI")
                            ?.Element("WindowTitle")
                            ?.Value ?? "Program Updater";
                    }
                }
                catch (Exception)
                {
                    // Fallback to default if there's any error
                }
                return "Program Updater";
            }
        }

        public string TitleLabelText
        {
            get
            {
                try
                {
                    if (userSettings != null)
                    {
                        return userSettings.Root
                            ?.Element("UI")
                            ?.Element("TitleText")
                            ?.Value ?? "Program Update in Progress";
                    }
                }
                catch (Exception)
                {
                    // Fallback to default if there's any error
                }
                return "Program Update in Progress";
            }
        }
    }
} 