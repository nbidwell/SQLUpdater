using NUnit.Framework;
using SQLUpdater.Lib;
using SQLUpdater.Lib.DBTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLUpdater.UnitTests.ParserTests
{
	[TestFixture]
	public class ViewTests : BaseUnitTest
	{
		[Test]
		public void BasicViewTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE foo( a int)");
			parser.Parse("CREATE VIEW A AS SELECT * FROM foo");
			Assert.AreEqual(1, parser.Database.Views.Count);
			Assert.AreEqual("CREATE VIEW A AS SELECT * FROM foo", parser.Database.Views[0].Body);

			Script createScript=parser.Database.Views[0].GenerateCreateScript();
			Assert.AreEqual(@"CREATE VIEW A AS SELECT * FROM foo
GO",
				createScript.Text);
			ExecuteScripts(parser.Database.Tables[0].GenerateCreateScript());
			ExecuteScripts(createScript);

			ScriptParser database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void ParenthesesTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE foo( a int, b char(1))");
			parser.Parse("CREATE VIEW A AS SELECT count(a) count, b FROM foo GROUP BY ((b))");

			ScriptSet scripts=parser.Database.CreateDiffScripts(new Database());
			Assert.AreEqual(2, scripts.Count);
			Assert.AreEqual(@"CREATE VIEW A AS SELECT count(a) count, b FROM foo GROUP BY ((b))
GO",
				scripts[1].Text);
			ExecuteScripts(scripts);

			ScriptParser database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}
	}
}
