using NUnit.Framework;
using NUnit.Framework.Legacy;
using SQLUpdater.Lib;
using SQLUpdater.Lib.DBTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SQLUpdater.UnitTests.ParserTests
{
    [TestFixture]
    public class TypeTests : BaseUnitTest
    {
        [Test]
        public void TableGrantTest()
        {
            ScriptParser parser = new ScriptParser();
            parser.Parse("CREATE TYPE foo AS TABLE ( a int )");
            parser.Parse("GRANT CONTROL ON TYPE::dbo.foo TO public");
            ClassicAssert.AreEqual(1, parser.Database.Tables.Count);

            ScriptSet difference = parser.Database.CreateDiffScripts(new Database());
            ClassicAssert.AreEqual(2, difference.Count);

            ClassicAssert.AreEqual(difference[0].Type, ScriptType.TableType);
            ClassicAssert.AreEqual(@"CREATE TYPE [dbo].[foo] AS TABLE(
	[a] [int] NULL
)

GO",
                difference[0].Text);

            ClassicAssert.AreEqual(difference[1].Type, ScriptType.Permission);
            ClassicAssert.AreEqual(@"GRANT CONTROL ON TYPE::[dbo].[foo] TO [public]

GO

",
                difference[1].Text);

            ExecuteScripts(difference);

            ScriptParser database = ParseDatabase();
            difference = parser.Database.CreateDiffScripts(database.Database);
            ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
        }

        [Test]
        public void TableTypeTest()
        {
            ScriptParser parser = new ScriptParser();
            parser.Parse("CREATE TYPE foo AS TABLE ( a int )");
            ClassicAssert.AreEqual(1, parser.Database.Tables.Count);

            Script createScript = parser.Database.Tables[0].GenerateCreateScript();
            ClassicAssert.AreEqual(@"CREATE TYPE [dbo].[foo] AS TABLE(
	[a] [int] NULL
)

GO",
                createScript.Text);
            ExecuteScripts(createScript);

            ScriptParser database = ParseDatabase();
            ScriptSet difference = parser.Database.CreateDiffScripts(database.Database);
            ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
        }
    }
}
