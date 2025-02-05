using System;
using System.Windows.Forms;
using System.Threading;

namespace ProgramUpdater
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
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
                Application.Run(new MainForm(configUrl));
            }
            catch (Exception ex)
            {
                HandleFatalError(ex);
            }
        }

        private static string GetConfigUrl(string[] args)
        {
            const string defaultConfigUrl = "http://yourserver.com/path/to/update_config.json";
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
                  uriResult.Scheme == Uri.UriSchemeFtp))
            {
                throw new ArgumentException("Invalid configuration URL format. Supported schemes are HTTP, HTTPS, and FTP.");
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