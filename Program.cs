using System;
using System.Windows.Forms;
using System.Threading;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using ProgramUpdater.Services;
using System.Linq;
using ProgramUpdater.Extensions;
using System.Net;

namespace ProgramUpdater
{
    static class Program
    {
        private static IServiceProvider _serviceProvider;
        private static string configUrl;

        [STAThread]
        static void Main(string[] args)
        {
            SetupSecurityProtocol();

            // Set up dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services);

            try
            {
                _serviceProvider = services.BuildServiceProvider();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"서비스 초기화 실패: {ex.Message}\n프로그램을 다시 시작해주세요.",
                    "초기화 오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            // Set up global exception handling
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                // Resolve MainForm from the service provider
                var mainForm = _serviceProvider.GetRequiredService<MainForm>();
                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                HandleFatalError(ex);
            }
            finally
            {
                DisposeServices();
            }
        }

        private static void SetupSecurityProtocol()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
            ServicePointManager.DefaultConnectionLimit = 10;
            ServicePointManager.Expect100Continue = false;
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Add options: ensures that the Options system is available to the HttpClientFactory.
            services.AddOptions();

            // Register SettingsService
            services.AddSingleton<SettingsService>();

            // Register IHttpClientFactory with a named client "UpdateClient"
            services.AddHttpClient("UpdateClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.ConnectionClose = true;
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 10,
                UseProxy = true,
                UseCookies = false
            });

            // Register ConfigurationService using a valid IHttpClientFactory instance.
            services.AddSingleton(serviceProvider =>
            {
                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                return new ConfigurationService(httpClientFactory);
            });

            // Register UpdateService with its dependencies.
            services.AddSingleton(serviceProvider =>
            {
                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                var configService = serviceProvider.GetRequiredService<ConfigurationService>();
                var settingsService = serviceProvider.GetRequiredService<SettingsService>();
                configUrl = settingsService.ConfigurationFilePath;
                return new UpdateService(
                    configUrl,
                    (message, level) => LogMessage(message, level),
                    (progress, status) => UpdateProgress(progress, status),
                    httpClientFactory,
                    configService
                );
            });

            // Register MainForm.
            services.AddTransient(serviceProvider =>
            {
                var configService = serviceProvider.GetRequiredService<ConfigurationService>();
                var updateService = serviceProvider.GetRequiredService<UpdateService>();
                var settingsService = serviceProvider.GetRequiredService<SettingsService>();
                return new MainForm(configService, updateService, settingsService);
            });
        }

        private static void DisposeServices()
        {
            if (_serviceProvider == null)
            {
                return;
            }

            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        private static void HandleFatalError(Exception ex)
        {
            string message = $"A fatal error occurred:\n\n{ex.Message}";
            if (ex.InnerException != null)
            {
                message += $"\n\nAdditional details:\n{ex.InnerException.Message}";
            }

            MessageBox.Show(
                message,
                "Fatal Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            HandleFatalError(e.Exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                HandleFatalError(ex);
            }
        }

        private static void LogMessage(string message, LogLevel level)
        {
            // This will be called by UpdateService to log messages
            // The MainForm will handle displaying these messages
            Application.OpenForms.OfType<MainForm>().FirstOrDefault()?.LogMessage(message, level);
        }

        private static void UpdateProgress(int progress, string status)
        {
            // This will be called by UpdateService to update progress
            // The MainForm will handle displaying the progress
            Application.OpenForms.OfType<MainForm>().FirstOrDefault()?.UpdateProgress(progress, status);
        }
    }
} 