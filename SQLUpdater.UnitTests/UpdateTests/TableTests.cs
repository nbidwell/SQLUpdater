using NUnit.Framework;
using SQLUpdater.Lib;
using SQLUpdater.Lib.DBTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLUpdater.UnitTests.UpdateTests
{
	[TestFixture]
	public class TableTests : BaseUnitTest
	{
		[Test]
		public void AddColumnTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a int)");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE foo( a int, b int)");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			Assert.AreEqual(3, scripts.Count);

			Assert.AreEqual(scripts[0].Type, ScriptType.TableSaveData);
			Assert.AreEqual("EXEC sp_rename '[dbo].[foo]', 'Tmp__foo', 'OBJECT'", scripts[0].Text);

			Assert.AreEqual(scripts[1].Type, ScriptType.Table);
			//the actual script should be tested in the parser tests

			Assert.AreEqual(scripts[2].Type, ScriptType.TableRestoreData);
			Assert.AreEqual(@"INSERT INTO [dbo].[foo] (
	[a]
)
SELECT
	[a]
FROM [dbo].[Tmp__foo] WITH (HOLDLOCK TABLOCKX)

DROP TABLE [dbo].[Tmp__foo]

GO

", scripts[2].Text);
			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

        [Test]
        public void AddDefaultTest()
        {
            ScriptParser startingDatabase = new ScriptParser();
            startingDatabase.Parse("CREATE TABLE foo( a int, b int)");
            startingDatabase.Parse("INSERT INTO foo(a) VALUES(1)");

            ScriptParser currentDatabase = new ScriptParser();
            ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

            ScriptParser endingDatabase = new ScriptParser();
            endingDatabase.Parse("CREATE TABLE foo( a int, b int NOT NULL DEFAULT(0))");

            ScriptSet scripts = endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
            scripts.Sort();
            Assert.AreEqual(4, scripts.Count);

            Assert.AreEqual(scripts[0].Type, ScriptType.TableSaveData);
            Assert.AreEqual("EXEC sp_rename '[dbo].[foo]', 'Tmp__foo', 'OBJECT'", scripts[0].Text);

            Assert.AreEqual(scripts[1].Type, ScriptType.Table);
            //the actual script should be tested in the parser tests

            Assert.AreEqual(scripts[2].Type, ScriptType.DefaultConstraint);
            Assert.AreEqual(@"ALTER TABLE [dbo].[foo]
ADD CONSTRAINT [DF_foo_b]
DEFAULT ( 0 )
FOR [b]", scripts[2].Text);

            Assert.AreEqual(scripts[3].Type, ScriptType.TableRestoreData);
            Assert.AreEqual(@"INSERT INTO [dbo].[foo] (
	[a],
	[b]
)
SELECT
	[a],
	ISNULL([b], 0)
FROM [dbo].[Tmp__foo] WITH (HOLDLOCK TABLOCKX)

DROP TABLE [dbo].[Tmp__foo]

GO

", scripts[3].Text);

            ExecuteScripts(scripts);

            currentDatabase = ParseDatabase();
            ScriptSet difference = endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
            Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
        }

		[Test]
		public void AsColumnTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a int, b as [a])");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE foo(a int, b int)");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			Assert.AreEqual(3, scripts.Count);

			Assert.AreEqual(scripts[0].Type, ScriptType.TableSaveData);
			Assert.AreEqual("EXEC sp_rename '[dbo].[foo]', 'Tmp__foo', 'OBJECT'", scripts[0].Text);

			Assert.AreEqual(scripts[1].Type, ScriptType.Table);
			//the actual script should be tested in the parser tests

			Assert.AreEqual(scripts[2].Type, ScriptType.TableRestoreData);
			Assert.AreEqual(@"INSERT INTO [dbo].[foo] (
	[a],
	[b]
)
SELECT
	[a],
	CAST([b] AS [int])
FROM [dbo].[Tmp__foo] WITH (HOLDLOCK TABLOCKX)

DROP TABLE [dbo].[Tmp__foo]

GO

