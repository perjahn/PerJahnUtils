using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace DBUtil
{
    class ExportScripts
    {
        public string _dbname;
        public string _sql;

        public string _result;

        public string[] databases { get; set; }
        public string path { get; set; }
        public bool writemodified { get; set; }  // Only write to file if db != existing file content

        bool singlescript { get; set; }

        public string dbprovider { get; set; }
        public string dbserver { get; set; }
        public string dbusername { get; set; }
        public string dbpassword { get; set; }

        public void Export()
        {
            _dbname = "";
            _sql = "";
            _result = "";


            foreach (string database in databases)
            {
                string connstr = db.ConstructConnectionString(
                    dbprovider, dbserver, database, dbusername, dbpassword);

                using (db mydb = new db(dbprovider, connstr))
                {
                    _sql =
                    "select *" +
                    " from [" + database + "].dbo.sysobjects so" +
                    " join [" + database + "].dbo.syscomments sc on sc.id = so.id" +
                    " where so.type='P' or so.type='PC'" +
                    " order by so.name";

                    mydb.FillSchema = false;
                    DataTable dt = mydb.ExecuteDataTableSQL(_sql);

                    foreach (DataRow dr in dt.Rows)
                    {
                        if ((short)dr["colid"] > 1)
                        {
                            // Multipart SP
                            continue;
                        }

                        string name = dr["name"].ToString();

                        string filename = Path.Combine(path, name + ".sql");

                        string sqlwhere = "(type='P' or type='PC') and name='" + name + "'";

                        DataRow[] rows = dt.Select(sqlwhere, "colid");

                        StringBuilder sb = new StringBuilder();

                        foreach (DataRow drPart in rows)
                        {
                            string text = drPart["text"].ToString();
                            sb.Append(text);
                        }

                        string sp = sb.ToString();

                        if (writemodified)
                        {
                            if (File.Exists(filename))
                            {
                                string buf;
                                using (StreamReader sr = new StreamReader(filename))
                                {
                                    buf = sr.ReadToEnd();
                                }
                                if (buf == sp)
                                {
                                    continue;
                                }
                            }
                        }

                        if (!Directory.Exists(path))
                            Directory.CreateDirectory(path);

                        using (StreamWriter sw = new StreamWriter(filename))
                        {
                            sw.Write(sp);
                        }
                    }
                }
            }

            return;
        }
    }
}
