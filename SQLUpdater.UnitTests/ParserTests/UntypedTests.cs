using NUnit.Framework;
using SQLUpdater.Lib;
using SQLUpdater.Lib.DBTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SQLUpdater.UnitTests.ParserTests
{
	[TestFixture]
	public class UntypedTests : BaseUnitTest
	{
        [Test]
        public void FulltextCatalogTest()
        {
            ScriptParser parser = new ScriptParser();
            parser.Parse("CREATE FULLTEXT CATALOG FulltextCatalog");
            Assert.AreEqual(1, parser.Database.FulltextCatalogs.Count);

            Script createScript = parser.Database.FulltextCatalogs[0].GenerateCreateScript();
            Assert.AreEqual(@"CREATE FULLTEXT CATALOG [FulltextCatalog]
",
                createScript.Text);
            ExecuteScripts(createScript);

            ScriptParser database = ParseDatabase();
            ScriptSet difference = parser.Database.CreateDiffScripts(database.Database);
            Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
        }

        [Test]
        public void GrantWithPrincipalTest()
        {
            ScriptSet scripts = new ScriptSet();
            scripts.Add(new Script(@"CREATE TABLE dbo.Foo( x int )
GO

GRANT SELECT ON dbo.Foo to PUBLIC as dbo",
                "Foo.sql", ScriptType.Unknown));

            ScriptParser parser = new ScriptParser();
            parser.RetrieveParsableObjects(scripts);
            Assert.AreEqual(0, scripts.Count);
            Assert.AreEqual(1, parser.Database.Tables.Count);

            Assert.AreEqual(0, ((TestLogger)RunOptions.Current.Logger).messages.Count);
        }

		[Test]
		public void TableScriptTest()
		{
			ScriptSet scripts=new ScriptSet();
			scripts.Add(new Script(@"IF EXISTS (SELECT * FROM sysobjects WHERE type = 'U' AND name = 'dbo.Foo')
	BEGIN
		DROP  Table dbo.Foo
	END

GO

CREATE TABLE dbo.Foo
(
	x int
)
GO

GRANT SELECT ON dbo.Foo to PUBLIC",
				"Foo.sql", ScriptType.Unknown));

			ScriptParser parser=new ScriptParser();
			parser.RetrieveParsableObjects(scripts);
			Assert.AreEqual(0, scripts.Count);
			Assert.AreEqual(1, parser.Database.Tables.Count);
		}

        [Test]
        public void UseTest()
        {
            //This needs to have a type, otherwise we don't really invoke the parser
			ScriptSet scripts=new ScriptSet();
			scripts.Add(new Script(@"USE master", "Foo.sql", ScriptType.StoredProc));

			ScriptParser parser=new ScriptParser();
			parser.RetrieveParsableObjects(scripts);
			Assert.AreEqual(0, scripts.Count);
        }

		[Test]
		public void UnparsableTest()
		{
			ScriptSet scripts=new ScriptSet();
			scripts.Add(new Script("SELECT * FROM dbo.Foo", "Foo.sql", ScriptType.Unknown));

			ScriptParser parser=new ScriptParser();
			parser.RetrieveParsableObjects(scripts);
			Assert.AreEqual(1, scripts.Count);
		}
	}
}
