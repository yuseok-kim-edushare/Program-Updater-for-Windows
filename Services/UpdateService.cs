using System;
using System.IO;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Diagnostics;
using Newtonsoft.Json;
using ProgramUpdater.Models;
using ProgramUpdater.Extensions;

namespace ProgramUpdater.Services
{
    public class UpdateService
    {
        private readonly string _configUrl;
        private readonly Action<string, LogLevel> _logCallback;
        private readonly Action<int, string> _progressCallback;
        private readonly HttpClient _httpClient;
        private bool _isCancellationRequested;

        public UpdateService(
            string configUrl,
            Action<string, LogLevel> logCallback,
            Action<int, string> progressCallback)
        {
            _configUrl = configUrl;
            _logCallback = logCallback;
            _progressCallback = progressCallback;
            
            // Configure HTTP client with secure defaults
            var handler = new HttpClientHandler
            {
                // Optional: Configure certificate validation
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                {
                    // You might want to implement proper certificate validation here
                    // For now, we'll use the default validation
                    return sslPolicyErrors == System.Net.Security.SslPolicyErrors.None;
                }
            };

            // Set security protocol to TLS 1.2 and 1.3
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
            
            _httpClient = new HttpClient(handler);
            _httpClient.Timeout = TimeSpan.FromMinutes(30); // Adjust timeout as needed
        }

        public void RequestCancellation()
        {
            _isCancellationRequested = true;
        }

