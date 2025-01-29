using NUnit.Framework;
using NUnit.Framework.Legacy;
using SQLUpdater.Lib;
using SQLUpdater.Lib.DBTypes;
using System.Collections.Generic;
using System.Text;

namespace SQLUpdater.UnitTests.UpdateTests
{
	[TestFixture]
	public class IndexTests : BaseUnitTest
	{
		[Test]
		public void ClusterTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo(a int)");
			startingDatabase.Parse("CREATE INDEX foo_a ON dbo.foo(a)");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE foo(a int)");
			endingDatabase.Parse("CREATE CLUSTERED INDEX foo_a ON dbo.foo(a)");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			ClassicAssert.AreEqual(2, scripts.Count);

			ClassicAssert.AreEqual(scripts[0].Type, ScriptType.DropIndex);
			ClassicAssert.AreEqual(@"DROP INDEX [foo_a] ON [dbo].[foo]
", scripts[0].Text);

			ClassicAssert.AreEqual(scripts[1].Type, ScriptType.ClusteredIndex);
			//the actual script should be tested in the parser tests
			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void ColumnChangeTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo(a int, b int)");
			startingDatabase.Parse("CREATE INDEX foo_a ON dbo.foo(a)");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE foo(a int, b int)");
			endingDatabase.Parse("CREATE INDEX foo_a ON dbo.foo(b)");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			ClassicAssert.AreEqual(2, scripts.Count);

			ClassicAssert.AreEqual(scripts[0].Type, ScriptType.DropIndex);
			ClassicAssert.AreEqual(@"DROP INDEX [foo_a] ON [dbo].[foo]
", scripts[0].Text);

			ClassicAssert.AreEqual(scripts[1].Type, ScriptType.Index);
			//the actual script should be tested in the parser tests
			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void ColumnCountTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo(a int, b int)");
			startingDatabase.Parse("CREATE INDEX foo_a ON dbo.foo(a)");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE foo(a int, b int)");
			endingDatabase.Parse("CREATE INDEX foo_a ON dbo.foo(a, b)");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			ClassicAssert.AreEqual(2, scripts.Count);

			ClassicAssert.AreEqual(scripts[0].Type, ScriptType.DropIndex);
			ClassicAssert.AreEqual(@"DROP INDEX [foo_a] ON [dbo].[foo]
", scripts[0].Text);

			ClassicAssert.AreEqual(scripts[1].Type, ScriptType.Index);
			//the actual script should be tested in the parser tests
			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void ColumnDirectionTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo(a int)");
			startingDatabase.Parse("CREATE INDEX foo_a ON dbo.foo(a)");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE foo(a int)");
			endingDatabase.Parse("CREATE INDEX foo_a ON dbo.foo(a desc)");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			ClassicAssert.AreEqual(2, scripts.Count);

			ClassicAssert.AreEqual(scripts[0].Type, ScriptType.DropIndex);
			ClassicAssert.AreEqual(@"DROP INDEX [foo_a] ON [dbo].[foo]
", scripts[0].Text);

			ClassicAssert.AreEqual(scripts[1].Type, ScriptType.Index);
			//the actual script should be tested in the parser tests
			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void DropClusteredIndexOnViewTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE Foo(a int, b int, c int)");
			parser.Parse("CREATE VIEW Bar WITH SCHEMABINDING AS SELECT a, b FROM dbo.Foo");
			parser.Parse("CREATE UNIQUE CLUSTERED INDEX IX_Bar_a ON Bar(a)");
			parser.Parse("CREATE INDEX IX_Bar_b ON Bar(b)");

			ScriptSet scripts=parser.Database.CreateDiffScripts(new Database());
			scripts.Sort();
			ClassicAssert.AreEqual(4, scripts.Count);
			ClassicAssert.AreEqual(ScriptType.Table, scripts[0].Type);
			ClassicAssert.AreEqual(ScriptType.View, scripts[1].Type);
			ClassicAssert.AreEqual(ScriptType.ClusteredIndex, scripts[2].Type);
			ClassicAssert.AreEqual(ScriptType.Index, scripts[3].Type);
			ExecuteScripts(scripts);
			
			parser=new ScriptParser();
			parser.Parse("CREATE TABLE Foo(a int, b int, c int)");
			parser.Parse("CREATE VIEW Bar WITH SCHEMABINDING AS SELECT a, b, c FROM dbo.Foo");
			parser.Parse("CREATE UNIQUE CLUSTERED INDEX IX_Bar_a ON Bar(a)");
			parser.Parse("CREATE INDEX IX_Bar_b ON Bar(b)");

			ScriptParser currentDatabase=ParseDatabase();
			scripts=parser.Database.CreateDiffScripts(currentDatabase.Database);
			scripts.Sort();
			ClassicAssert.AreEqual(6, scripts.Count);
			ClassicAssert.AreEqual(ScriptType.DropIndex, scripts[0].Type);
			ClassicAssert.AreEqual(ScriptType.DropClusteredIndex, scripts[1].Type);
			ClassicAssert.AreEqual(ScriptType.DropView, scripts[2].Type);
			ClassicAssert.AreEqual(ScriptType.View, scripts[3].Type);
			ClassicAssert.AreEqual(ScriptType.ClusteredIndex, scripts[4].Type);
			ClassicAssert.AreEqual(ScriptType.Index, scripts[5].Type);
			ExecuteScripts(scripts);
		}

		[Test]
		public void DropIndexWithSchemaTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse(@"CREATE TABLE guest.a(b varchar(25))
GO 

CREATE INDEX IX_a_b ON guest.a(b)");

			ScriptSet scripts=parser.Database.CreateDiffScripts(new Database());
			scripts.Sort();
			ClassicAssert.AreEqual(2, scripts.Count);
			ClassicAssert.AreEqual(ScriptType.Table, scripts[0].Type);
			ClassicAssert.AreEqual(ScriptType.Index, scripts[1].Type);
			ExecuteScripts(scripts);

			ScriptParser currentDatabase=ParseDatabase();
			scripts=parser.Database.CreateDiffScripts(currentDatabase.Database);
			ClassicAssert.AreEqual(0, scripts.Count);

			parser=new ScriptParser();
			parser.Parse(@"CREATE TABLE guest.a(b varchar(25), x int)
GO

CREATE INDEX IX_a_b ON guest.a(b)");

			scripts=parser.Database.CreateDiffScripts(currentDatabase.Database);
			scripts.Sort();
			ClassicAssert.AreEqual(5, scripts.Count);
			ClassicAssert.AreEqual(ScriptType.DropIndex, scripts[0].Type);
			ClassicAssert.AreEqual(ScriptType.TableSaveData, scripts[1].Type);
			ClassicAssert.AreEqual(ScriptType.Table, scripts[2].Type);
			ClassicAssert.AreEqual(ScriptType.Index, scripts[3].Type);
			ClassicAssert.AreEqual(ScriptType.TableRestoreData, scripts[4].Type);
			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			scripts=parser.Database.CreateDiffScripts(currentDatabase.Database);
			ClassicAssert.AreEqual(0, scripts.Count);
		}

		[Test]
		public void FilegroupTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo(a int)");
			startingDatabase.Parse("CREATE INDEX foo_a ON dbo.foo(a)");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE foo(a int)");
			endingDatabase.Parse("CREATE INDEX foo_a ON dbo.foo(a) ON Secondary");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			ClassicAssert.AreEqual(2, scripts.Count);

			ClassicAssert.AreEqual(scripts[0].Type, ScriptType.DropIndex);
			ClassicAssert.AreEqual(@"DROP INDEX [foo_a] ON [dbo].[foo]
", scripts[0].Text);

			ClassicAssert.AreEqual(scripts[1].Type, ScriptType.Index);
			//the actual script should be tested in the parser tests
			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void FulltextTest()
		{
            ScriptParser parser = new ScriptParser();
            parser.Parse("CREATE FULLTEXT CATALOG Fulltext AS DEFAULT");
			parser.Parse(@"CREATE TABLE Searching
(
	SearchingID int NOT NULL,
	SearchText varchar(max),
	LastUpdate timestamp
)");
			parser.Parse("CREATE UNIQUE CLUSTERED INDEX PK_Searching ON dbo.Searching(SearchingID)");
			parser.Parse(@"CREATE FULLTEXT INDEX ON Searching(
	SearchText LANGUAGE English
)
KEY INDEX PK_Searching ON Fulltext
WITH CHANGE_TRACKING AUTO");
			ClassicAssert.AreEqual(1, parser.Database.Tables.Count);
			ClassicAssert.AreEqual(1, parser.Database.Tables[0].Indexes.Count);
			ClassicAssert.AreEqual(1, parser.Database.FulltextIndexes.Count);

			ScriptSet scripts=parser.Database.CreateDiffScripts(new Database());
			scripts.Sort();
			ClassicAssert.AreEqual(4, scripts.Count);
			ClassicAssert.AreEqual(ScriptType.Table, scripts[0].Type);
			ClassicAssert.AreEqual(ScriptType.ClusteredIndex, scripts[1].Type);
			ClassicAssert.AreEqual(ScriptType.FulltextCatalog, scripts[2].Type);
			ClassicAssert.AreEqual(ScriptType.FulltextIndex, scripts[3].Type);

			ExecuteScripts(scripts);

			ScriptParser database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());

            parser = new ScriptParser();
            parser.Parse("CREATE FULLTEXT CATALOG Fulltext AS DEFAULT");
			parser.Parse(@"CREATE TABLE Searching
(
	SearchingID int NOT NULL,
	Foo int,
	SearchText varchar(max),
	LastUpdate timestamp
)");
			parser.Parse("CREATE UNIQUE CLUSTERED INDEX PK_Searching ON dbo.Searching(SearchingID)");
			parser.Parse(@"CREATE FULLTEXT INDEX ON Searching(
	SearchText LANGUAGE English
)
KEY INDEX PK_Searching ON Fulltext
WITH CHANGE_TRACKING AUTO");
			ClassicAssert.AreEqual(1, parser.Database.Tables.Count);
			ClassicAssert.AreEqual(1, parser.Database.Tables[0].Indexes.Count);
			ClassicAssert.AreEqual(1, parser.Database.FulltextIndexes.Count);

			difference=parser.Database.CreateDiffScripts(database.Database);
			difference.Sort();
			ClassicAssert.AreEqual(7, difference.Count);

			ClassicAssert.AreEqual(ScriptType.DropFulltextIndex, difference[0].Type);
			ClassicAssert.AreEqual(ScriptType.DropClusteredIndex, difference[1].Type);
			ClassicAssert.AreEqual(ScriptType.TableSaveData, difference[2].Type);
			ClassicAssert.AreEqual(ScriptType.Table, difference[3].Type);
			ClassicAssert.AreEqual(ScriptType.ClusteredIndex, difference[4].Type);
			ClassicAssert.AreEqual(ScriptType.TableRestoreData, difference[5].Type);
			ClassicAssert.AreEqual(ScriptType.FulltextIndex, difference[6].Type);

			ExecuteScripts(difference);

			database=ParseDatabase();
			difference=parser.Database.CreateDiffScripts(database.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void FulltextOrderingTest()
		{
            ScriptParser parser = new ScriptParser();
            parser.Parse("CREATE FULLTEXT CATALOG Fulltext AS DEFAULT");
			parser.Parse(@"CREATE TABLE Searching
(
	SearchingID int NOT NULL,
	SearchText varchar(max),
	MoreText varchar(max),
	LastUpdate timestamp
)");
			parser.Parse("CREATE UNIQUE CLUSTERED INDEX PK_Searching ON dbo.Searching(SearchingID)");
			parser.Parse(@"CREATE FULLTEXT INDEX ON Searching(
	SearchText,
	MoreText
)
KEY INDEX PK_Searching ON Fulltext
WITH CHANGE_TRACKING AUTO");
			ClassicAssert.AreEqual(1, parser.Database.Tables.Count);
			ClassicAssert.AreEqual(1, parser.Database.Tables[0].Indexes.Count);
			ClassicAssert.AreEqual(1, parser.Database.FulltextIndexes.Count);

			ScriptSet scripts=parser.Database.CreateDiffScripts(new Database());
			scripts.Sort();
			ClassicAssert.AreEqual(4, scripts.Count);
			ClassicAssert.AreEqual(ScriptType.Table, scripts[0].Type);
			ClassicAssert.AreEqual(ScriptType.ClusteredIndex, scripts[1].Type);
			ClassicAssert.AreEqual(ScriptType.FulltextCatalog, scripts[2].Type);
			ClassicAssert.AreEqual(ScriptType.FulltextIndex, scripts[3].Type);

			ExecuteScripts(scripts);

			ScriptParser database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());

			parser=new ScriptParser();
			parser.Parse(@"CREATE TABLE Searching
(
	SearchingID int NOT NULL,
	SearchText varchar(max),
	MoreText varchar(max),
	LastUpdate timestamp
)");
			parser.Parse("CREATE UNIQUE CLUSTERED INDEX PK_Searching ON dbo.Searching(SearchingID)");
			parser.Parse(@"CREATE FULLTEXT INDEX ON Searching(
	MoreText,  --swap the order
	SearchText
)
KEY INDEX PK_Searching ON Fulltext
WITH CHANGE_TRACKING AUTO");
			ClassicAssert.AreEqual(1, parser.Database.Tables.Count);
			ClassicAssert.AreEqual(1, parser.Database.Tables[0].Indexes.Count);
			ClassicAssert.AreEqual(1, parser.Database.FulltextIndexes.Count);

			difference=parser.Database.CreateDiffScripts(database.Database);
			difference.Sort();
			ClassicAssert.AreEqual(0, difference.Count);
		}

		[Test]
		public void FulltextViewTest()
		{
            ScriptParser parser = new ScriptParser();
            parser.Parse("CREATE FULLTEXT CATALOG Fulltext AS DEFAULT");
			parser.Parse(@"CREATE TABLE Searching
(
	SearchingID int PRIMARY KEY,
	SearchText varchar(max)
)");
			parser.Parse(@"CREATE VIEW SearchView
WITH SCHEMABINDING
AS
SELECT
	SearchingID,
	SearchText
FROM dbo.Searching");
			parser.Parse("CREATE UNIQUE CLUSTERED INDEX IX_SearchView ON SearchView(SearchingID)");
			parser.Parse(@"CREATE FULLTEXT INDEX ON SearchView(
	SearchText LANGUAGE English
)
KEY INDEX IX_SearchView ON Fulltext
WITH CHANGE_TRACKING AUTO");
			ClassicAssert.AreEqual(1, parser.Database.Tables.Count);
			ClassicAssert.AreEqual(1, parser.Database.FulltextIndexes.Count);
			ClassicAssert.AreEqual(1, parser.Database.Views.Count);

			ScriptSet scripts=parser.Database.CreateDiffScripts(new Database());
			scripts.Sort();
			ClassicAssert.AreEqual(6, scripts.Count);

			ClassicAssert.AreEqual(ScriptType.Table, scripts[0].Type);
			ClassicAssert.AreEqual(ScriptType.View, scripts[1].Type);
			ClassicAssert.AreEqual(ScriptType.PrimaryKey, scripts[2].Type);
			ClassicAssert.AreEqual(ScriptType.ClusteredIndex, scripts[3].Type);
			ClassicAssert.AreEqual(ScriptType.FulltextCatalog, scripts[4].Type);
			ClassicAssert.AreEqual(ScriptType.FulltextIndex, scripts[5].Type);

			ExecuteScripts(scripts);

			ScriptParser database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());

			parser=new ScriptParser();
			parser.Parse(@"CREATE TABLE Searching
(
	SearchingID int PRIMARY KEY,
	Foo int,
	SearchText varchar(max)
)");
			parser.Parse(@"CREATE VIEW SearchView
WITH SCHEMABINDING
AS
SELECT
	SearchingID,
	SearchText
FROM dbo.Searching");
			parser.Parse("CREATE UNIQUE CLUSTERED INDEX IX_SearchView ON SearchView(SearchingID)");
			parser.Parse(@"CREATE FULLTEXT INDEX ON SearchView(
	SearchText LANGUAGE English
)
KEY INDEX IX_SearchView ON Fulltext
WITH CHANGE_TRACKING AUTO");
			ClassicAssert.AreEqual(1, parser.Database.Tables.Count);
			ClassicAssert.AreEqual(1, parser.Database.FulltextIndexes.Count);
			ClassicAssert.AreEqual(1, parser.Database.Views.Count);

			difference=parser.Database.CreateDiffScripts(database.Database);
			difference.Sort();
			ClassicAssert.AreEqual(11, difference.Count);

			ClassicAssert.AreEqual(ScriptType.DropFulltextIndex, difference[0].Type);
			ClassicAssert.AreEqual(ScriptType.DropPrimaryKey, difference[1].Type);
			ClassicAssert.AreEqual(ScriptType.DropClusteredIndex, difference[2].Type);
			ClassicAssert.AreEqual(ScriptType.DropView, difference[3].Type);
			ClassicAssert.AreEqual(ScriptType.TableSaveData, difference[4].Type);
			ClassicAssert.AreEqual(ScriptType.Table, difference[5].Type);
			ClassicAssert.AreEqual(ScriptType.View, difference[6].Type);
			ClassicAssert.AreEqual(ScriptType.PrimaryKey, difference[7].Type);
			ClassicAssert.AreEqual(ScriptType.ClusteredIndex, difference[8].Type);
			ClassicAssert.AreEqual(ScriptType.TableRestoreData, difference[9].Type);
			ClassicAssert.AreEqual(ScriptType.FulltextIndex, difference[10].Type);

			ExecuteScripts(difference);
			database=ParseDatabase();
			difference=parser.Database.CreateDiffScripts(database.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void IndexNameCompareTest()
		{
			Index a=new Index("a", "foo", false, false);
			Index b=new Index("b", "foo", false, false);

			Difference difference=a.GetDifferences(b, true);
			ClassicAssert.IsNotNull(difference);
			ClassicAssert.AreEqual(1, difference.Messages.Count);
		}

		[Test]
		public void TableNameTest()
		{
			RunOptions.Current.FullyScripted=false;

			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo(a int)");
			startingDatabase.Parse("CREATE INDEX foo_a ON dbo.foo(a)");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE bar(a int)");
			endingDatabase.Parse("CREATE INDEX foo_a ON dbo.bar(a)");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			ClassicAssert.AreEqual(3, scripts.Count);

			ClassicAssert.AreEqual(scripts[0].Type, ScriptType.DropIndex);
			//the actual script should be tested in the parser tests

			ClassicAssert.AreEqual(scripts[1].Type, ScriptType.Table);
			//the actual script should be tested in the parser tests

			ClassicAssert.AreEqual(scripts[2].Type, ScriptType.Index);
			//the actual script should be tested in the parser tests

			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void TableRemovedTest()
        {
            RunOptions.Current.FullyScripted = true;

			ScriptParser currentDatabase=new ScriptParser();
            currentDatabase.Parse("CREATE TABLE foo(a int)");
            currentDatabase.Parse("CREATE INDEX foo_a ON dbo.foo(a)");

			ScriptParser endingDatabase=new ScriptParser();
			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			scripts.Sort();
			ClassicAssert.AreEqual(2, scripts.Count);

            ClassicAssert.AreEqual(scripts[0].Type, ScriptType.DropIndex);
            ClassicAssert.AreEqual(scripts[1].Type, ScriptType.DropTable);
		}

		[Test]
		public void UniqueTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo(a int)");
			startingDatabase.Parse("CREATE INDEX foo_a ON dbo.foo(a)");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE foo(a int)");
			endingDatabase.Parse("CREATE UNIQUE INDEX foo_a ON dbo.foo(a)");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			ClassicAssert.AreEqual(2, scripts.Count);

			ClassicAssert.AreEqual(scripts[0].Type, ScriptType.DropIndex);
			ClassicAssert.AreEqual(@"DROP INDEX [foo_a] ON [dbo].[foo]
", scripts[0].Text);

			ClassicAssert.AreEqual(scripts[1].Type, ScriptType.Index);
			//the actual script should be tested in the parser tests
			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}
	}
}
