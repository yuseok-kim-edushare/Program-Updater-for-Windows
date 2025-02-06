using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ProgramUpdater.Models;

namespace ProgramUpdater.Services
{
    public class ConfigurationService : IDisposable
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private HttpClient _fallbackClient;

        public ConfigurationService(IHttpClientFactory httpClientFactory)
        {
            try
            {
                _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            }
            catch (Exception ex)
            {
                // Create a fallback HttpClient if factory initialization fails
                _fallbackClient = CreateFallbackHttpClient();
                System.Diagnostics.Trace.WriteLine($"HttpClientFactory 초기화 실패, 대체 HttpClient 사용: {ex.Message}");
            }
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
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

        private HttpClient GetHttpClient()
        {
            try
            {
                if (_httpClientFactory != null)
                {
                    return _httpClientFactory.CreateClient();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"HttpClientFactory CreateClient 실패: {ex.Message}");
            }

            _fallbackClient ??= CreateFallbackHttpClient();
            return _fallbackClient;
        }

        public async Task<UpdateConfiguration> GetConfiguration(string configUrl)
        {
            if (string.IsNullOrEmpty(configUrl))
            {
                throw new ArgumentException("설정 URL이 비어 있거나 null입니다");
            }

            Uri uri;
            try
            {
                uri = new Uri(configUrl);
            }
            catch (UriFormatException ex)
            {
                throw new ArgumentException($"잘못된 URL 형식: {configUrl}", ex);
            }

            string jsonContent;
            try
            {
                switch (uri.Scheme.ToLower())
                {
                    case "http":
                    case "https":
                        jsonContent = await DownloadConfigViaHttp(uri).ConfigureAwait(false);
                        break;
                    case "ftp":
                    case "ftps":
                        jsonContent = await DownloadConfigViaFtp(uri).ConfigureAwait(false);
                        break;
                    default:
                        throw new ArgumentException($"지원되지 않는 프로토콜: {uri.Scheme}. HTTP, HTTPS, FTP, FTPS만 지원됩니다.");
                }
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is WebException || ex is TaskCanceledException)
            {
                var errorMessage = GetDetailedErrorMessage(ex);
                throw new InvalidOperationException($"설정 파일 다운로드 실패: {errorMessage}", ex);
            }

            if (string.IsNullOrEmpty(jsonContent))
            {
                throw new InvalidOperationException("다운로드된 설정이 비어 있습니다");
            }

            try
            {
                var config = JsonConvert.DeserializeObject<UpdateConfiguration>(jsonContent);
                if (config == null)
                {
                    throw new InvalidOperationException("설정 파일을 파싱할 수 없습니다");
                }
                return config;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"잘못된 설정 파일 형식: {ex.Message}", ex);
            }
        }

        private string GetDetailedErrorMessage(Exception ex)
        {
            if (ex is HttpRequestException httpEx)
            {
                return $"HTTP 오류: {httpEx.Message}";
            }
            if (ex is WebException webEx)
            {
                return GetWebExceptionMessage(webEx);
            }
            if (ex is TaskCanceledException)
            {
                return "설정 다운로드 시간 초과";
            }
            return ex.Message;
        }

        private string GetWebExceptionMessage(WebException ex)
        {
            if (ex.Response == null) return ex.Message;

            if (ex.Response is HttpWebResponse httpResponse)
            {
                return $"HTTP 상태 코드: {(int)httpResponse.StatusCode} - {httpResponse.StatusDescription}";
            }
            if (ex.Response is FtpWebResponse ftpResponse)
            {
                return $"FTP 상태 코드: {ftpResponse.StatusCode} - {ftpResponse.StatusDescription}";
            }
            return ex.Message;
        }

        private async Task<string> DownloadConfigViaHttp(Uri uri)
        {
            using var client = GetHttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            
            try
            {
                var response = await client.GetAsync(uri).ConfigureAwait(false);
                
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(
                        $"HTTP 요청 실패 - 상태 코드: {(int)response.StatusCode} ({response.StatusCode}), " +
                        $"사유: {response.ReasonPhrase}");
                }
                
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (TaskCanceledException)
            {
                throw new TimeoutException("HTTP 요청 시간 초과");
            }
            catch (Exception ex)
            {
                throw new HttpRequestException($"HTTP 요청 중 오류 발생: {ex.Message}", ex);
            }
        }

        private async Task<string> DownloadConfigViaFtp(Uri uri)
        {
            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.Timeout = 30000; // 30 seconds
            request.UsePassive = true;
            request.UseBinary = true;
            request.KeepAlive = false;

            if (uri.Scheme.Equals("ftps", StringComparison.OrdinalIgnoreCase))
            {
                request.EnableSsl = true;
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
                using var response = (FtpWebResponse)await request.GetResponseAsync().ConfigureAwait(false);
                using var responseStream = response.GetResponseStream();
                using var reader = new StreamReader(responseStream ?? throw new InvalidOperationException("FTP 응답 스트림이 null입니다"), Encoding.UTF8);
                
                var content = await reader.ReadToEndAsync().ConfigureAwait(false);
                if (string.IsNullOrEmpty(content))
                {
                    throw new InvalidOperationException("FTP 서버에서 빈 응답을 받았습니다");
                }
                return content;
            }
            catch (WebException ex)
            {
                var ftpResponse = ex.Response as FtpWebResponse;
                var errorMessage = ftpResponse != null
                    ? $"FTP 오류: {ftpResponse.StatusCode} - {ftpResponse.StatusDescription}"
                    : ex.Message;
                throw new WebException($"FTP 다운로드 실패: {errorMessage}", ex, ex.Status, ex.Response);
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                throw new InvalidOperationException($"FTP 다운로드 중 오류 발생: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            _fallbackClient?.Dispose();
        }
    }
} 