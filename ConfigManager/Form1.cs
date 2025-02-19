using System;
using System.Windows.Forms;
using System.Collections.Generic;
using ProgramUpdater.ConfigManager.Models;
using ProgramUpdater.ConfigManager.Services;
using System.Linq;
using System.IO;

namespace ProgramUpdater.ConfigManager
{
    public partial class Form1 : Form
    {
        private readonly UpdateConfigurationService _configService;
        private readonly SettingsService _settingsService;
        private UpdateConfiguration _currentConfig;
        private Settings _currentSettings;
        private BindingSource _bindingSource;
        private OpenFileDialog _openFileDialog;

        public Form1()
        {
            InitializeComponent();
            _configService = new UpdateConfigurationService();
            _settingsService = new SettingsService();
            _currentConfig = new UpdateConfiguration();
            _currentSettings = new Settings();
            _bindingSource = new BindingSource();
            _openFileDialog = new OpenFileDialog
            {
                Filter = "All files (*.*)|*.*",
                Multiselect = false
            };
            
            InitializeUI();
            LoadConfiguration();
            LoadSettings();
        }

        private void InitializeUI()
        {
            // Create tab control
            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            // JSON Config Tab
            var jsonTab = new TabPage("JSON Configuration");
            
            // Create main panel for JSON tab
            var jsonPanel = new Panel { Dock = DockStyle.Fill };
            
            // Add DataGridView to the panel
            dgvFiles.Dock = DockStyle.Fill;
            jsonPanel.Controls.Add(dgvFiles);
            
            // Create button panel for JSON tab
            var jsonButtonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                Padding = new Padding(5)
            };
            
            // Add buttons to the panel
            btnAdd.Location = new System.Drawing.Point(5, 8);
            btnRemove.Location = new System.Drawing.Point(85, 8);
            btnSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLoad.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSave.Location = new System.Drawing.Point(jsonButtonPanel.Width - 160, 8);
            btnLoad.Location = new System.Drawing.Point(jsonButtonPanel.Width - 80, 8);
            
            jsonButtonPanel.Controls.AddRange(new Control[] { btnAdd, btnRemove, btnSave, btnLoad });
            jsonPanel.Controls.Add(jsonButtonPanel);
            
            jsonTab.Controls.Add(jsonPanel);

            // XML Settings Tab
            var xmlTab = new TabPage("XML Settings");
            var xmlPanel = new Panel { Dock = DockStyle.Fill };
            
            // UI Settings group
            var uiGroup = new GroupBox
            {
                Text = "UI Settings",
                Dock = DockStyle.Top,
                Height = 100,
                Padding = new Padding(10)
            };

            var txtWindowTitle = new TextBox
            {
                Location = new System.Drawing.Point(120, 20),
                Width = 200
            };
            txtWindowTitle.DataBindings.Add("Text", _currentSettings.UI, "WindowTitle");

            var txtTitleText = new TextBox
            {
                Location = new System.Drawing.Point(120, 50),
                Width = 200
            };
            txtTitleText.DataBindings.Add("Text", _currentSettings.UI, "TitleText");

            uiGroup.Controls.AddRange(new Control[] {
                new Label { Text = "Window Title:", Location = new System.Drawing.Point(15, 23), AutoSize = true },
                txtWindowTitle,
                new Label { Text = "Title Text:", Location = new System.Drawing.Point(15, 53), AutoSize = true },
                txtTitleText
            });

            // Configuration Settings group
            var configGroup = new GroupBox
            {
                Text = "Configuration Settings",
                Dock = DockStyle.Top,
                Height = 100,
                Top = 110,
                Padding = new Padding(10)
            };

            var txtConfigPath = new TextBox
            {
                Location = new System.Drawing.Point(120, 20),
                Width = 200
            };
            txtConfigPath.DataBindings.Add("Text", _currentSettings.Configuration, "ConfigurationFilePath");

            configGroup.Controls.AddRange(new Control[] {
                new Label { Text = "Config File Path:", Location = new System.Drawing.Point(15, 23), AutoSize = true },
                txtConfigPath
            });

            // XML button panel
            var xmlButtonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                Padding = new Padding(5)
            };

            var btnSaveXml = new Button
            {
                Text = "Save XML Settings",
                Location = new System.Drawing.Point(5, 8),
                Width = 100,
                Height = 23
            };
            btnSaveXml.Click += BtnSaveXml_Click;

            xmlButtonPanel.Controls.Add(btnSaveXml);
            
