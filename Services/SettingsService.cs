using System;
using System.IO;
using System.Xml.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Reflection;
using System.Windows.Forms;
using ProgramUpdater.Extensions;
using System.Linq;

namespace ProgramUpdater.Services
{
    public class SettingsService
    {
        private readonly XDocument _settings;
        private const string DEFAULT_SETTINGS_FILENAME = "settings.xml";
        private readonly Action<string, LogLevel> _logCallback;

        public SettingsService(Action<string, LogLevel> logCallback)
        {
            _logCallback = logCallback ?? throw new ArgumentNullException(nameof(logCallback));
            
            _logCallback("Starting settings.xml search...", LogLevel.Info);
            string settingsPath = FindSettingsFile();
            
            if (string.IsNullOrEmpty(settingsPath))
            {
                _logCallback("Could not find settings.xml in any of the expected locations.", LogLevel.Error);
                throw new FileNotFoundException("Could not find settings.xml in any of the expected locations.");
            }

            try
            {
                _logCallback($"Found settings file at: {settingsPath}", LogLevel.Info);
                _logCallback("Attempting to ensure file access...", LogLevel.Info);
                EnsureFileAccess(settingsPath);
                _logCallback("Loading XML content...", LogLevel.Info);
                
                // Load the XML with explicit namespace handling
                _settings = XDocument.Load(settingsPath);
                
                // Verify the document was loaded correctly
                if (_settings == null)
                {
                    _logCallback("Failed to load XML document - document is null", LogLevel.Error);
                    throw new InvalidOperationException("Failed to load XML document - document is null");
                }
                
                // Log the actual XML content for debugging
                _logCallback($"Loaded XML content: {_settings}", LogLevel.Info);
                
                _logCallback("Successfully loaded settings.xml", LogLevel.Success);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logCallback($"Access denied to settings.xml at {settingsPath}. Error: {ex.Message}", LogLevel.Error);
                throw new InvalidOperationException($"Access denied to settings.xml at {settingsPath}. Error: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logCallback($"Failed to load settings.xml at {settingsPath}: {ex.Message}", LogLevel.Error);
                throw new InvalidOperationException($"Failed to load settings.xml at {settingsPath}: {ex.Message}", ex);
            }
        }

        private string FindSettingsFile()
        {
            try
            {
                string exePath = Assembly.GetExecutingAssembly().Location;
                string exeDir = Path.GetDirectoryName(exePath);
                string currentDir = Environment.CurrentDirectory;
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                _logCallback($"Searching for settings.xml in multiple locations...", LogLevel.Info);

                // List of possible locations to check
                var possiblePaths = new[]
                {
                    // 1. Check the executable directory first
                    Path.Combine(exeDir, DEFAULT_SETTINGS_FILENAME),
                    // 2. Check the current directory
                    Path.Combine(currentDir, DEFAULT_SETTINGS_FILENAME),
                    // 3. Check the BaseDirectory
                    Path.Combine(baseDir, DEFAULT_SETTINGS_FILENAME),
                    // 4. Check one level up from the executable
                    Path.Combine(Path.GetDirectoryName(exeDir) ?? string.Empty, DEFAULT_SETTINGS_FILENAME),
                    // 5. Check the application startup path
                    Path.Combine(Application.StartupPath, DEFAULT_SETTINGS_FILENAME)
                };

                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        try
                        {
                            // Try to open the file to verify we have read access
                            using (File.OpenRead(path))
                            {
                                _logCallback($"Found settings.xml at: {path}", LogLevel.Success);
                                return path;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logCallback($"Found file at {path} but cannot access it: {ex.Message}", LogLevel.Warning);
                        }
                    }
                }

                // If we get here, we've checked all locations and found nothing
                _logCallback("Settings.xml not found in any of the following locations:", LogLevel.Error);
                foreach (var path in possiblePaths)
                {
                    _logCallback($"  - {path}", LogLevel.Info);
                }
            }
            catch (Exception ex)
            {
                _logCallback($"Error during settings file search: {ex.Message}", LogLevel.Error);
            }

            return null;
        }

        private void EnsureFileAccess(string filePath)
        {
            if (!File.Exists(filePath))
            {
                _logCallback($"Settings file not found: {filePath}", LogLevel.Error);
                throw new FileNotFoundException($"Settings file not found: {filePath}");
            }

            try
            {
                // Try to open the file for reading to test access
                using (var fs = File.OpenRead(filePath))
                {
                    _logCallback("Verified read access to settings.xml", LogLevel.Success);
                }
            }
            catch (UnauthorizedAccessException)
            {
                _logCallback("Access denied, attempting to fix permissions...", LogLevel.Warning);
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
                        _logCallback("Successfully updated file permissions", LogLevel.Success);
                    }
                }
                catch (Exception ex)
                {
                    _logCallback($"Failed to update permissions: {ex.Message}", LogLevel.Error);
                    throw new UnauthorizedAccessException(
                        $"Cannot access settings.xml. Please ensure proper permissions: {ex.Message}", ex);
                }
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
                _logCallback($"Attempting to read setting - Section: {section}, Key: {key}", LogLevel.Info);
                
                if (_settings == null)
                {
                    _logCallback("Settings document is null", LogLevel.Error);
                    return null;
                }

                _logCallback($"XML Content: {_settings}", LogLevel.Info);
                
                // Use local name to ignore namespaces
                var settingsElement = _settings.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "Settings");
                    
                if (settingsElement == null)
                {
                    _logCallback("Settings element is null. Available elements: " + 
                        string.Join(", ", _settings.Descendants().Select(e => e.Name.LocalName)), LogLevel.Error);
                    return null;
                }

                var sectionElement = settingsElement.Elements()
                    .FirstOrDefault(e => e.Name.LocalName == section);
                    
                if (sectionElement == null)
                {
                    _logCallback($"Section element '{section}' is null", LogLevel.Error);
                    return null;
                }

                var keyElement = sectionElement.Elements()
                    .FirstOrDefault(e => e.Name.LocalName == key);
                    
                if (keyElement == null)
                {
                    _logCallback($"Key element '{key}' is null", LogLevel.Error);
                    return null;
                }

                var value = keyElement.Value;
                _logCallback($"Successfully read value for {section}/{key}: {value}", LogLevel.Success);
                return value;
            }
            catch (Exception ex)
            {
                _logCallback($"Error reading setting {section}/{key}: {ex.Message}", LogLevel.Error);
                return null;
            }
        }
    }
} 