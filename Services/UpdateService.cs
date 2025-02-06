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
    public class UpdateService : IDisposable
    {
        private readonly string _configUrl;
        private readonly Action<string, LogLevel> _logCallback;
        private readonly Action<int, string> _progressCallback;
        private readonly IHttpClientFactory _httpClientFactory;
        private bool _isCancellationRequested;

        public UpdateService(
            string configUrl,
            Action<string, LogLevel> logCallback,
            Action<int, string> progressCallback,
            IHttpClientFactory httpClientFactory)
        {
            _configUrl = configUrl;
            _logCallback = logCallback;
            _progressCallback = progressCallback;
            _httpClientFactory = httpClientFactory;
            
            // Set security protocol to TLS 1.2 and 1.3
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
            
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
            var uri = new Uri(_configUrl);
            if (uri.Scheme == Uri.UriSchemeHttp || 
                  uri.Scheme == Uri.UriSchemeHttps)
            {
                using (var client = _httpClientFactory.CreateClient())
                {
                    var jsonString = await client.GetStringAsync(_configUrl);
                    return JsonConvert.DeserializeObject<UpdateConfiguration>(jsonString);
                }
            }
            else
            {
                var request = (FtpWebRequest)WebRequest.Create(uri);
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.Timeout = 30000; // 30 second timeout
                if (uri.Scheme.Equals("ftps", StringComparison.OrdinalIgnoreCase))
                {
                    request.EnableSsl = true;
                    request.KeepAlive = false;
                }
                if (!string.IsNullOrEmpty(uri.UserInfo))
                {
                    var credentials = uri.UserInfo.Split(':');
                    request.Credentials = new NetworkCredential(
                        credentials[0],
                        credentials.Length > 1 ? credentials[1] : string.Empty
                    );
                }
                using (var response = (FtpWebResponse)await request.GetResponseAsync())
                using (var responseStream = response.GetResponseStream())
                using (var reader = new StreamReader(responseStream))
                {
                    var jsonString = await reader.ReadToEndAsync();
                    return JsonConvert.DeserializeObject<UpdateConfiguration>(jsonString);
                }
            }
        }


        private async Task DownloadFile(string url, string destination)
        {
            try
            {
                var uri = new Uri(url);
                if (uri.Scheme == Uri.UriSchemeFtp || uri.Scheme == "ftps")
                {
                    await DownloadFileViaFtp(uri, destination);
                }
                else
                {
                    await DownloadFileViaHttp(url, destination);
                }
            }
            catch (HttpRequestException ex)
            {
                _logCallback($"HTTP download failed: {ex.Message}", LogLevel.Error);
                throw new Exception($"Failed to download file from {url}: {ex.Message}", ex);
            }
            catch (WebException ex)
            {
                _logCallback($"Network error during download: {ex.Message}", LogLevel.Error);
                throw new Exception($"Network error downloading from {url}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logCallback($"Unexpected error during download: {ex.Message}", LogLevel.Error);
                throw new Exception($"Failed to download file from {url}: {ex.Message}", ex);
            }
        }

        private async Task DownloadFileViaHttp(string url, string destination)
        {
            using (var client = _httpClientFactory.CreateClient())
            {
                var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var buffer = new byte[8192];
                var bytesRead = 0L;

                using (var fileStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var downloadStream = await response.Content.ReadAsStreamAsync())
                {
                    while (!_isCancellationRequested)
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

                // Verify the download size if Content-Length was provided
                if (totalBytes > 0 && bytesRead != totalBytes)
                {
                    throw new Exception($"Download incomplete. Expected {totalBytes} bytes but got {bytesRead} bytes.");
                }

                if (_isCancellationRequested)
                {
                    _logCallback("Download cancelled by user", LogLevel.Warning);
                    throw new OperationCanceledException("Download cancelled by user");
                }
            }
        }

        private async Task DownloadFileViaFtp(Uri uri, string destination)
        {
            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.Timeout = 30000; // 30 second timeout
            
            if (uri.Scheme.Equals("ftps", StringComparison.OrdinalIgnoreCase))
            {
                request.EnableSsl = true;
                request.KeepAlive = false;
            }
            
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
                using (var fileStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var buffer = new byte[8192];
                    var totalBytes = response.ContentLength;
                    var bytesRead = 0L;

                    while (!_isCancellationRequested)
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

                    await fileStream.FlushAsync();

                    // Verify the download size
                    if (totalBytes > 0 && bytesRead != totalBytes)
                    {
                        throw new Exception($"FTP download incomplete. Expected {totalBytes} bytes but got {bytesRead} bytes.");
                    }

                    if (_isCancellationRequested)
                    {
                        _logCallback("FTP download cancelled by user", LogLevel.Warning);
                        throw new OperationCanceledException("Download cancelled by user");
                    }
                }
            }
            catch (Exception ex)
            {
                _logCallback($"FTP download failed: {ex.Message}", LogLevel.Error);
                throw new Exception($"FTP download failed: {ex.Message}", ex);
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
                _logCallback($"{file.Name} installed successfully", LogLevel.Info);
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

        public void Dispose()
        {
            // Dispose of any resources here
        }
    }
} 