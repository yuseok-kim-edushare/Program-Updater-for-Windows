using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Threading.Tasks;
using ProgramUpdater.Services;

namespace ProgramUpdater
{
    public partial class MainForm : Form
    {
        private readonly string _configUrl;
        private TableLayoutPanel mainLayout;
        private Label titleLabel;
        private Label statusLabel;
        private ProgressBar progressBar;
        private Button cancelButton;
        private RichTextBox logTextBox;
        private bool isCancellationRequested;
        private UpdateService updateService;

        public MainForm(string configUrl)
        {
            _configUrl = configUrl;
            InitializeComponent();
            InitializeCustomComponents();
            SetupEventHandlers();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            this.AutoScaleDimensions = new SizeF(96F, 96F);
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.ClientSize = new Size(600, 400);
            this.MinimumSize = new Size(600, 400);
            this.Text = "Program Updater";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            
            this.ResumeLayout(false);
        }

        private void InitializeCustomComponents()
        {
            // Main layout
            mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                RowCount = 5,
                ColumnCount = 1
            };

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));  // Title
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));  // Status
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));  // Progress Bar
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // Log
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));  // Button

            // Title Label
            titleLabel = new Label
            {
                Text = "Program Update in Progress",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Status Label
            statusLabel = new Label
            {
                Text = "Initializing...",
                Font = new Font("Segoe UI", 9F),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Progress Bar
            progressBar = new ProgressBar
            {
                Dock = DockStyle.Fill,
                Style = ProgressBarStyle.Continuous,
                Height = 23
            };

            // Log TextBox
            logTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.White,
                Font = new Font("Consolas", 9F),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Cancel Button
            cancelButton = new Button
            {
                Text = "Cancel",
                Dock = DockStyle.Right,
                Width = 100,
                Height = 30,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(230, 230, 230)
            };

            // Add controls to layout
            mainLayout.Controls.Add(titleLabel, 0, 0);
            mainLayout.Controls.Add(statusLabel, 0, 1);
            mainLayout.Controls.Add(progressBar, 0, 2);
            mainLayout.Controls.Add(logTextBox, 0, 3);
            mainLayout.Controls.Add(cancelButton, 0, 4);

            this.Controls.Add(mainLayout);
        }

        private void SetupEventHandlers()
        {
            this.Load += MainForm_Load;
            cancelButton.Click += CancelButton_Click;
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                var configService = new ConfigurationService();
                var config = await configService.GetConfiguration(_configUrl);
                
                updateService = new UpdateService((progress, status) =>
                {
                    this.InvokeIfRequired(() =>
                    {
                        progressBar.Value = progress;
                        statusLabel.Text = status;
                    });
                });

                await updateService.StartUpdate(config);
            }
            catch (Exception ex)
            {
                LogError($"Update failed: {ex.Message}");
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                "Are you sure you want to cancel the update process?",
                "Confirm Cancellation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                isCancellationRequested = true;
                updateService?.RequestCancellation();
                cancelButton.Enabled = false;
                statusLabel.Text = "Cancelling...";
            }
        }

        public void UpdateProgress(int percentage, string status)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateProgress(percentage, status)));
                return;
            }

            progressBar.Value = percentage;
            statusLabel.Text = status;
        }

        public void LogMessage(string message, LogLevel level = LogLevel.Info)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => LogMessage(message, level)));
                return;
            }

            Color textColor;
            switch (level)
            {
                case LogLevel.Error:
                    textColor = Color.Red;
                    break;
                case LogLevel.Warning:
                    textColor = Color.Orange;
                    break;
                case LogLevel.Success:
                    textColor = Color.Green;
                    break;
                default:
                    textColor = Color.Black;
                    break;
            }

            logTextBox.SelectionStart = logTextBox.TextLength;
            logTextBox.SelectionLength = 0;
            logTextBox.SelectionColor = textColor;
            logTextBox.AppendText($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
            logTextBox.ScrollToCaret();
        }

        private async Task StartUpdateProcess()
        {
            try
            {
                updateService = new UpdateService(
                    _configUrl,
                    LogMessage,
                    UpdateProgress);

                LogMessage("Update process started", LogLevel.Info);
                bool success = await updateService.PerformUpdate();

                if (success)
                {
                    LogMessage("Update completed successfully", LogLevel.Success);
                    await Task.Delay(3000); // Give user time to read the success message
                    Application.Exit();
                }
                else if (isCancellationRequested)
                {
                    LogMessage("Update was cancelled by user", LogLevel.Warning);
                    MessageBox.Show(
                        "The update process was cancelled. The application may be in an inconsistent state.",
                        "Update Cancelled",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Update failed: {ex.Message}", LogLevel.Error);
                MessageBox.Show(
                    "An error occurred during the update process. Check the log for details.",
                    "Update Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }

    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Success
    }
} 