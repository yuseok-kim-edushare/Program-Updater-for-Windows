using System;
using System.IO;
using System.Net.Http;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Collections.Generic;
using Newtonsoft.Json;
using ProgramUpdater.Models;
using ProgramUpdater.Extensions;
using System.Runtime.CompilerServices;

namespace ProgramUpdater.Services
{
    public class UpdateService : IDisposable
    {
        private readonly string _configUrl;
        private readonly Action<string, LogLevel> _logCallback;
        private readonly Action<int, string> _progressCallback;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ConfigurationService _configurationService;
        private CancellationTokenSource _cts;
        private HttpClient _fallbackClient;
        private readonly HashSet<string> _checkedDirectories;

        public UpdateService(
            string configUrl,
            Action<string, LogLevel> logCallback,
            Action<int, string> progressCallback,
            IHttpClientFactory httpClientFactory,
            ConfigurationService configurationService)
        {
            _configUrl = configUrl;
            _logCallback = logCallback;
            _progressCallback = progressCallback;
            _httpClientFactory = httpClientFactory;
            _configurationService = configurationService;
            _cts = new CancellationTokenSource();
            _checkedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            // Set security protocol to TLS 1.2 and 1.3
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
        }

        public void RequestCancellation()
        {
            _cts?.Cancel();
        }

        public async Task<bool> PerformUpdate()
        {
            var updatedFiles = new List<FileConfiguration>();
            try
            {
                // Download and parse configuration
                _logCallback("Downloading update configuration...", LogLevel.Info);
                UpdateConfiguration config;
                try
                {
                    config = await _configurationService.GetConfiguration(_configUrl);
                    if (config?.Files == null || config.Files.Count == 0)
                    {
                        throw new InvalidOperationException("Update configuration contains no files to update");
                    }
                }
                catch (Exception ex)
                {
                    _logCallback($"Failed to get update configuration: {ex.Message}", LogLevel.Error);
                    throw;
                }

                // Calculate total steps for progress
                int totalSteps = config.Files.Count * 3; // Download, Verify, Replace
                int currentStep = 0;

                // Stop running executables
                await StopRunningExecutables(config.Files);

                // First phase: Download and verify all files
                foreach (var file in config.Files)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    currentStep = await DownloadAndVerifyFile(file, currentStep, totalSteps);
                }
                
                // Second phase: Backup and replace all files
                foreach (var file in config.Files)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    currentStep = await BackupAndReplaceFile(file, currentStep, totalSteps);
                    updatedFiles.Add(file);
                }

                // Start executables that were previously running
                await StartExecutables(config.Files);

                _progressCallback(100, "Update completed successfully");
                _logCallback("Update process completed successfully", LogLevel.Success);

                // Cleanup backup files
                foreach (var file in config.Files)
                {
                    await CleanupBackupFiles(file);
                    _logCallback($"Cleaning up temporary files completed", LogLevel.Info);
                }

