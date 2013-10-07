using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SQLUtil
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			DataTable dtProviders = System.Data.Common.DbProviderFactories.GetFactoryClasses();
			dtProviders.DefaultView.Sort = "Name";
			cbProviders.DataSource = dtProviders;

			string connstrs = Properties.Settings.Default.ConnectionString;
			if (!string.IsNullOrEmpty(connstrs))
			{
				string[] arr = connstrs.Replace("\r", string.Empty).Split('\n');
				foreach (string connstr in arr)
				{
					cbConnstr.Items.Add(connstr);
				}
			}
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			string connstrs = string.Empty;
			foreach (string connstr in cbConnstr.Items)
			{
				if (connstr != string.Empty)
				{
					if (connstrs == string.Empty)
						connstrs = connstr;
					else
						connstrs += Environment.NewLine + connstr;
				}
			}
			Properties.Settings.Default.ConnectionString = connstrs;

			Properties.Settings.Default.Save();
		}

		private void cbConnstr_Leave(object sender, EventArgs e)
		{
			string text = cbConnstr.Text;
			if (!cbConnstr.Items.Contains(text) && !string.IsNullOrEmpty(text))
			{
				cbConnstr.Items.Add(text);
			}
		}

		private void button1_Click(object sender, EventArgs e)
		{
			try
			{
				DataTable dt;
				using (db mydb = new db(cbProviders.SelectedValue.ToString(), cbConnstr.Text))
				{
					mydb.FillSchema = cbSchema.Checked;

					dt = mydb.ExecuteDataTableSQL(tbSql.Text, null);

					if (cbSchema.Checked)
					{
						foreach (DataColumn dc in dt.Columns)
						{
							dc.ColumnName = dc.DataType + "|" + dc.ColumnName + "|";
						}
					}
				}

				if (cbSaveToFile.Checked)
				{
					if (saveFileDialog1.ShowDialog() != DialogResult.OK)
					{
						return;
					}

					string filename = saveFileDialog1.FileName;

					string separator;
					if (saveFileDialog1.FilterIndex == 1)
					{
						separator = ",";  // 1=csv
					}
					else
					{
						separator = "\t";  // 2=tab
					}

					using (System.IO.StreamWriter sw = new System.IO.StreamWriter(filename))
					{
						for (int c = 0; c < dt.Columns.Count; c++)
						{
							sw.Write((c == 0 ? string.Empty : separator) + dt.Columns[c].ColumnName);
						}
						sw.WriteLine();

						foreach (DataRow dr in dt.Rows)
						{
							for (int c = 0; c < dt.Columns.Count; c++)
							{
								sw.Write((c == 0 ? string.Empty : separator) + dr[c]);
							}
							sw.WriteLine();
						}
					}
				}
				else
				{
					dataGridView1.DataSource = null;  // Forget column order
					dataGridView1.DataSource = dt;
				}
				this.Text = "SQL Util - Rows: " + dt.Rows.Count + ", Columns: " + dt.Columns.Count;

			}
			catch (Exception ex)
			{
				System.Windows.Forms.MessageBox.Show(ex.ToString());
			}
		}
	}
}