", scripts[2].Text);
			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void CollationTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a varchar(5))");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE foo( a varchar(5) collate Latin1_General_CI_AS)");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			Assert.AreEqual(3, scripts.Count);

			Assert.AreEqual(scripts[0].Type, ScriptType.TableSaveData);
			Assert.AreEqual("EXEC sp_rename '[dbo].[foo]', 'Tmp__foo', 'OBJECT'", scripts[0].Text);

			Assert.AreEqual(scripts[1].Type, ScriptType.Table);
			//the actual script should be tested in the parser tests

			Assert.AreEqual(scripts[2].Type, ScriptType.TableRestoreData);
			Assert.AreEqual(@"INSERT INTO [dbo].[foo] (
	[a]
)
SELECT
	[a]
FROM [dbo].[Tmp__foo] WITH (HOLDLOCK TABLOCKX)

DROP TABLE [dbo].[Tmp__foo]

GO

", scripts[2].Text);
			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void ColumnNameTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a int)");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE foo( b int)");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			Assert.AreEqual(3, scripts.Count);

			Assert.AreEqual(scripts[0].Type, ScriptType.TableSaveData);
			Assert.AreEqual("EXEC sp_rename '[dbo].[foo]', 'Tmp__foo', 'OBJECT'", scripts[0].Text);

			Assert.AreEqual(scripts[1].Type, ScriptType.Table);
			//the actual script should be tested in the parser tests

			Assert.AreEqual(scripts[2].Type, ScriptType.TableRestoreData);
			Assert.AreEqual(@"INSERT INTO [dbo].[foo] (
	[b]
)
SELECT
	[a]
FROM [dbo].[Tmp__foo] WITH (HOLDLOCK TABLOCKX)

DROP TABLE [dbo].[Tmp__foo]

GO

", scripts[2].Text);
			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void ColumnNullableTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a int)");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE foo( a int not null)");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			Assert.AreEqual(3, scripts.Count);

			Assert.AreEqual(scripts[0].Type, ScriptType.TableSaveData);
			Assert.AreEqual("EXEC sp_rename '[dbo].[foo]', 'Tmp__foo', 'OBJECT'", scripts[0].Text);

			Assert.AreEqual(scripts[1].Type, ScriptType.Table);
			//the actual script should be tested in the parser tests

			Assert.AreEqual(scripts[2].Type, ScriptType.TableRestoreData);
			Assert.AreEqual(@"INSERT INTO [dbo].[foo] (
	[a]
)
SELECT
	[a]
FROM [dbo].[Tmp__foo] WITH (HOLDLOCK TABLOCKX)

DROP TABLE [dbo].[Tmp__foo]

GO

", scripts[2].Text);
			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void ColumnSizeTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a varchar(5))");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE foo( a varchar(10))");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			Assert.AreEqual(3, scripts.Count);

			Assert.AreEqual(scripts[0].Type, ScriptType.TableSaveData);
			Assert.AreEqual("EXEC sp_rename '[dbo].[foo]', 'Tmp__foo', 'OBJECT'", scripts[0].Text);

			Assert.AreEqual(scripts[1].Type, ScriptType.Table);
			//the actual script should be tested in the parser tests

			Assert.AreEqual(scripts[2].Type, ScriptType.TableRestoreData);
			Assert.AreEqual(@"INSERT INTO [dbo].[foo] (
	[a]
)
SELECT
	[a]
FROM [dbo].[Tmp__foo] WITH (HOLDLOCK TABLOCKX)

DROP TABLE [dbo].[Tmp__foo]

GO

", scripts[2].Text);
			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void ColumnTypeTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a char(10))");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE foo( a varchar(10))");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			Assert.AreEqual(3, scripts.Count);

			Assert.AreEqual(scripts[0].Type, ScriptType.TableSaveData);
			Assert.AreEqual("EXEC sp_rename '[dbo].[foo]', 'Tmp__foo', 'OBJECT'", scripts[0].Text);

			Assert.AreEqual(scripts[1].Type, ScriptType.Table);
			//the actual script should be tested in the parser tests

			Assert.AreEqual(scripts[2].Type, ScriptType.TableRestoreData);
			Assert.AreEqual(@"INSERT INTO [dbo].[foo] (
	[a]
)
SELECT
	CAST([a] AS [varchar](10))
