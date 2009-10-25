﻿//  
//  DatabaseMetadata.cs
//  
//  Author:
//       Antonello Provenzano <antonello@deveel.com>
// 
//  Copyright (c) 2009 Deveel
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;

namespace Deveel.Data.Client {
	internal class DatabaseMetadata {
		public DatabaseMetadata(DeveelDbConnection connection) {
			this.connection = connection;
		}

		private readonly DeveelDbConnection connection;

		public System.Data.DataTable GetTables(string[] restrictions) {
			if (restrictions == null)
				throw new ArgumentNullException("restrictions");
			if (restrictions.Length < 3)
				throw new ArgumentException();

			//TODO: still not officially supported...
			string catalog = restrictions[0];
			string schema = restrictions[1];
			string table = restrictions[2];

			string[] types = new string[restrictions.Length - 3];
			Array.Copy(restrictions, 3, types, 0, types.Length);

			System.Data.DataTable dataTable = new System.Data.DataTable("Tables");
			dataTable.Columns.Add("TABLE_CATELOG");
			dataTable.Columns.Add("TABLE_SCHEMA");
			dataTable.Columns.Add("TABLE_NAME");
			dataTable.Columns.Add("TABLE_TYPE");
			dataTable.Columns.Add("REMARKS");
			dataTable.Columns.Add("TYPE_CATALOG");
			dataTable.Columns.Add("TYPE_SCHEMA");
			dataTable.Columns.Add("TYPE_NAME");
			dataTable.Columns.Add("SELF_REFERENCING_COL_NAME");
			dataTable.Columns.Add("REF_GENERATION");

			if (table == null)
				table = "%";
			if (schema == null)
				schema = "%";

			// The 'types' argument
			String type_part = "";
			int type_size = 0;
			if (types.Length > 0) {
				StringBuilder buf = new StringBuilder();
				buf.Append("      AND \"TABLE_TYPE\" IN ( ");
				for (int i = 0; i < types.Length - 1; ++i) {
					buf.Append("?, ");
				}
				buf.Append("? ) \n");
				type_size = types.Length;
				type_part = buf.ToString();
			}

			// Create the statement

			DeveelDbCommand command = connection.CreateCommand("   SELECT * \n" +
			                                                   "     FROM \"INFORMATION_SCHEMA.Tables\" \n" +
			                                                   "    WHERE \"TABLE_SCHEMA\" LIKE ? \n" +
			                                                   "      AND \"TABLE_NAME\" LIKE ? \n" +
			                                                   type_part +
			                                                   " ORDER BY \"TABLE_TYPE\", \"TABLE_SCHEMA\", \"TABLE_NAME\" \n");
			command.Parameters.Add(schema);
			command.Parameters.Add(table);
			if (type_size > 0) {
				for (int i = 0; i < type_size; ++i)
					command.Parameters.Add(types[i]);
			}

			command.Prepare();

			using (DeveelDbDataReader reader = command.ExecuteReader()) {
				while (reader.Read()) {
					DataRow row = dataTable.NewRow();
					row["TABLE_CATALOG"] = reader.GetString(0);
					row["TABLE_SCHEMA"] = reader.GetString(1);
					row["TABLE_NAME"] = reader.GetString(2);
					row["TABLE_TYPE"] = reader.GetString(3);
					row["REMARKS"] = reader.GetString(4);
					// the other columns are always NULL so it's useless to read...

					dataTable.Rows.Add(row);
				}
			}

			return dataTable;
		}

