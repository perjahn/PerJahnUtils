namespace SQLUtil
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.cbProviders = new System.Windows.Forms.ComboBox();
            this.tbSql = new System.Windows.Forms.TextBox();
            this.cbSchema = new System.Windows.Forms.CheckBox();
            this.cbConnstr = new System.Windows.Forms.ComboBox();
            this.cbSaveToFile = new System.Windows.Forms.CheckBox();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            //
            // label1
            //
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(92, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "&Connection string:";
            //
            // button1
            //
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Location = new System.Drawing.Point(850, 11);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 3;
            this.button1.Text = "Go!";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            //
            // dataGridView1
            //
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(12, 222);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(984, 496);
            this.dataGridView1.TabIndex = 6;
            //
            // cbProviders
            //
            this.cbProviders.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbProviders.DisplayMember = "Name";
            this.cbProviders.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbProviders.FormattingEnabled = true;
            this.cbProviders.Location = new System.Drawing.Point(594, 12);
            this.cbProviders.MaxDropDownItems = 30;
            this.cbProviders.Name = "cbProviders";
            this.cbProviders.Size = new System.Drawing.Size(250, 21);
            this.cbProviders.TabIndex = 2;
            this.cbProviders.ValueMember = "InvariantName";
            //
            // tbSql
            //
            this.tbSql.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbSql.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::SQLUtil.Properties.Settings.Default, "SQL", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.tbSql.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbSql.Location = new System.Drawing.Point(12, 41);
            this.tbSql.Multiline = true;
            this.tbSql.Name = "tbSql";
            this.tbSql.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbSql.Size = new System.Drawing.Size(984, 175);
            this.tbSql.TabIndex = 5;
            this.tbSql.Text = global::SQLUtil.Properties.Settings.Default.SQL;
            //
            // cbSchema
            //
            this.cbSchema.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbSchema.AutoSize = true;
            this.cbSchema.Location = new System.Drawing.Point(931, 4);
            this.cbSchema.Name = "cbSchema";
            this.cbSchema.Size = new System.Drawing.Size(65, 17);
            this.cbSchema.TabIndex = 4;
            this.cbSchema.Text = "Schema";
            this.cbSchema.UseVisualStyleBackColor = true;
            //
            // cbConnstr
            //
            this.cbConnstr.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbConnstr.FormattingEnabled = true;
            this.cbConnstr.Location = new System.Drawing.Point(110, 12);
            this.cbConnstr.Name = "cbConnstr";
            this.cbConnstr.Size = new System.Drawing.Size(478, 21);
            this.cbConnstr.TabIndex = 7;
            this.cbConnstr.Leave += new System.EventHandler(this.cbConnstr_Leave);
            //
            // cbSaveToFile
            //
            this.cbSaveToFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbSaveToFile.AutoSize = true;
            this.cbSaveToFile.Location = new System.Drawing.Point(931, 21);
            this.cbSaveToFile.Name = "cbSaveToFile";
            this.cbSaveToFile.Size = new System.Drawing.Size(79, 17);
            this.cbSaveToFile.TabIndex = 8;
            this.cbSaveToFile.Text = "Save to file";
            this.cbSaveToFile.UseVisualStyleBackColor = true;
            //
            // saveFileDialog1
            //
            this.saveFileDialog1.Filter = "csv files|*.txt|tab files|*.txt";
            this.saveFileDialog1.FilterIndex = 2;
            this.saveFileDialog1.RestoreDirectory = true;
            //
            // Form1
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1008, 730);
            this.Controls.Add(this.cbSaveToFile);
            this.Controls.Add(this.cbConnstr);
            this.Controls.Add(this.cbSchema);
            this.Controls.Add(this.cbProviders);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.tbSql);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label1);
            this.Name = "Form1";
            this.Text = "SQL Util";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox tbSql;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.ComboBox cbProviders;
        private System.Windows.Forms.CheckBox cbSchema;
        private System.Windows.Forms.ComboBox cbConnstr;
        private System.Windows.Forms.CheckBox cbSaveToFile;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
    }
}

