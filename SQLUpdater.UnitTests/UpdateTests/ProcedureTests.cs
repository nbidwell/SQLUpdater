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
	public class ProcedureTests : BaseUnitTest
	{
		[Test]
		public void BodyChangedTest()
		{
			RunOptions.Current.FullyScripted=false;

			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a int)");
			startingDatabase.Parse("CREATE PROCEDURE A (@B int) AS SELECT * FROM foo");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE PROCEDURE A (@B int) AS SELECT * FROM foo WHERE a=@B");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			ClassicAssert.AreEqual(2, scripts.Count);

			ClassicAssert.AreEqual(scripts[0].Type, ScriptType.DropStoredProc);
			ClassicAssert.AreEqual(@"DROP PROCEDURE [dbo].[A]

GO", scripts[0].Text);

			ClassicAssert.AreEqual(scripts[1].Type, ScriptType.StoredProc);
			//the actual script should be tested in the parser tests
			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void ProcedureNameChangeTest()
		{
			RunOptions.Current.FullyScripted=false;

			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a int)");
			startingDatabase.Parse("CREATE PROCEDURE A (@B int) AS SELECT * FROM foo");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE PROCEDURE B (@B int) AS SELECT * FROM foo");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			ClassicAssert.AreEqual(1, scripts.Count);
			ClassicAssert.AreEqual(scripts[0].Type, ScriptType.StoredProc);

			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void ProcedureNameCompareTest()
		{
			Procedure a=new Procedure("a", "");
			Procedure b=new Procedure("b", "");

			Difference difference=a.GetDifferences(b, true);
			ClassicAssert.IsNotNull(difference);
			ClassicAssert.AreEqual(1, difference.Messages.Count);
		}
	}
}