		public System.Data.DataTable GetColumns(string[] restrictions) {
			if (restrictions == null)
				throw new ArgumentNullException("restrictions");
			if (restrictions.Length < 4)
				throw new ArgumentException();

			string catalog = restrictions[0];
			string schema = restrictions[1];
			string table = restrictions[2];
			string column = restrictions[3];

			if (table == null)
				table = "%";
			if (schema == null)
				schema = "%";
			if (column == null)
				column = "%";

			System.Data.DataTable dataTable = new System.Data.DataTable("Columns");
			dataTable.Columns.Add("TABLE_CATALOG");
			dataTable.Columns.Add("TABLE_SCHEMA");
			dataTable.Columns.Add("TABLE_NAME");
			dataTable.Columns.Add("COLUMN_NAME");
			dataTable.Columns.Add("DATA_TYPE", typeof(int));
			dataTable.Columns.Add("TYPE_NAME");
			dataTable.Columns.Add("COLUMN_SIZE", typeof(int));
			dataTable.Columns.Add("BUFFER_LENGTH", typeof(int));
			dataTable.Columns.Add("DECIMAL_DIGITS", typeof(int));
			dataTable.Columns.Add("NUM_PREC_RADIX", typeof(int));
			dataTable.Columns.Add("NULLABLE", typeof(bool));
			dataTable.Columns.Add("REMARKS");
			dataTable.Columns.Add("COLUMN_DEFAULT");
			dataTable.Columns.Add("SQL_DATA_TYPE", typeof(int));
			dataTable.Columns.Add("SQL_DATETIME_SUB");
			dataTable.Columns.Add("CHAR_OCTET_LENGTH", typeof(int));
			dataTable.Columns.Add("ORDINAL_POSITION", typeof(int));
			dataTable.Columns.Add("IS_NULLABLE", typeof (bool));

			DeveelDbCommand command = connection.CreateCommand("  SELECT * \n" +
			                                                   "    FROM INFORMATION_SCHEMA.Columns \n" +
			                                                   "   WHERE \"TABLE_SCHEMA\" LIKE ? \n" +
			                                                   "     AND \"TABLE_NAME\" LIKE ? \n" +
			                                                   "     AND \"COLUMN_NAME\" LIKE ? \n" +
			                                                   "ORDER BY \"TABLE_SCHEMA\", \"TABLE_NAME\", \"ORDINAL_POSITION\"");
			command.Parameters.Add(schema);
			command.Parameters.Add(table);
			command.Parameters.Add(column);
			command.Prepare();

			using (DeveelDbDataReader reader = command.ExecuteReader()) {
				while (reader.Read()) {
					DataRow row = dataTable.NewRow();
					row["TABLE_CATALOG"] = reader.GetString(0);
					row["TABLE_SCHEMA"] = reader.GetString(1);
					row["TABLE_NAME"] = reader.GetString(2);
					row["COLUMN_NAME"] = reader.GetString(3);
					row["DATA_TYPE"] = reader.GetInt32(4);
					row["TYPE_NAME"] = reader.GetString(5);
					row["COLUMN_SIZE"] = reader.GetInt32(6);
					row["BUFFER_LENGTH"] = reader.GetInt32(7);
					row["TYPE_NAME"] = reader.GetString(8);
					row["DECIMAL_DIGITS"] = reader.GetInt32(9);
					row["NUM_PREC_RADIX"] = reader.GetInt32(10);
					row["NULLABLE"] = reader.GetBoolean(11);
					row["REMARKS"] = reader.GetString(12);
					row["COLUMN_DEFAULT"] = reader.GetString(13);
					row["SQL_DATA_TYPE"] = reader.GetInt32(14);
					row["SQL_DATETIME_SUB"] = reader.GetString(15);
					row["CHAR_OCTET_LENGTH"] = reader.GetInt32(16);
					row["ORDINAL_POSITION"] = reader.GetInt32(17);
					row["IS_NULLABLE"] = reader.GetBoolean(18);
					dataTable.Rows.Add(row);
				}
			}

			return dataTable;
		}

