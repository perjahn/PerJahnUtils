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
            _dbname = string.Empty;
            _sql = string.Empty;
            _result = string.Empty;

            foreach (var database in databases)
            {
                var connstr = db.ConstructConnectionString(dbprovider, dbserver, database, dbusername, dbpassword);

                using db mydb = new(dbprovider, connstr);
                _sql =
                    "select *" +
                    " from [" + database + "].dbo.sysobjects so" +
                    " join [" + database + "].dbo.syscomments sc on sc.id = so.id" +
                    " where so.type='P' or so.type='PC'" +
                    " order by so.name";

                mydb.FillSchema = false;
                DataTable dt = mydb.ExecuteDataTableSQL(_sql);

                foreach (var dr in dt.Rows)
                {
                    if ((short)dr["colid"] > 1)
                    {
                        // Multipart SP
                        continue;
                    }

                    var name = dr["name"].ToString();

                    var filename = Path.Combine(path, name + ".sql");

                    var sqlwhere = "(type='P' or type='PC') and name='" + name + "'";

                    DataRow[] rows = dt.Select(sqlwhere, "colid");

                    StringBuilder sb = new();

                    foreach (var drPart in rows)
                    {
                        var text = drPart["text"].ToString();
                        sb.Append(text);
                    }

                    var sp = sb.ToString();

                    if (writemodified)
                    {
                        if (File.Exists(filename))
                        {
                            string buf;
                            using StreamReader sr = new(filename);
                            buf = sr.ReadToEnd();
                            if (buf == sp)
                            {
                                continue;
                            }
                        }
                    }

                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    using StreamWriter sw = new(filename);
                    sw.Write(sp);
                }
            }
        }
    }
}