            xmlPanel.Controls.AddRange(new Control[] { uiGroup, configGroup, xmlButtonPanel });
            xmlTab.Controls.Add(xmlPanel);

            // Add tabs to tab control
            tabControl.TabPages.AddRange(new TabPage[] { jsonTab, xmlTab });
            this.Controls.Add(tabControl);

            // Set up the DataGridView
            dgvFiles.AutoGenerateColumns = false;
            dgvFiles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Name",
                DataPropertyName = "Name",
                HeaderText = "File Name"
            });
            dgvFiles.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "IsExecutable",
                DataPropertyName = "IsExecutable",
                HeaderText = "Is Executable"
            });
            dgvFiles.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "CurrentPathSelect",
                HeaderText = "",
                Text = "...",
                UseColumnTextForButtonValue = true,
                Width = 30
            });
            dgvFiles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "CurrentPath",
                DataPropertyName = "CurrentPath",
                HeaderText = "Current Path"
            });
            dgvFiles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "NewPath",
                DataPropertyName = "NewPath",
                HeaderText = "New Path"
            });
            dgvFiles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "BackupPath",
                DataPropertyName = "BackupPath",
                HeaderText = "Backup Path"
            });
            dgvFiles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "DownloadUrl",
                DataPropertyName = "DownloadUrl",
                HeaderText = "Download URL"
            });
            dgvFiles.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ExpectedHash",
                DataPropertyName = "ExpectedHash",
                HeaderText = "Expected Hash"
            });

            _bindingSource.DataSource = _currentConfig.Files;
            dgvFiles.DataSource = _bindingSource;

            // Add event handlers
            btnAdd.Click += BtnAdd_Click;
            btnRemove.Click += BtnRemove_Click;
            btnSave.Click += BtnSave_Click;
            btnLoad.Click += BtnLoad_Click;
            dgvFiles.CellClick += DgvFiles_CellClick;
        }

        private void LoadSettings()
        {
            try
            {
                _currentSettings = _settingsService.LoadSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading XML settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSaveXml_Click(object sender, EventArgs e)
        {
            try
            {
                _settingsService.SaveSettings(_currentSettings);
                MessageBox.Show("XML Settings saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving XML settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvFiles_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var fileConfig = _currentConfig.Files[e.RowIndex];
            
            // Handle button column clicks
            if (e.ColumnIndex == dgvFiles.Columns["CurrentPathSelect"].Index )
            {
                if (_openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = _openFileDialog.FileName;
                    
                    if (e.ColumnIndex == dgvFiles.Columns["CurrentPathSelect"].Index)
                    {
                        fileConfig.CurrentPath = selectedPath;
                        fileConfig.Name = Path.GetFileName(selectedPath);
                        fileConfig.IsExecutable = Path.GetExtension(selectedPath).ToLower() == ".exe";
                        fileConfig.BackupPath = Path.Combine(Path.GetDirectoryName(selectedPath), "backup", fileConfig.Name);
                        fileConfig.NewPath = Path.Combine(Path.GetDirectoryName(selectedPath), "new", fileConfig.Name);
                        // Update hash when current path is selected
                        _configService.UpdateFileHash(fileConfig);
                    }
                    else if (e.ColumnIndex == dgvFiles.Columns["NewPathSelect"].Index)
                    {
                        fileConfig.NewPath = selectedPath;
                    }
                    else if (e.ColumnIndex == dgvFiles.Columns["BackupPathSelect"].Index)
                    {
                        fileConfig.BackupPath = selectedPath;
                    }

                    _bindingSource.ResetBindings(false);
                }
            }
        }

        private void LoadConfiguration()
        {
            try
            {
                _currentConfig = _configService.LoadConfiguration();
                _bindingSource.DataSource = _currentConfig.Files;
                dgvFiles.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            _currentConfig.Files.Add(new FileConfiguration());
            _bindingSource.ResetBindings(false);
        }

        private void BtnRemove_Click(object sender, EventArgs e)
        {
            if (dgvFiles.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in dgvFiles.SelectedRows)
                {
                    if (row.DataBoundItem is FileConfiguration file)
                    {
                        _currentConfig.Files.Remove(file);
                    }
                }
                _bindingSource.ResetBindings(false);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                _configService.SaveConfiguration(_currentConfig);
                MessageBox.Show("Configuration saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnLoad_Click(object sender, EventArgs e)
        {
            LoadConfiguration();
        }
    }
}
