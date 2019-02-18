﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Deveel.Data.Sql.Expressions;
using Deveel.Data.Sql.Tables;

namespace Deveel.Data.Sql.Statements {
	abstract class ColumnChecker {
		public string StripTableName(string tableDomain, string column) {
			var index = column.IndexOf('.');

			if (index != -1) {
				var columnPrefix = column.Substring(0, index);

				if (!columnPrefix.Equals(tableDomain))
					throw new InvalidOperationException($"Column '{column}' is not within the expected table'{tableDomain}'");

				column = column.Substring(index + 1);
			}

			return column;
		}

		public IEnumerable<string> StripColumnList(string tableDomain, IEnumerable<string> columnList) {
			return columnList.Select(x => StripTableName(tableDomain, x));
		}

		public abstract string ResolveColumnName(string columnName);

		public SqlExpression CheckExpression(SqlExpression expression) {
			var expChecker = new ExpressionChecker(this);

			return expChecker.Visit(expression);
		}

		public IEnumerable<string> CheckColumns(IEnumerable<string> columnNames) {
			var result = new List<string>();

			foreach (var columnName in columnNames) {
				var resolved = ResolveColumnName(columnName);

				if (resolved == null)
					throw new InvalidOperationException($"Column '{columnName}' not found in table.");

				result.Add(resolved);
			}

			return result.ToArray();
		}

		public static async Task<ColumnChecker> CreateDefaultAsync(ICommand context, ObjectName tableName) {
			var table = await context.GetTableAsync(tableName);

			if (table == null)
				throw new InvalidOperationException($"Table '{tableName}' not found in the context.");

			var tableInfo = table.TableInfo;
			var ignoreCase = context.IgnoreCase();

			return new DefaultChecker(tableInfo, ignoreCase);
		}

		#region DefaultChecker

		class DefaultChecker : ColumnChecker {
			private readonly TableInfo tableInfo;
			private readonly bool ignoreCase;

			public DefaultChecker(TableInfo tableInfo, bool ignoreCase) {
				this.tableInfo = tableInfo;
				this.ignoreCase = ignoreCase;
			}

			public override string ResolveColumnName(string columnName) {
				var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
				string foundColumn = null;

				foreach (var columnInfo in tableInfo.Columns) {
					if (String.Equals(columnInfo.ColumnName, columnName, comparison)) {
						if (foundColumn != null)
							throw new InvalidOperationException($"Column name '{columnName}' caused an ambiguous match in table.");

						foundColumn = columnInfo.ColumnName;
					}
				}

				return foundColumn;
			}
		}

		#endregion

		#region ExpressionChecker

		class ExpressionChecker : SqlExpressionVisitor {
			private readonly ColumnChecker checker;

			public ExpressionChecker(ColumnChecker checker) {
				this.checker = checker;
			}

			public override SqlExpression VisitReference(SqlReferenceExpression reference) {
				var refName = reference.ReferenceName;
				var origColumn = refName.Name;
				var resolvedColumn = checker.ResolveColumnName(origColumn);

				if (resolvedColumn == null)
					throw new InvalidOperationException($"Column '{origColumn} not found in table.");

				if (!origColumn.Equals(resolvedColumn))
					refName = new ObjectName(refName.Parent, resolvedColumn);

				return SqlExpression.Reference(refName);
			}

			public override SqlExpression VisitQuery(SqlQueryExpression query) {
				throw new InvalidOperationException("Sub-queries are not permitted in a CHECK expression.");
			}
		}

		#endregion
	}
}