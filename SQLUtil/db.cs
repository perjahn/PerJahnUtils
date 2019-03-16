using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace SQLUtil
{
    public class db : IDisposable
    {
        private DbConnection _cn = null;
        private DbProviderFactory _factory = null;

        public bool FillSchema { get; set; }  // Ctor sets this to true
        private int CommandTimeout { get; set; }  // Ctor sets this to 600s (default is 60s)

        #region Dtor/Ctor
        public db(string connstrname)
        {
            System.Configuration.ConnectionStringSettings connString = System.Configuration.ConfigurationManager.ConnectionStrings[connstrname];

            // Retrieve the installed providers and factories.
            DataTable dtProviders = DbProviderFactories.GetFactoryClasses();

            DataRow[] rows = dtProviders.Select("InvariantName='" + connString.ProviderName + "'");

            _factory = DbProviderFactories.GetFactory(rows[0]);


            _cn = _factory.CreateConnection();
            _cn.ConnectionString = connString.ConnectionString;

            _cn.Open();

            FillSchema = true;
            CommandTimeout = 600;
        }

        public db(string DbProvider, string connstr)
        {
            // Retrieve the installed providers and factories.
            DataTable dtProviders = DbProviderFactories.GetFactoryClasses();

            DataRow[] rows = dtProviders.Select("InvariantName='" + DbProvider + "'");

            _factory = DbProviderFactories.GetFactory(rows[0]);


            _cn = _factory.CreateConnection();
            _cn.ConnectionString = connstr;

            _cn.Open();

            FillSchema = true;
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

        #region DataTable
        /// <summary>
        /// Returns a DataTable object.
        /// </summary>
        /// <param name="SPName">The stored procedure to be executed</param>
        /// <param name="parameters">SQL parameters</param>
        /// <returns>An in memory datatable</returns>
        public DataTable ExecuteDataTableSP(string SPName, Dictionary<string, object> parameters = null)
        {
            return ExecuteDataTable(SPName, parameters, CommandType.StoredProcedure);
        }

        /// <summary>
        /// Returns a DataTable object.
        /// </summary>
        /// <param name="SPName">The stored procedure to be executed</param>
        /// <param name="parameters">SQL parameters</param>
        /// <returns>An in memory datatable</returns>
        public DataTable ExecuteDataTableSQL(string sql, Dictionary<string, object> parameters = null)
        {
            return ExecuteDataTable(sql, parameters, CommandType.Text);
        }

        private DataTable ExecuteDataTable(string sql, Dictionary<string, object> parameters, CommandType ct)
        {
            DataTable dt = new DataTable();

            using (DbCommand cmd = _cn.CreateCommand())
            {
                cmd.Connection = _cn;
                cmd.CommandType = ct;
                cmd.CommandText = sql;
                cmd.CommandTimeout = CommandTimeout;

                using (DbDataAdapter da = _factory.CreateDataAdapter())
                {
                    da.SelectCommand = cmd;

                    if (parameters != null)
                    {
                        foreach (var pair in parameters)
                        {
                            DbParameter p = cmd.CreateParameter();
                            p.ParameterName = pair.Key;
                            p.Value = pair.Value;
                            cmd.Parameters.Add(p);
                        }
                    }

                    if (FillSchema)
                    {
                        da.FillSchema(dt, SchemaType.Source);
                    }
                    da.Fill(dt);
                }
            }

            return dt;
        }

        public void UpdateDataTable(string sql, DataTable dt)
        {
            using (DbCommand cmd = _cn.CreateCommand())
            {
                cmd.Connection = _cn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = sql;
                cmd.CommandTimeout = CommandTimeout;

                using (DbDataAdapter da = _factory.CreateDataAdapter())
                {
                    da.SelectCommand = cmd;

                    DbCommandBuilder cb = _factory.CreateCommandBuilder();
                    cb.DataAdapter = da;
                    da.InsertCommand = cb.GetInsertCommand();
                    da.UpdateCommand = cb.GetUpdateCommand();
                    da.DeleteCommand = cb.GetDeleteCommand();

                    da.Update(dt);
                }
            }

            return;
        }
        #endregion DataTable

        #region DataSet
        /// <summary>
        /// Returns a DataSet object.
        /// </summary>
        /// <param name="SPName">The stored procedure to be executed</param>
        /// <param name="parameters">SQL parameters</param>
        /// <returns>An in memory dataset</returns>
        public DataSet ExecuteDataSetSP(string SPName, Dictionary<string, object> parameters = null)
        {
            return ExecuteDataSet(SPName, parameters, CommandType.StoredProcedure);
        }

        /// <summary>
        /// Returns a DataSet object.
        /// </summary>
        /// <param name="sql">The sql select statement to be executed</param>
        /// <param name="parameters">SQL parameters</param>
        /// <returns>An in memory dataset</returns>
        public DataSet ExecuteDataSetSQL(string sql, Dictionary<string, object> parameters = null)
        {
            return ExecuteDataSet(sql, parameters, CommandType.Text);
        }

        private DataSet ExecuteDataSet(string sql, Dictionary<string, object> parameters, CommandType ct)
        {
            DataSet ds = new DataSet();

            using (DbCommand cmd = _cn.CreateCommand())
            {
                cmd.Connection = _cn;
                cmd.CommandType = ct;
                cmd.CommandText = sql;
                cmd.CommandTimeout = CommandTimeout;

                using (DbDataAdapter da = _factory.CreateDataAdapter())
                {
                    da.SelectCommand = cmd;

                    if (parameters != null)
                    {
                        foreach (var pair in parameters)
                        {
                            DbParameter p = cmd.CreateParameter();
                            p.ParameterName = pair.Key;
                            p.Value = pair.Value;
                            cmd.Parameters.Add(p);
                        }
                    }

                    if (FillSchema)
                    {
                        da.FillSchema(ds, SchemaType.Source);
                    }
                    da.Fill(ds);
                }
            }

            return ds;
        }
        #endregion DataSet

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
            int iReturnValue = 0;

            using (DbCommand cmd = _cn.CreateCommand())
            {
                cmd.Connection = _cn;
                cmd.CommandType = ct;
                cmd.CommandText = sql;
                cmd.CommandTimeout = CommandTimeout;

                iReturnValue = cmd.ExecuteNonQuery();
            }

            return iReturnValue;
        }
        #endregion NonQuery

        #region Scalar
        /// <summary>
        /// Returns an object.
        /// </summary>
        /// <param name="sql">The stored procedure to be executed</param>
        /// <param name="parameters">SQL parameters</param>
        /// <returns>An in memory dataset</returns>
        public object ExecuteScalarSP(string sql, Dictionary<string, object> parameters = null)
        {
            return ExecuteScalar(sql, parameters, CommandType.StoredProcedure);
        }

        /// <summary>
        /// Returns an object.
        /// </summary>
        /// <param name="sql">The sql select statement to be executed</param>
        /// <param name="parameters">SQL parameters</param>
        /// <returns>An in memory dataset</returns>
        public object ExecuteScalarSQL(string sql, Dictionary<string, object> parameters = null)
        {
            return ExecuteScalar(sql, parameters, CommandType.Text);
        }

        private object ExecuteScalar(string sql, Dictionary<string, object> parameters, CommandType ct)
        {
            using (DbCommand cmd = _cn.CreateCommand())
            {
                cmd.Connection = _cn;
                cmd.CommandType = ct;
                cmd.CommandText = sql;
                cmd.CommandTimeout = CommandTimeout;

                if (parameters != null)
                {
                    foreach (var pair in parameters)
                    {
                        DbParameter p = cmd.CreateParameter();
                        p.ParameterName = pair.Key;
                        p.Value = pair.Value;
                        cmd.Parameters.Add(p);
                    }
                }

                return cmd.ExecuteScalar();
            }
        }
        #endregion Scalar

        #region Reader
        /// <summary>
        /// Runs reader.
        /// </summary>
        /// <param name="sql">The stored procedure to be executed</param>
        /// <param name="parameters">SQL parameters</param>
        /// <returns>data</returns>
        public DbDataReader ExecuteReaderSP(string sql, Dictionary<string, object> parameters = null)
        {
            return ExecuteReader(sql, parameters, CommandType.StoredProcedure);
        }

        /// <summary>
        /// Runs reader.
        /// </summary>
        /// <param name="sql">The sql select statement to be executed</param>
        /// <param name="parameters">SQL parameters</param>
        /// <returns>data</returns>
        public DbDataReader ExecuteReaderSQL(string sql, Dictionary<string, object> parameters = null)
        {
            return ExecuteReader(sql, parameters, CommandType.Text);
        }

        private DbDataReader ExecuteReader(string sql, Dictionary<string, object> parameters, CommandType ct)
        {
            using (DbCommand cmd = _cn.CreateCommand())
            {
                cmd.Connection = _cn;
                cmd.CommandType = ct;
                cmd.CommandText = sql;
                cmd.CommandTimeout = CommandTimeout;

                if (parameters != null)
                {
                    foreach (var pair in parameters)
                    {
                        DbParameter p = cmd.CreateParameter();
                        p.ParameterName = pair.Key;
                        p.Value = pair.Value;
                        cmd.Parameters.Add(p);
                    }
                }

                return cmd.ExecuteReader();
            }
        }
        #endregion Reader
    }
}
