using NUnit.Framework;
using SQLUpdater.Lib;
using SQLUpdater.Lib.DBTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLUpdater.UnitTests
{
	[TestFixture]
	public class ScriptOrderingTests : BaseUnitTest
	{
        [Test]
        public void FalseDependencyTest()
        {
            //This behavior isn't ideal, but we don't want it to crash

			ScriptSet set=new ScriptSet();
			Script a=new Script("CREATE TABLE Foo(Bar int)", "Foo", ScriptType.Table);
			set.Add(a);
			Script b=new Script("CREATE TABLE Bar(Foo varchar(10)", "Bar", ScriptType.Table);
			set.Add(b);

            TestLogger logger = (TestLogger)RunOptions.Current.Logger;
            Assert.IsEmpty(logger.messages);
            set.Sort();

            Assert.AreEqual(1, logger.messages.Count);
            Assert.AreEqual("Warning: [dbo].[Bar] and [dbo].[Foo] both seem to refer to each other", logger.messages[0]);
            Assert.AreEqual(OutputLevel.Differences, logger.levels[0]);
        }

		[Test, ExpectedException(ExpectedException=typeof(ApplicationException),
			ExpectedMessage="Adding duplicate script: [dbo].[Foo] [Unknown]")]
		public void ScriptTwiceTest()
		{
			ScriptSet set=new ScriptSet();
			Script a=new Script("Foo", "Foo", ScriptType.Unknown);
			set.Add(a);
			Script b=new Script("Foo", "Foo", ScriptType.Unknown);
			set.Add(b);
		}

		[Test]
		public void TableFunctionViewTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse(@"CREATE TABLE dbo.foo(
	fooID int identity(0, 1)
)");
			startingDatabase.Parse("CREATE Function dbo.bar (@A int) RETURNS int AS BEGIN RETURN @A END");
			startingDatabase.Parse(@"CREATE VIEW dbo.baz
AS

SELECT FooID, 
	dbo.bar(FooID) 'bonk'
FROM dbo.foo");
			Assert.AreEqual(1, startingDatabase.Database.Functions.Count);
			Assert.AreEqual(1, startingDatabase.Database.Tables.Count);
			Assert.AreEqual(1, startingDatabase.Database.Views.Count);

			//make sure this runs
			ScriptParser currentDatabase=new ScriptParser();
			ScriptSet scripts=startingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

			//make sure it got results
			currentDatabase=ParseDatabase();
			ScriptSet difference=startingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}
	}
}
