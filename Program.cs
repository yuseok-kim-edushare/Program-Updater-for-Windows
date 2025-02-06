using System;
using System.Windows.Forms;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using ProgramUpdater.Services;

namespace ProgramUpdater
{
    static class Program
    {
        private static IServiceProvider _serviceProvider;

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

            string configUrl = GetConfigUrl(args);

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

            // Register other services, including MainForm
            services.AddTransient<ConfigurationService>();
            services.AddTransient<UpdateService>();
            services.AddTransient<MainForm>(); // Register MainForm as transient
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
    }
} 