		public System.Data.DataTable GetColumnPrivileges(string[] restrictions) {
			if (restrictions == null)
				throw new ArgumentNullException("restrictions");
			if (restrictions.Length < 3)
				throw new ArgumentException();

			string catalog = restrictions[0];
			string schema = restrictions[1];
			string table = restrictions[2];
			string column = restrictions[3];

			if (column == null)
				column = "%";

			System.Data.DataTable dataTable = new System.Data.DataTable("Column_Privileges");

			dataTable.Columns.Add("TABLE_CATALOG");
			dataTable.Columns.Add("TABLE_SCHEMA");
			dataTable.Columns.Add("TABLE_NAME");
			dataTable.Columns.Add("COLUMN_NAME");
			dataTable.Columns.Add("GRANTOR");
			dataTable.Columns.Add("GRANTEE");
			dataTable.Columns.Add("IS_GRANTABLE", typeof(bool));

			DeveelDbCommand command = connection.CreateCommand("   SELECT * FROM INFORMATION_SCHEMA.Column_Privileges \n" +
			                                                   "    WHERE (? IS NOT NULL OR \"TABLE_SCHEMA\" = ? ) \n" +
			                                                   "      AND (? IS NOT NULL OR \"TABLE_NAME\" = ? ) \n" +
			                                                   "      AND \"COLUMN_NAME\" LIKE ? \n" +
			                                                   " ORDER BY \"COLUMN_NAME\", \"PRIVILEGE\" ");
			command.Parameters.Add(schema);
			command.Parameters.Add(schema);
			command.Parameters.Add(table);
			command.Parameters.Add(table);
			command.Parameters.Add(column);

			command.Prepare();

			using (DeveelDbDataReader reader = command.ExecuteReader()) {
				while (reader.Read()) {
					DataRow row = dataTable.NewRow();
					row["TABLE_CATALOG"] = reader.GetString(0);
					row["TABLE_SCHEMA"] = reader.GetString(1);
					row["TABLE_NAME"] = reader.GetString(2);
					row["COLUMN_NAME"] = reader.GetString(3);
					row["GRANTOR"] = reader.GetString(4);
					row["GRANTEE"] = reader.GetString(5);
					row["IS_GRANTABLE"] = reader.GetBoolean(6);
					dataTable.Rows.Add(row);
				}
			}

			return dataTable;
		}

		public System.Data.DataTable GetTablePrivileges(string[] restrictions) {
			if (restrictions == null)
				throw new ArgumentNullException("restrictions");
			if (restrictions.Length < 3)
				throw new ArgumentException();

			string catalog = restrictions[0];
			string schema = restrictions[1];
			string table = restrictions[2];

			if (schema == null)
				schema = "%";
			if (table == null)
				table = "%";

			System.Data.DataTable dataTable = new System.Data.DataTable("TablePrivileges");
			dataTable.Columns.Add("TABLE_CATALOG");
			dataTable.Columns.Add("TABLE_SCHEMA");
			dataTable.Columns.Add("TABLE_NAME");
			dataTable.Columns.Add("GRANTOR");
			dataTable.Columns.Add("GRANTEE");
			dataTable.Columns.Add("IS_GRANTABLE", typeof(bool));

			DeveelDbCommand command = connection.CreateCommand("   SELECT * FROM INFORMATION_SCHEMA.Table_Privileges \n" +
			                                                   "    WHERE \"TABLE_SCHEM\" LIKE ? \n" +
			                                                   "      AND \"TABLE_NAME\" LIKE ? \n" +
			                                                   " ORDER BY \"TABLE_SCHEM\", \"TABLE_NAME\", \"PRIVILEGE\" ");

			command.Parameters.Add(schema);
			command.Parameters.Add(table);
			command.Prepare();

			using (DeveelDbDataReader reader = command.ExecuteReader()) {
				while (reader.Read()) {
					DataRow row = dataTable.NewRow();
					row["TABLE_CATALOG"] = reader.GetString(0);
					row["TABLE_SCHEMA"] = reader.GetString(1);
					row["TABLE_NAME"] = reader.GetString(2);
					row["GRANTOR"] = reader.GetString(3);
					row["GRANTEE"] = reader.GetString(4);
					row["IS_GRANTABLE"] = reader.GetBoolean(5);
					dataTable.Rows.Add(row);
				}
			}

			return dataTable;
		}

