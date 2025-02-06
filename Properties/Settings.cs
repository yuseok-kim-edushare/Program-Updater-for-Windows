using System;
using System.IO;
using System.Xml.Linq;
using System.Windows.Forms;

namespace ProgramUpdater
{
    public class Settings
    {
        private static readonly Settings _instance = new Settings();
        private static XDocument _userSettings;
        private const string DEFAULT_WINDOW_TITLE = "Program Updater";
        private const string DEFAULT_TITLE_TEXT = "Program Update in Progress";
        public static event Action<string> OnSettingsError;

        private Settings()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.xml");
                if (File.Exists(settingsPath))
                {
                    _userSettings = XDocument.Load(settingsPath);
                }
                else
                {
                    NotifyError($"Settings file not found at: {settingsPath}");
                }
            }
            catch (Exception ex)
            {
                NotifyError($"Failed to load settings: {ex.Message}");
                _userSettings = null;
            }
        }

        private void NotifyError(string message)
        {
            OnSettingsError?.Invoke(message);
            MessageBox.Show(message, "Settings Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static Settings Default => _instance;

        public string WindowTitle
        {
            get
            {
                try
                {
                    if (_userSettings?.Root != null)
                    {
                        var element = _userSettings.Root
                            .Element("UI")
                            ?.Element("WindowTitle");

                        if (element != null)
                        {
                            return element.Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    NotifyError($"Error reading WindowTitle: {ex.Message}");
                }
                return DEFAULT_WINDOW_TITLE;
            }
        }

        public string TitleLabelText
        {
            get
            {
                try
                {
                    if (_userSettings?.Root != null)
                    {
                        var element = _userSettings.Root
                            .Element("UI")
                            ?.Element("TitleText");

                        if (element != null)
                        {
                            return element.Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    NotifyError($"Error reading TitleText: {ex.Message}");
                }
                return DEFAULT_TITLE_TEXT;
            }
        }

        public void Reload()
        {
            LoadSettings();
        }
    }
} 