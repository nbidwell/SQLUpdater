using NUnit.Framework;
using NUnit.Framework.Legacy;
using SQLUpdater.Lib;
using SQLUpdater.Lib.DBTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLUpdater.UnitTests.ParserTests
{
	[TestFixture]
	public class ProcedureTests : BaseUnitTest
	{
		[Test]
		public void BasicProcedureTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE foo( a int)");
			parser.Parse("CREATE PROCEDURE A (@B int) AS SELECT * FROM foo");
			ClassicAssert.AreEqual(1, parser.Database.Procedures.Count);
			ClassicAssert.AreEqual("CREATE PROCEDURE A (@B int) AS SELECT * FROM foo", parser.Database.Procedures[0].Body);

			Script createScript=parser.Database.Procedures[0].GenerateCreateScript();
			ClassicAssert.AreEqual(@"CREATE PROCEDURE A (@B int) AS SELECT * FROM foo
GO",
				createScript.Text);
			ExecuteScripts(parser.Database.Tables[0].GenerateCreateScript());
			ExecuteScripts(createScript);

			ScriptParser database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void GrantTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE foo( a int)");
            parser.Parse("CREATE PROCEDURE A (@B int) AS SELECT * FROM foo GO GRANT Execute ON A TO public");

			ScriptSet difference=parser.Database.CreateDiffScripts(new Database());
			ClassicAssert.AreEqual(3, difference.Count);

			ClassicAssert.AreEqual(difference[0].Type, ScriptType.Table);
			ClassicAssert.AreEqual(difference[1].Type, ScriptType.StoredProc);

			ClassicAssert.AreEqual(difference[2].Type, ScriptType.Permission);
            ClassicAssert.AreEqual(@"GRANT Execute ON [dbo].[A] TO [public]

GO

",
				difference[2].Text);

			ExecuteScripts(difference);

			ScriptParser database=ParseDatabase();
			difference=parser.Database.CreateDiffScripts(database.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}
	}
}
