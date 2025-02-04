using System;
using System.Windows.Forms;

namespace ProgramUpdater
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string configUrl = "http://yourserver.com/path/to/update_config.json";
            if (args.Length > 0)
            {
                configUrl = args[0];
            }

            try
            {
                Application.Run(new MainForm(configUrl));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"A fatal error occurred: {ex.Message}",
                    "Fatal Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
} 