		public System.Data.DataTable GetPrimaryKeys(string[] restrictions) {
			if (restrictions == null)
				throw new ArgumentNullException("restrictions");
			if (restrictions.Length < 3)
				throw new ArgumentException();

			string catalog = restrictions[0];
			string schema = restrictions[1];
			string table = restrictions[2];

			System.Data.DataTable dataTable = new System.Data.DataTable("PrimaryKeys");
			dataTable.Columns.Add("TABLE_CATALOG");
			dataTable.Columns.Add("TABLE_SCHEMA");
			dataTable.Columns.Add("TABLE_NAME");
			dataTable.Columns.Add("COLUMN_NAME");
			dataTable.Columns.Add("KEY_SEQ", typeof(int));
			dataTable.Columns.Add("PK_NAME");

			DeveelDbCommand command = connection.CreateCommand("   SELECT * \n" +
			                                                   "     FROM INFORMATION_SCHEMA.PrimaryKeys \n" +
			                                                   "    WHERE ( ? IS NULL OR \"TABLE_SCHEMA\" = ? ) \n" +
			                                                   "      AND \"TABLE_NAME\" = ? \n" +
			                                                   " ORDER BY \"COLUMN_NAME\"");

			command.Parameters.Add(schema);
			command.Parameters.Add(schema);
			command.Parameters.Add(table);

			command.Prepare();

			using (DeveelDbDataReader reader = command.ExecuteReader()) {
				DataRow row = dataTable.NewRow();
				row["TABLE_CATALOG"] = reader.GetString(0);
				row["TABLE_SCHEMA"] = reader.GetString(1);
				row["TABLE_NAME"] = reader.GetString(2);
				row["COLUMN_NAME"] = reader.GetString(3);
				row["KEY_SEQ"] = reader.GetInt32(4);
				row["PK_NAME"] = reader.GetString(5);
				dataTable.Rows.Add(row);
			}

			return dataTable;
		}

		public System.Data.DataTable GetImportedKeys(string[] restrictions) {
			if (restrictions == null)
				throw new ArgumentNullException("restrictions");
			if (restrictions.Length < 3)
				throw new ArgumentException();

			string catalog = restrictions[0];
			string schema = restrictions[1];
			string table = restrictions[2];

			System.Data.DataTable dataTable = new System.Data.DataTable("ImportedKey");
			dataTable.Columns.Add("PKTABLE_CATALOG");
			dataTable.Columns.Add("PKTABLE_SCHEMA");
			dataTable.Columns.Add("PKTABLE_NAME");
			dataTable.Columns.Add("PKCOLUMN_NAME");
			dataTable.Columns.Add("FKTABLE_CATALOG");
			dataTable.Columns.Add("FKTABLE_SCHEMA");
			dataTable.Columns.Add("FKTABLE_NAME");
			dataTable.Columns.Add("FKCOLUMN_NAME");
			dataTable.Columns.Add("KEY_SEQ");
			dataTable.Columns.Add("UPDATE_RULE");
			dataTable.Columns.Add("DELETE_RULE");
			dataTable.Columns.Add("FK_NAME");
			dataTable.Columns.Add("PK_NAME");
			dataTable.Columns.Add("DEFERRABILITY");

			DeveelDbCommand command = connection.CreateCommand("   SELECT * FROM INFORMATION_SCHEMA.ImportedKeys \n" +
			                                                   "    WHERE ( ? IS NULL OR \"FKTABLE_SCHEM\" = ? )\n" +
			                                                   "      AND \"FKTABLE_NAME\" = ? \n" +
			                                                   "ORDER BY \"FKTABLE_SCHEM\", \"FKTABLE_NAME\", \"KEY_SEQ\"");

			command.Parameters.Add(schema);
			command.Parameters.Add(schema);
			command.Parameters.Add(table);

			command.Prepare();

			using(DeveelDbDataReader reader = command.ExecuteReader()) {
				//TODO: check ...
				dataTable.Load(reader);
				/*
				while (reader.Read()) {
					DataRow row = dataTable.NewRow();
					row["PKTABLE_CATALOG"] = reader.GetString(0);
					row["PKTABLE_SCHEMA"] = reader.GetString(1);
					row["PKTABLE_NAME"] = reader.GetString(2);
					row["PKCOLUMN_NAME"] = reader.GetString(3);
					row["FKTABLE_CATALOG"] = reader.GetString(4);
					row["FKTABLE_SCHEMA"] = reader.GetString(5);
					row["FKTABLE_NAME"] = reader.GetString(6);
					row["FKCOLUMN_NAME"] = reader.GetString(7);
					dataTable.Rows.Add(row);
				}
				*/
			}

			return dataTable;
		}

