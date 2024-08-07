using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;

namespace ExportDataTable
{
    public class Db : IDisposable
    {
        private MySqlConnection _cn = null;

        private int CommandTimeout { get; set; }  // Ctor sets this to 600s (default is 60s)

        #region Dtor/Ctor
        public Db(string connstr)
        {
            _cn = new MySqlConnection(connstr);
            _cn.Open();
            CommandTimeout = 600;
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
        #endregion Dtor/Ctor

        #region NonQuery
        public int ExecuteNonQuerySP(string SPName)
        {
            return ExecuteNonQuery(SPName, CommandType.StoredProcedure);
        }

        public int ExecuteNonQuerySQL(string sql)
        {
            return ExecuteNonQuery(sql, CommandType.Text);
        }

        private int ExecuteNonQuery(string sql, CommandType ct)
        {
            using var cmd = _cn.CreateCommand();

            cmd.Connection = _cn;
            cmd.CommandType = ct;
            cmd.CommandText = sql;
            cmd.CommandTimeout = CommandTimeout;

            var iReturnValue = cmd.ExecuteNonQuery();

            return iReturnValue;
        }
        #endregion NonQuery

        #region Scalar
        public object ExecuteScalarSP(string sql, Dictionary<string, object> parameters = null)
        {
            return ExecuteScalar(sql, parameters, CommandType.StoredProcedure);
        }

        public object ExecuteScalarSQL(string sql, Dictionary<string, object> parameters = null)
        {
            return ExecuteScalar(sql, parameters, CommandType.Text);
        }

        private object ExecuteScalar(string sql, Dictionary<string, object> parameters, CommandType ct)
        {
            using var cmd = _cn.CreateCommand();

            cmd.Connection = _cn;
            cmd.CommandType = ct;
            cmd.CommandText = sql;
            cmd.CommandTimeout = CommandTimeout;

            if (parameters != null)
            {
                foreach (var pair in parameters)
                {
                    MySqlParameter p = cmd.CreateParameter();
                    p.ParameterName = pair.Key.ToString();
                    p.Value = pair.Value;
                    cmd.Parameters.Add(p);
                }
            }

            return cmd.ExecuteScalar();
        }
        #endregion Scalar

        #region Reader
        public MySqlDataReader ExecuteReaderSP(string sql, Dictionary<string, object> parameters = null)
        {
            return ExecuteReader(sql, parameters, CommandType.StoredProcedure);
        }

        public MySqlDataReader ExecuteReaderSQL(string sql, Dictionary<string, object> parameters = null)
        {
            return ExecuteReader(sql, parameters, CommandType.Text);
        }

        private MySqlDataReader ExecuteReader(string sql, Dictionary<string, object> parameters, CommandType ct)
        {
            using var cmd = _cn.CreateCommand();

            cmd.Connection = _cn;
            cmd.CommandType = ct;
            cmd.CommandText = sql;
            cmd.CommandTimeout = CommandTimeout;

            if (parameters != null)
            {
                foreach (var pair in parameters)
                {
                    MySqlParameter p = cmd.CreateParameter();
                    p.ParameterName = pair.Key.ToString();
                    p.Value = pair.Value;
                    cmd.Parameters.Add(p);
                }
            }

            return cmd.ExecuteReader();
        }
        #endregion Reader
    }
}
