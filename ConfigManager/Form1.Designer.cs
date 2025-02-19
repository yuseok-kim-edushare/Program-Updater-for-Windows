namespace ProgramUpdater.ConfigManager
{
    partial class Form1
    {
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
            }
            base.Dispose(disposing);
        }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.dgvFiles = new System.Windows.Forms.DataGridView();
        this.btnAdd = new System.Windows.Forms.Button();
        this.btnRemove = new System.Windows.Forms.Button();
        this.btnSave = new System.Windows.Forms.Button();
        this.btnLoad = new System.Windows.Forms.Button();
        this.panel1 = new System.Windows.Forms.Panel();
        ((System.ComponentModel.ISupportInitialize)(this.dgvFiles)).BeginInit();
        this.panel1.SuspendLayout();
        this.SuspendLayout();
        // 
        // dgvFiles
        // 
        this.dgvFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
        this.dgvFiles.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this.dgvFiles.Location = new System.Drawing.Point(12, 12);
        this.dgvFiles.Name = "dgvFiles";
        this.dgvFiles.Size = new System.Drawing.Size(776, 397);
        this.dgvFiles.TabIndex = 0;
        // 
        // panel1
        // 
        this.panel1.Controls.Add(this.btnLoad);
        this.panel1.Controls.Add(this.btnSave);
        this.panel1.Controls.Add(this.btnRemove);
        this.panel1.Controls.Add(this.btnAdd);
        this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.panel1.Location = new System.Drawing.Point(0, 415);
        this.panel1.Name = "panel1";
        this.panel1.Size = new System.Drawing.Size(800, 35);
        this.panel1.TabIndex = 1;
        // 
        // btnAdd
        // 
        this.btnAdd.Location = new System.Drawing.Point(12, 6);
        this.btnAdd.Name = "btnAdd";
        this.btnAdd.Size = new System.Drawing.Size(75, 23);
        this.btnAdd.TabIndex = 0;
        this.btnAdd.Text = "Add";
        this.btnAdd.UseVisualStyleBackColor = true;
        // 
        // btnRemove
        // 
        this.btnRemove.Location = new System.Drawing.Point(93, 6);
        this.btnRemove.Name = "btnRemove";
        this.btnRemove.Size = new System.Drawing.Size(75, 23);
        this.btnRemove.TabIndex = 1;
        this.btnRemove.Text = "Remove";
        this.btnRemove.UseVisualStyleBackColor = true;
        // 
        // btnSave
        // 
        this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.btnSave.Location = new System.Drawing.Point(632, 6);
        this.btnSave.Name = "btnSave";
        this.btnSave.Size = new System.Drawing.Size(75, 23);
        this.btnSave.TabIndex = 2;
        this.btnSave.Text = "Save";
        this.btnSave.UseVisualStyleBackColor = true;
        // 
        // btnLoad
        // 
        this.btnLoad.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.btnLoad.Location = new System.Drawing.Point(713, 6);
        this.btnLoad.Name = "btnLoad";
        this.btnLoad.Size = new System.Drawing.Size(75, 23);
        this.btnLoad.TabIndex = 3;
        this.btnLoad.Text = "Load";
        this.btnLoad.UseVisualStyleBackColor = true;
        // 
        // Form1
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(800, 450);
        this.Controls.Add(this.panel1);
        this.Controls.Add(this.dgvFiles);
        this.Name = "Form1";
        this.Text = "Program Updater Configuration Manager";
        ((System.ComponentModel.ISupportInitialize)(this.dgvFiles)).EndInit();
        this.panel1.ResumeLayout(false);
        this.ResumeLayout(false);
    }

        #endregion

        private System.Windows.Forms.DataGridView dgvFiles;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnLoad;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.Button btnAdd;
    }
}
