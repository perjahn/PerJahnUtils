using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace sqltoelastic
{
    public class db : IDisposable
    {
        private DbConnection _cn = null;
        private DbProviderFactory _factory = null;

        public db(string dbprovider, string connstr)
        {
            DataTable providers = DbProviderFactories.GetFactoryClasses();

            DataRow[] rows = providers.Select($"InvariantName='{dbprovider}'");
            if (rows.Length == 0)
            {
                StringBuilder error = new StringBuilder();
                error.AppendLine($"Couldn't find db provider: '{dbprovider}'");
                error.AppendLine($"Installed db providers:");
                foreach (DataRow row in providers.Rows)
                {
                    error.AppendLine($"'{row["InvariantName"]}'");
                }
                throw new ApplicationException(error.ToString());
            }

            _factory = DbProviderFactories.GetFactory(rows[0]);

            _cn = _factory.CreateConnection();
            _cn.ConnectionString = connstr;

            _cn.Open();
        }

        void IDisposable.Dispose()
        {
            if (_cn != null)
            {
                _cn.Close();
                _cn.Dispose();
                _cn = null;
            }
        }

        public DbDataReader ExecuteReaderSQL(string sql)
        {
            using (DbCommand cmd = _cn.CreateCommand())
            {
                cmd.Connection = _cn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = sql;

                return cmd.ExecuteReader();
            }
        }
    }
}
