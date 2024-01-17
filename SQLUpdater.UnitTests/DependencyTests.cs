using NUnit.Framework;
using NUnit.Framework.Legacy;
using SQLUpdater.Lib;
using SQLUpdater.Lib.DBTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLUpdater.UnitTests
{
	[TestFixture]
	public class DependencyTests : BaseUnitTest
	{
		[Test]
		public void ForeignKeyWithSchemaTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE guest.foo( a int PRIMARY KEY, b int)");
			startingDatabase.Parse("CREATE TABLE guest.bar( c int FOREIGN KEY REFERENCES guest.foo(a))");

			ScriptSet scripts=startingDatabase.Database.CreateDiffScripts(new Database());
			ExecuteScripts(scripts);

			ScriptParser currentDatabase=ParseDatabase();
			scripts=currentDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ClassicAssert.AreEqual(0, scripts.Count);
		}

		[Test]
		public void RenamePrimaryKeyTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a int, b int)");
			startingDatabase.Parse(@"ALTER TABLE foo
ADD CONSTRAINT PK_BADNAME
PRIMARY KEY CLUSTERED(a)");
			startingDatabase.Parse("CREATE TABLE bar( c int FOREIGN KEY REFERENCES foo(a))");

			ScriptSet scripts=startingDatabase.Database.CreateDiffScripts(new Database());
			ExecuteScripts(scripts);

			ScriptParser modifiedDatabase=new ScriptParser();
			modifiedDatabase.Parse("CREATE TABLE foo( a int PRIMARY KEY, b int)");
			modifiedDatabase.Parse("CREATE TABLE bar( c int FOREIGN KEY REFERENCES foo(a))");

			scripts=modifiedDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			ClassicAssert.AreEqual(4, scripts.Count);
			ClassicAssert.AreEqual(ScriptType.DropForeignKey, scripts[0].Type);
			ClassicAssert.AreEqual(ScriptType.DropPrimaryKey, scripts[1].Type);
			ClassicAssert.AreEqual(ScriptType.PrimaryKey, scripts[2].Type);
			ClassicAssert.AreEqual(ScriptType.ForeignKey, scripts[3].Type);

			ExecuteScripts(scripts);
		}

		[Test]
		public void StoredProcedureWithSchemaTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a int, b int)");
			startingDatabase.Parse("CREATE TABLE guest.foo( a int, b int)");
			startingDatabase.Parse("CREATE PROCEDURE GetFoo(@a int) AS SELECT * FROM foo WHERE a=@a");
			startingDatabase.Parse("CREATE PROCEDURE guest.GetFoo(@a int) AS SELECT * FROM guest.foo WHERE a=@a");

			ScriptSet scripts=startingDatabase.Database.CreateDiffScripts(new Database());
			scripts.Sort();
			ClassicAssert.AreEqual(4, scripts.Count);
			ExecuteScripts(scripts);

			ScriptParser modifiedDatabase=new ScriptParser();
			modifiedDatabase.Parse("CREATE TABLE foo( a int, b int, c int)");
			modifiedDatabase.Parse("CREATE TABLE guest.foo( a int, b int)");
			modifiedDatabase.Parse("CREATE PROCEDURE GetFoo(@a int) AS SELECT * FROM foo WHERE a=@a");
			modifiedDatabase.Parse("CREATE PROCEDURE guest.GetFoo(@a int) AS SELECT * FROM guest.foo WHERE a=@a");

			scripts=modifiedDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			ClassicAssert.AreEqual(5, scripts.Count);
			ClassicAssert.AreEqual(ScriptType.DropStoredProc, scripts[0].Type);
			ClassicAssert.AreEqual(ScriptType.TableSaveData, scripts[1].Type);
			ClassicAssert.AreEqual(ScriptType.Table, scripts[2].Type);
			ClassicAssert.AreEqual(ScriptType.TableRestoreData, scripts[3].Type);
			ClassicAssert.AreEqual(ScriptType.StoredProc, scripts[4].Type);

			ExecuteScripts(scripts);
		}

		[Test]
		public void StoredProcedureWithTableTypeTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
            startingDatabase.Parse("CREATE TYPE foo AS TABLE( a int)");
			startingDatabase.Parse("CREATE PROCEDURE GetFoo(@a foo READONLY) AS SELECT * FROM @a");

			ScriptSet scripts=startingDatabase.Database.CreateDiffScripts(new Database());
			scripts.Sort();
			ClassicAssert.AreEqual(2, scripts.Count);
			ExecuteScripts(scripts);

            ScriptParser modifiedDatabase = new ScriptParser();
            modifiedDatabase.Parse("CREATE TYPE foo AS TABLE( a int, b int)");
            modifiedDatabase.Parse("CREATE PROCEDURE GetFoo(@a foo READONLY) AS SELECT * FROM @a");

			scripts=modifiedDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			ClassicAssert.AreEqual(4, scripts.Count);
			ClassicAssert.AreEqual(ScriptType.DropStoredProc, scripts[0].Type);
			ClassicAssert.AreEqual(ScriptType.DropTableType, scripts[1].Type);
			ClassicAssert.AreEqual(ScriptType.TableType, scripts[2].Type);
			ClassicAssert.AreEqual(ScriptType.StoredProc, scripts[3].Type);

			ExecuteScripts(scripts);
		}

		[Test]
		public void UpdateTableDataTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a int PRIMARY KEY, b int)");
			startingDatabase.Parse("INSERT INTO foo(a) VALUES (1)");
			startingDatabase.Parse("CREATE TABLE bar( c int FOREIGN KEY REFERENCES foo(a))");
			startingDatabase.Parse("INSERT INTO bar(c) VALUES (1)");

			ScriptParser currentDatabase=new ScriptParser();
			ScriptSet scripts=startingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			GetData(currentDatabase.Database, startingDatabase.Database.GetTablesWithData());
			ScriptSet difference=startingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE foo( a int PRIMARY KEY, b int)");
			endingDatabase.Parse("INSERT INTO foo(a, b) VALUES (1, 2)");
			endingDatabase.Parse("CREATE TABLE bar( c int FOREIGN KEY REFERENCES foo(a))");
			endingDatabase.Parse("INSERT INTO bar(c) VALUES (1)");

			scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			ClassicAssert.AreEqual(4, scripts.Count);

			ClassicAssert.AreEqual(ScriptType.DropForeignKey, scripts[0].Type);
			ClassicAssert.AreEqual(ScriptType.TableRemoveData, scripts[1].Type);
			ClassicAssert.AreEqual(@"DELETE FROM [dbo].[foo]
WHERE
	[a] = 1

", scripts[1].Text);
			ClassicAssert.AreEqual(ScriptType.TableData, scripts[2].Type);
			ClassicAssert.AreEqual(@"INSERT INTO [dbo].[foo]([a], [b])
VALUES(1, 2)

", scripts[2].Text);
			ClassicAssert.AreEqual(ScriptType.ForeignKey, scripts[3].Type);

			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			GetData(currentDatabase.Database, endingDatabase.Database.GetTablesWithData());
			difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void UpdateTableWithForeignKeyTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a int PRIMARY KEY)");
			startingDatabase.Parse("INSERT INTO foo(a) VALUES (1)");
			startingDatabase.Parse("CREATE TABLE bar( c int FOREIGN KEY REFERENCES foo(a))");
			startingDatabase.Parse("INSERT INTO bar(c) VALUES (1)");

			ScriptParser currentDatabase=new ScriptParser();
			ScriptSet scripts=startingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			GetData(currentDatabase.Database, startingDatabase.Database.GetTablesWithData());
			ScriptSet difference=startingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE foo( a int PRIMARY KEY, b int)");
			endingDatabase.Parse("INSERT INTO foo(a) VALUES (1)");
			endingDatabase.Parse("CREATE TABLE bar( c int FOREIGN KEY REFERENCES foo(a))");
			endingDatabase.Parse("INSERT INTO bar(c) VALUES (1)");

			scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			ClassicAssert.AreEqual(7, scripts.Count);

			ClassicAssert.AreEqual(scripts[0].Type, ScriptType.DropForeignKey);
			ClassicAssert.AreEqual(@"ALTER TABLE [dbo].[bar] DROP CONSTRAINT [FK_bar_foo]", scripts[0].Text);

			ClassicAssert.AreEqual(scripts[1].Type, ScriptType.DropPrimaryKey);
			ClassicAssert.AreEqual(@"ALTER TABLE [dbo].[foo] DROP CONSTRAINT [PK_foo]", scripts[1].Text);

			ClassicAssert.AreEqual(scripts[2].Type, ScriptType.TableSaveData);
			ClassicAssert.AreEqual("EXEC sp_rename '[dbo].[foo]', 'Tmp__foo', 'OBJECT'", scripts[2].Text);

			ClassicAssert.AreEqual(scripts[3].Type, ScriptType.Table);
			ClassicAssert.AreEqual(@"CREATE TABLE [dbo].[foo](
	[a] [int] NOT NULL,
	[b] [int] NULL
)

GO", scripts[3].Text);

			ClassicAssert.AreEqual(scripts[4].Type, ScriptType.PrimaryKey);
			ClassicAssert.AreEqual(@"ALTER TABLE [dbo].[foo]
ADD CONSTRAINT [PK_foo]
PRIMARY KEY(
	[a]
)", scripts[4].Text);

			ClassicAssert.AreEqual(scripts[5].Type, ScriptType.TableRestoreData);
			ClassicAssert.AreEqual(@"INSERT INTO [dbo].[foo] (
	[a]
)
SELECT
	[a]
FROM [dbo].[Tmp__foo] WITH (HOLDLOCK TABLOCKX)

DROP TABLE [dbo].[Tmp__foo]

GO

", scripts[5].Text);

			ClassicAssert.AreEqual(scripts[6].Type, ScriptType.ForeignKey);
			ClassicAssert.AreEqual(@"ALTER TABLE [dbo].[bar]
ADD CONSTRAINT [FK_bar_foo]
FOREIGN KEY(
	[c]
)
REFERENCES [dbo].[foo](
	[a]
)", scripts[6].Text);

			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			GetData(currentDatabase.Database, endingDatabase.Database.GetTablesWithData());
			difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}
	}
}
