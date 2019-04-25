namespace RigidViewer
{
    partial class MainForm
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.butOpen = new System.Windows.Forms.Button();
            this.tmr = new System.Windows.Forms.Timer(this.components);
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.txtInfo = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.butReset = new System.Windows.Forms.Button();
            this.panel3 = new System.Windows.Forms.Panel();
            this.butDelete = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.dlgOpen = new System.Windows.Forms.OpenFileDialog();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // butOpen
            // 
            this.butOpen.Dock = System.Windows.Forms.DockStyle.Top;
            this.butOpen.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.butOpen.Location = new System.Drawing.Point(0, 0);
            this.butOpen.Name = "butOpen";
            this.butOpen.Size = new System.Drawing.Size(267, 64);
            this.butOpen.TabIndex = 0;
            this.butOpen.Text = "Open";
            this.butOpen.UseVisualStyleBackColor = true;
            this.butOpen.Click += new System.EventHandler(this.butOpen_Click);
            // 
            // tmr
            // 
            this.tmr.Interval = 1;
            this.tmr.Tick += new System.EventHandler(this.tmr_Tick);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.txtInfo);
            this.splitContainer1.Panel1.Controls.Add(this.panel1);
            this.splitContainer1.Panel1.Padding = new System.Windows.Forms.Padding(3);
            this.splitContainer1.Size = new System.Drawing.Size(1584, 961);
            this.splitContainer1.SplitterDistance = 273;
            this.splitContainer1.TabIndex = 1;
            // 
            // txtInfo
            // 
            this.txtInfo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.txtInfo.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtInfo.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtInfo.Location = new System.Drawing.Point(3, 209);
            this.txtInfo.Multiline = true;
            this.txtInfo.Name = "txtInfo";
            this.txtInfo.ReadOnly = true;
            this.txtInfo.Size = new System.Drawing.Size(267, 749);
            this.txtInfo.TabIndex = 6;
            this.txtInfo.Text = resources.GetString("txtInfo.Text");
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.butReset);
            this.panel1.Controls.Add(this.panel3);
            this.panel1.Controls.Add(this.butDelete);
            this.panel1.Controls.Add(this.panel2);
            this.panel1.Controls.Add(this.butOpen);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(3, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(267, 206);
            this.panel1.TabIndex = 5;
            // 
            // butReset
            // 
            this.butReset.Dock = System.Windows.Forms.DockStyle.Top;
            this.butReset.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.butReset.Location = new System.Drawing.Point(0, 140);
            this.butReset.Name = "butReset";
            this.butReset.Size = new System.Drawing.Size(267, 64);
            this.butReset.TabIndex = 4;
            this.butReset.Text = "Reset";
            this.butReset.UseVisualStyleBackColor = true;
            this.butReset.Click += new System.EventHandler(this.butReset_Click);
            // 
            // panel3
            // 
            this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel3.Location = new System.Drawing.Point(0, 134);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(267, 6);
            this.panel3.TabIndex = 3;
            // 
            // butDelete
            // 
            this.butDelete.Dock = System.Windows.Forms.DockStyle.Top;
            this.butDelete.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.butDelete.Location = new System.Drawing.Point(0, 70);
            this.butDelete.Name = "butDelete";
            this.butDelete.Size = new System.Drawing.Size(267, 64);
            this.butDelete.TabIndex = 2;
            this.butDelete.Text = "Delete Active Coordinate System";
            this.butDelete.UseVisualStyleBackColor = true;
            this.butDelete.Click += new System.EventHandler(this.butDelete_Click);
            // 
            // panel2
            // 
            this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel2.Location = new System.Drawing.Point(0, 64);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(267, 6);
            this.panel2.TabIndex = 1;
            // 
            // dlgOpen
            // 
            this.dlgOpen.Filter = "OFF|*.off";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.ClientSize = new System.Drawing.Size(1584, 961);
            this.Controls.Add(this.splitContainer1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.Text = "Rigid";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button butOpen;
        private System.Windows.Forms.Timer tmr;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Button butDelete;
        private System.Windows.Forms.OpenFileDialog dlgOpen;
        private System.Windows.Forms.TextBox txtInfo;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button butReset;
        private System.Windows.Forms.Panel panel3;
    }
}

