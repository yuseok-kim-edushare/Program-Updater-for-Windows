using System.Collections.Generic;
using Newtonsoft.Json;

namespace ProgramUpdater.Models
{
    public class UpdateConfiguration
    {
        [JsonProperty("files")]
        public List<FileConfiguration> Files { get; set; } = new List<FileConfiguration>();
    }

    public class FileConfiguration
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("isExecutable")]
        public bool IsExecutable { get; set; }

        [JsonProperty("currentPath")]
        public string CurrentPath { get; set; }

        [JsonProperty("newPath")]
        public string NewPath { get; set; }

        [JsonProperty("backupPath")]
        public string BackupPath { get; set; }

        [JsonProperty("downloadUrl")]
        public string DownloadUrl { get; set; }

        [JsonProperty("expectedHash")]
        public string ExpectedHash { get; set; }
    }
} 