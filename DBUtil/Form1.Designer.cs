namespace DBUtil
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
			this.cbProvider = new System.Windows.Forms.ComboBox();
			this.cbSeparator = new System.Windows.Forms.ComboBox();
			this.cbExtension = new System.Windows.Forms.ComboBox();
			this.cbBinaryhex = new System.Windows.Forms.CheckBox();
			this.cbBinaryfile = new System.Windows.Forms.CheckBox();
			this.tbMaxrows = new System.Windows.Forms.TextBox();
			this.cbOverwrite = new System.Windows.Forms.CheckBox();
			this.cbSQLLogin = new System.Windows.Forms.CheckBox();
			this.cbHeader = new System.Windows.Forms.CheckBox();
			this.cbEscapecharacters = new System.Windows.Forms.CheckBox();
			this.lblPassword = new System.Windows.Forms.Label();
			this.lblUsername = new System.Windows.Forms.Label();
			this.tbSelect = new System.Windows.Forms.TextBox();
			this.tbColumn = new System.Windows.Forms.TextBox();
			this.tbFilename = new System.Windows.Forms.TextBox();
			this.cbWriteModified = new System.Windows.Forms.CheckBox();
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this.label4 = new System.Windows.Forms.Label();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.cbMaxrows = new System.Windows.Forms.CheckBox();
			this.cbSortColumns = new System.Windows.Forms.CheckBox();
			this.cbUseRegexpColumns = new System.Windows.Forms.CheckBox();
			this.tbExcludeColumns = new System.Windows.Forms.TextBox();
			this.label13 = new System.Windows.Forms.Label();
			this.cbUseRegexpTables = new System.Windows.Forms.CheckBox();
			this.tbExcludeTables = new System.Windows.Forms.TextBox();
			this.label12 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.btExport = new System.Windows.Forms.Button();
			this.tbOutputPath = new System.Windows.Forms.TextBox();
			this.tabPage2 = new System.Windows.Forms.TabPage();
			this.btImportBlob = new System.Windows.Forms.Button();
			this.label10 = new System.Windows.Forms.Label();
			this.label9 = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.tabPage3 = new System.Windows.Forms.TabPage();
			this.btExportScripts = new System.Windows.Forms.Button();
			this.label11 = new System.Windows.Forms.Label();
			this.tbOutputPathScripts = new System.Windows.Forms.TextBox();
			this.tabPage4 = new System.Windows.Forms.TabPage();
			this.btImport = new System.Windows.Forms.Button();
			this.tbInputPath = new System.Windows.Forms.TextBox();
			this.label7 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.btGetAllDBs = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.label3 = new System.Windows.Forms.Label();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.tbUsername = new System.Windows.Forms.TextBox();
			this.tbPassword = new System.Windows.Forms.TextBox();
			this.cbServer = new System.Windows.Forms.ComboBox();
			this.tbDatabases = new System.Windows.Forms.TextBox();
			this.cbExportEmpty = new System.Windows.Forms.CheckBox();
			this.tabControl1.SuspendLayout();
			this.tabPage1.SuspendLayout();
			this.groupBox3.SuspendLayout();
			this.tabPage2.SuspendLayout();
			this.tabPage3.SuspendLayout();
			this.tabPage4.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
			// 
			// cbProvider
			// 
			this.cbProvider.DisplayMember = "Name";
			this.cbProvider.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbProvider.FormattingEnabled = true;
			this.cbProvider.Location = new System.Drawing.Point(98, 19);
			this.cbProvider.Name = "cbProvider";
			this.cbProvider.Size = new System.Drawing.Size(300, 21);
			this.cbProvider.TabIndex = 2;
			this.cbProvider.ValueMember = "InvariantName";
			// 
			// cbSeparator
			// 
			this.cbSeparator.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbSeparator.FormattingEnabled = true;
			this.cbSeparator.Items.AddRange(new object[] {
            "Comma",
            "Tab"});
			this.cbSeparator.Location = new System.Drawing.Point(130, 65);
			this.cbSeparator.Name = "cbSeparator";
			this.cbSeparator.Size = new System.Drawing.Size(100, 21);
			this.cbSeparator.TabIndex = 27;
			// 
			// cbExtension
			// 
			this.cbExtension.FormattingEnabled = true;
			this.cbExtension.Items.AddRange(new object[] {
            "bin",
            "jpg",
            "jpeg"});
			this.cbExtension.Location = new System.Drawing.Point(130, 138);
			this.cbExtension.Name = "cbExtension";
			this.cbExtension.Size = new System.Drawing.Size(100, 21);
			this.cbExtension.TabIndex = 31;
			// 
			// cbBinaryhex
			// 
			this.cbBinaryhex.AutoSize = true;
			this.cbBinaryhex.Checked = true;
			this.cbBinaryhex.CheckState = System.Windows.Forms.CheckState.Checked;
			this.cbBinaryhex.Location = new System.Drawing.Point(6, 92);
			this.cbBinaryhex.Name = "cbBinaryhex";
			this.cbBinaryhex.Size = new System.Drawing.Size(150, 17);
			this.cbBinaryhex.TabIndex = 28;
			this.cbBinaryhex.Text = "Write binary as hex values";
			this.cbBinaryhex.UseVisualStyleBackColor = true;
			// 
			// cbBinaryfile
			// 
			this.cbBinaryfile.AutoSize = true;
			this.cbBinaryfile.Location = new System.Drawing.Point(6, 115);
			this.cbBinaryfile.Name = "cbBinaryfile";
			this.cbBinaryfile.Size = new System.Drawing.Size(152, 17);
			this.cbBinaryfile.TabIndex = 29;
			this.cbBinaryfile.Text = "Write binary as external file";
			this.cbBinaryfile.UseVisualStyleBackColor = true;
			// 
			// tbMaxrows
			// 
			this.tbMaxrows.Location = new System.Drawing.Point(130, 188);
			this.tbMaxrows.Name = "tbMaxrows";
			this.tbMaxrows.Size = new System.Drawing.Size(100, 20);
			this.tbMaxrows.TabIndex = 34;
			this.tbMaxrows.Text = "1000000";
			// 
			// cbOverwrite
			// 
			this.cbOverwrite.AutoSize = true;
			this.cbOverwrite.Location = new System.Drawing.Point(6, 165);
			this.cbOverwrite.Name = "cbOverwrite";
			this.cbOverwrite.Size = new System.Drawing.Size(130, 17);
			this.cbOverwrite.TabIndex = 32;
			this.cbOverwrite.Text = "Overwrite existing files";
			this.cbOverwrite.UseVisualStyleBackColor = true;
			// 
			// cbSQLLogin
			// 
			this.cbSQLLogin.AutoSize = true;
			this.cbSQLLogin.Location = new System.Drawing.Point(6, 0);
			this.cbSQLLogin.Name = "cbSQLLogin";
			this.cbSQLLogin.Size = new System.Drawing.Size(117, 17);
			this.cbSQLLogin.TabIndex = 9;
			this.cbSQLLogin.Text = "SQL authentication";
			this.cbSQLLogin.UseVisualStyleBackColor = true;
			this.cbSQLLogin.CheckedChanged += new System.EventHandler(this.cbSQLLogin_CheckedChanged);
			// 
			// cbHeader
			// 
			this.cbHeader.AutoSize = true;
			this.cbHeader.Checked = true;
			this.cbHeader.CheckState = System.Windows.Forms.CheckState.Checked;
			this.cbHeader.Location = new System.Drawing.Point(6, 19);
			this.cbHeader.Name = "cbHeader";
			this.cbHeader.Size = new System.Drawing.Size(149, 17);
			this.cbHeader.TabIndex = 24;
			this.cbHeader.Text = "Column names on first row";
			this.cbHeader.UseVisualStyleBackColor = true;
			// 
			// cbEscapecharacters
			// 
			this.cbEscapecharacters.AutoSize = true;
			this.cbEscapecharacters.Checked = true;
			this.cbEscapecharacters.CheckState = System.Windows.Forms.CheckState.Checked;
			this.cbEscapecharacters.Location = new System.Drawing.Point(6, 42);
			this.cbEscapecharacters.Name = "cbEscapecharacters";
			this.cbEscapecharacters.Size = new System.Drawing.Size(115, 17);
			this.cbEscapecharacters.TabIndex = 25;
			this.cbEscapecharacters.Text = "Escape characters";
			this.cbEscapecharacters.UseVisualStyleBackColor = true;
			// 
			// lblPassword
			// 
			this.lblPassword.AutoSize = true;
			this.lblPassword.Enabled = false;
			this.lblPassword.Location = new System.Drawing.Point(25, 25);
			this.lblPassword.Name = "lblPassword";
			this.lblPassword.Size = new System.Drawing.Size(58, 13);
			this.lblPassword.TabIndex = 10;
			this.lblPassword.Text = "&Username:";
			// 
			// lblUsername
			// 
			this.lblUsername.AutoSize = true;
			this.lblUsername.Enabled = false;
			this.lblUsername.Location = new System.Drawing.Point(25, 51);
			this.lblUsername.Name = "lblUsername";
			this.lblUsername.Size = new System.Drawing.Size(56, 13);
			this.lblUsername.TabIndex = 12;
			this.lblUsername.Text = "&Password:";
			// 
			// tbSelect
			// 
			this.tbSelect.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tbSelect.Location = new System.Drawing.Point(78, 6);
			this.tbSelect.Multiline = true;
			this.tbSelect.Name = "tbSelect";
			this.tbSelect.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.tbSelect.Size = new System.Drawing.Size(585, 210);
			this.tbSelect.TabIndex = 52;
			// 
			// tbColumn
			// 
			this.tbColumn.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tbColumn.Location = new System.Drawing.Point(78, 222);
			this.tbColumn.Name = "tbColumn";
			this.tbColumn.Size = new System.Drawing.Size(585, 20);
			this.tbColumn.TabIndex = 54;
			// 
			// tbFilename
			// 
			this.tbFilename.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tbFilename.Location = new System.Drawing.Point(78, 248);
			this.tbFilename.Name = "tbFilename";
			this.tbFilename.Size = new System.Drawing.Size(585, 20);
			this.tbFilename.TabIndex = 56;
			// 
			// cbWriteModified
			// 
			this.cbWriteModified.AutoSize = true;
			this.cbWriteModified.Checked = true;
			this.cbWriteModified.CheckState = System.Windows.Forms.CheckState.Checked;
			this.cbWriteModified.Location = new System.Drawing.Point(9, 32);
			this.cbWriteModified.Name = "cbWriteModified";
			this.cbWriteModified.Size = new System.Drawing.Size(122, 17);
			this.cbWriteModified.TabIndex = 63;
			this.cbWriteModified.Text = "Only write if modified";
			this.cbWriteModified.UseVisualStyleBackColor = true;
			// 
			// tabControl1
			// 
			this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tabControl1.Controls.Add(this.tabPage1);
			this.tabControl1.Controls.Add(this.tabPage2);
			this.tabControl1.Controls.Add(this.tabPage3);
			this.tabControl1.Controls.Add(this.tabPage4);
			this.tabControl1.Location = new System.Drawing.Point(12, 250);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(758, 351);
			this.tabControl1.TabIndex = 19;
			// 
			// tabPage1
			// 
			this.tabPage1.Controls.Add(this.label4);
			this.tabPage1.Controls.Add(this.groupBox3);
			this.tabPage1.Controls.Add(this.btExport);
			this.tabPage1.Controls.Add(this.tbOutputPath);
			this.tabPage1.Location = new System.Drawing.Point(4, 22);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage1.Size = new System.Drawing.Size(750, 325);
			this.tabPage1.TabIndex = 0;
			this.tabPage1.Text = "Export data";
			this.tabPage1.UseVisualStyleBackColor = true;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(6, 9);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(66, 13);
			this.label4.TabIndex = 21;
			this.label4.Text = "&Output path:";
			// 
			// groupBox3
			// 
			this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox3.Controls.Add(this.cbMaxrows);
			this.groupBox3.Controls.Add(this.cbExportEmpty);
			this.groupBox3.Controls.Add(this.cbSortColumns);
			this.groupBox3.Controls.Add(this.cbUseRegexpColumns);
			this.groupBox3.Controls.Add(this.tbExcludeColumns);
			this.groupBox3.Controls.Add(this.label13);
			this.groupBox3.Controls.Add(this.cbUseRegexpTables);
			this.groupBox3.Controls.Add(this.tbExcludeTables);
			this.groupBox3.Controls.Add(this.label12);
			this.groupBox3.Controls.Add(this.label6);
			this.groupBox3.Controls.Add(this.label5);
			this.groupBox3.Controls.Add(this.cbHeader);
			this.groupBox3.Controls.Add(this.cbEscapecharacters);
			this.groupBox3.Controls.Add(this.tbMaxrows);
			this.groupBox3.Controls.Add(this.cbExtension);
			this.groupBox3.Controls.Add(this.cbOverwrite);
			this.groupBox3.Controls.Add(this.cbSeparator);
			this.groupBox3.Controls.Add(this.cbBinaryhex);
			this.groupBox3.Controls.Add(this.cbBinaryfile);
			this.groupBox3.Location = new System.Drawing.Point(6, 32);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(738, 288);
			this.groupBox3.TabIndex = 23;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "Options";
			// 
			// cbMaxrows
			// 
			this.cbMaxrows.AutoSize = true;
			this.cbMaxrows.Location = new System.Drawing.Point(6, 190);
			this.cbMaxrows.Name = "cbMaxrows";
			this.cbMaxrows.Size = new System.Drawing.Size(118, 17);
			this.cbMaxrows.TabIndex = 33;
			this.cbMaxrows.Text = "Max rows per table:";
			this.cbMaxrows.UseVisualStyleBackColor = true;
			// 
			// cbSortColumns
			// 
			this.cbSortColumns.AutoSize = true;
			this.cbSortColumns.Location = new System.Drawing.Point(6, 214);
			this.cbSortColumns.Name = "cbSortColumns";
			this.cbSortColumns.Size = new System.Drawing.Size(87, 17);
			this.cbSortColumns.TabIndex = 35;
			this.cbSortColumns.Text = "Sort columns";
			this.cbSortColumns.UseVisualStyleBackColor = true;
			// 
			// cbUseRegexpColumns
			// 
			this.cbUseRegexpColumns.AutoSize = true;
			this.cbUseRegexpColumns.Location = new System.Drawing.Point(585, 19);
			this.cbUseRegexpColumns.Name = "cbUseRegexpColumns";
			this.cbUseRegexpColumns.Size = new System.Drawing.Size(74, 17);
			this.cbUseRegexpColumns.TabIndex = 42;
			this.cbUseRegexpColumns.Text = "Use regex";
			this.cbUseRegexpColumns.UseVisualStyleBackColor = true;
			// 
			// tbExcludeColumns
			// 
			this.tbExcludeColumns.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.tbExcludeColumns.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::DBUtil.Properties.Settings.Default, "ExcludeColumns", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.tbExcludeColumns.Location = new System.Drawing.Point(492, 42);
			this.tbExcludeColumns.Multiline = true;
			this.tbExcludeColumns.Name = "tbExcludeColumns";
			this.tbExcludeColumns.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.tbExcludeColumns.Size = new System.Drawing.Size(240, 240);
			this.tbExcludeColumns.TabIndex = 41;
			this.tbExcludeColumns.Text = global::DBUtil.Properties.Settings.Default.ExcludeColumns;
			// 
			// label13
			// 
			this.label13.AutoSize = true;
			this.label13.Location = new System.Drawing.Point(489, 20);
			this.label13.Name = "label13";
			this.label13.Size = new System.Drawing.Size(90, 13);
			this.label13.TabIndex = 40;
			this.label13.Text = "Exclude &columns:";
			// 
			// cbUseRegexpTables
			// 
			this.cbUseRegexpTables.AutoSize = true;
			this.cbUseRegexpTables.Location = new System.Drawing.Point(328, 19);
			this.cbUseRegexpTables.Name = "cbUseRegexpTables";
			this.cbUseRegexpTables.Size = new System.Drawing.Size(74, 17);
			this.cbUseRegexpTables.TabIndex = 39;
			this.cbUseRegexpTables.Text = "Use regex";
			this.cbUseRegexpTables.UseVisualStyleBackColor = true;
			// 
			// tbExcludeTables
			// 
			this.tbExcludeTables.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.tbExcludeTables.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::DBUtil.Properties.Settings.Default, "ExcludeTables", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.tbExcludeTables.Location = new System.Drawing.Point(246, 42);
			this.tbExcludeTables.Multiline = true;
			this.tbExcludeTables.Name = "tbExcludeTables";
			this.tbExcludeTables.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.tbExcludeTables.Size = new System.Drawing.Size(240, 240);
			this.tbExcludeTables.TabIndex = 38;
			this.tbExcludeTables.Text = global::DBUtil.Properties.Settings.Default.ExcludeTables;
			// 
			// label12
			// 
			this.label12.AutoSize = true;
			this.label12.Location = new System.Drawing.Point(243, 20);
			this.label12.Name = "label12";
			this.label12.Size = new System.Drawing.Size(79, 13);
			this.label12.TabIndex = 37;
			this.label12.Text = "Exclude &tables:";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(22, 141);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(74, 13);
			this.label6.TabIndex = 30;
			this.label6.Text = "File extension:";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(6, 68);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(92, 13);
			this.label5.TabIndex = 26;
			this.label5.Text = "Column separator:";
			// 
			// btExport
			// 
			this.btExport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btExport.Location = new System.Drawing.Point(669, 6);
			this.btExport.Name = "btExport";
			this.btExport.Size = new System.Drawing.Size(75, 23);
			this.btExport.TabIndex = 43;
			this.btExport.Text = "Export";
			this.btExport.UseVisualStyleBackColor = true;
			this.btExport.Click += new System.EventHandler(this.btExport_Click);
			// 
			// tbOutputPath
			// 
			this.tbOutputPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tbOutputPath.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::DBUtil.Properties.Settings.Default, "OutputpathData", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.tbOutputPath.Location = new System.Drawing.Point(78, 6);
			this.tbOutputPath.Name = "tbOutputPath";
			this.tbOutputPath.Size = new System.Drawing.Size(400, 20);
			this.tbOutputPath.TabIndex = 22;
			this.tbOutputPath.Text = global::DBUtil.Properties.Settings.Default.OutputpathData;
			// 
			// tabPage2
			// 
			this.tabPage2.Controls.Add(this.btImportBlob);
			this.tabPage2.Controls.Add(this.label10);
			this.tabPage2.Controls.Add(this.label9);
			this.tabPage2.Controls.Add(this.label8);
			this.tabPage2.Controls.Add(this.tbSelect);
			this.tabPage2.Controls.Add(this.tbColumn);
			this.tabPage2.Controls.Add(this.tbFilename);
			this.tabPage2.Location = new System.Drawing.Point(4, 22);
			this.tabPage2.Name = "tabPage2";
			this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage2.Size = new System.Drawing.Size(750, 325);
			this.tabPage2.TabIndex = 1;
			this.tabPage2.Text = "Import blob";
			this.tabPage2.UseVisualStyleBackColor = true;
			// 
			// btImportBlob
			// 
			this.btImportBlob.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btImportBlob.Location = new System.Drawing.Point(669, 6);
			this.btImportBlob.Name = "btImportBlob";
			this.btImportBlob.Size = new System.Drawing.Size(75, 23);
			this.btImportBlob.TabIndex = 57;
			this.btImportBlob.Text = "Import";
			this.btImportBlob.UseVisualStyleBackColor = true;
			this.btImportBlob.Click += new System.EventHandler(this.btImportBlob_Click);
			// 
			// label10
			// 
			this.label10.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label10.AutoSize = true;
			this.label10.Location = new System.Drawing.Point(6, 251);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(52, 13);
			this.label10.TabIndex = 55;
			this.label10.Text = "&Filename:";
			// 
			// label9
			// 
			this.label9.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(6, 225);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(45, 13);
			this.label9.TabIndex = 53;
			this.label9.Text = "&Column:&";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(6, 9);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(40, 13);
			this.label8.TabIndex = 51;
			this.label8.Text = "&Select:";
			// 
			// tabPage3
			// 
			this.tabPage3.Controls.Add(this.btExportScripts);
			this.tabPage3.Controls.Add(this.label11);
			this.tabPage3.Controls.Add(this.cbWriteModified);
			this.tabPage3.Controls.Add(this.tbOutputPathScripts);
			this.tabPage3.Location = new System.Drawing.Point(4, 22);
			this.tabPage3.Name = "tabPage3";
			this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage3.Size = new System.Drawing.Size(750, 325);
			this.tabPage3.TabIndex = 2;
			this.tabPage3.Text = "Export scripts";
			this.tabPage3.UseVisualStyleBackColor = true;
			// 
			// btExportScripts
			// 
			this.btExportScripts.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btExportScripts.Location = new System.Drawing.Point(669, 6);
			this.btExportScripts.Name = "btExportScripts";
			this.btExportScripts.Size = new System.Drawing.Size(75, 23);
			this.btExportScripts.TabIndex = 64;
			this.btExportScripts.Text = "Export";
			this.btExportScripts.UseVisualStyleBackColor = true;
			this.btExportScripts.Click += new System.EventHandler(this.btExportScripts_Click);
			// 
			// label11
			// 
			this.label11.AutoSize = true;
			this.label11.Location = new System.Drawing.Point(6, 9);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(66, 13);
			this.label11.TabIndex = 61;
			this.label11.Text = "&Output path:";
			// 
			// tbOutputPathScripts
			// 
			this.tbOutputPathScripts.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tbOutputPathScripts.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::DBUtil.Properties.Settings.Default, "OutputpathScripts", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.tbOutputPathScripts.Location = new System.Drawing.Point(78, 6);
			this.tbOutputPathScripts.Name = "tbOutputPathScripts";
			this.tbOutputPathScripts.Size = new System.Drawing.Size(400, 20);
			this.tbOutputPathScripts.TabIndex = 62;
			this.tbOutputPathScripts.Text = global::DBUtil.Properties.Settings.Default.OutputpathScripts;
			// 
			// tabPage4
			// 
			this.tabPage4.Controls.Add(this.btImport);
			this.tabPage4.Controls.Add(this.tbInputPath);
			this.tabPage4.Controls.Add(this.label7);
			this.tabPage4.Location = new System.Drawing.Point(4, 22);
			this.tabPage4.Name = "tabPage4";
			this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage4.Size = new System.Drawing.Size(750, 325);
			this.tabPage4.TabIndex = 3;
			this.tabPage4.Text = "Import data";
			this.tabPage4.UseVisualStyleBackColor = true;
			// 
			// btImport
			// 
			this.btImport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btImport.Location = new System.Drawing.Point(669, 6);
			this.btImport.Name = "btImport";
			this.btImport.Size = new System.Drawing.Size(75, 23);
			this.btImport.TabIndex = 73;
			this.btImport.Text = "Import";
			this.btImport.UseVisualStyleBackColor = true;
			this.btImport.Click += new System.EventHandler(this.btImport_Click);
			// 
			// tbInputPath
			// 
			this.tbInputPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tbInputPath.Location = new System.Drawing.Point(78, 6);
			this.tbInputPath.Name = "tbInputPath";
			this.tbInputPath.Size = new System.Drawing.Size(583, 20);
			this.tbInputPath.TabIndex = 72;
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(6, 9);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(58, 13);
			this.label7.TabIndex = 71;
			this.label7.Text = "&Input path:";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(6, 24);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(49, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "&Provider:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(6, 49);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(41, 13);
			this.label2.TabIndex = 3;
			this.label2.Text = "&Server:";
			// 
			// btGetAllDBs
			// 
			this.btGetAllDBs.Location = new System.Drawing.Point(404, 73);
			this.btGetAllDBs.Name = "btGetAllDBs";
			this.btGetAllDBs.Size = new System.Drawing.Size(75, 23);
			this.btGetAllDBs.TabIndex = 7;
			this.btGetAllDBs.Text = "Get all DBs";
			this.btGetAllDBs.UseVisualStyleBackColor = true;
			this.btGetAllDBs.Click += new System.EventHandler(this.btGetAllDBs_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Controls.Add(this.groupBox2);
			this.groupBox1.Controls.Add(this.btGetAllDBs);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.cbProvider);
			this.groupBox1.Controls.Add(this.cbServer);
			this.groupBox1.Controls.Add(this.tbDatabases);
			this.groupBox1.Location = new System.Drawing.Point(12, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(758, 232);
			this.groupBox1.TabIndex = 0;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Login";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(6, 76);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(61, 13);
			this.label3.TabIndex = 5;
			this.label3.Text = "&Databases:";
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.cbSQLLogin);
			this.groupBox2.Controls.Add(this.tbUsername);
			this.groupBox2.Controls.Add(this.tbPassword);
			this.groupBox2.Controls.Add(this.lblPassword);
			this.groupBox2.Controls.Add(this.lblUsername);
			this.groupBox2.Location = new System.Drawing.Point(404, 126);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(269, 84);
			this.groupBox2.TabIndex = 8;
			this.groupBox2.TabStop = false;
			// 
			// tbUsername
			// 
			this.tbUsername.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::DBUtil.Properties.Settings.Default, "DBUsername", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.tbUsername.Enabled = false;
			this.tbUsername.Location = new System.Drawing.Point(95, 22);
			this.tbUsername.Name = "tbUsername";
			this.tbUsername.Size = new System.Drawing.Size(150, 20);
			this.tbUsername.TabIndex = 11;
			this.tbUsername.Text = global::DBUtil.Properties.Settings.Default.DBUsername;
			// 
			// tbPassword
			// 
			this.tbPassword.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::DBUtil.Properties.Settings.Default, "DBPassword", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.tbPassword.Enabled = false;
			this.tbPassword.Location = new System.Drawing.Point(95, 48);
			this.tbPassword.Name = "tbPassword";
			this.tbPassword.PasswordChar = '*';
			this.tbPassword.Size = new System.Drawing.Size(150, 20);
			this.tbPassword.TabIndex = 13;
			this.tbPassword.Text = global::DBUtil.Properties.Settings.Default.DBPassword;
			// 
			// cbServer
			// 
			this.cbServer.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::DBUtil.Properties.Settings.Default, "DBServer", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.cbServer.FormattingEnabled = true;
			this.cbServer.Location = new System.Drawing.Point(98, 46);
			this.cbServer.Name = "cbServer";
			this.cbServer.Size = new System.Drawing.Size(300, 21);
			this.cbServer.TabIndex = 4;
			this.cbServer.Text = global::DBUtil.Properties.Settings.Default.DBServer;
			this.cbServer.Leave += new System.EventHandler(this.cbServer_Leave);
			// 
			// tbDatabases
			// 
			this.tbDatabases.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::DBUtil.Properties.Settings.Default, "DBDatabases", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
			this.tbDatabases.Location = new System.Drawing.Point(98, 73);
			this.tbDatabases.Multiline = true;
			this.tbDatabases.Name = "tbDatabases";
			this.tbDatabases.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.tbDatabases.Size = new System.Drawing.Size(300, 137);
			this.tbDatabases.TabIndex = 6;
			this.tbDatabases.Text = global::DBUtil.Properties.Settings.Default.DBDatabases;
			// 
			// cbExportEmpty
			// 
			this.cbExportEmpty.AutoSize = true;
			this.cbExportEmpty.Checked = true;
			this.cbExportEmpty.CheckState = System.Windows.Forms.CheckState.Checked;
			this.cbExportEmpty.Location = new System.Drawing.Point(6, 237);
			this.cbExportEmpty.Name = "cbExportEmpty";
			this.cbExportEmpty.Size = new System.Drawing.Size(118, 17);
			this.cbExportEmpty.TabIndex = 36;
			this.cbExportEmpty.Text = "Export empty tables";
			this.cbExportEmpty.UseVisualStyleBackColor = true;
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(782, 613);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.tabControl1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "Form1";
			this.Text = "DBUtil";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
			this.Load += new System.EventHandler(this.Form1_Load);
			this.tabControl1.ResumeLayout(false);
			this.tabPage1.ResumeLayout(false);
			this.tabPage1.PerformLayout();
			this.groupBox3.ResumeLayout(false);
			this.groupBox3.PerformLayout();
			this.tabPage2.ResumeLayout(false);
			this.tabPage2.PerformLayout();
			this.tabPage3.ResumeLayout(false);
			this.tabPage3.PerformLayout();
			this.tabPage4.ResumeLayout(false);
			this.tabPage4.PerformLayout();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ComboBox cbProvider;
		private System.Windows.Forms.ComboBox cbServer;
		private System.Windows.Forms.ComboBox cbSeparator;
		private System.Windows.Forms.ComboBox cbExtension;
		private System.Windows.Forms.TextBox tbOutputPath;
		private System.Windows.Forms.TextBox tbDatabases;
		private System.Windows.Forms.CheckBox cbBinaryhex;
		private System.Windows.Forms.CheckBox cbBinaryfile;
		private System.Windows.Forms.TextBox tbMaxrows;
		private System.Windows.Forms.CheckBox cbOverwrite;
		private System.Windows.Forms.CheckBox cbSQLLogin;
		private System.Windows.Forms.TextBox tbUsername;
		private System.Windows.Forms.TextBox tbPassword;
		private System.Windows.Forms.CheckBox cbHeader;
		private System.Windows.Forms.CheckBox cbEscapecharacters;
		private System.Windows.Forms.Label lblPassword;
		private System.Windows.Forms.Label lblUsername;
		private System.Windows.Forms.TextBox tbSelect;
		private System.Windows.Forms.TextBox tbColumn;
		private System.Windows.Forms.TextBox tbFilename;
		private System.Windows.Forms.TextBox tbOutputPathScripts;
		private System.Windows.Forms.CheckBox cbWriteModified;
		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.TabPage tabPage2;
		private System.Windows.Forms.TabPage tabPage3;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button btGetAllDBs;
		private System.Windows.Forms.Button btExport;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button btImportBlob;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Button btExportScripts;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox tbExcludeTables;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.CheckBox cbUseRegexpTables;
        private System.Windows.Forms.CheckBox cbUseRegexpColumns;
        private System.Windows.Forms.TextBox tbExcludeColumns;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.CheckBox cbSortColumns;
        private System.Windows.Forms.CheckBox cbMaxrows;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.Button btImport;
        private System.Windows.Forms.TextBox tbInputPath;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.CheckBox cbExportEmpty;
	}
}