		public System.Data.DataTable GetExportedKeys(string[] restrictions) {
			if (restrictions == null)
				throw new ArgumentNullException("restrictions");
			if (restrictions.Length < 3)
				throw new ArgumentException();

			string catalog = restrictions[0];
			string schema = restrictions[1];
			string table = restrictions[2];

			System.Data.DataTable dataTable = new System.Data.DataTable("ExportedKey");
			dataTable.Columns.Add("PKTABLE_CATALOG");
			dataTable.Columns.Add("PKTABLE_SCHEMA");
			dataTable.Columns.Add("PKTABLE_NAME");
			dataTable.Columns.Add("PKCOLUMN_NAME");
			dataTable.Columns.Add("FKTABLE_CATALOG");
			dataTable.Columns.Add("FKTABLE_SCHEMA");
			dataTable.Columns.Add("FKTABLE_NAME");
			dataTable.Columns.Add("FKCOLUMN_NAME");
			dataTable.Columns.Add("KEY_SEQ");
			dataTable.Columns.Add("UPDATE_RULE");
			dataTable.Columns.Add("DELETE_RULE");
			dataTable.Columns.Add("FK_NAME");
			dataTable.Columns.Add("PK_NAME");
			dataTable.Columns.Add("DEFERRABILITY");

			DeveelDbCommand command = connection.CreateCommand("   SELECT * FROM SYS_JDBC.ImportedKeys \n" +
			                                                   "    WHERE ( ? IS NULL OR \"PKTABLE_SCHEM\" = ? ) \n" +
			                                                   "      AND \"PKTABLE_NAME\" = ? \n" +
			                                                   "ORDER BY \"FKTABLE_SCHEM\", \"FKTABLE_NAME\", \"KEY_SEQ\"");

			command.Parameters.Add(schema);
			command.Parameters.Add(schema);
			command.Parameters.Add(table);

			command.Prepare();

			using (DeveelDbDataReader reader = command.ExecuteReader()) {
				//TODO: check ...
				dataTable.Load(reader);
			}

			return dataTable;
		}

