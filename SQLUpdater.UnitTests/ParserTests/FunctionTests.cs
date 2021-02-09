using NUnit.Framework;
using SQLUpdater.Lib;
using SQLUpdater.Lib.DBTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLUpdater.UnitTests.ParserTests
{
	[TestFixture]
	public class FunctionTests : BaseUnitTest
	{
		[Test]
		public void BasicFunctionTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE Function A (@B int) RETURNS int AS BEGIN RETURN @B END");
			Assert.AreEqual(1, parser.Database.Functions.Count);
			Assert.AreEqual("CREATE Function A (@B int) RETURNS int AS BEGIN RETURN @B END", parser.Database.Functions[0].Body);

			Script createScript=parser.Database.Functions[0].GenerateCreateScript();
			Assert.AreEqual(@"CREATE Function A (@B int) RETURNS int AS BEGIN RETURN @B END
GO",
				createScript.Text);
			ExecuteScripts(createScript);

			ScriptParser database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void GrantTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE Function A (@B int) RETURNS int AS BEGIN RETURN @B END");
			parser.Parse("GRANT Execute ON A TO public");

			ScriptSet difference=parser.Database.CreateDiffScripts(new Database());
			Assert.AreEqual(2, difference.Count);

			Assert.AreEqual(difference[0].Type, ScriptType.UserDefinedFunction);

			Assert.AreEqual(difference[1].Type, ScriptType.Permission);
            Assert.AreEqual(@"GRANT Execute ON [dbo].[A] TO [public]

GO

",
				difference[1].Text);

			ExecuteScripts(difference);

			ScriptParser database=ParseDatabase();
			difference=parser.Database.CreateDiffScripts(database.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}
	}
}