FROM [dbo].[Tmp__foo] WITH (HOLDLOCK TABLOCKX)

DROP TABLE [dbo].[Tmp__foo]

GO

", scripts[2].Text);
			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void DefaultCollationTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a varchar(5))");

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE foo( a varchar(5) collate SQL_Latin1_General_CP1_CI_AS)");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			Assert.AreEqual(0, scripts.Count);
		}

		[Test]
		public void FilegroupDefaultTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a int)");

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE foo( a int) ON Primary");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			Assert.AreEqual(0, scripts.Count);
		}

		[Test]
		public void FilegroupTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a int)");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE foo( a int) ON Secondary");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			Assert.AreEqual(3, scripts.Count);

			Assert.AreEqual(scripts[0].Type, ScriptType.TableSaveData);
			Assert.AreEqual("EXEC sp_rename '[dbo].[foo]', 'Tmp__foo', 'OBJECT'", scripts[0].Text);

			Assert.AreEqual(scripts[1].Type, ScriptType.Table);
			//the actual script should be tested in the parser tests

			Assert.AreEqual(scripts[2].Type, ScriptType.TableRestoreData);
			Assert.AreEqual(@"INSERT INTO [dbo].[foo] (
	[a]
)
SELECT
	[a]
FROM [dbo].[Tmp__foo] WITH (HOLDLOCK TABLOCKX)

DROP TABLE [dbo].[Tmp__foo]

GO