		public System.Data.DataTable GetRestrictions() {
			object[][] restrictions = new object[][]
                {
                    new object[] {"Tables", "Catalog", "", 0},
                    new object[] {"Tables", "Schema", "", 1},
                    new object[] {"Tables", "Table", "", 2},
                    new object[] {"Tables", "TableType", "", 3},
                    new object[] {"Columns", "Catalog", "", 0},
                    new object[] {"Columns", "Schema", "", 1},
                    new object[] {"Columns", "Table", "", 2},
                    new object[] {"Columns", "Column", "", 3},
                    new object[] {"PrimaryKeys", "Database", "", 0},
                    new object[] {"PrimaryKeys", "Schema", "", 1},
                    new object[] {"PrimaryKeys", "Table", "", 2},
                    new object[] {"ExportedKeys", "Catalog", "", 0},
                    new object[] {"ExportedKeys", "Schema", "", 1},
                    new object[] {"ExportedKeys", "Table", "", 2},
					new object[] {"ImportedKeys", "Catalog", "", 0},
					new object[] {"ImportedKeys", "Schema", "", 1}, 
					new object[] {"ImportedKeys", "Table", "", 2},
					new object[] {"ColumnPrivileges", "Catalog", "", 0},
					new object[] {"ColumnPrivileges", "Schema", "", 1},
					new object[] {"ColumnPrivileges", "Table", "", 2}, 
					new object[] {"ColumnPrivileges", "Column", "", 3},
					new object[] {"TablePrivileges", "Catalog", "", 0},
					new object[] {"TablePrivileges", "Schema", "", 1}, 
					new object[] {"TablePrivileges", "Table", "", 2}, 
                };

			System.Data.DataTable dt = new System.Data.DataTable("Restrictions");
			dt.Columns.Add(new DataColumn("CollectionName", typeof(string)));
			dt.Columns.Add(new DataColumn("RestrictionName", typeof(string)));
			dt.Columns.Add(new DataColumn("RestrictionDefault", typeof(string)));
			dt.Columns.Add(new DataColumn("RestrictionNumber", typeof(int)));

			FillTable(dt, restrictions);

			return dt;
		}

		private System.Data.DataTable GetCollections() {
			object[][] collections = new object[][]
                {
                    new object[] {"MetaDataCollections", 0, 0},
                    new object[] {"DataSourceInformation", 0, 0},
                    new object[] {"DataTypes", 0, 0},
                    new object[] {"Restrictions", 0, 0},
                    new object[] {"ReservedWords", 0, 0},
                    new object[] {"Databases", 1, 1},
                    new object[] {"Tables", 4, 2},
                    new object[] {"Columns", 4, 4},
                    new object[] {"PrimaryKeys", 4, 3},
					new object[] {"ExportedKeys", 4, 3},
					new object[] {"ImportedKeys", 4, 3},
                };

			System.Data.DataTable dt = new System.Data.DataTable("MetaDataCollections");
			dt.Columns.Add("CollectionName", typeof(string));
			dt.Columns.Add("NumberOfRestrictions", typeof(int));
			dt.Columns.Add("NumberOfIdentifierParts", typeof(int));

			FillTable(dt, collections);

			return dt;
		}

