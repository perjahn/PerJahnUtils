using System;
using System.Data;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace DBUtil
{
	class DumpData
	{
		// Used to generate extra info for exception description
		public string _dbname;
		public string _tablename;
		public string _sql;

		public string _result;
		public string[] databases { get; set; }

		public bool useRegexpTables { get; set; }
		public string[] excludeTables { get; set; }
		public bool useRegexpColumns { get; set; }
		public string[] excludeColumns { get; set; }

		public bool sortColumns { get; set; }
		public bool sortRows { get; set; }
		public string path { get; set; }
		public bool exportempty { get; set; }

		public int maxrows { get; set; }
		public bool binaryhex { get; set; }
		public bool binaryfile { get; set; }
		public string separator { get; set; }
		public bool overwrite { get; set; }
		public bool header { get; set; }
		public bool escapecharacters { get; set; }
		public string extension { get; set; }

		public string dbprovider { get; set; }
		public string dbserver { get; set; }
		public string dbusername { get; set; }
		public string dbpassword { get; set; }

		// counts
		int c_d, c_t, c_c;

		public void Dump()
		{
			c_d = c_t = c_c = 0;

			foreach (string database in databases)
			{
				_dbname = database;

				DumpDatabase(database);

				c_d++;
			}

			_result = "Databases:" + c_d + " Tables:" + c_t + " Columns:" + c_c;

			return;
		}

		private void DumpDatabase(string database)
		{
			string connstr = db.ConstructConnectionString(dbprovider, dbserver, database, dbusername, dbpassword);

			using (db mydb = new db(dbprovider, connstr))
			{
				string[] tables = mydb.GetAllTables(dbprovider, database);

				foreach (string table in tables)
				{
					_tablename = table;

					string outdir;

					// Create DB specific subdir if multiple DBs.
					if (databases.Length > 1)
						outdir = Path.Combine(path, database);
					else
						outdir = path;

					DumpTable(mydb, database, table, outdir);

					c_t++;
				}
			}
		}

		private void DumpTable(db mydb, string database, string table, string outdir)
		{
			if (excludeTables.Any(c => useRegexpTables ? Regex.IsMatch(table, c) : c == table))
				return;

			if (!Directory.Exists(outdir))
				Directory.CreateDirectory(outdir);

			string filename = Path.Combine(outdir, table + ".txt");

			if (!overwrite)
			{
				if (File.Exists(filename))
					return;
			}

			string tablename = db.GetTableName(dbprovider, database, table);

			// Check number of rows
			string sqlCount = _sql = "select count(*) from " + tablename;

			int rowcount;

			object o = mydb.ExecuteScalarSQL(sqlCount);
			if (o.GetType() == typeof(long))
			{
				rowcount = (int)(long)o;
			}
			else
			{
				rowcount = (int)o;
			}

			if (!exportempty && rowcount == 0)
			{
				return;
			}

			string sql = GetTableQuery(mydb, tablename, rowcount);

			using (StreamWriter sw = new StreamWriter(filename))
			{
				using (System.Data.Common.DbDataReader reader = mydb.ExecuteReaderSQL(sql))
				{
					List<int> columns = new List<int>();

					for (int i = 0; i < reader.FieldCount; i++)
					{
						string colname = reader.GetName(i);
						if (excludeColumns.Any(c => useRegexpColumns ? Regex.IsMatch(colname, c) : c == colname))
						{
							continue;
						}

						if (reader.GetFieldType(i) == typeof(byte[]) && !binaryhex && !binaryfile)
						{
							continue;
						}

						columns.Add(i);
					}


					if (sortColumns)
					{
						columns = columns
							.OrderBy(c => reader.GetName(c))
							.Select(c => c)
							.ToList();
					}


					// Write column names
					if (header)
					{
						bool isFirstCol = true;
						for (int i = 0; i < columns.Count; i++)
						{
							if (reader.GetFieldType(i) == typeof(byte[]) && !binaryhex)
								continue;

							if (!isFirstCol)
								sw.Write(separator);
							isFirstCol = false;

							sw.Write(reader.GetName(columns[i]));
						}
						sw.WriteLine();
					}

					if (maxrows != -1 && rowcount > maxrows)
					{
						sw.WriteLine(rowcount + " rows.");
					}
					else
					{
						// Write data
						int rownum = 0;
						while (reader.Read())
						{
							bool isFirstCol = true;
							for (int i = 0; i < columns.Count; i++)
							{
								if (reader.GetFieldType(i) != typeof(byte[]))
								{
									if (!isFirstCol)
										sw.Write(separator);
									isFirstCol = false;

									if (escapecharacters)
									{
										sw.Write(FixValue(reader.GetValue(columns[i])));
									}
									else
									{
										sw.Write(reader.GetValue(columns[i]));
									}
								}
								else
								{
									if (binaryhex)
									{
										if (!isFirstCol)
											sw.Write(separator);
										isFirstCol = false;

										if (!reader.IsDBNull(i))
										{
											int length = (int)reader.GetBytes(i, 0, null, 0, 0);
											byte[] buffer = new byte[length];
											reader.GetBytes(i, 0, buffer, 0, length);

											sw.Write(FixBinaryValue(buffer));
										}
									}

									if (binaryfile)
									{
										if (!reader.IsDBNull(i))
										{
											string filename_data = Path.Combine(outdir, table + "_" + reader.GetName(i) + "_" + rownum + "." + extension);

											int length = (int)reader.GetBytes(i, 0, null, 0, 0);
											byte[] buffer = new byte[length];
											reader.GetBytes(i, 0, buffer, 0, length);

											WriteBinaryfile(filename_data, buffer);
										}
									}
								}
							}
							sw.WriteLine();

							c_c += columns.Count;
							rownum++;
						}
					}

					reader.Close();
				}
			}
		}

		private string GetTableQuery(db mydb, string tablename, int rowcount)
		{
			string sql;

			if (maxrows != -1 && rowcount > maxrows)
			{
				_sql = sql = "select * from " + tablename + " where 0=1";
			}
			else
			{
				if (sortRows)
				{
					_sql = sql = "select * from " + tablename + " where 0=1";
					DataTable dt = mydb.ExecuteDataTableSQL(sql);
					string pkcols = string.Join(", ", dt.PrimaryKey.Select(c => db.GetColumnName(dbprovider, c.ColumnName)));

					if (pkcols == string.Empty)
						_sql = sql = "select * from " + _tablename;
					else
						_sql = sql = "select * from " + _tablename + " order by " + pkcols;
				}
				else
				{
					_sql = sql = "select * from " + _tablename;
				}
			}

			return sql;
		}

		// Replace control characters with escape sequences
		// \   =>  \\
		// Tab =>  \t
		// LF  =>  \n
		// CR  =>  \r
		private object FixValue(object value)
		{
			if (value is string)
				return value.ToString().Replace("\\", @"\\").Replace("\t", @"\t").Replace("\n", @"\n").Replace("\r", @"\r");

			return value;
		}

		private string FixBinaryValue(byte[] value)
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.Append("0x");
			foreach (byte b in value)
			{
				sb.Append(b.ToString("X2"));
			}

			return sb.ToString();
		}

		private void WriteBinaryfile(string path, byte[] value)
		{
			using (FileStream fs = new FileStream(path, FileMode.Create))
			{
				fs.Write(value, 0, value.Length);
			}
		}
	}
}
