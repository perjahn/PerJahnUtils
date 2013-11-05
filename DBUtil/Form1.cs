using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DBUtil
{
	public partial class Form1 : Form
	{
		DataTable _dtProviders;

		public Form1()
		{
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			// Retrieve the installed providers and factories.
			_dtProviders = System.Data.Common.DbProviderFactories.GetFactoryClasses();
			_dtProviders.DefaultView.Sort = "Name";

			cbProvider.DataSource = _dtProviders;
			cbProvider.Text = "SqlClient Data Provider";

			foreach (string dbserver in Properties.Settings.Default.DBServers.Replace("\r", "").Split('\n'))
			{
				cbServer.Items.Add(dbserver);
			}

			cbSeparator.Text = "Tab";

			cbExtension.Text = "bin";
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			StringBuilder sb = new StringBuilder();
			bool first = true;
			foreach (string dbserver in cbServer.Items)
			{
				string s = dbserver.Trim();
				if (s != string.Empty)
				{
					sb.Append(first ? dbserver : Environment.NewLine + dbserver);
					first = false;
				}
			}
			Properties.Settings.Default.DBServers = sb.ToString();

			Properties.Settings.Default.Save();
		}

		private void cbServer_Leave(object sender, EventArgs e)
		{
			string s = cbServer.Text.Trim();

			if (s != string.Empty && !cbServer.Items.Contains(s))
			{
				cbServer.Items.Add(s);
			}
		}

		private void btGetAllDBs_Click(object sender, EventArgs e)
		{
			try
			{
				GetAllDBs();
			}
			catch (System.Exception ex)
			{
				System.Windows.Forms.MessageBox.Show(ex.ToString());
			}
		}

		private void btExport_Click(object sender, EventArgs e)
		{
			DumpData Dump = new DumpData();

			try
			{
				string[] dbs = tbDatabases.Text.Replace("\r", "").Split('\n').Select(db => db.Trim()).Where(db => db != "").ToArray();

				Dump.databases = dbs;
				Dump.path = tbOutputPath.Text;
				Dump.exportempty = cbExportEmpty.Checked;

				Dump.maxrows = cbMaxrows.Checked ? int.Parse(tbMaxrows.Text) : -1;
				Dump.binaryhex = cbBinaryhex.Checked;
				Dump.binaryfile = cbBinaryfile.Checked;

				Dump.excludeTables = tbExcludeTables.Text.Replace("\r", "").Split('\n').Select(table => table.Trim()).Where(table => table != "").ToArray();
				if (Dump.excludeTables.Length == 1 && string.IsNullOrEmpty(Dump.excludeTables[0]))
				{
					Dump.excludeTables = new string[] { };
				}
				Dump.useRegexpTables = cbUseRegexpTables.Checked;

				Dump.excludeColumns = tbExcludeColumns.Text.Replace("\r", "").Split('\n').Select(col => col.Trim()).Where(col => col != "").ToArray();
				if (Dump.excludeColumns.Length == 1 && string.IsNullOrEmpty(Dump.excludeColumns[0]))
				{
					Dump.excludeColumns = new string[] { };
				}
				Dump.useRegexpColumns = cbUseRegexpColumns.Checked;

				Dump.sortColumns = cbSortColumns.Checked;
				Dump.sortRows = cbSortRows.Checked;

				switch (cbSeparator.Text)
				{
					case "Comma":
						Dump.separator = ",";
						break;
					case "Tab":
						Dump.separator = "\t";
						break;
					default:
						System.Windows.Forms.MessageBox.Show("No separator selected!");
						return;
				}

				Dump.overwrite = cbOverwrite.Checked;
				Dump.header = cbHeader.Checked;
				Dump.escapecharacters = cbEscapecharacters.Checked;
				Dump.extension = cbExtension.Text;

				Dump.dbprovider = cbProvider.SelectedValue.ToString();
				Dump.dbserver = cbServer.Text;

				if (cbSQLLogin.Checked)
				{
					Dump.dbusername = tbUsername.Text;
					Dump.dbpassword = tbPassword.Text;
				}
				else
				{
					Dump.dbusername = null;
					Dump.dbpassword = null;
				}

				Dump.Dump();

				this.Text = Dump._result;
			}
			catch (System.Exception ex)
			{
				System.Windows.Forms.MessageBox.Show(
								"DB: " + Dump._dbname + "\n" +
								"Table: " + Dump._tablename + "\n" +
								"SQL: " + Dump._sql + "\n" +
								ex.ToString());
			}
		}

		private void GetAllDBs()
		{
			string[] dbs;

			if (cbSQLLogin.Checked)
			{
				dbs = db.GetAllDatabases(
					cbProvider.SelectedValue.ToString(), cbServer.Text, tbUsername.Text, tbPassword.Text);
			}
			else
			{
				dbs = db.GetAllDatabases(
					cbProvider.SelectedValue.ToString(), cbServer.Text, null, null);
			}

			StringBuilder sbdbs = new StringBuilder();
			foreach (string db in dbs)
			{
				sbdbs.AppendLine(db);
			}

			tbDatabases.Text = sbdbs.ToString().TrimEnd();
		}

		private void cbSQLLogin_CheckedChanged(object sender, EventArgs e)
		{
			if (cbSQLLogin.Checked)
			{
				lblPassword.Enabled = true;
				tbPassword.Enabled = true;
				lblUsername.Enabled = true;
				tbUsername.Enabled = true;
			}
			else
			{
				lblPassword.Enabled = false;
				tbPassword.Enabled = false;
				lblUsername.Enabled = false;
				tbUsername.Enabled = false;
			}
		}

		private void btImportBlob_Click(object sender, EventArgs e)
		{
			ImportBlob imp = new ImportBlob();

			try
			{
				string[] dbs = tbDatabases.Text.Replace("\r", "").Split('\n').Select(db => db.Trim()).Where(db => db != "").ToArray();

				if (dbs.Length != 1)
				{
					MessageBox.Show("Can only import blob into 1 database at a time.");
					return;
				}

				imp.dbprovider = cbProvider.SelectedValue.ToString();
				imp.dbserver = cbServer.Text;
				imp.dbdatabase = dbs[0];

				if (cbSQLLogin.Checked)
				{
					imp.dbusername = tbUsername.Text;
					imp.dbpassword = tbPassword.Text;
				}
				else
				{
					imp.dbusername = null;
					imp.dbpassword = null;
				}

				imp.InsertFileIntoCell(tbSelect.Text, tbColumn.Text, tbFilename.Text);
			}
			catch (System.Exception ex)
			{
				System.Windows.Forms.MessageBox.Show(ex.ToString());
			}
		}

		private void btExportScripts_Click(object sender, EventArgs e)
		{
			ExportScripts es = new ExportScripts();

			try
			{
				string[] dbs = tbDatabases.Text.Replace("\r", "").Split('\n').Select(db => db.Trim()).Where(db => db != "").ToArray();

				es.dbprovider = cbProvider.SelectedValue.ToString();
				es.dbserver = cbServer.Text;
				es.databases = dbs;
				es.path = tbOutputPathScripts.Text;
				es.writemodified = cbWriteModified.Checked;

				if (cbSQLLogin.Checked)
				{
					es.dbusername = tbUsername.Text;
					es.dbpassword = tbPassword.Text;
				}
				else
				{
					es.dbusername = null;
					es.dbpassword = null;
				}

				es.Export();

				this.Text = es._result;
			}
			catch (System.Exception ex)
			{
				System.Windows.Forms.MessageBox.Show(
								"DB: " + es._dbname + "\n" +
								"SQL: " + es._sql + "\n" +
								ex.ToString());
			}
		}

		private void btImport_Click(object sender, EventArgs e)
		{
			Import imp = new Import();

			try
			{
				string[] dbs = tbDatabases.Text.Replace("\r", "").Split('\n').Select(db => db.Trim()).Where(db => db != "").ToArray();

				if (dbs.Length != 1)
				{
					MessageBox.Show("Can only import data into 1 database at a time.");
					return;
				}

				imp.dbprovider = cbProvider.SelectedValue.ToString();
				imp.dbserver = cbServer.Text;
				imp.dbdatabase = dbs[0];

				if (cbSQLLogin.Checked)
				{
					imp.dbusername = tbUsername.Text;
					imp.dbpassword = tbPassword.Text;
				}
				else
				{
					imp.dbusername = null;
					imp.dbpassword = null;
				}

				imp.ImportData(tbInputPath.Text);
			}
			catch (System.Exception ex)
			{
				System.Windows.Forms.MessageBox.Show(ex.ToString());
			}
		}
	}
}
