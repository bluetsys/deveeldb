﻿using System;
using System.Linq;

using Deveel.Data.Sql.Expressions;
using Deveel.Data.Sql.Statements;

using NUnit.Framework;

namespace Deveel.Data.Sql.Compile {
	[TestFixture]
	public class CreateTableTests : SqlCompileTestBase {
		[Test]
		public void SimpleTable() {
			const string sql = "CREATE TABLE test (a INT NOT NULL, b VARCHAR NULL)";

			var result = Compile(sql);
			Assert.IsNotNull(result);
			Assert.IsFalse(result.HasErrors);
			Assert.AreEqual(1, result.Statements.Count);

			var statement = (CreateTableStatement) result.Statements.ElementAt(0);

			Assert.IsNotNull(statement);
			Assert.IsInstanceOf<CreateTableStatement>(statement);

			var createTable = (CreateTableStatement) statement;

			Assert.IsNotNull(createTable.TableName);
			Assert.AreEqual("test", createTable.TableName.Name);
			Assert.IsFalse(createTable.Temporary);
			Assert.IsFalse(createTable.IfNotExists);

			Assert.AreEqual(2, createTable.Columns.Count);
			Assert.AreEqual("a", createTable.Columns[0].ColumnName);
			Assert.IsTrue(createTable.Columns[0].IsNotNull);

			Assert.AreEqual("b", createTable.Columns[1].ColumnName);
			Assert.IsFalse(createTable.Columns[1].IsNotNull);
		}

		[Test]
		public void WithIdentityColumn() {
			const string sql = "CREATE TABLE test (id INT IDENTITY, name VARCHAR NOT NULL)";

			var result = Compile(sql);
			Assert.IsNotNull(result);
			Assert.IsFalse(result.HasErrors);
			Assert.AreEqual(1, result.Statements.Count);

			var statement = (CreateTableStatement) result.Statements.ElementAt(0);

			Assert.IsNotNull(statement);
			Assert.IsInstanceOf<CreateTableStatement>(statement);

			var createTable = (CreateTableStatement) statement;

			Assert.IsNotNull(createTable.TableName);
			Assert.AreEqual("test", createTable.TableName.Name);
			Assert.IsFalse(createTable.Temporary);
			Assert.IsFalse(createTable.IfNotExists);

			Assert.AreEqual(2, createTable.Columns.Count);
			Assert.IsTrue(createTable.Columns[0].IsIdentity);
			Assert.IsNotNull(createTable.Columns[0].DefaultExpression);
			Assert.IsInstanceOf<SqlFunctionCallExpression>(createTable.Columns[0].DefaultExpression);
		}

		[Test]
		public void WithColumnDefault() {
			const string sql = "CREATE TABLE test (a VARCHAR DEFAULT 'one')";

			var result = Compile(sql);
			Assert.IsNotNull(result);
			Assert.IsFalse(result.HasErrors);
			Assert.AreEqual(1, result.Statements.Count);

			var statement = (CreateTableStatement) result.Statements.ElementAt(0);

			Assert.IsNotNull(statement);
			Assert.IsInstanceOf<CreateTableStatement>(statement);

			var createTable = (CreateTableStatement) statement;

			Assert.IsNotNull(createTable.TableName);
			Assert.AreEqual("test", createTable.TableName.Name);

			Assert.AreEqual(1, createTable.Columns.Count);
			Assert.IsNotNull(createTable.Columns[0].DefaultExpression);
			Assert.IsInstanceOf<SqlConstantExpression>(createTable.Columns[0].DefaultExpression);
		}

		[Test]
		public void WithConstraints() {
			const string sql = "CREATE TABLE test (id INT NOT NULL, UNIQUE(id))";

			var result = Compile(sql);
			Assert.IsNotNull(result);
			Assert.IsFalse(result.HasErrors);
			Assert.AreEqual(2, result.Statements.Count);

			Assert.IsInstanceOf<CreateTableStatement>(result.Statements.ElementAt(0));
			Assert.IsInstanceOf<AlterTableStatement>(result.Statements.ElementAt(1));

			var statement = (CreateTableStatement) result.Statements.ElementAt(0);

			Assert.IsNotNull(statement);
			Assert.IsInstanceOf<CreateTableStatement>(statement);

			var createTable = (CreateTableStatement) statement;

			Assert.IsNotNull(createTable.TableName);
			Assert.AreEqual("test", createTable.TableName.Name);
		}

		[Test]
		public void WithColumnConstraints() {
			const string sql = "CREATE TABLE test (id INT NOT NULL PRIMARY KEY)";

			var result = Compile(sql);
			Assert.IsNotNull(result);
			Assert.IsFalse(result.HasErrors);
			Assert.AreEqual(2, result.Statements.Count);

			Assert.IsInstanceOf<CreateTableStatement>(result.Statements.ElementAt(0));
			Assert.IsInstanceOf<AlterTableStatement>(result.Statements.ElementAt(1));

			var statement = (CreateTableStatement) result.Statements.ElementAt(0);

			Assert.IsNotNull(statement);
			Assert.IsInstanceOf<CreateTableStatement>(statement);

			var createTable = (CreateTableStatement) statement;

			Assert.IsNotNull(createTable.TableName);
			Assert.AreEqual("test", createTable.TableName.Name);
		}
	}
}
