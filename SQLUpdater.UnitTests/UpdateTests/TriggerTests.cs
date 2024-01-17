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
	public class TriggerTests : BaseUnitTest
	{
		[Test]
		public void BodyChangedTest()
		{
			RunOptions.Current.FullyScripted=false;

			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a int)");
			startingDatabase.Parse("CREATE Trigger A ON foo FOR INSERT AS SELECT 1");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE Trigger A ON foo FOR INSERT AS SELECT 2");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			ClassicAssert.AreEqual(2, scripts.Count);

			ClassicAssert.AreEqual(scripts[0].Type, ScriptType.DropTrigger);
			ClassicAssert.AreEqual(@"if exists(
	select 1
	from dbo.sysobjects
	where type = 'TR' AND id = object_id('[dbo].[A]')
)
DROP TRIGGER [dbo].[A]

GO", scripts[0].Text);

			ClassicAssert.AreEqual(scripts[1].Type, ScriptType.Trigger);
			//the actual script should be tested in the parser tests
			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void TriggerNameChangeTest()
		{
			RunOptions.Current.FullyScripted=false;

			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a int)");
			startingDatabase.Parse("CREATE Trigger A ON foo FOR INSERT AS SELECT 1");

			ScriptParser currentDatabase=new ScriptParser();
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE Trigger B ON foo FOR INSERT AS SELECT 1");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			ClassicAssert.AreEqual(1, scripts.Count);
			ClassicAssert.AreEqual(scripts[0].Type, ScriptType.Trigger);

			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void TriggerNameCompareTest()
		{
			Trigger a=new Trigger("a", "", "foo");
			Trigger b=new Trigger("b", "", "foo");

			Difference difference=a.GetDifferences(b, true);
			ClassicAssert.IsNotNull(difference);
			ClassicAssert.AreEqual(1, difference.Messages.Count);
		}
	}
}
