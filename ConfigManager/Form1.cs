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
        private UpdateConfiguration _currentConfig;
        private BindingSource _bindingSource;
        private OpenFileDialog _openFileDialog;

        public Form1()
        {
            InitializeComponent();
            _configService = new UpdateConfigurationService();
            _currentConfig = new UpdateConfiguration();
            _bindingSource = new BindingSource();
            _openFileDialog = new OpenFileDialog
            {
                Filter = "All files (*.*)|*.*",
                Multiselect = false
            };
            
            InitializeUI();
            LoadConfiguration();
        }

        private void InitializeUI()
        {
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