        public async Task<bool> PerformUpdate()
        {
            try
            {
                // Download and parse configuration
                _logCallback("Downloading update configuration...", LogLevel.Info);
                var config = await DownloadConfiguration();
                if (_isCancellationRequested) return false;

                // Calculate total steps for progress
                int totalSteps = config.Files.Count * 3; // Download, Verify, Replace
                int currentStep = 0;

                foreach (var file in config.Files)
                {
                    if (_isCancellationRequested) return false;

                    // Check if main program needs to be stopped
                    if (file.IsExecutable && IsProcessRunning(file.CurrentPath))
                    {
                        _logCallback($"Stopping process: {file.Name}", LogLevel.Info);
                        await StopProcess(file.CurrentPath);
                    }

                    // Download new version
                    _progressCallback((currentStep++ * 100) / totalSteps, $"Downloading {file.Name}...");
                    await DownloadFile(file.DownloadUrl, file.NewPath);
                    if (_isCancellationRequested) return false;

                    // Verify hash
                    _progressCallback((currentStep++ * 100) / totalSteps, $"Verifying {file.Name}...");
                    if (!await VerifyFileHash(file.NewPath, file.ExpectedHash))
                    {
                        throw new Exception($"Hash verification failed for {file.Name}");
                    }
                    if (_isCancellationRequested) return false;

                    // Backup and replace
                    _progressCallback((currentStep++ * 100) / totalSteps, $"Installing {file.Name}...");
                    await BackupAndReplace(file);
                }

                // Start executable if it was running before
                foreach (var file in config.Files)
                {
                    if (file.IsExecutable)
                    {
                        _logCallback($"Starting process: {file.Name}", LogLevel.Info);
                        StartProcess(file.CurrentPath);
                    }
                }

                _progressCallback(100, "Update completed successfully");
                _logCallback("Update process completed successfully", LogLevel.Success);
                return true;
            }
            catch (Exception ex)
            {
                _logCallback($"Update failed: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        private async Task<UpdateConfiguration> DownloadConfiguration()
        {
            var jsonString = await _httpClient.GetStringAsync(_configUrl);
            return JsonConvert.DeserializeObject<UpdateConfiguration>(jsonString);
        }

        private async Task DownloadFile(string url, string destination)
        {
            var uri = new Uri(url);
            if (uri.Scheme == Uri.UriSchemeFtp)
            {
                await DownloadFileViaFtp(uri, destination);
            }
            else
            {
                await DownloadFileViaHttp(url, destination);
            }
        }

        private async Task DownloadFileViaHttp(string url, string destination)
        {
            var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var buffer = new byte[8192];
            var bytesRead = 0L;

            using (var fileStream = File.Create(destination))
            using (var downloadStream = await response.Content.ReadAsStreamAsync())
            {
                while (true)
                {
                    var count = await downloadStream.ReadAsync(buffer, 0, buffer.Length);
                    if (count == 0) break;

                    await fileStream.WriteAsync(buffer, 0, count);
                    bytesRead += count;

                    if (totalBytes > 0)
                    {
                        var percentage = (int)((bytesRead * 100) / totalBytes);
                        _progressCallback(percentage, $"Downloading... {percentage}%");
                    }
                }
            }
        }

        private async Task DownloadFileViaFtp(Uri uri, string destination)
        {
            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            
            // Enable FTPS if the scheme is "ftps"
            if (uri.Scheme.Equals("ftps", StringComparison.OrdinalIgnoreCase))
            {
                request.EnableSsl = true;
                // Optional: Configure SSL/TLS settings
                request.KeepAlive = false; // Recommended for FTPS
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                
                // Optional: Server certificate validation
                ServicePointManager.ServerCertificateValidationCallback = 
                    (sender, certificate, chain, sslPolicyErrors) =>
                    {
                        // You might want to implement proper certificate validation here
                        // For now, we'll accept all certificates
                        return true;
                    };
            }
            
            // If credentials are needed, set them here
            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                var credentials = uri.UserInfo.Split(':');
                request.Credentials = new NetworkCredential(
                    credentials[0], 
                    credentials.Length > 1 ? credentials[1] : string.Empty
                );
            }

            try
            {
                using (var response = (FtpWebResponse)await request.GetResponseAsync())
                using (var responseStream = response.GetResponseStream())
                using (var fileStream = File.Create(destination))
                {
                    var buffer = new byte[8192];
                    var totalBytes = response.ContentLength;
                    var bytesRead = 0L;

                    while (true)
                    {
                        var count = await responseStream.ReadAsync(buffer, 0, buffer.Length);
                        if (count == 0) break;

                        await fileStream.WriteAsync(buffer, 0, count);
                        bytesRead += count;

                        if (totalBytes > 0)
                        {
                            var percentage = (int)((bytesRead * 100) / totalBytes);
                            _progressCallback(percentage, $"Downloading... {percentage}%");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"FTPS download failed: {ex.Message}", ex);
            }
        }

        private async Task<bool> VerifyFileHash(string filePath, string expectedHash)
        {
            SHA256 sha256 = null;
            FileStream stream = null;
            try
            {
                sha256 = SHA256.Create();
                stream = File.OpenRead(filePath);
                var hash = await Task.Run(() => sha256.ComputeHash(stream));
                var actualHash = BitConverter.ToString(hash).Replace("-", "");
                return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (sha256 != null)
                    sha256.Dispose();
                if (stream != null)
                    stream.Dispose();
            }
        }

        private async Task BackupAndReplace(FileConfiguration file)
        {
            try
            {
                if (File.Exists(file.CurrentPath))
                {
                    // Create backup directory if it doesn't exist
                    var backupDir = Path.GetDirectoryName(file.BackupPath);
                    if (!string.IsNullOrEmpty(backupDir))
                    {
                        Directory.CreateDirectory(backupDir);
                    }

                    // Backup existing file
                    if (File.Exists(file.BackupPath))
                    {
                        File.Delete(file.BackupPath);
                    }
                    await Task.Run(() => File.Move(file.CurrentPath, file.BackupPath));
                }

                // Move new file to destination
                await Task.Run(() => File.Move(file.NewPath, file.CurrentPath));
            }
            catch (IOException ex)
            {
                _logCallback($"Error during file backup/replace: {ex.Message}", LogLevel.Error);
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logCallback($"Access denied during file backup/replace: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        private bool IsProcessRunning(string exePath)
        {
            var processName = Path.GetFileNameWithoutExtension(exePath);
            return Process.GetProcessesByName(processName).Length > 0;
        }

        private async Task StopProcess(string exePath)
        {
            var processName = Path.GetFileNameWithoutExtension(exePath);
            var processes = Process.GetProcessesByName(processName);

            foreach (var process in processes)
            {
                process.Kill();
                await Task.Delay(1000); // Give process time to exit
            }
        }

        private void StartProcess(string exePath)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true
            });
        }
    }
} 