", scripts[2].Text);
			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void GrantTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a varchar(5))");
			startingDatabase.Parse("GRANT SELECT ON foo TO public");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE foo( a varchar(10)) GO GRANT SELECT ON foo TO public");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			Assert.AreEqual(4, scripts.Count);

			Assert.AreEqual(scripts[0].Type, ScriptType.TableSaveData);
			Assert.AreEqual(scripts[1].Type, ScriptType.Table);
			Assert.AreEqual(scripts[2].Type, ScriptType.TableRestoreData);

			Assert.AreEqual(scripts[3].Type, ScriptType.Permission);
			Assert.AreEqual(@"GRANT SELECT ON [dbo].[foo] TO [public]

GO

",
				scripts[3].Text);

			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void IdentityColumnIncrementTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a int IDENTITY(0, 2))");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE foo( a int IDENTITY(0, 1))");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			Assert.AreEqual(3, scripts.Count);

			Assert.AreEqual(scripts[0].Type, ScriptType.TableSaveData);
			Assert.AreEqual("EXEC sp_rename '[dbo].[foo]', 'Tmp__foo', 'OBJECT'", scripts[0].Text);

			Assert.AreEqual(scripts[1].Type, ScriptType.Table);
			//the actual script should be tested in the parser tests

			Assert.AreEqual(scripts[2].Type, ScriptType.TableRestoreData);
			Assert.AreEqual(@"SET IDENTITY_INSERT [dbo].[foo] ON
GO

INSERT INTO [dbo].[foo] (
	[a]
)
SELECT
	[a]
FROM [dbo].[Tmp__foo] WITH (HOLDLOCK TABLOCKX)

DROP TABLE [dbo].[Tmp__foo]

GO

SET IDENTITY_INSERT [dbo].[foo] OFF
GO

", scripts[2].Text);
			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void IdentityColumnSeedTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a int IDENTITY(0, 1))");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE foo( a int IDENTITY(1, 1))");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			Assert.AreEqual(3, scripts.Count);

			Assert.AreEqual(scripts[0].Type, ScriptType.TableSaveData);
			Assert.AreEqual("EXEC sp_rename '[dbo].[foo]', 'Tmp__foo', 'OBJECT'", scripts[0].Text);

			Assert.AreEqual(scripts[1].Type, ScriptType.Table);
			//the actual script should be tested in the parser tests

			Assert.AreEqual(scripts[2].Type, ScriptType.TableRestoreData);
			Assert.AreEqual(@"SET IDENTITY_INSERT [dbo].[foo] ON
GO

INSERT INTO [dbo].[foo] (
	[a]
)
SELECT
	[a]
FROM [dbo].[Tmp__foo] WITH (HOLDLOCK TABLOCKX)

DROP TABLE [dbo].[Tmp__foo]

GO

SET IDENTITY_INSERT [dbo].[foo] OFF
GO

", scripts[2].Text);
			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void IdentityColumnTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a int)");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE foo( a int IDENTITY(0, 1))");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			Assert.AreEqual(3, scripts.Count);

			Assert.AreEqual(scripts[0].Type, ScriptType.TableSaveData);
			Assert.AreEqual("EXEC sp_rename '[dbo].[foo]', 'Tmp__foo', 'OBJECT'", scripts[0].Text);

			Assert.AreEqual(scripts[1].Type, ScriptType.Table);
			//the actual script should be tested in the parser tests

			Assert.AreEqual(scripts[2].Type, ScriptType.TableRestoreData);
			Assert.AreEqual(@"SET IDENTITY_INSERT [dbo].[foo] ON
GO

INSERT INTO [dbo].[foo] (
	[a]
)
SELECT
	[a]
FROM [dbo].[Tmp__foo] WITH (HOLDLOCK TABLOCKX)

DROP TABLE [dbo].[Tmp__foo]

GO

SET IDENTITY_INSERT [dbo].[foo] OFF
GO

", scripts[2].Text);
			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void InsertColumnTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse(@"CREATE TABLE a ( b int, c int)");

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse(@"CREATE TABLE a ( b int, d int, c int)");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			Assert.AreEqual(3, scripts.Count);
			Assert.AreEqual(scripts[0].Type, ScriptType.TableSaveData);
			Assert.AreEqual(scripts[1].Type, ScriptType.Table);
			Assert.AreEqual(scripts[2].Type, ScriptType.TableRestoreData);

			Assert.AreEqual(@"INSERT INTO [dbo].[a] (
	[b],
	[c]
)
SELECT
	[b],
	[c]
FROM [dbo].[Tmp__a] WITH (HOLDLOCK TABLOCKX)

DROP TABLE [dbo].[Tmp__a]

GO

",
				scripts[2].Text);
		}

        [Test]
        public void PermissionTest()
        {
            ScriptParser startingDatabase = new ScriptParser();
            startingDatabase.Parse("CREATE TABLE foo( a int)");

            ScriptParser currentDatabase = new ScriptParser();
            ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

            ScriptParser endingDatabase = new ScriptParser();
            endingDatabase.Parse("CREATE TABLE foo( a int)");
            endingDatabase.Parse("GRANT SELECT ON foo TO PUBLIC");

            ScriptSet scripts = endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
            Assert.AreEqual(1, scripts.Count);
            Assert.AreEqual(scripts[0].Type, ScriptType.Permission);

            ExecuteScripts(scripts);

            currentDatabase = ParseDatabase();
            ScriptSet difference = endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
            Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
        }

		[Test]
		public void TableNameChangeTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a int)");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE bar( a int)");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			Assert.AreEqual(1, scripts.Count);
			Assert.AreEqual(scripts[0].Type, ScriptType.Table);

			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void TableNameCompareTest()
		{
			Table a=new Table("a");
			Table b=new Table("b");

			Difference difference=a.GetDifferences(b, true);
			Assert.IsNotNull(difference);
			Assert.AreEqual(1, difference.Messages.Count);
		}

		[Test]
		public void TextFilegroupTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a int, b varchar(max))");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE foo( a int, b varchar(max)) TEXTIMAGE_ON Secondary");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			Assert.AreEqual(3, scripts.Count);

			Assert.AreEqual(scripts[0].Type, ScriptType.TableSaveData);
			Assert.AreEqual("EXEC sp_rename '[dbo].[foo]', 'Tmp__foo', 'OBJECT'", scripts[0].Text);

			Assert.AreEqual(scripts[1].Type, ScriptType.Table);
			//the actual script should be tested in the parser tests

			Assert.AreEqual(scripts[2].Type, ScriptType.TableRestoreData);
			Assert.AreEqual(@"INSERT INTO [dbo].[foo] (
	[a],
	[b]
)
SELECT
	[a],
	[b]
FROM [dbo].[Tmp__foo] WITH (HOLDLOCK TABLOCKX)

DROP TABLE [dbo].[Tmp__foo]

GO

", scripts[2].Text);
			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}
	}
}
