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
        ((System.ComponentModel.ISupportInitialize)(this.dgvFiles)).BeginInit();
        this.SuspendLayout();
        // 
        // dgvFiles
        // 
        this.dgvFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
        this.dgvFiles.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
        this.dgvFiles.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this.dgvFiles.Location = new System.Drawing.Point(0, 0);
        this.dgvFiles.Name = "dgvFiles";
        this.dgvFiles.Size = new System.Drawing.Size(800, 400);
        this.dgvFiles.TabIndex = 0;
        // 
        // btnAdd
        // 
        this.btnAdd.Text = "Add";
        this.btnAdd.Name = "btnAdd";
        this.btnAdd.Size = new System.Drawing.Size(75, 23);
        this.btnAdd.TabIndex = 1;
        this.btnAdd.UseVisualStyleBackColor = true;
        // 
        // btnRemove
        // 
        this.btnRemove.Text = "Remove";
        this.btnRemove.Name = "btnRemove";
        this.btnRemove.Size = new System.Drawing.Size(75, 23);
        this.btnRemove.TabIndex = 2;
        this.btnRemove.UseVisualStyleBackColor = true;
        // 
        // btnSave
        // 
        this.btnSave.Text = "Save";
        this.btnSave.Name = "btnSave";
        this.btnSave.Size = new System.Drawing.Size(75, 23);
        this.btnSave.TabIndex = 3;
        this.btnSave.UseVisualStyleBackColor = true;
        // 
        // btnLoad
        // 
        this.btnLoad.Text = "Load";
        this.btnLoad.Name = "btnLoad";
        this.btnLoad.Size = new System.Drawing.Size(75, 23);
        this.btnLoad.TabIndex = 4;
        this.btnLoad.UseVisualStyleBackColor = true;
        // 
        // Form1
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(800, 450);
        this.Name = "Form1";
        this.Text = "Program Updater Configuration Manager";
        ((System.ComponentModel.ISupportInitialize)(this.dgvFiles)).EndInit();
        this.ResumeLayout(false);
    }

        #endregion

        private System.Windows.Forms.DataGridView dgvFiles;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnLoad;
    }
}
