namespace DBSearch
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.tbPassword = new System.Windows.Forms.TextBox();
            this.lblPassword = new System.Windows.Forms.Label();
            this.tbUsername = new System.Windows.Forms.TextBox();
            this.lblUsername = new System.Windows.Forms.Label();
            this.cbSQLLogin = new System.Windows.Forms.CheckBox();
            this.cbDBPattern = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.rbDBObjects = new System.Windows.Forms.RadioButton();
            this.rbData = new System.Windows.Forms.RadioButton();
            this.tbMaxHits = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.btGetDBs = new System.Windows.Forms.Button();
            this.listView1 = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyNameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyValueToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.label6 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.btSearch = new System.Windows.Forms.Button();
            this.cbProvider = new System.Windows.Forms.ComboBox();
            this.cbServer = new System.Windows.Forms.ComboBox();
            this.tbSearch = new System.Windows.Forms.TextBox();
            this.tbDatabases = new System.Windows.Forms.TextBox();
            this.cbSearchtype = new System.Windows.Forms.ComboBox();
            this.dtp1 = new System.Windows.Forms.DateTimePicker();
            this.dtp2 = new System.Windows.Forms.DateTimePicker();
            this.dtp3 = new System.Windows.Forms.DateTimePicker();
            this.dtp4 = new System.Windows.Forms.DateTimePicker();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            //
            // groupBox2
            //
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.tbPassword);
            this.groupBox2.Controls.Add(this.lblPassword);
            this.groupBox2.Controls.Add(this.tbUsername);
            this.groupBox2.Controls.Add(this.lblUsername);
            this.groupBox2.Controls.Add(this.cbSQLLogin);
            this.groupBox2.Location = new System.Drawing.Point(545, 143);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(227, 75);
            this.groupBox2.TabIndex = 41;
            this.groupBox2.TabStop = false;
            //
            // tbPassword
            //
            this.tbPassword.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::DBSearch.Properties.Settings.Default, "Password", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.tbPassword.Enabled = false;
            this.tbPassword.Location = new System.Drawing.Point(70, 45);
            this.tbPassword.Name = "tbPassword";
            this.tbPassword.PasswordChar = '*';
            this.tbPassword.Size = new System.Drawing.Size(146, 20);
            this.tbPassword.TabIndex = 46;
            this.tbPassword.Text = global::DBSearch.Properties.Settings.Default.Password;
            //
            // lblPassword
            //
            this.lblPassword.AutoSize = true;
            this.lblPassword.Enabled = false;
            this.lblPassword.Location = new System.Drawing.Point(6, 48);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(56, 13);
            this.lblPassword.TabIndex = 45;
            this.lblPassword.Text = "&Password:";
            //
            // tbUsername
            //
            this.tbUsername.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::DBSearch.Properties.Settings.Default, "Username", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.tbUsername.Enabled = false;
            this.tbUsername.Location = new System.Drawing.Point(70, 19);
            this.tbUsername.Name = "tbUsername";
            this.tbUsername.Size = new System.Drawing.Size(146, 20);
            this.tbUsername.TabIndex = 44;
            this.tbUsername.Text = global::DBSearch.Properties.Settings.Default.Username;
            //
            // lblUsername
            //
            this.lblUsername.AutoSize = true;
            this.lblUsername.Enabled = false;
            this.lblUsername.Location = new System.Drawing.Point(6, 22);
            this.lblUsername.Name = "lblUsername";
            this.lblUsername.Size = new System.Drawing.Size(58, 13);
            this.lblUsername.TabIndex = 43;
            this.lblUsername.Text = "&Username:";
            //
            // cbSQLLogin
            //
            this.cbSQLLogin.AutoSize = true;
            this.cbSQLLogin.Location = new System.Drawing.Point(6, 0);
            this.cbSQLLogin.Name = "cbSQLLogin";
            this.cbSQLLogin.Size = new System.Drawing.Size(118, 17);
            this.cbSQLLogin.TabIndex = 42;
            this.cbSQLLogin.Text = "SQL Authentication";
            this.cbSQLLogin.UseVisualStyleBackColor = true;
            this.cbSQLLogin.CheckedChanged += new System.EventHandler(this.cbSQLLogin_CheckedChanged);
            //
            // cbDBPattern
            //
            this.cbDBPattern.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbDBPattern.AutoSize = true;
            this.cbDBPattern.Location = new System.Drawing.Point(551, 122);
            this.cbDBPattern.Name = "cbDBPattern";
            this.cbDBPattern.Size = new System.Drawing.Size(77, 17);
            this.cbDBPattern.TabIndex = 34;
            this.cbDBPattern.Text = "DB pattern";
            this.cbDBPattern.UseVisualStyleBackColor = true;
            //
            // groupBox1
            //
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.rbDBObjects);
            this.groupBox1.Controls.Add(this.rbData);
            this.groupBox1.Location = new System.Drawing.Point(632, 68);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(140, 69);
            this.groupBox1.TabIndex = 38;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Search Options";
            //
            // rbDBObjects
            //
            this.rbDBObjects.AutoSize = true;
            this.rbDBObjects.Location = new System.Drawing.Point(6, 42);
            this.rbDBObjects.Name = "rbDBObjects";
            this.rbDBObjects.Size = new System.Drawing.Size(108, 17);
            this.rbDBObjects.TabIndex = 40;
            this.rbDBObjects.Text = "Database objects";
            this.rbDBObjects.UseVisualStyleBackColor = true;
            //
            // rbData
            //
            this.rbData.AutoSize = true;
            this.rbData.Checked = true;
            this.rbData.Location = new System.Drawing.Point(6, 19);
            this.rbData.Name = "rbData";
            this.rbData.Size = new System.Drawing.Size(48, 17);
            this.rbData.TabIndex = 39;
            this.rbData.TabStop = true;
            this.rbData.Text = "Data";
            this.rbData.UseVisualStyleBackColor = true;
            //
            // tbMaxHits
            //
            this.tbMaxHits.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.tbMaxHits.Location = new System.Drawing.Point(722, 42);
            this.tbMaxHits.Name = "tbMaxHits";
            this.tbMaxHits.Size = new System.Drawing.Size(50, 20);
            this.tbMaxHits.TabIndex = 37;
            this.tbMaxHits.Text = "100";
            //
            // label3
            //
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(623, 45);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(93, 13);
            this.label3.TabIndex = 36;
            this.label3.Text = "&Max hits per table:";
            //
            // btGetDBs
            //
            this.btGetDBs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btGetDBs.Location = new System.Drawing.Point(545, 93);
            this.btGetDBs.Name = "btGetDBs";
            this.btGetDBs.Size = new System.Drawing.Size(75, 23);
            this.btGetDBs.TabIndex = 33;
            this.btGetDBs.Text = "Get all DBs";
            this.btGetDBs.UseVisualStyleBackColor = true;
            this.btGetDBs.Click += new System.EventHandler(this.btGetDBs_Click);
            //
            // listView1
            //
            this.listView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4});
            this.listView1.ContextMenuStrip = this.contextMenuStrip1;
            this.listView1.FullRowSelect = true;
            this.listView1.HideSelection = false;
            this.listView1.Location = new System.Drawing.Point(12, 224);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(760, 327);
            this.listView1.TabIndex = 50;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            //
            // columnHeader1
            //
            this.columnHeader1.Text = "Database";
            this.columnHeader1.Width = 100;
            //
            // columnHeader2
            //
            this.columnHeader2.Text = "Table";
            this.columnHeader2.Width = 100;
            //
            // columnHeader3
            //
            this.columnHeader3.Text = "Column";
            this.columnHeader3.Width = 100;
            //
            // columnHeader4
            //
            this.columnHeader4.Text = "Value";
            this.columnHeader4.Width = 275;
            //
            // contextMenuStrip1
            //
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyToolStripMenuItem,
            this.copyNameToolStripMenuItem,
            this.copyValueToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(180, 70);
            //
            // copyToolStripMenuItem
            //
            this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            this.copyToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
            this.copyToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
            this.copyToolStripMenuItem.Text = "&Copy";
            this.copyToolStripMenuItem.Click += new System.EventHandler(this.copyToolStripMenuItem_Click);
            //
            // copyNameToolStripMenuItem
            //
            this.copyNameToolStripMenuItem.Name = "copyNameToolStripMenuItem";
            this.copyNameToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
            this.copyNameToolStripMenuItem.Text = "Copy &name column";
            this.copyNameToolStripMenuItem.Click += new System.EventHandler(this.copyNameToolStripMenuItem_Click);
            //
            // copyValueToolStripMenuItem
            //
            this.copyValueToolStripMenuItem.Name = "copyValueToolStripMenuItem";
            this.copyValueToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
            this.copyValueToolStripMenuItem.Text = "Copy &value column";
            this.copyValueToolStripMenuItem.Click += new System.EventHandler(this.copyValueToolStripMenuItem_Click);
            //
            // label6
            //
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 16);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(31, 13);
            this.label6.TabIndex = 20;
            this.label6.Text = "&Text:";
            //
            // label2
            //
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 97);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(61, 13);
            this.label2.TabIndex = 31;
            this.label2.Text = "&Databases:";
            //
            // label5
            //
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 42);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(49, 13);
            this.label5.TabIndex = 27;
            this.label5.Text = "&Provider:";
            //
            // label1
            //
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 69);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(41, 13);
            this.label1.TabIndex = 29;
            this.label1.Text = "&Server:";
            //
            // btSearch
            //
            this.btSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btSearch.Location = new System.Drawing.Point(697, 12);
            this.btSearch.Name = "btSearch";
            this.btSearch.Size = new System.Drawing.Size(75, 23);
            this.btSearch.TabIndex = 35;
            this.btSearch.Text = "Go!";
            this.btSearch.UseVisualStyleBackColor = true;
            this.btSearch.Click += new System.EventHandler(this.btSearch_Click);
            //
            // cbProvider
            //
            this.cbProvider.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbProvider.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::DBSearch.Properties.Settings.Default, "Provider", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbProvider.DisplayMember = "Name";
            this.cbProvider.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbProvider.FormattingEnabled = true;
            this.cbProvider.Location = new System.Drawing.Point(79, 39);
            this.cbProvider.Name = "cbProvider";
            this.cbProvider.Size = new System.Drawing.Size(460, 21);
            this.cbProvider.TabIndex = 28;
            this.cbProvider.Text = global::DBSearch.Properties.Settings.Default.Provider;
            this.cbProvider.ValueMember = "InvariantName";
            //
            // cbServer
            //
            this.cbServer.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbServer.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::DBSearch.Properties.Settings.Default, "Server", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbServer.FormattingEnabled = true;
            this.cbServer.Location = new System.Drawing.Point(79, 66);
            this.cbServer.Name = "cbServer";
            this.cbServer.Size = new System.Drawing.Size(460, 21);
            this.cbServer.TabIndex = 30;
            this.cbServer.Text = global::DBSearch.Properties.Settings.Default.Server;
            //
            // tbSearch
            //
            this.tbSearch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbSearch.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::DBSearch.Properties.Settings.Default, "SearchText", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.tbSearch.Location = new System.Drawing.Point(79, 13);
            this.tbSearch.Name = "tbSearch";
            this.tbSearch.Size = new System.Drawing.Size(460, 20);
            this.tbSearch.TabIndex = 21;
            this.tbSearch.Text = global::DBSearch.Properties.Settings.Default.SearchText;
            //
            // tbDatabases
            //
            this.tbDatabases.AcceptsReturn = true;
            this.tbDatabases.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbDatabases.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::DBSearch.Properties.Settings.Default, "Databases", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.tbDatabases.Location = new System.Drawing.Point(79, 93);
            this.tbDatabases.Multiline = true;
            this.tbDatabases.Name = "tbDatabases";
            this.tbDatabases.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbDatabases.Size = new System.Drawing.Size(460, 125);
            this.tbDatabases.TabIndex = 32;
            this.tbDatabases.Text = global::DBSearch.Properties.Settings.Default.Databases;
            //
            // cbSearchtype
            //
            this.cbSearchtype.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbSearchtype.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbSearchtype.FormattingEnabled = true;
            this.cbSearchtype.Items.AddRange(new object[] {
            "Exact match",
            "SQL Like",
            "Date interval"});
            this.cbSearchtype.Location = new System.Drawing.Point(545, 13);
            this.cbSearchtype.Name = "cbSearchtype";
            this.cbSearchtype.Size = new System.Drawing.Size(121, 21);
            this.cbSearchtype.TabIndex = 26;
            this.cbSearchtype.SelectedIndexChanged += new System.EventHandler(this.cbSearchtype_SelectedIndexChanged);
            //
            // dtp1
            //
            this.dtp1.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtp1.Location = new System.Drawing.Point(79, 13);
            this.dtp1.Name = "dtp1";
            this.dtp1.Size = new System.Drawing.Size(125, 20);
            this.dtp1.TabIndex = 22;
            this.dtp1.Value = new System.DateTime(2011, 4, 20, 1, 23, 0, 0);
            //
            // dtp2
            //
            this.dtp2.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dtp2.Location = new System.Drawing.Point(210, 13);
            this.dtp2.Name = "dtp2";
            this.dtp2.ShowUpDown = true;
            this.dtp2.Size = new System.Drawing.Size(85, 20);
            this.dtp2.TabIndex = 23;
            this.dtp2.Value = new System.DateTime(2011, 5, 1, 1, 23, 45, 0);
            //
            // dtp3
            //
            this.dtp3.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtp3.Location = new System.Drawing.Point(301, 13);
            this.dtp3.Name = "dtp3";
            this.dtp3.Size = new System.Drawing.Size(125, 20);
            this.dtp3.TabIndex = 24;
            //
            // dtp4
            //
            this.dtp4.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dtp4.Location = new System.Drawing.Point(432, 13);
            this.dtp4.Name = "dtp4";
            this.dtp4.ShowUpDown = true;
            this.dtp4.Size = new System.Drawing.Size(85, 20);
            this.dtp4.TabIndex = 25;
            this.dtp4.Value = new System.DateTime(2011, 5, 2, 1, 23, 45, 0);
            //
            // Form1
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 564);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.cbProvider);
            this.Controls.Add(this.cbDBPattern);
            this.Controls.Add(this.cbServer);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.tbMaxHits);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.btGetDBs);
            this.Controls.Add(this.listView1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tbDatabases);
            this.Controls.Add(this.btSearch);
            this.Controls.Add(this.cbSearchtype);
            this.Controls.Add(this.dtp4);
            this.Controls.Add(this.dtp3);
            this.Controls.Add(this.dtp2);
            this.Controls.Add(this.dtp1);
            this.Controls.Add(this.tbSearch);
            this.Controls.Add(this.label6);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "DB Search";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox tbPassword;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.TextBox tbUsername;
        private System.Windows.Forms.Label lblUsername;
        private System.Windows.Forms.CheckBox cbSQLLogin;
        private System.Windows.Forms.ComboBox cbProvider;
        private System.Windows.Forms.CheckBox cbDBPattern;
        private System.Windows.Forms.ComboBox cbServer;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton rbDBObjects;
                private System.Windows.Forms.RadioButton rbData;
        private System.Windows.Forms.TextBox tbMaxHits;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btGetDBs;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyNameToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyValueToolStripMenuItem;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbSearch;
        private System.Windows.Forms.TextBox tbDatabases;
                private System.Windows.Forms.Button btSearch;
                private System.Windows.Forms.ComboBox cbSearchtype;
                private System.Windows.Forms.DateTimePicker dtp1;
                private System.Windows.Forms.DateTimePicker dtp2;
                private System.Windows.Forms.DateTimePicker dtp3;
                private System.Windows.Forms.DateTimePicker dtp4;
    }
}

