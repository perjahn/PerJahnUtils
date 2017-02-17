using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DBUtil
{
    class Import
    {
        public string dbprovider { get; set; }
        public string dbserver { get; set; }
        public string dbdatabase { get; set; }
        public string dbusername { get; set; }
        public string dbpassword { get; set; }

        public void ImportData(string InputPath, string TableName)
        {
            string[] rows = File.ReadAllLines(InputPath).Where(r => r.Length > 0).ToArray();
            int colcount = 0;
            StringBuilder sb = new StringBuilder();

            for (int row = 0; row < rows.Length; row++)
            {
                string[] data = rows[row].Split('\t');

                if (row == 0)
                {
                    colcount = data.Length;
                }
                else
                {
                    if (data.Length != colcount)
                    {
                        sb.AppendLine("Error on row: " + row + ", had " + data.Length + " columns, should have had " + colcount);
                    }
                }
            }

            string error = sb.ToString();
            if (error != string.Empty)
            {
                MessageBox.Show(error);
                return;
            }

            using (db mydb = new db(dbprovider, dbserver, dbdatabase, dbusername, dbpassword))
            {
                int batchsize = 10000;
                sb = new StringBuilder();

                for (int row = 0; row < rows.Length; row++)
                {
                    if (row == 0)
                    {
                        string[] colnames = rows[row].Split('\t');
                        continue;
                    }

                    sb.AppendLine("insert into " + TableName + " values('" + string.Join("','", rows[row].Split('\t')) + "');");

                    string sql = string.Empty;

                    try
                    {
                        if (row % batchsize == 0 || row == rows.Length - 1)
                        {
                            sql = sb.ToString();
                            mydb.ExecuteNonQuerySQL(sql);
                            sb = new StringBuilder();
                        }
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show("Row" + row + ": " + sql.Substring(0, 10000) + Environment.NewLine + ex.ToString());
                        return;
                    }
                }
            }
        }
    }
}
