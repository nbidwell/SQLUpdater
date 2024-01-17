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
	public class ViewTests : BaseUnitTest
	{
		[Test]
		public void BodyChangedTest()
		{
			RunOptions.Current.FullyScripted=false;

			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a int)");
			startingDatabase.Parse("CREATE View A AS SELECT * FROM foo");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE View A AS SELECT * FROM foo WHERE a=1");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			ClassicAssert.AreEqual(2, scripts.Count);

			ClassicAssert.AreEqual(scripts[0].Type, ScriptType.DropView);
			ClassicAssert.AreEqual(@"if exists(
	select 1
	from dbo.sysobjects
	where type = 'V' AND id = object_id('[dbo].[A]')
)
DROP VIEW [dbo].[A]

GO", scripts[0].Text);

			ClassicAssert.AreEqual(scripts[1].Type, ScriptType.View);
			//the actual script should be tested in the parser tests
			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void ViewNameChangeTest()
		{
			RunOptions.Current.FullyScripted=false;

			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a int)");
			startingDatabase.Parse("CREATE View A AS SELECT * FROM foo");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE View B AS SELECT * FROM foo");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			ClassicAssert.AreEqual(1, scripts.Count);
			ClassicAssert.AreEqual(scripts[0].Type, ScriptType.View);

			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void ViewNameCompareTest()
		{
			View a=new View("a", "");
			View b=new View("b", "");

			Difference difference=a.GetDifferences(b, true);
			ClassicAssert.IsNotNull(difference);
			ClassicAssert.AreEqual(1, difference.Messages.Count);
		}
	}
}