                return true;
            }
            catch (OperationCanceledException)
            {
                _logCallback("Update was cancelled by user. Rolling back changes...", LogLevel.Warning);
                await RollbackUpdates(updatedFiles);
                return false;
            }
            catch (Exception ex)
            {
                string errorMessage;
                if (ex.InnerException != null)
                {
                    errorMessage = $"업데이트 실패: {ex.InnerException.Message}";
                    _logCallback(errorMessage, LogLevel.Error);
                    _logCallback($"상세 오류: {ex.Message}", LogLevel.Error);
                }
                else
                {
                    errorMessage = $"업데이트 실패: {ex.Message}";
                    _logCallback(errorMessage, LogLevel.Error);
                }

                await RollbackUpdates(updatedFiles);
                
                // Log the full stack trace for debugging
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    _logCallback($"스택 추적: {ex.StackTrace}", LogLevel.Error);
                }
                throw;
            }
        }

        private async Task StopRunningExecutables(IEnumerable<FileConfiguration> files)
        {
            foreach (var file in files)
            {
                if (file.IsExecutable && IsProcessRunning(file.CurrentPath))
                {
                    _logCallback($"Stopping process: {file.Name}", LogLevel.Info);
                    await StopProcess(file.CurrentPath);
                }
            }
        }

        private async Task<int> DownloadAndVerifyFile(FileConfiguration file, int currentStep, int totalSteps)
        {
            // Pre-verify if current file exists and has correct hash
            if (File.Exists(file.CurrentPath))
            {
                _progressCallback((currentStep++ * 100) / totalSteps, $"Pre-verifying {file.Name}...");
                if (await VerifyFileHash(file.CurrentPath, file.ExpectedHash, _cts.Token))
                {
                    _logCallback($"File {file.Name} is already up to date, skipping download", LogLevel.Info);
                    // Skip both download and verify steps to maintain progress
                    currentStep += 2; // Skip both download and verify steps
                    return currentStep;
                }
            }

            // Download new version
            _progressCallback((currentStep++ * 100) / totalSteps, $"Downloading {file.Name}...");
            await DownloadFile(file.DownloadUrl, file.NewPath, _cts.Token);

            // Verify hash
            _progressCallback((currentStep++ * 100) / totalSteps, $"Verifying {file.Name}...");
            if (!await VerifyFileHash(file.NewPath, file.ExpectedHash, _cts.Token))
            {
                if (File.Exists(file.NewPath))
                {
                    File.Delete(file.NewPath);
                }
                throw new Exception($"Hash verification failed for {file.Name}");
            }

            return currentStep;
        }

        private async Task<int> BackupAndReplaceFile(FileConfiguration file, int currentStep, int totalSteps)
        {
            // Backup and replace
            _progressCallback((currentStep++ * 100) / totalSteps, $"Installing {file.Name}...");
            await BackupAndReplace(file);

            return currentStep;
        }

        private async Task StartExecutables(IEnumerable<FileConfiguration> files)
        {
            foreach (var file in files)
            {
                if (file.IsExecutable)
                {
                    _logCallback($"Starting process: {file.Name}", LogLevel.Info);
                    await Task.Run(() => StartProcess(file.CurrentPath));
                }
            }
        }

        private HttpClient GetHttpClient()
        {
            try
            {
                if (_httpClientFactory != null)
                {
                    return _httpClientFactory.CreateClient("UpdateClient");
                }
            }
            catch (Exception ex)
            {
                _logCallback($"HttpClientFactory 생성 실패: {ex.Message}", LogLevel.Warning);
                _logCallback($"stack trace: {ex.StackTrace}", LogLevel.Error);
            }

            _fallbackClient ??= CreateFallbackHttpClient();
            return _fallbackClient;
        }

        private HttpClient CreateFallbackHttpClient()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 10,
                UseProxy = true,
                UseCookies = false
            };

            return new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        private void EnsureDirectoryExists(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            var dir = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(dir)) return;

            if (_checkedDirectories.Add(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        private async Task DownloadFile(string url, string destination, CancellationToken cancellationToken)
        {
            try
            {
                // Create directory if it doesn't exist using the cache
                EnsureDirectoryExists(destination);

                var uri = new Uri(url);
                if (uri.Scheme == Uri.UriSchemeFtp || uri.Scheme == "ftps")
                {
                    await DownloadFileViaFtp(uri, destination, cancellationToken);
                }
                else
                {
                    await DownloadFileViaHttp(url, destination, cancellationToken);
                }
            }
            catch (HttpRequestException ex)
            {
                _logCallback($"HTTP 다운로드 실패: {ex.Message}", LogLevel.Error);
                throw new Exception($"파일 다운로드 실패 ({url}): {ex.Message}", ex);
            }
            catch (WebException ex)
            {
                _logCallback($"네트워크 오류: {ex.Message}", LogLevel.Error);
                throw new Exception($"네트워크 오류 ({url}): {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                    throw;
                    
                _logCallback($"예기치 않은 오류: {ex.Message}", LogLevel.Error);
                throw new Exception($"파일 다운로드 실패 ({url}): {ex.Message}", ex);
            }
        }

        private async Task DownloadFileViaHttp(string url, string destination, CancellationToken cancellationToken)
        {
            using (var client = GetHttpClient())
            {
                var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var buffer = new byte[8192];
                var bytesRead = 0L;

                using (var fileStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var downloadStream = await response.Content.ReadAsStreamAsync())
                {
                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        var count = await downloadStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                        if (count == 0) break;

                        await fileStream.WriteAsync(buffer, 0, count, cancellationToken);
                        bytesRead += count;

                        if (totalBytes > 0)
                        {
                            var percentage = (int)((bytesRead * 100) / totalBytes);
                            _progressCallback(percentage, $"다운로드 중... {percentage}%");
                        }
                    }
                }

                // Verify the download size if Content-Length was provided
                if (totalBytes > 0 && bytesRead != totalBytes)
                {
                    throw new Exception($"다운로드가 불완전합니다. 예상 크기: {totalBytes} 바이트, 실제 크기: {bytesRead} 바이트");
                }
            }
        }

        private async Task DownloadFileViaFtp(Uri uri, string destination, CancellationToken cancellationToken)
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

                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        var count = await responseStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                        if (count == 0) break;

                        await fileStream.WriteAsync(buffer, 0, count, cancellationToken);
                        bytesRead += count;

                        if (totalBytes > 0)
                        {
                            var percentage = (int)((bytesRead * 100) / totalBytes);
                            _progressCallback(percentage, $"Downloading... {percentage}%");
                        }
                    }

                    await fileStream.FlushAsync(cancellationToken);

                    // Verify the download size
                    if (totalBytes > 0 && bytesRead != totalBytes)
                    {
                        throw new Exception($"FTP download incomplete. Expected {totalBytes} bytes but got {bytesRead} bytes.");
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                    throw;
                    
                _logCallback($"FTP download failed: {ex.Message}", LogLevel.Error);
                throw new Exception($"FTP download failed: {ex.Message}", ex);
            }
        }

        private async Task<bool> VerifyFileHash(string filePath, string expectedHash, CancellationToken cancellationToken)
        {
            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hash = await Task.Run(() => sha256.ComputeHash(stream), cancellationToken);
                var actualHash = BitConverter.ToString(hash).Replace("-", "");
                return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
            }
        }

        // Refactored BackupAndReplace method using asynchronous file I/O 
        private async Task BackupAndReplace(FileConfiguration file)
        {
            try
            {
                // Create directories for current path and new path if they don't exist using the cache
                EnsureDirectoryExists(file.CurrentPath);
                EnsureDirectoryExists(file.NewPath);
                EnsureDirectoryExists(file.BackupPath);

                if (File.Exists(file.CurrentPath))
                {
                    // Backup existing file by copying it asynchronously instead of wrapping File.Move in Task.Run.
                    if (File.Exists(file.BackupPath))
                    {
                        File.Delete(file.BackupPath);
                    }
                    
                    using (var sourceStream = new FileStream(file.CurrentPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
                    using (var backupStream = new FileStream(file.BackupPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                    {
                        await sourceStream.CopyToAsync(backupStream);
                    }
                    // Delete the original file after a successful backup copy.
                    File.Delete(file.CurrentPath);
                }

                // Move new file to destination by copying asynchronously and then deleting the source new file.
                using (var newFileStream = new FileStream(file.NewPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
                using (var destinationStream = new FileStream(file.CurrentPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                {
                    await newFileStream.CopyToAsync(destinationStream);
                }
                File.Delete(file.NewPath);

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

        private async Task RollbackUpdates(List<FileConfiguration> updatedFiles)
        {
            if (updatedFiles.Count == 0)
            {
                _logCallback("No files to roll back", LogLevel.Info);
                return;
            }

            _logCallback("Starting rollback process...", LogLevel.Warning);
            foreach (var file in updatedFiles)
            {
                try
                {
                    // Create directory for current path if it doesn't exist using the cache
                    EnsureDirectoryExists(file.CurrentPath);

                    // Delete the current (new) file if it exists
                    if (File.Exists(file.CurrentPath))
                    {
                        File.Delete(file.CurrentPath);
                    }

                    // Restore from backup if it exists
                    if (File.Exists(file.BackupPath))
                    {
                        _logCallback($"Rolling back {file.Name}...", LogLevel.Info);
                        using (var backupStream = new FileStream(file.BackupPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
                        using (var currentStream = new FileStream(file.CurrentPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                        {
                            await backupStream.CopyToAsync(currentStream);
                        }
                        
                        // Delete the backup after successful restore
                        File.Delete(file.BackupPath);
                        _logCallback($"Successfully rolled back {file.Name}", LogLevel.Info);
                    }

                    // Clean up any downloaded files that might still exist
                    if (File.Exists(file.NewPath))
                    {
                        File.Delete(file.NewPath);
                    }
                }
                catch (Exception ex)
                {
                    _logCallback($"Failed to roll back {file.Name}: {ex.Message}", LogLevel.Error);
                    // Continue with other files even if one fails
                }
            }
            _logCallback("Rollback process completed", LogLevel.Warning);
        }

        private async Task CleanupBackupFiles(FileConfiguration file)
        {
            if (File.Exists(file.BackupPath))
            {
                await Task.Run(() => File.Delete(file.BackupPath));
            }
        }

        public void Dispose()
        {
            _cts?.Dispose();
            _cts = null;
            _fallbackClient?.Dispose();
            _fallbackClient = null;
            _checkedDirectories.Clear();
        }
    }
} 