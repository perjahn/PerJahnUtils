using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DBSearch
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			// Retrieve the installed providers and factories.
			DataTable dtProviders = System.Data.Common.DbProviderFactories.GetFactoryClasses();

			cbProvider.DataSource = dtProviders;

			cbProvider.Text = "SqlClient Data Provider";
			cbSearchtype.SelectedIndex = 0;

			DateTime now = DateTime.Now;
			dtp1.Value = now.AddDays(-1);
			dtp2.Value = now.AddDays(-1);
			dtp3.Value = now;
			dtp4.Value = now;

			cbServer.Items.Add(@"(local)");
			cbServer.Items.Add(@".");
			cbServer.Items.Add(@".\SQLEXPRESS");
			cbServer.Items.Add(@"SOME_SERVER");
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			Properties.Settings.Default.Save();
		}

		private void cbSearchtype_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (cbSearchtype.Text == "Date interval")
			{
				tbSearch.Visible = false;
				dtp1.Visible = true;
				dtp2.Visible = true;
				dtp3.Visible = true;
				dtp4.Visible = true;
			}
			else
			{
				tbSearch.Visible = true;
				tbSearch.Top = 13;
				tbSearch.Left = 79;
				dtp1.Visible = false;
				dtp2.Visible = false;
				dtp3.Visible = false;
				dtp4.Visible = false;
			}
		}

		private void btSearch_Click(object sender, EventArgs e)
		{
			Searcher search = new Searcher();

			try
			{
				this.Text = "Searching...";

				listView1.Items.Clear();
				listView1.Columns.Clear();

				if (cbSQLLogin.Checked)
				{
					search.username = tbUsername.Text;
					search.password = tbPassword.Text;
				}
				else
				{
					search.username = null;
					search.password = null;
				}

				search.provider = cbProvider.SelectedValue.ToString();

				string[] dbs = tbDatabases.Text.Replace("\r", "").Split('\n').Select(db => db.Trim()).Where(db => db != "").ToArray();

				switch (cbSearchtype.Text)
				{
					case "Exact match":
						search.searchtype = Searcher.SearchType.exact;
						break;
					case "SQL Like":
						search.searchtype = Searcher.SearchType.sqllike;
						break;
					case "Date interval":
						search.searchtype = Searcher.SearchType.dateinterval;
						break;
					default:
						return;
				}

				search.maxhits = int.Parse(tbMaxHits.Text);
				search.server = cbServer.Text;
				search.databases = dbs;

				DataTable dtResult;

				if (rbData.Checked)
				{
					listView1.Columns.Add("Database", 100);
					listView1.Columns.Add("Table", 100);
					listView1.Columns.Add("Column", 100);
					listView1.Columns.Add("Value", 275);

					DateTime from, to;
					from = dtp1.Value.Date + dtp2.Value.TimeOfDay;
					to = dtp3.Value.Date + dtp4.Value.TimeOfDay;

					dtResult = search.SearchData(tbSearch.Text, from, to);
				}
				else
				{
					listView1.Columns.Add("Database", 125);
					listView1.Columns.Add("Name", 350);
					listView1.Columns.Add("Value", 100);

					dtResult = search.SearchObjects(tbSearch.Text);
				}

				foreach (DataRow dr in dtResult.Rows)
				{
					System.Windows.Forms.ListViewItem lvi = new System.Windows.Forms.ListViewItem();
					lvi.Text = dr[0].ToString();
					for (int c = 1; c < dtResult.Columns.Count; c++)
					{
						lvi.SubItems.Add(dr[c].ToString());
					}

					listView1.Items.Add(lvi);
				}

				this.Text = search.stats.ToString();
			}
			catch (System.Exception ex)
			{
				ShowError(ex.ToString());
			}

			string log = search.log.ToString();
			if (log != string.Empty)
			{
				ShowError(log);
			}
		}

		private void btGetDBs_Click(object sender, EventArgs e)
		{
			try
			{
				GetAllDBs();
			}
			catch (System.Exception ex)
			{
				ShowError(ex.ToString());
			}
		}

		private void GetAllDBs()
		{
			List<string> searchlist;

			string connstr;
			string sql;
			string provider = cbProvider.SelectedValue.ToString();


			// Get database names
			if (provider == "System.Data.SqlClient")
			{
				if (cbSQLLogin.Checked)
				{
					connstr = "Server=" + cbServer.Text + "; Database=master; Trusted_Connection=False; User ID=" + tbUsername.Text + "; Password=" + tbPassword.Text + ";";
				}
				else
				{
					connstr = "Server=" + cbServer.Text + "; Database=master; Trusted_Connection=True;";
				}
				sql = "select name from sysdatabases order by name";
			}
			else if (provider == "MySql.Data.MySqlClient")
			{
				connstr = "Data Source=" + cbServer.Text + "; Database=information_schema; User ID=" + tbUsername.Text + "; Password=" + tbPassword.Text + ";";
				sql = "select schema_name name from schemata order by schema_name";
			}
			else  // Unknown provider
			{
				throw new NotImplementedException("Unsupported database provider: '" + provider + "'.");
			}

			System.Data.DataTable dtDBs;

			string dbs = string.Empty;

			using (db mydb = new db(provider, connstr))
			{
				dtDBs = mydb.ExecuteDataTableSQL(sql, null);
			}


			if (cbDBPattern.Checked)
			{
				searchlist = new List<string>();
				foreach (string row in tbDatabases.Text.Replace("\r", "").Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
				{
					searchlist.Add(row);
				}
			}
			else
				searchlist = null;


			foreach (DataRow drDB in dtDBs.Rows)
			{
				if (searchlist == null)
				{
					dbs += drDB["name"].ToString() + Environment.NewLine;
				}
				else
				{
					string dbname = drDB["name"].ToString();

					foreach (string search in searchlist)
					{
						if (System.Text.RegularExpressions.Regex.IsMatch(
										dbname, search, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
						{
							dbs += dbname + Environment.NewLine;
							break;
						}
					}
				}
			}

			tbDatabases.Text = dbs.TrimEnd();

			return;
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

		private void copyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			StringBuilder sb = new StringBuilder();
			int rownum = 0;
			foreach (System.Windows.Forms.ListViewItem lvi in listView1.Items)
			{
				if (lvi.Selected)
				{
					if (rownum > 0)
						sb.Append(Environment.NewLine);
					foreach (System.Windows.Forms.ListViewItem.ListViewSubItem lvsi in lvi.SubItems)
					{
						sb.Append("\t" + lvsi.Text);
					}
					rownum++;
				}
			}
			// Only insert into clipboard if any rows selected
			if (rownum > 0)
			{
				string result = sb.ToString();
				System.Windows.Forms.Clipboard.SetText(result);
			}
		}

		private void copyColumn(int colindex)
		{
			StringBuilder sb = new StringBuilder();
			int rownum = 0;
			foreach (System.Windows.Forms.ListViewItem lvi in listView1.Items)
			{
				if (lvi.Selected)
				{
					System.Windows.Forms.ListViewItem.ListViewSubItem lvsi = lvi.SubItems[colindex];
					if (rownum > 0)
						sb.Append(Environment.NewLine);
					sb.Append(lvsi.Text);
					rownum++;
				}
			}
			// Only insert into clipboard if any rows selected
			if (rownum > 0)
			{
				string result = sb.ToString();
				System.Windows.Forms.Clipboard.SetText(result);
			}
		}

		private void copyNameToolStripMenuItem_Click(object sender, EventArgs e)
		{
			copyColumn(listView1.Columns.Count - 2);
		}

		private void copyValueToolStripMenuItem_Click(object sender, EventArgs e)
		{
			copyColumn(listView1.Columns.Count - 1);
		}

		private void cbServer_Leave(object sender, EventArgs e)
		{
			bool found = false;
			foreach (string s in cbServer.Items)
			{
				if (s == cbServer.Text)
				{
					found = true;
					break;
				}
			}
			if (!found)
			{
				cbServer.Items.Add(cbServer.Text);
			}
		}

		private void ShowError(string error)
		{
			Error win = new Error();
			win.textBox1.Text = error;

			win.ShowDialog();
		}
	}
}
