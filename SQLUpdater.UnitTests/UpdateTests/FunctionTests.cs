using NUnit.Framework;
using NUnit.Framework.Legacy;
using SQLUpdater.Lib;
using SQLUpdater.Lib.DBTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLUpdater.UnitTests.UpdateTests
{
	[TestFixture]
	public class FunctionTests : BaseUnitTest
	{
		[Test]
		public void BodyChangedTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE Function A (@B int) RETURNS int AS BEGIN RETURN @B END");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE Function A (@B int) RETURNS int AS BEGIN RETURN @B+1 END");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			ClassicAssert.AreEqual(2, scripts.Count);

			ClassicAssert.AreEqual(scripts[0].Type, ScriptType.DropUserDefinedFunction);
			ClassicAssert.AreEqual(@"DROP FUNCTION [dbo].[A]

GO", scripts[0].Text);

			ClassicAssert.AreEqual(scripts[1].Type, ScriptType.UserDefinedFunction);
			//the actual script should be tested in the parser tests
			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ClassicAssert.AreEqual(0, difference.Count);
		}

		[Test]
		public void FunctionNameChangeTest()
		{
			RunOptions.Current.FullyScripted=false;

			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE Function A (@B int) RETURNS int AS BEGIN RETURN @B END");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE Function B (@B int) RETURNS int AS BEGIN RETURN @B END");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			ClassicAssert.AreEqual(1, scripts.Count);
			ClassicAssert.AreEqual(scripts[0].Type, ScriptType.UserDefinedFunction);

			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ClassicAssert.AreEqual(0, difference.Count);
		}

		[Test]
		public void FunctionNameCompareTest()
		{
			Function a=new Function("a", "");
			Function b=new Function("b", "");

			Difference difference=a.GetDifferences(b, true);
			ClassicAssert.IsNotNull(difference);
			ClassicAssert.AreEqual(1, difference.Messages.Count);
		}

		[Test]
		public void GrantTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE Function A (@B int) RETURNS int AS BEGIN RETURN @B END");
			startingDatabase.Parse("GRANT Execute ON A TO public");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE Function A (@B int) RETURNS int AS BEGIN RETURN @B+1 END");
			endingDatabase.Parse("GRANT Execute ON A TO public");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			ClassicAssert.AreEqual(3, scripts.Count);

			ClassicAssert.AreEqual(scripts[0].Type, ScriptType.DropUserDefinedFunction);
			ClassicAssert.AreEqual(@"DROP FUNCTION [dbo].[A]

GO", scripts[0].Text);

			//the actual script should be tested in the parser tests
			ClassicAssert.AreEqual(scripts[1].Type, ScriptType.UserDefinedFunction);
			ClassicAssert.AreEqual(scripts[2].Type, ScriptType.Permission);

			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ClassicAssert.AreEqual(0, difference.Count);
		}
	}
}
