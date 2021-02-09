using NUnit.Framework;
using SQLUpdater.Lib;
using SQLUpdater.Lib.DBTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLUpdater.UnitTests.ParserTests
{
	[TestFixture]
	public class TriggerTests : BaseUnitTest
	{
		[Test]
		public void BasicTriggerTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE foo( a int)");
			parser.Parse("CREATE Trigger A ON foo FOR INSERT AS SELECT 1");
			Assert.AreEqual(1, parser.Database.Triggers.Count);
			Assert.AreEqual("CREATE Trigger A ON foo FOR INSERT AS SELECT 1", parser.Database.Triggers[0].Body);
			Assert.AreEqual((Name)"foo", parser.Database.Triggers[0].ReferencedTable);

			ScriptSet difference=parser.Database.CreateDiffScripts(new Database());
			Assert.AreEqual(2, difference.Count);
			Assert.AreEqual(@"CREATE Trigger A ON foo FOR INSERT AS SELECT 1
GO",
				difference[1].Text);
			ExecuteScripts(difference);

			ScriptParser database=ParseDatabase();
			difference=parser.Database.CreateDiffScripts(database.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void FirstAfterTriggerTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE foo( a int)");
			parser.Parse("CREATE Trigger A ON foo AFTER INSERT AS SELECT 1");
			parser.Parse("EXEC sp_settriggerorder @triggername='A', @order='First', @stmttype='INSERT'");

			ScriptSet createScripts=parser.Database.CreateDiffScripts(new Database());
			Assert.AreEqual(3, createScripts.Count);
			ExecuteScripts(createScripts);

			ScriptParser database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
        }

        [Test]
        public void SchemaTest()
        {
            ScriptParser parser = new ScriptParser();
            parser.Parse("CREATE TABLE guest.foo( a int)");
            parser.Parse("CREATE Trigger guest.A ON guest.foo FOR INSERT AS SELECT 1");
            Assert.AreEqual(1, parser.Database.Triggers.Count);
            Assert.AreEqual("CREATE Trigger guest.A ON guest.foo FOR INSERT AS SELECT 1", parser.Database.Triggers[0].Body);
            Assert.AreEqual((Name)"guest.foo", parser.Database.Triggers[0].ReferencedTable);

            ScriptSet difference = parser.Database.CreateDiffScripts(new Database());
            Assert.AreEqual(2, difference.Count);
            Assert.AreEqual(@"CREATE Trigger guest.A ON guest.foo FOR INSERT AS SELECT 1
GO",
                difference[1].Text);
            ExecuteScripts(difference);

            ScriptParser database = ParseDatabase();
            difference = parser.Database.CreateDiffScripts(database.Database);
            Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
        }
	}
}
