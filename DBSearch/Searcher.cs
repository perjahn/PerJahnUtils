/*
Todo: Support for other schemas than dbo
*/
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DBSearch
{
	class Searcher
	{
		public string provider { get; set; }
		public string username { get; set; }
		public string password { get; set; }

		public int maxhits { get; set; }

		public enum SearchType { exact, sqllike, dateinterval };
		public SearchType searchtype { get; set; }

		public string server { get; set; }
		public string[] databases { get; set; }

		public StringBuilder stats { get; set; }
		public StringBuilder log { get; set; }


		private int c_d, c_t, c_c;

		private string _dbname;
		private string _coltype;

		private db _db;

		private DataTable dtResult;

		class sqlerror
		{
			public int count;
			public string sql;
			public string Message;
		}

		Dictionary<int, sqlerror> _errors;

		private DataTable SafeSelect(string sql)
		{
			try
			{
				return _db.ExecuteDataTableSQL(sql, null);
			}
			catch (System.Data.SqlClient.SqlException ex)
			{
				if (_errors.ContainsKey(ex.Number))
				{
					_errors[ex.Number].count++;
				}
				else
				{
					sqlerror error = new sqlerror();
					error.count = 1;
					error.sql = sql;
					error.Message = ex.Message;
					_errors.Add(ex.Number, error);
				}
			}

			return null;
		}

		public DataTable SearchData(string searchtext, DateTime dt1, DateTime dt2)
		{
			DateTime t1, t2;

			c_d = c_t = c_c = 0;

			_errors = new Dictionary<int, sqlerror>();

			dtResult = new DataTable();
			dtResult.Columns.Add("database");
			dtResult.Columns.Add("table");
			dtResult.Columns.Add("column");
			dtResult.Columns.Add("value");

			t1 = DateTime.Now;
			foreach (string database in databases)
			{
				using (_db = new db(provider, GetConnStr()))
				{
					_dbname = database;
					SearchInternal(searchtext, dt1, dt2, database);
				}

				c_d++;
			}
			t2 = DateTime.Now;

			foreach (var error in _errors)
			{
				log.AppendLine(
					"Error: " + error.Key + Environment.NewLine +
					"Count: " + error.Value.count + Environment.NewLine +
					"DB: " + _dbname + Environment.NewLine +
					"Coltype: " + _coltype + Environment.NewLine +
					"SQL: " + error.Value.sql + Environment.NewLine +
					error.Value.Message + Environment.NewLine);
			}

			WriteStats(c_d, c_t, c_c, t1, t2);

			return dtResult;
		}

		private void SearchInternal(string searchtext, DateTime dt1, DateTime dt2, string database)
		{
			int hits;  // Count of displayed hits per table
			string sql;
			bool dummy;
			bool IsSearchTextBoolParsable = bool.TryParse(searchtext, out dummy);

			stats = new StringBuilder();
			log = new StringBuilder();
			DataTable dtTables, dtCols, dt;

			// Get table names
			if (provider == "System.Data.SqlClient")
			{
				sql = "select name from [" + database + "].dbo.sysobjects where type='U' order by name";
			}
			else if (provider == "MySql.Data.MySqlClient")
			{
				sql = "select table_name name from `information_schema`.`tables` where table_schema='" + database + "' order by table_name";
			}
			else if (provider == "Oracle.DataAccess.Client")  // Oracle?
			{
				sql = "select name from USER_TABLES order by name";
			}
			else  // Unknown provider - bogus query
			{
				sql = "select name from tables order by name";
			}

			dtTables = SafeSelect(sql);

			foreach (DataRow drTable in dtTables.Rows)
			{
				string tablename = GetTableName(database, drTable["name"].ToString());

				// Databasdiagram sparas i dtproperties.
				// Temporära tabeller brukar tydligen vara döpta med inledande #
				if (drTable["name"].ToString() == "dtproperties" || drTable["name"].ToString().StartsWith("#"))
					continue;


				hits = 0;

				// Get column names
				sql = "select * from " + tablename + " where 1=0";

				try
				{
					dtCols = SafeSelect(sql);
				}
				catch (System.Exception ex)
				{
					log.AppendLine(
						"DB: " + _dbname + Environment.NewLine +
						"Coltype: " + _coltype + Environment.NewLine +
						"SQL: " + sql + Environment.NewLine +
						ex.Message + Environment.NewLine);

					continue;
				}

				foreach (DataColumn dc in dtCols.Columns)
				{
					string colname = GetColumnName(database, dc.ColumnName);
					if (hits == maxhits)
						break;

					string where;

					if (searchtype == SearchType.exact)
					{
						if (dc.DataType == typeof(bool) && !IsSearchTextBoolParsable)
						{
							continue;
						}

						if (dc.DataType == typeof(short) || dc.DataType == typeof(int) || dc.DataType == typeof(long) ||
							dc.DataType == typeof(float) || dc.DataType == typeof(double) || dc.DataType == typeof(decimal))
						{
							where = "where " + colname + " = " + searchtext;
						}
						else
						{
							where = "where " + colname + " = '" + searchtext + "'";
						}
					}
					else if (searchtype == SearchType.sqllike)
					{
						where = "where " + colname + " like '%" + searchtext + "%'";
					}
					else
					{
						if (dc.DataType == typeof(bool) || dc.DataType == typeof(string) ||
							dc.DataType == typeof(short) || dc.DataType == typeof(int) || dc.DataType == typeof(long) ||
							dc.DataType == typeof(float) || dc.DataType == typeof(double) || dc.DataType == typeof(decimal))
						{
							continue;
						}

						where = "where " + colname + ">'" + dt1.ToString() + "' and " + colname + "<'" + dt2.ToString() + "'";
					}

					// Get rows
					if (maxhits > 0)
					{
						if (provider == "System.Data.SqlClient")
						{
							sql = "select top " + maxhits + " " + colname + " from " + tablename + " " + where;
						}
						else if (provider == "MySql.Data.MySqlClient")
						{
							sql = "select " + colname + " from " + tablename + " " + where + " limit 0," + maxhits;
						}
						else if (provider == "Oracle.DataAccess.Client")  // Oracle?
						{
							sql = "select " + colname + " from " + tablename + " " + where + " and rownum<=" + maxhits;
						}
						else  // Unknown provider
						{
							sql = "select " + colname + " from " + tablename + " " + where;
						}
					}
					else
					{
						sql = "select " + colname + " from " + tablename + " " + where;
					}


					_coltype = dc.DataType.ToString();

					if (_coltype == "System.Byte[]" || _coltype == "System.Decimal" || _coltype == "System.Object")
						continue;

					dt = SafeSelect(sql);
					if (dt == null)
						continue;

					foreach (DataRow dr in dt.Rows)
					{
						if (hits == maxhits)
							break;

						hits++;

						DataRow drNew = dtResult.NewRow();
						drNew["database"] = database;
						drNew["table"] = drTable["name"].ToString();
						drNew["column"] = dc.ColumnName;
						drNew["value"] = dr[0].ToString();
						dtResult.Rows.Add(drNew);
					}

					c_c++;
				}
				c_t++;
			}

			return;
		}

		public DataTable SearchObjects(string searchtext)
		{
			DateTime t1, t2;

			stats = new StringBuilder();
			log = new StringBuilder();

			c_d = c_t = c_c = 0;

			dtResult = new DataTable();
			dtResult.Columns.Add("database");
			dtResult.Columns.Add("name");
			dtResult.Columns.Add("value");

			t1 = DateTime.Now;
			foreach (string database in databases)
			{
				using (_db = new db(provider, GetConnStr()))
				{
					_dbname = database;
					SearchObjectsInternal(searchtext, database);
				}

				c_d++;
			}
			t2 = DateTime.Now;

			WriteStats(c_d, c_t, c_c, t1, t2);

			return dtResult;
		}

		private void SearchObjectsInternal(string searchtext, string database)
		{
			string sql;

			DataTable dtTables, dtCols, dt;


			// Search generic DB objects (except columns)
			if (searchtype == SearchType.exact)
				sql =
					"select *" +
					" from [" + database + "].dbo.sysobjects" +
					" where name like '" + searchtext + "' " +
					" order by name,type";
			else if (searchtype == SearchType.sqllike)
				sql =
					"select *" +
					" from [" + database + "].dbo.sysobjects" +
					" where name like '%" + searchtext + "%'" +
					" order by name,type";
			else
				return;

			dt = SafeSelect(sql);
			if (dt != null)
			{
				foreach (DataRow dr in dt.Rows)
				{
					string type = dr["type"].ToString();
					switch (type.TrimEnd())
					{
						case "F":
							type = "Foreign Key";
							break;
						case "K":
							type = "Primary Key";
							break;
						case "P":
							type = "Stored Procedure";
							break;
						case "S":
							type = "System Table";
							break;
						case "U":
							type = "Table";
							break;
					}

					DataRow drNew = dtResult.NewRow();
					drNew["database"] = database;
					drNew["name"] = dr["name"].ToString();
					drNew["value"] = dr[0].ToString();
					dtResult.Rows.Add(drNew);
				}
			}


			// Search columns
			sql = "select name from [" + database + "].dbo.sysobjects where type='U' order by name";

			dtTables = SafeSelect(sql);
			if (dtTables != null)
			{
				foreach (DataRow drTable in dtTables.Rows)
				{
					if (drTable["name"].ToString() == "dtproperties")
						continue;

					// Get column names
					sql = "select * from [" + database + "].dbo.[" + drTable["name"] + "] where 1=0";

					dtCols = SafeSelect(sql);

					foreach (DataColumn dc in dtCols.Columns)
					{
						string colname = dc.ColumnName;
						_coltype = dc.DataType.ToString();

						if (((searchtype == SearchType.exact) && colname.ToLower() == searchtext.ToLower()) ||
							((searchtype == SearchType.sqllike) && colname.ToLower().Contains(searchtext.ToLower())))
						{
							if (_coltype.StartsWith("System."))
								_coltype = _coltype.Substring(7);

							DataRow drNew = dtResult.NewRow();
							drNew["database"] = database;
							drNew["name"] = drTable["name"] + "." + dc.ColumnName;
							drNew["value"] = "Column: " + _coltype;
							dtResult.Rows.Add(drNew);
						}

						c_c++;
					}
					c_t++;
				}
			}


			// Search content of SPs

			// Observera att SPs är uppdelade i 4k-block, så det är risk att inte hela SPn
			// kommer med. sc.colno anger ordningen bland blocken. Men för att lägga ihop
			// värden från flera rader behövs något av följande:
			// 1. xml-stöd (finns ej i sql2000).
			// 2. skriva in en SP (som använder cursors för att lägga ihop rader) i DBn.
			// 3. lägga ihop raderna med c#-kod.
			// 4. i c#-kod skapa en dynamisk sql som lägger ihop alla blocken, kräver en select-fråga per sp.

			// Bäst vore kanske att inte visa något SP-innehåll i GUI, utan att hämta alla
			// blocken "on demand" med metod 4.

			sql =
				"select so.name, sc.text" +
				" from [" + database + "].dbo.sysobjects so" +
				" join [" + database + "].dbo.syscomments sc on sc.id = so.id" +
				" where sc.text like '%" + searchtext + "%'" +
				" order by so.name";
			_coltype = string.Empty;


			dtTables = SafeSelect(sql);
			if (dtTables != null)
			{
				foreach (DataRow dr in dtTables.Rows)
				{
					string sptext = dr["text"].ToString();

					DataRow drNew = dtResult.NewRow();
					drNew["database"] = database;
					drNew["name"] = dr["name"].ToString();
					drNew["value"] = sptext;
					dtResult.Rows.Add(drNew);
				}
			}


			return;
		}

		private string GetConnStr()
		{
			string connstr;

			if (provider == "MySql.Data.MySqlClient")
			{
				connstr = "Data Source=" + server + "; Database=information_schema; User ID=" + username + "; Password=" + password + ";";
			}
			else
			{
				if (username != null && password != null)
				{
					connstr = "Server=" + server + "; Database=master; Trusted_Connection=False; User ID=" + username + "; Password=" + password + ";";
				}
				else
				{
					connstr = "Server=" + server + "; Database=master; Trusted_Connection=True;";
				}
			}

			//connstr = "DSN=admin2;Uid=;Pwd=;";

			return connstr;
		}

		private string GetTableName(string schema, string table)
		{
			if (provider == "System.Data.SqlClient")
			{
				return "[" + schema + "].dbo.[" + table + "]";
			}
			else if (provider == "MySql.Data.MySqlClient")
			{
				return "`" + schema + "`.`" + table + "`";
			}
			else if (provider == "Oracle.DataAccess.Client")  // Oracle?
			{
				return schema + "." + table;
			}
			else  // Unknown provider
			{
				return schema + "." + table;
			}
		}

		private string GetColumnName(string schema, string column)
		{
			if (provider == "System.Data.SqlClient")
			{
				return "[" + column + "]";
			}
			else if (provider == "MySql.Data.MySqlClient")
			{
				return "`" + column + "`";
			}
			else if (provider == "Oracle.DataAccess.Client")  // Oracle?
			{
				return column;
			}
			else  // Unknown provider
			{
				return column;
			}
		}

		private void WriteStats(int d, int t, int c, DateTime t1, DateTime t2)
		{
			stats.Append("Databases:" + d + " Tables:" + t + " Columns:" + c);

			TimeSpan ts = t2 - t1;

			stats.Append(" Time:");

			if (ts.TotalMinutes >= 1)
			{
				stats.Append((int)ts.TotalMinutes + "m");
			}

			stats.Append(ts.Seconds + "s." + ts.Milliseconds + "ms");
		}
	}
}
