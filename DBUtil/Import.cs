using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DBUtil
{
    class Import
    {
        public string dbprovider { get; set; }
        public string dbserver { get; set; }
        public string dbdatabase { get; set; }
        public string dbusername { get; set; }
        public string dbpassword { get; set; }

        public void ImportData(string InputPath)
        {
            using db mydb = new(dbprovider, dbserver, dbdatabase, dbusername, dbpassword);
            /*DataTable dt;

            dt = mydb.ExecuteDataTableSQL(select);

            if (dt.Rows.Count != 1)
            {
                MessageBox.Show("Error: Found " + dt.Rows.Count + " rows.");
                return;
            }

            DataRow dr = dt.Rows[0];

            FileInfo fi = new FileInfo(filename);

            int filesize = (int)fi.Length;

            byte[] buf = new byte[filesize];

            using FileStream fs = new(filename, FileMode.Open);
            fs.Read(buf, 0, filesize);

            dr[column] = buf;

            mydb.UpdateDataTable(select, dt);*/
        }
    }
}
