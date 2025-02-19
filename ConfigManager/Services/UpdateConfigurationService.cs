using System;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;
using ProgramUpdater.ConfigManager.Models;

namespace ProgramUpdater.ConfigManager.Services
{
    public class UpdateConfigurationService
    {
        private readonly string _configPath;

        public UpdateConfigurationService(string configPath = "example.json")
        {
            _configPath = configPath;
        }

        public UpdateConfiguration LoadConfiguration()
        {
            if (!File.Exists(_configPath))
            {
                return new UpdateConfiguration();
            }

            var json = File.ReadAllText(_configPath);
            return JsonConvert.DeserializeObject<UpdateConfiguration>(json) ?? new UpdateConfiguration();
        }

        public void SaveConfiguration(UpdateConfiguration config)
        {
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(_configPath, json);
        }

        public string CalculateFileHash(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "");
        }

        public void UpdateFileHash(FileConfiguration file)
        {
            if (File.Exists(file.CurrentPath))
            {
                file.ExpectedHash = CalculateFileHash(file.CurrentPath);
            }
        }
    }
} 