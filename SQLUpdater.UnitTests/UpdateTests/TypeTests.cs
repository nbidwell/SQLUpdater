using NUnit.Framework;
using NUnit.Framework.Legacy;
using SQLUpdater.Lib;
using SQLUpdater.Lib.DBTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SQLUpdater.UnitTests.UpdateTests
{
    [TestFixture]
    public class TypeTests : BaseUnitTest
    {
        [Test]
        public void TableTypeTest()
        {
            ScriptParser startingDatabase = new ScriptParser();
            startingDatabase.Parse("CREATE TYPE foo AS TABLE( a int)");

            ScriptParser currentDatabase = new ScriptParser();
            ExecuteScripts(startingDatabase.Database.CreateDiffScripts(currentDatabase.Database));

            ScriptParser endingDatabase = new ScriptParser();
            endingDatabase.Parse("CREATE TYPE foo AS TABLE( a int, b int)");

            ScriptSet scripts = endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
            scripts.Sort();
            ClassicAssert.AreEqual(2, scripts.Count);

            ClassicAssert.AreEqual(scripts[0].Type, ScriptType.DropTableType);
            ClassicAssert.AreEqual("DROP TYPE [dbo].[foo]", scripts[0].Text);

            ClassicAssert.AreEqual(scripts[1].Type, ScriptType.TableType);
            //the actual script should be tested in the parser tests

            ExecuteScripts(scripts);

            currentDatabase = ParseDatabase();
            ScriptSet difference = endingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
            ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
        }
    }
}
