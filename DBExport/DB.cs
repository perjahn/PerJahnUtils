using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Threading.Tasks;

namespace DBExport
{
    public class DB : IDisposable
    {
        readonly SqlConnection Connection;

        public DB(string connstr)
        {
            Connection = new SqlConnection(connstr);
            Connection.Open();
        }

        void IDisposable.Dispose()
        {
            if (Connection != null)
            {
                Connection.Close();
                Connection.Dispose();
            }
        }

        public async Task<SqlDataReader> ExecuteReaderSQLAsync(string sql)
        {
            using var cmd = Connection.CreateCommand();

            cmd.Connection = Connection;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = sql;

            return await cmd.ExecuteReaderAsync();
        }
    }
}
