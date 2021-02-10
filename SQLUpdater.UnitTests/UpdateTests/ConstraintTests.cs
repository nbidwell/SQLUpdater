using NUnit.Framework;
using SQLUpdater.Lib;
using SQLUpdater.Lib.DBTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLUpdater.UnitTests.UpdateTests
{
	[TestFixture]
	public class ConstraintTests : BaseUnitTest
    {
        [Test]
        public void CheckTest()
        {
            ScriptParser startingDatabase = new ScriptParser();
            startingDatabase.Parse("CREATE TABLE foo( a varchar(10) CHECK ( a in ('X', 'Y')))");

            ScriptParser currentDatabase = new ScriptParser();
            ScriptSet scripts = startingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
            scripts.Sort();

            Assert.AreEqual(2, scripts.Count);
            Assert.AreEqual(scripts[0].Type, ScriptType.Table);
            Assert.AreEqual(scripts[1].Type, ScriptType.CheckConstraint);

            ExecuteScripts(scripts);

            ScriptParser endingDatabase = new ScriptParser();
            endingDatabase.Parse("CREATE TABLE foo( a varchar(10) CHECK ( a in ('X', 'Y', 'Z')))");

            scripts = endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
            scripts.Sort();

            Assert.AreEqual(2, scripts.Count);
            Assert.AreEqual(scripts[0].Type, ScriptType.DropConstraint);
            Assert.AreEqual(scripts[1].Type, ScriptType.CheckConstraint);

            ExecuteScripts(scripts);

            currentDatabase = ParseDatabase();
            ScriptSet difference = endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
            Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
        }

        [Test]
        public void CheckFunctionTest()
        {
            ScriptParser startingDatabase = new ScriptParser();
            startingDatabase.Parse("CREATE FUNCTION dbo.checker(@val varchar(10)) RETURNS int AS BEGIN RETURN 1 END");
            startingDatabase.Parse("CREATE TABLE foo( a varchar(10) CHECK ( dbo.checker(a) = 1))");

            ScriptParser currentDatabase = new ScriptParser();
            ScriptSet scripts = startingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
            scripts.Sort();

            Assert.AreEqual(3, scripts.Count);
            Assert.AreEqual(scripts[0].Type, ScriptType.Table);
            Assert.AreEqual(scripts[1].Type, ScriptType.UserDefinedFunction);
            Assert.AreEqual(scripts[2].Type, ScriptType.CheckConstraint);

            ExecuteScripts(scripts);

            ScriptParser endingDatabase = new ScriptParser();
            endingDatabase.Parse("CREATE FUNCTION dbo.checker(@val varchar(10)) RETURNS int AS BEGIN RETURN 1 END");
            endingDatabase.Parse("CREATE TABLE foo( a varchar(10) CHECK ( dbo.checker(a) = 1), b int)");

            scripts = endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
            scripts.Sort();

            Assert.AreEqual(5, scripts.Count);
            Assert.AreEqual(scripts[0].Type, ScriptType.DropConstraint);
            Assert.AreEqual(scripts[1].Type, ScriptType.TableSaveData);
            Assert.AreEqual(scripts[2].Type, ScriptType.Table);
            Assert.AreEqual(scripts[3].Type, ScriptType.CheckConstraint);
            Assert.AreEqual(scripts[4].Type, ScriptType.TableRestoreData);

            ExecuteScripts(scripts);

            currentDatabase = ParseDatabase();
            ScriptSet difference = endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
            Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
        }

        [Test]
		public void DefaultTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a int default 0)");

			ScriptParser currentDatabase=new ScriptParser();
			ScriptSet scripts=startingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ExecuteScripts(scripts);

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE foo( a int default 2)");

			scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			Assert.AreEqual(2, scripts.Count);

			Assert.AreEqual(scripts[0].Type, ScriptType.DropConstraint);
			Assert.AreEqual(@"ALTER TABLE [dbo].[foo] DROP CONSTRAINT [DF_foo_a]", scripts[0].Text);

			Assert.AreEqual(scripts[1].Type, ScriptType.DefaultConstraint);
			Assert.AreEqual(@"ALTER TABLE [dbo].[foo]
ADD CONSTRAINT [DF_foo_a]
DEFAULT ( 2 )
FOR [a]", scripts[1].Text);

			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void DefaultNameTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a int)");
			startingDatabase.Parse("ALTER TABLE foo ADD CONSTRAINT DF_foo_a123456 DEFAULT 0 FOR a");

			ScriptParser currentDatabase=new ScriptParser();
			ScriptSet scripts=startingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ExecuteScripts(scripts);

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE foo( a int default 0)");

			scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			Assert.AreEqual(2, scripts.Count);

			Assert.AreEqual(scripts[0].Type, ScriptType.DropConstraint);
			Assert.AreEqual(@"ALTER TABLE [dbo].[foo] DROP CONSTRAINT [DF_foo_a123456]", scripts[0].Text);

			Assert.AreEqual(scripts[1].Type, ScriptType.DefaultConstraint);
			Assert.AreEqual(@"ALTER TABLE [dbo].[foo]
ADD CONSTRAINT [DF_foo_a]
DEFAULT ( 0 )
FOR [a]", scripts[1].Text);

			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void DefaultNameNotFullyScriptedTest()
		{
			RunOptions.Current.FullyScripted=false;

			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a int)");
			startingDatabase.Parse("ALTER TABLE foo ADD CONSTRAINT DF_foo_a123456 DEFAULT 0 FOR a");

			ScriptParser currentDatabase=new ScriptParser();
			ScriptSet scripts=startingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ExecuteScripts(scripts);

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE foo( a int default 0)");

			scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			Assert.AreEqual(2, scripts.Count);

			Assert.AreEqual(scripts[0].Type, ScriptType.DropConstraint);
			Assert.AreEqual(@"ALTER TABLE [dbo].[foo] DROP CONSTRAINT [DF_foo_a123456]", scripts[0].Text);

			Assert.AreEqual(scripts[1].Type, ScriptType.DefaultConstraint);
			Assert.AreEqual(@"ALTER TABLE [dbo].[foo]
ADD CONSTRAINT [DF_foo_a]
DEFAULT ( 0 )
FOR [a]", scripts[1].Text);

			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void ForeignKeyColumnTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a int PRIMARY KEY, b int)");
			startingDatabase.Parse("CREATE TABLE bar( c int FOREIGN KEY REFERENCES foo(a))");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE foo( a int, b int PRIMARY KEY)");
			endingDatabase.Parse("CREATE TABLE bar( c int FOREIGN KEY REFERENCES foo(b))");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			Assert.AreEqual(7, scripts.Count);

			Assert.AreEqual(scripts[0].Type, ScriptType.DropForeignKey);
			Assert.AreEqual(@"ALTER TABLE [dbo].[bar] DROP CONSTRAINT [FK_bar_foo]", scripts[0].Text);

			Assert.AreEqual(scripts[1].Type, ScriptType.DropPrimaryKey);
			Assert.AreEqual(@"ALTER TABLE [dbo].[foo] DROP CONSTRAINT [PK_foo]", scripts[1].Text);

			Assert.AreEqual(scripts[2].Type, ScriptType.TableSaveData);
			Assert.AreEqual("EXEC sp_rename '[dbo].[foo]', 'Tmp__foo', 'OBJECT'", scripts[2].Text);

			Assert.AreEqual(scripts[3].Type, ScriptType.Table);
			Assert.AreEqual(@"CREATE TABLE [dbo].[foo](
	[a] [int] NULL,
	[b] [int] NOT NULL
)

GO", scripts[3].Text);

			Assert.AreEqual(scripts[4].Type, ScriptType.PrimaryKey);
			Assert.AreEqual(@"ALTER TABLE [dbo].[foo]
ADD CONSTRAINT [PK_foo]
PRIMARY KEY(
	[b]
)", scripts[4].Text);

			Assert.AreEqual(scripts[5].Type, ScriptType.TableRestoreData);
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

", scripts[5].Text);

			Assert.AreEqual(scripts[6].Type, ScriptType.ForeignKey);
			Assert.AreEqual(@"ALTER TABLE [dbo].[bar]
ADD CONSTRAINT [FK_bar_foo]
FOREIGN KEY(
	[c]
)
REFERENCES [dbo].[foo](
	[b]
)", scripts[6].Text);

			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void ForeignKeyNameTest()
		{
			RunOptions.Current.FullyScripted=false;

			ExecuteScripts(new Script(@"CREATE TABLE foo( a int NOT NULL)
GO
ALTER TABLE [dbo].[foo]
ADD CONSTRAINT [PK_foo]
PRIMARY KEY CLUSTERED(
	[a]
)
GO
CREATE TABLE bar( c int FOREIGN KEY REFERENCES foo(a))
", "INIT", ScriptType.Unknown));

			ScriptParser currentDatabase=ParseDatabase();

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE foo( a int PRIMARY KEY)");
			endingDatabase.Parse("CREATE TABLE bar( c int FOREIGN KEY REFERENCES foo(a))");

			ScriptSet differences=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			differences.Sort();
			Assert.AreEqual(2, differences.Count);

			Assert.AreEqual(ScriptType.DropForeignKey, differences[0].Type);
			//don't know what the name will be, doesn't matter

			Assert.AreEqual(ScriptType.ForeignKey, differences[1].Type);
			Assert.AreEqual(@"ALTER TABLE [dbo].[bar]
ADD CONSTRAINT [FK_bar_foo]
FOREIGN KEY(
	[c]
)
REFERENCES [dbo].[foo](
	[a]
)", differences[1].Text);

			ExecuteScripts(differences);

			currentDatabase=ParseDatabase();
			differences=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			Assert.AreEqual(0, differences.Count);
		}

		[Test]
		public void ForeignKeyUniqueConstraintNameChangeTest()
		{
			RunOptions.Current.FullyScripted=true;

			//Set up the database
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE base(a int unique)");
			parser.Parse("CREATE TABLE dependant(a int foreign key references base(a))");

			ScriptSet differences=parser.Database.CreateDiffScripts(new Database());
			differences.Sort();
			Assert.AreEqual(4, differences.Count);
			Assert.AreEqual(ScriptType.Table, differences[0].Type);
			Assert.AreEqual(ScriptType.Table, differences[1].Type);
			Assert.AreEqual(ScriptType.UniqueConstraint, differences[2].Type);
			Assert.AreEqual(ScriptType.ForeignKey, differences[3].Type);

			//Make sure everything applies correctly
			ExecuteScripts(differences);
			ScriptParser currentDatabase=ParseDatabase();

			differences=parser.Database.CreateDiffScripts(currentDatabase.Database);
			Assert.AreEqual(0, differences.Count);

			//Rename the unique constraint
			parser=new ScriptParser();
			parser.Parse("CREATE TABLE base(a int CONSTRAINT base_a unique)");
			parser.Parse("CREATE TABLE dependant(a int foreign key references base(a))");

			differences=parser.Database.CreateDiffScripts(currentDatabase.Database);
			differences.Sort();
			Assert.AreEqual(4, differences.Count);
			Assert.AreEqual(ScriptType.DropForeignKey, differences[0].Type);
			Assert.AreEqual(ScriptType.DropConstraint, differences[1].Type);
			Assert.AreEqual(ScriptType.UniqueConstraint, differences[2].Type);
			Assert.AreEqual(ScriptType.ForeignKey, differences[3].Type);

			//Make sure everything applies correctly
			ExecuteScripts(differences);
			currentDatabase=ParseDatabase();

			differences=parser.Database.CreateDiffScripts(currentDatabase.Database);
			Assert.AreEqual(0, differences.Count);
		}

		[Test]
		public void ForeignKeyUniqueIndexNameChangeTest()
		{
			RunOptions.Current.FullyScripted=true;

			//Set up the database
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE base(a int)");
			parser.Parse("CREATE TABLE dependant(a int foreign key references base(a))");
			parser.Parse("CREATE UNIQUE NONCLUSTERED INDEX ix_base ON base(a)");

			ScriptSet differences=parser.Database.CreateDiffScripts(new Database());
			differences.Sort();
			Assert.AreEqual(4, differences.Count);
			Assert.AreEqual(ScriptType.Table, differences[0].Type);
			Assert.AreEqual(ScriptType.Table, differences[1].Type);
			Assert.AreEqual(ScriptType.Index, differences[2].Type);
			Assert.AreEqual(ScriptType.ForeignKey, differences[3].Type);

			//Make sure everything applies correctly
			ExecuteScripts(differences);
			ScriptParser currentDatabase=ParseDatabase();

			differences=parser.Database.CreateDiffScripts(currentDatabase.Database);
			Assert.AreEqual(0, differences.Count);

			//Rename the unique constraint
			parser=new ScriptParser();
			parser.Parse("CREATE TABLE base(a int)");
			parser.Parse("CREATE TABLE dependant(a int foreign key references base(a))");
			parser.Parse("CREATE UNIQUE NONCLUSTERED INDEX ix_base_a ON base(a)");

			differences=parser.Database.CreateDiffScripts(currentDatabase.Database);
			differences.Sort();
			Assert.AreEqual(4, differences.Count);
			Assert.AreEqual(ScriptType.DropForeignKey, differences[0].Type);
			Assert.AreEqual(ScriptType.DropIndex, differences[1].Type);
			Assert.AreEqual(ScriptType.Index, differences[2].Type);
			Assert.AreEqual(ScriptType.ForeignKey, differences[3].Type);

			//Make sure everything applies correctly
			ExecuteScripts(differences);
			currentDatabase=ParseDatabase();

			differences=parser.Database.CreateDiffScripts(currentDatabase.Database);
			Assert.AreEqual(0, differences.Count);
		}

		[Test]
		public void PrimaryKeyNameTest()
		{
			RunOptions.Current.FullyScripted=false;

			ExecuteScripts(new Script("CREATE TABLE foo( a int PRIMARY KEY)", "INIT", ScriptType.Unknown));

			ScriptParser currentDatabase=ParseDatabase();

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE foo( a int PRIMARY KEY)");

			ScriptSet differences=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			differences.Sort();
			Assert.AreEqual(2, differences.Count);

			Assert.AreEqual(ScriptType.DropPrimaryKey, differences[0].Type);
			//don't know what the name will be, doesn't matter

			Assert.AreEqual(ScriptType.PrimaryKey, differences[1].Type);
			Assert.AreEqual(@"ALTER TABLE [dbo].[foo]
ADD CONSTRAINT [PK_foo]
PRIMARY KEY(
	[a]
)", differences[1].Text);

			ExecuteScripts(differences);

			currentDatabase=ParseDatabase();
			differences=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			Assert.AreEqual(0, differences.Count);
		}

		[Test]
		public void PrimaryKeyTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a int PRIMARY KEY, b int)");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE foo( a int, b int PRIMARY KEY)");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			Assert.AreEqual(5, scripts.Count);

			Assert.AreEqual(scripts[0].Type, ScriptType.DropPrimaryKey);
			Assert.AreEqual(@"ALTER TABLE [dbo].[foo] DROP CONSTRAINT [PK_foo]", scripts[0].Text);

			Assert.AreEqual(scripts[1].Type, ScriptType.TableSaveData);
			Assert.AreEqual("EXEC sp_rename '[dbo].[foo]', 'Tmp__foo', 'OBJECT'", scripts[1].Text);

			Assert.AreEqual(scripts[2].Type, ScriptType.Table);
			Assert.AreEqual(@"CREATE TABLE [dbo].[foo](
	[a] [int] NULL,
	[b] [int] NOT NULL
)

GO", scripts[2].Text);

			Assert.AreEqual(scripts[3].Type, ScriptType.PrimaryKey);
			Assert.AreEqual(@"ALTER TABLE [dbo].[foo]
ADD CONSTRAINT [PK_foo]
PRIMARY KEY(
	[b]
)", scripts[3].Text);

			Assert.AreEqual(scripts[4].Type, ScriptType.TableRestoreData);
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

", scripts[4].Text);

			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}
	}
}
