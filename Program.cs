using System;
using System.Windows.Forms;
using System.Threading;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using ProgramUpdater.Services;
using System.Linq;
using ProgramUpdater.Extensions;

namespace ProgramUpdater
{
    static class Program
    {
        private static IServiceProvider _serviceProvider;
        private static string configUrl;

        [STAThread]
        static void Main(string[] args)
        {
            // Set up dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services);

            _serviceProvider = services.BuildServiceProvider();
            // Set up global exception handling
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            configUrl = GetConfigUrl(args);

            try
            {
                ValidateConfigUrl(configUrl);                
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

        private static void ConfigureServices(IServiceCollection services)
        {
            // Register HttpClientFactory
            services.AddHttpClient();
            
            // Register services
            services.AddSingleton(serviceProvider =>
            {
                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                return new ConfigurationService(httpClientFactory);
            });
            
            // Register UpdateService with its dependencies
            services.AddSingleton(serviceProvider =>
            {
                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                var configService = serviceProvider.GetRequiredService<ConfigurationService>();
                return new UpdateService(
                    configUrl,
                    (message, level) => LogMessage(message, level),
                    (progress, status) => UpdateProgress(progress, status),
                    httpClientFactory,
                    configService
                );
            });
            
            // Register MainForm with its configuration URL
            services.AddTransient(serviceProvider =>
            {
                var configService = serviceProvider.GetRequiredService<ConfigurationService>();
                var updateService = serviceProvider.GetRequiredService<UpdateService>();
                return new MainForm(configService, updateService);
            });
        }

        private static void DisposeServices()
        {
            if (_serviceProvider == null)
            {
                return;
            }
            if (_serviceProvider is IDisposable)
            {
                ((IDisposable)_serviceProvider).Dispose();
            }
        }

        private static string GetConfigUrl(string[] args)
        {
            const string defaultConfigUrl = "https://raw.githubusercontent.com/yuseok-kim-edushare/Program-Updater-for-Windows/refs/heads/main/example.json";
            return args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]) 
                ? args[0] 
                : defaultConfigUrl;
        }

        private static void ValidateConfigUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException("Configuration URL cannot be empty.");
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult) || 
                !(uriResult.Scheme == Uri.UriSchemeHttp || 
                  uriResult.Scheme == Uri.UriSchemeHttps || 
                  uriResult.Scheme == Uri.UriSchemeFtp ||
                  uriResult.Scheme == "ftps"))
            {
                throw new ArgumentException("Invalid configuration URL format. Supported schemes are HTTP, HTTPS, FTP, and FTPS.");
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