		private System.Data.DataTable GetDataSourceInformation() {
			System.Data.DataTable dt = new System.Data.DataTable("DataSourceInformation");
			dt.Columns.Add("CompositeIdentifierSeparatorPattern", typeof(string));
			dt.Columns.Add("DataSourceProductName", typeof(string));
			dt.Columns.Add("DataSourceProductVersion", typeof(string));
			dt.Columns.Add("DataSourceProductVersionNormalized", typeof(string));
			dt.Columns.Add("GroupByBehavior", typeof(GroupByBehavior));
			dt.Columns.Add("IdentifierPattern", typeof(string));
			dt.Columns.Add("IdentifierCase", typeof(IdentifierCase));
			dt.Columns.Add("OrderByColumnsInSelect", typeof(bool));
			dt.Columns.Add("ParameterMarkerFormat", typeof(string));
			dt.Columns.Add("ParameterMarkerPattern", typeof(string));
			dt.Columns.Add("ParameterNameMaxLength", typeof(int));
			dt.Columns.Add("ParameterNamePattern", typeof(string));
			dt.Columns.Add("QuotedIdentifierPattern", typeof(string));
			dt.Columns.Add("QuotedIdentifierCase", typeof(IdentifierCase));
			dt.Columns.Add("StatementSeparatorPattern", typeof(string));
			dt.Columns.Add("StringLiteralPattern", typeof(string));
			dt.Columns.Add("SupportedJoinOperators", typeof(SupportedJoinOperators));

			DataRow row = dt.NewRow();
			row["CompositeIdentifierSeparatorPattern"] = "\\.";
			row["DataSourceProductName"] = "MySQL";
			row["DataSourceProductVersion"] = connection.ServerVersion;
			row["DataSourceProductVersionNormalized"] = connection.ServerVersion;
			row["GroupByBehavior"] = GroupByBehavior.Unrelated;
			row["IdentifierPattern"] =
				@"(^\`\p{Lo}\p{Lu}\p{Ll}_@#][\p{Lo}\p{Lu}\p{Ll}\p{Nd}@$#_]*$)|(^\`[^\`\0]|\`\`+\`$)|(^\"" + [^\""\0]|\""\""+\""$)";
			row["IdentifierCase"] = IdentifierCase.Insensitive;
			row["OrderByColumnsInSelect"] = false;
			row["ParameterMarkerFormat"] = "{0}";
			row["ParameterMarkerPattern"] = "(@[A-Za-z0-9_$#]*)";
			row["ParameterNameMaxLength"] = 128;
			row["ParameterNamePattern"] = @"^[\p{Lo}\p{Lu}\p{Ll}\p{Lm}_@#][\p{Lo}\p{Lu}\p{Ll}\p{Lm}\p{Nd}\uff3f_@#\$]*(?=\s+|$)";
			row["QuotedIdentifierPattern"] = @"(([^\`]|\`\`)*)";
			row["QuotedIdentifierCase"] = IdentifierCase.Sensitive;
			row["StatementSeparatorPattern"] = ";";
			row["StringLiteralPattern"] = "'(([^']|'')*)'";
			row["SupportedJoinOperators"] = 15;
			dt.Rows.Add(row);

			return dt;
		}

		public virtual System.Data.DataTable GetSchema(string collection, String[] restrictions) {
			if (connection.State != ConnectionState.Open)
				throw new DataException("GetSchema can only be called on an open connection.");

			collection = collection.ToUpper(CultureInfo.InvariantCulture);

			System.Data.DataTable dt = null;

			switch (collection) {
				// common collections
				case "METADATACOLLECTIONS":
					dt = GetCollections();
					break;
				case "DATASOURCEINFORMATION":
					dt = GetDataSourceInformation();
					break;
				case "DATATYPES":
					//TODO: dt = GetDataTypes();
					break;
				case "RESTRICTIONS":
					dt = GetRestrictions();
					break;
				case "RESERVEDWORDS":
					//TODO: dt = GetReservedWords();
					break;
			}

			if (restrictions == null)
				restrictions = new string[2];
			if (connection != null &&
				connection.Database != null &&
				connection.Database.Length > 0 &&
				restrictions.Length > 1 &&
				restrictions[1] == null)
				restrictions[1] = connection.Database;

			switch (collection) {
				case "TABLES":
					dt = GetTables(restrictions);
					break;
				case "COLUMNS":
					dt = GetColumns(restrictions);
					break;
				case "TABLEPRIVILEGES":
					dt = GetTablePrivileges(restrictions);
					break;
				case "COLUMNPRIVILEGES":
					dt = GetColumnPrivileges(restrictions);
					break;
				case "PRIMARYKEYS":
					dt = GetPrimaryKeys(restrictions);
					break;
				case "EXPORTEDKEYS":
					dt = GetExportedKeys(restrictions);
					break;
				case "IMPORTEDKEYS":
					dt = GetImportedKeys(restrictions);
					break;
			}


			if (dt == null)
				throw new DataException("Invalid collection name");

			return dt;
		}

		private static void FillTable(System.Data.DataTable dt, object[][] data) {
			foreach (object[] dataItem in data) {
				DataRow row = dt.NewRow();
				for (int i = 0; i < dataItem.Length; i++)
					row[i] = dataItem[i];
				dt.Rows.Add(row);
			}
		}
	}
}