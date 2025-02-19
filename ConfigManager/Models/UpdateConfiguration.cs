using System.Collections.Generic;
using Newtonsoft.Json;

namespace ProgramUpdater.ConfigManager.Models
{
    public class UpdateConfiguration
    {
        [JsonProperty("files")]
        public List<FileConfiguration> Files { get; set; } = new List<FileConfiguration>();
    }

    public class FileConfiguration
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("isExecutable")]
        public bool IsExecutable { get; set; }

        [JsonProperty("currentPath")]
        public string CurrentPath { get; set; } = string.Empty;

        [JsonProperty("newPath")]
        public string NewPath { get; set; } = string.Empty;

        [JsonProperty("backupPath")]
        public string BackupPath { get; set; } = string.Empty;

        [JsonProperty("downloadUrl")]
        public string DownloadUrl { get; set; } = string.Empty;

        [JsonProperty("expectedHash")]
        public string ExpectedHash { get; set; } = string.Empty;
    }
} 