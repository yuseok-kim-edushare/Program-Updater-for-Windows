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
        private readonly HttpClient _httpClient;

        public ConfigurationService()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                    sslPolicyErrors == System.Net.Security.SslPolicyErrors.None
            };

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
            
            _httpClient = new HttpClient(handler);
        }

        public async Task<UpdateConfiguration> GetConfiguration(string configUrl)
        {
            var uri = new Uri(configUrl);
            string jsonContent;

            switch (uri.Scheme.ToLower())
            {
                case "http":
                case "https":
                    jsonContent = await DownloadConfigViaHttp(uri);
                    break;

                case "ftp":
                case "ftps":
                    jsonContent = await DownloadConfigViaFtp(uri);
                    break;

                default:
                    throw new ArgumentException($"Unsupported protocol: {uri.Scheme}");
            }

            return JsonConvert.DeserializeObject<UpdateConfiguration>(jsonContent);
        }

        private async Task<string> DownloadConfigViaHttp(Uri uri)
        {
            var response = await _httpClient.GetStringAsync(uri);
            return response;
        }

        private async Task<string> DownloadConfigViaFtp(Uri uri)
        {
            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.Method = WebRequestMethods.Ftp.DownloadFile;

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
            using (var reader = new StreamReader(responseStream, Encoding.UTF8))
            {
                return await reader.ReadToEndAsync();
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
} 