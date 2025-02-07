using System;
using System.IO;
using System.Xml.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows.Forms;

namespace ProgramUpdater.Services
{
    public class SettingsService
    {
        private readonly XDocument _settings;
        private const string DEFAULT_SETTINGS_FILENAME = "settings.xml";

        public SettingsService()
        {
            try
            {
                string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DEFAULT_SETTINGS_FILENAME);
                EnsureFileAccess(settingsPath);
                _settings = XDocument.Load(settingsPath);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException($"Access denied to settings.xml. Please run the application as administrator. Error: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load settings.xml file: {ex.Message}", ex);
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

        private void EnsureFileAccess(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Settings file not found: {filePath}");
            }

            try
            {
                // Try to open the file for reading to test access
                using (var fs = File.OpenRead(filePath))
                {
                    // File can be read, no need for further action
                }
            }
            catch (UnauthorizedAccessException)
            {
                // If we can't read the file, try to modify its permissions
                try
                {
                    var fileInfo = new FileInfo(filePath);
                    var fileSecurity = fileInfo.GetAccessControl();
                    var currentUser = WindowsIdentity.GetCurrent().User;
                    
                    if (currentUser != null)
                    {
                        fileSecurity.AddAccessRule(new FileSystemAccessRule(
                            currentUser,
                            FileSystemRights.Read,
                            AccessControlType.Allow));
                        fileInfo.SetAccessControl(fileSecurity);
                    }
                }
                catch (Exception ex)
                {
                    throw new UnauthorizedAccessException(
                        $"Cannot access settings.xml. Please ensure the application has proper permissions: {ex.Message}", ex);
                }
            }
        }
    }
} 