using NUnit.Framework;
using SQLUpdater.Lib;
using SQLUpdater.Lib.DBTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLUpdater.UnitTests.ParserTests
{
	[TestFixture]
	public class IndexTests : BaseUnitTest
	{
		[Test]
		public void BasicTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE foo(a int)");
			parser.Parse("CREATE INDEX foo_a ON dbo.foo(a)");
			Assert.AreEqual(1, parser.Database.Tables.Count);
			Assert.AreEqual(1, parser.Database.Tables[0].Indexes.Count);

			ExecuteScripts(parser.Database.Tables[0].GenerateCreateScript());
			Script createScript=parser.Database.Tables[0].Indexes[0].GenerateCreateScript();
			Assert.AreEqual(@"CREATE INDEX [foo_a]
ON [dbo].[foo](
	[a] ASC
)
",
				createScript.Text);
			ExecuteScripts(createScript);

			ScriptParser database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void DisabledTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE foo(a int)");
			parser.Parse("CREATE INDEX foo_a ON dbo.foo(a)");
            parser.Parse("ALTER INDEX foo_a ON dbo.foo DISABLE");

            ScriptSet scripts = parser.Database.CreateDiffScripts(new Database());
            scripts.Sort();
            Assert.AreEqual(2, scripts.Count);

            Assert.AreEqual(ScriptType.Table, scripts[0].Type);
            Assert.AreEqual(@"CREATE TABLE [dbo].[foo](
	[a] [int] NULL
)

GO", scripts[0].Text);

            Assert.AreEqual(ScriptType.Index, scripts[1].Type);
            Assert.AreEqual(@"CREATE INDEX [foo_a]
ON [dbo].[foo](
	[a] ASC
)

ALTER INDEX [foo_a] ON [dbo].[foo] DISABLE
", scripts[1].Text);

            ExecuteScripts(scripts);

            ScriptParser database = ParseDatabase();
            ScriptSet difference = parser.Database.CreateDiffScripts(database.Database);
            Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void FilegroupTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE foo(a int)");
			parser.Parse("CREATE INDEX foo_a ON dbo.foo(a) ON Primary");
			Assert.AreEqual(1, parser.Database.Tables.Count);
			Assert.AreEqual(1, parser.Database.Tables[0].Indexes.Count);

			ExecuteScripts(parser.Database.Tables[0].GenerateCreateScript());
			Script createScript=parser.Database.Tables[0].Indexes[0].GenerateCreateScript();
			Assert.AreEqual(@"CREATE INDEX [foo_a]
ON [dbo].[foo](
	[a] ASC
)
ON [Primary]
",
				createScript.Text);
			ExecuteScripts(createScript);

			ScriptParser database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void ForeignKeyTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE foo(a int PRIMARY KEY)");
			parser.Parse("CREATE TABLE bar(a int)");
			parser.Parse(@"ALTER TABLE bar WITH CHECK
ADD CONSTRAINT FK_Bar_Foo_a
FOREIGN KEY( a )
REFERENCES foo(a)");
			Assert.AreEqual(2, parser.Database.Tables.Count);
            Assert.AreEqual(2, parser.Database.Constraints.Count);

            ScriptSet scripts = parser.Database.CreateDiffScripts(new Database());
            scripts.Sort();
            Assert.AreEqual(4, scripts.Count);
            Assert.AreEqual(@"CREATE TABLE [dbo].[bar](
	[a] [int] NULL
)

GO", scripts[0].Text);
            Assert.AreEqual(@"CREATE TABLE [dbo].[foo](
	[a] [int] NOT NULL
)

GO", scripts[1].Text);
            Assert.AreEqual(@"ALTER TABLE [dbo].[foo]
ADD CONSTRAINT [PK_foo]
PRIMARY KEY(
	[a]
)", scripts[2].Text);
            Assert.AreEqual(@"ALTER TABLE [dbo].[bar]
WITH CHECK
ADD CONSTRAINT [FK_Bar_Foo_a]
FOREIGN KEY(
	[a]
)
REFERENCES [dbo].[foo](
	[a]
)", scripts[3].Text);

			ExecuteScripts(scripts);

			ScriptParser database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

        [Test]
        public void FilteredUniqueTest()
        {
            ScriptParser parser = new ScriptParser();
            parser.Parse("CREATE TABLE foo(a int)");
            parser.Parse("CREATE UNIQUE INDEX foo_a ON dbo.foo(a) WHERE a IS NOT NULL");
            Assert.AreEqual(1, parser.Database.Tables.Count);
            Assert.AreEqual(1, parser.Database.Tables[0].Indexes.Count);

            ExecuteScripts(parser.Database.Tables[0].GenerateCreateScript());
            Script createScript = parser.Database.Tables[0].Indexes[0].GenerateCreateScript();
            Assert.AreEqual(@"CREATE UNIQUE INDEX [foo_a]
ON [dbo].[foo](
	[a] ASC
)
WHERE a IS NOT NULL
",
                createScript.Text);
            ExecuteScripts(createScript);

            ScriptParser database = ParseDatabase();
            ScriptSet difference = parser.Database.CreateDiffScripts(database.Database);
            Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
        }

		[Test]
		public void FillfactorTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE foo(a int)");
			parser.Parse("CREATE INDEX foo_a ON dbo.foo(a) WITH FILLFACTOR=95");
			Assert.AreEqual(1, parser.Database.Tables.Count);
			Assert.AreEqual(1, parser.Database.Tables[0].Indexes.Count);

			ExecuteScripts(parser.Database.Tables[0].GenerateCreateScript());
			Script createScript=parser.Database.Tables[0].Indexes[0].GenerateCreateScript();
			Assert.AreEqual(@"CREATE INDEX [foo_a]
ON [dbo].[foo](
	[a] ASC
)
WITH FILLFACTOR = 95
",
				createScript.Text);
			ExecuteScripts(createScript);

			ScriptParser database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
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
			Assert.AreEqual(1, parser.Database.Tables.Count);
			Assert.AreEqual(1, parser.Database.Tables[0].Indexes.Count);
			Assert.AreEqual(1, parser.Database.FulltextIndexes.Count);

			ScriptSet scripts=parser.Database.CreateDiffScripts(new Database());
			scripts.Sort();
			Assert.AreEqual(4, scripts.Count);

			Assert.AreEqual(ScriptType.Table, scripts[0].Type);
			Assert.AreEqual(@"CREATE TABLE [dbo].[Searching](
	[SearchingID] [int] NOT NULL,
	[SearchText] [varchar](max) NULL,
	[LastUpdate] [timestamp] NULL
)

GO", scripts[0].Text);

			Assert.AreEqual(ScriptType.ClusteredIndex, scripts[1].Type);
			Assert.AreEqual(@"CREATE UNIQUE CLUSTERED INDEX [PK_Searching]
ON [dbo].[Searching](
	[SearchingID] ASC
)
", scripts[1].Text);

			Assert.AreEqual(ScriptType.FulltextCatalog, scripts[2].Type);

			Assert.AreEqual(ScriptType.FulltextIndex, scripts[3].Type);
			Assert.AreEqual(@"CREATE FULLTEXT INDEX ON [dbo].[Searching](
	[SearchText] LANGUAGE [English]
)
KEY INDEX [PK_Searching]
ON ([Fulltext])
WITH (CHANGE_TRACKING AUTO)", scripts[3].Text);

			ExecuteScripts(scripts);

			ScriptParser database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void FulltextNoCatalogTest()
		{
			ScriptParser parser=new ScriptParser();
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
KEY INDEX PK_Searching
WITH CHANGE_TRACKING AUTO");
			Assert.AreEqual(1, parser.Database.Tables.Count);
			Assert.AreEqual(1, parser.Database.Tables[0].Indexes.Count);
			Assert.AreEqual(1, parser.Database.FulltextIndexes.Count);

			ScriptSet scripts=parser.Database.CreateDiffScripts(new Database());
			scripts.Sort();
			Assert.AreEqual(4, scripts.Count);
			Assert.AreEqual(ScriptType.Table, scripts[0].Type);
			Assert.AreEqual(ScriptType.ClusteredIndex, scripts[1].Type);
            Assert.AreEqual(ScriptType.FulltextCatalog, scripts[2].Type);
            Assert.AreEqual(ScriptType.FulltextIndex, scripts[3].Type);

			ExecuteScripts(scripts);

			ScriptParser database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void FulltextPermissionTest()
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
			parser.Parse("GRANT SELECT ON Searching TO PUBLIC");
			Assert.AreEqual(1, parser.Database.Tables.Count);
			Assert.AreEqual(1, parser.Database.Tables[0].Indexes.Count);
			Assert.AreEqual(1, parser.Database.FulltextIndexes.Count);

			ScriptSet scripts=parser.Database.CreateDiffScripts(new Database());
			scripts.Sort();
			Assert.AreEqual(5, scripts.Count);

			Assert.AreEqual(ScriptType.Table, scripts[0].Type);
			Assert.AreEqual(@"CREATE TABLE [dbo].[Searching](
	[SearchingID] [int] NOT NULL,
	[SearchText] [varchar](max) NULL,
	[LastUpdate] [timestamp] NULL
)

GO", scripts[0].Text);

			Assert.AreEqual(ScriptType.ClusteredIndex, scripts[1].Type);
			Assert.AreEqual(@"CREATE UNIQUE CLUSTERED INDEX [PK_Searching]
ON [dbo].[Searching](
	[SearchingID] ASC
)
", scripts[1].Text);

			Assert.AreEqual(ScriptType.FulltextCatalog, scripts[2].Type);

			Assert.AreEqual(ScriptType.FulltextIndex, scripts[3].Type);
			Assert.AreEqual(@"CREATE FULLTEXT INDEX ON [dbo].[Searching](
	[SearchText] LANGUAGE [English]
)
KEY INDEX [PK_Searching]
ON ([Fulltext])
WITH (CHANGE_TRACKING AUTO)", scripts[3].Text);

			Assert.AreEqual(ScriptType.Permission, scripts[4].Type);
			Assert.AreEqual(@"GRANT SELECT ON [dbo].[Searching] TO [PUBLIC]

GO

", scripts[4].Text);

			ExecuteScripts(scripts);

			ScriptParser database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
        }

        [Test]
        public void FulltextStoplistTest()
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
WITH (CHANGE_TRACKING = AUTO, STOPLIST = OFF)");
            Assert.AreEqual(1, parser.Database.Tables.Count);
            Assert.AreEqual(1, parser.Database.Tables[0].Indexes.Count);
            Assert.AreEqual(1, parser.Database.FulltextIndexes.Count);

            ScriptSet scripts = parser.Database.CreateDiffScripts(new Database());
            scripts.Sort();
            Assert.AreEqual(4, scripts.Count);

            Assert.AreEqual(ScriptType.Table, scripts[0].Type);
            Assert.AreEqual(@"CREATE TABLE [dbo].[Searching](
	[SearchingID] [int] NOT NULL,
	[SearchText] [varchar](max) NULL,
	[LastUpdate] [timestamp] NULL
)

GO", scripts[0].Text);

            Assert.AreEqual(ScriptType.ClusteredIndex, scripts[1].Type);
            Assert.AreEqual(@"CREATE UNIQUE CLUSTERED INDEX [PK_Searching]
ON [dbo].[Searching](
	[SearchingID] ASC
)
", scripts[1].Text);

            Assert.AreEqual(ScriptType.FulltextCatalog, scripts[2].Type);

            Assert.AreEqual(ScriptType.FulltextIndex, scripts[3].Type);
            Assert.AreEqual(@"CREATE FULLTEXT INDEX ON [dbo].[Searching](
	[SearchText] LANGUAGE [English]
)
KEY INDEX [PK_Searching]
ON ([Fulltext])
WITH (CHANGE_TRACKING AUTO, STOPLIST OFF)", scripts[3].Text);

            ExecuteScripts(scripts);

            ScriptParser database = ParseDatabase();
            ScriptSet difference = parser.Database.CreateDiffScripts(database.Database);
            Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
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
			Assert.AreEqual(1, parser.Database.Tables.Count);
			Assert.AreEqual(1, parser.Database.FulltextIndexes.Count);
			Assert.AreEqual(1, parser.Database.Views.Count);

			ScriptSet scripts=parser.Database.CreateDiffScripts(new Database());
			scripts.Sort();
			Assert.AreEqual(6, scripts.Count);

			Assert.AreEqual(ScriptType.Table, scripts[0].Type);
			Assert.AreEqual(@"CREATE TABLE [dbo].[Searching](
	[SearchingID] [int] NOT NULL,
	[SearchText] [varchar](max) NULL
)

GO", scripts[0].Text);

			Assert.AreEqual(ScriptType.View, scripts[1].Type);
			Assert.AreEqual(@"CREATE VIEW SearchView
WITH SCHEMABINDING
AS
SELECT
	SearchingID,
	SearchText
FROM dbo.Searching
GO", scripts[1].Text);

			Assert.AreEqual(ScriptType.PrimaryKey, scripts[2].Type);
			Assert.AreEqual(@"ALTER TABLE [dbo].[Searching]
ADD CONSTRAINT [PK_Searching]
PRIMARY KEY(
	[SearchingID]
)", scripts[2].Text);

			Assert.AreEqual(ScriptType.ClusteredIndex, scripts[3].Type);
			Assert.AreEqual(@"CREATE UNIQUE CLUSTERED INDEX [IX_SearchView]
ON [dbo].[SearchView](
	[SearchingID] ASC
)
", scripts[3].Text);

			Assert.AreEqual(ScriptType.FulltextCatalog, scripts[4].Type);

			Assert.AreEqual(ScriptType.FulltextIndex, scripts[5].Type);
			Assert.AreEqual(@"CREATE FULLTEXT INDEX ON [dbo].[SearchView](
	[SearchText] LANGUAGE [English]
)
KEY INDEX [IX_SearchView]
ON ([Fulltext])
WITH (CHANGE_TRACKING AUTO)", scripts[5].Text);

			ExecuteScripts(scripts);

			ScriptParser database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

        [Test]
        public void IncludeTest()
        {
            ScriptParser parser = new ScriptParser();
            parser.Parse("CREATE TABLE foo(a int, b int)");
            parser.Parse("CREATE INDEX foo_a ON dbo.foo(a) INCLUDE(b)");
            Assert.AreEqual(1, parser.Database.Tables.Count);
            Assert.AreEqual(1, parser.Database.Tables[0].Indexes.Count);

            ExecuteScripts(parser.Database.Tables[0].GenerateCreateScript());
            Script createScript = parser.Database.Tables[0].Indexes[0].GenerateCreateScript();
            Assert.AreEqual(@"CREATE INDEX [foo_a]
ON [dbo].[foo](
	[a] ASC
)
INCLUDE(
	[b]
)
",
                createScript.Text);
            ExecuteScripts(createScript);

            ScriptParser database = ParseDatabase();
            ScriptSet difference = parser.Database.CreateDiffScripts(database.Database);
            Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
        }

		[Test]
		public void NameOverlapTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE foo(a int)");
			parser.Parse("CREATE TABLE bar(a int)");
			parser.Parse("CREATE INDEX a ON dbo.foo(a)");
			parser.Parse("CREATE INDEX a ON dbo.bar(a)");
			Assert.AreEqual(2, parser.Database.Tables.Count);
			Assert.AreEqual(1, parser.Database.Tables[0].Indexes.Count);
			Assert.AreEqual(1, parser.Database.Tables[1].Indexes.Count);

			ScriptSet scripts=parser.Database.CreateDiffScripts(new Database());
			Assert.AreEqual(4, scripts.Count);
			scripts.Sort();
			Assert.AreEqual(ScriptType.Table, scripts[0].Type);
			Assert.AreEqual(ScriptType.Table, scripts[1].Type);
			Assert.AreEqual(ScriptType.Index, scripts[2].Type);
			Assert.AreEqual(ScriptType.Index, scripts[3].Type);

			ExecuteScripts(scripts);

			ScriptParser database=ParseDatabase();
			Assert.AreEqual(2, database.Database.Tables.Count);
			Assert.AreEqual(1, database.Database.Tables[0].Indexes.Count);
			Assert.AreEqual(1, database.Database.Tables[1].Indexes.Count);
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void UniqueClusteredTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE foo(a int)");
			parser.Parse("CREATE UNIQUE CLUSTERED INDEX foo_a ON dbo.foo(a)");
			Assert.AreEqual(1, parser.Database.Tables.Count);
			Assert.AreEqual(1, parser.Database.Tables[0].Indexes.Count);

			ExecuteScripts(parser.Database.Tables[0].GenerateCreateScript());
			Script createScript=parser.Database.Tables[0].Indexes[0].GenerateCreateScript();
			Assert.AreEqual(@"CREATE UNIQUE CLUSTERED INDEX [foo_a]
ON [dbo].[foo](
	[a] ASC
)
",
				createScript.Text);
			ExecuteScripts(createScript);

			ScriptParser database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void ViewTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE foo(a int)");
			parser.Parse("CREATE VIEW bar WITH SCHEMABINDING AS SELECT a FROM dbo.foo"); 
			parser.Parse("CREATE UNIQUE CLUSTERED INDEX bar_a ON dbo.bar(a)");
			Assert.AreEqual(1, parser.Database.Views.Count);
			Assert.AreEqual(1, parser.Database.Views[0].Indexes.Count);

			ScriptParser database=ParseDatabase();
			ScriptSet scripts=parser.Database.CreateDiffScripts(database.Database);
			Assert.AreEqual(3, scripts.Count);
			Assert.AreEqual(@"CREATE UNIQUE CLUSTERED INDEX [bar_a]
ON [dbo].[bar](
	[a] ASC
)
",
				scripts[2].Text);
			ExecuteScripts(scripts);

			database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}
	}
}
