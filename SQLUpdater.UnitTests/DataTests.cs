using NUnit.Framework;
using NUnit.Framework.Legacy;
using SQLUpdater.Lib;
using SQLUpdater.Lib.DBTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLUpdater.UnitTests
{
	[TestFixture]
	public class DataTests : BaseUnitTest
    {
        [Test]
        public void DataTypesTest()
        {
            ScriptParser parser = new ScriptParser();
            parser.Parse(@"CREATE TABLE a(
    b bigint,
    c bit,
    d char(1),
    e datetime,
    f decimal,
    g float,
    h int,
    i money,
    j ntext,
    k nvarchar(max),
    l smalldatetime,
    m smallint,
    n text,
    o tinyint,
    p varchar(max)
)");
            parser.Parse(@"INSERT INTO a(b, c, d, e, f, g, h, i, j, k, l, m, n, o, p) VALUES(
    3333333333,
    1,
    'a',
    '2000-01-02 03:04:05.600',
    3,
    3.14,
    42,
    3.14,
    'lorem ipsum',
    'lorem ipsum',
    '1/2/2000',
    42,
    'lorem ipsum',
    42,
    'lorem ipsum'
)");
            ScriptSet initScripts = parser.Database.CreateDiffScripts(new Database());

            initScripts.Sort();
            ClassicAssert.AreEqual(2, initScripts.Count);
            ClassicAssert.AreEqual(ScriptType.Table, initScripts[0].Type);
            ClassicAssert.AreEqual(ScriptType.TableData, initScripts[1].Type);
            ExecuteScripts(initScripts);

            ScriptParser currentDatabase = ParseDatabase();
            GetData(currentDatabase.Database, parser.Database.GetTablesWithData());
            ScriptSet difference = parser.Database.CreateDiffScripts(currentDatabase.Database);
            ClassicAssert.AreEqual(0, difference.Count);
        }

		[Test]
		public void BasicDataTest()
		{
			ScriptSet initScripts=new ScriptSet();
			initScripts.Add(new Script("CREATE TABLE foo( a varchar(15))", "Foo", ScriptType.Table));
			initScripts.Add(new Script(@"INSERT INTO foo(a) VALUES('X')
INSERT INTO foo(a) VALUES('A')", "FooData", ScriptType.TableData));
			ExecuteScripts(initScripts);

			ScriptSet updateScripts=new ScriptSet();
			updateScripts.Add(new Script("CREATE TABLE foo( a varchar(15))", "Foo", ScriptType.Table));
			updateScripts.Add(new Script(
				@"INSERT INTO foo(a) VALUES('X')
INSERT INTO foo(a) VALUES('Y')
INSERT INTO dbo.foo(a) VALUES('Z')
insert into dbo.foo(a) values('yabba zabba')", "FooData", ScriptType.TableData));

			ScriptParser parsedScripts=new ScriptParser();
			parsedScripts.RetrieveParsableObjects(updateScripts);
			ClassicAssert.AreEqual(0, updateScripts.Count);

			ScriptParser parsedDatabase=ParseDatabase();
			GetData(parsedDatabase.Database, parsedScripts.Database.GetTablesWithData());

			updateScripts.Add(parsedScripts.Database.CreateDiffScripts(parsedDatabase.Database));
			ClassicAssert.AreEqual(2, updateScripts.Count);
			updateScripts.Sort();

			ClassicAssert.AreEqual(@"DELETE FROM [dbo].[foo]
WHERE
	[a] = 'A'

", updateScripts[0].Text);
			ClassicAssert.AreEqual(ScriptType.TableRemoveData, updateScripts[0].Type);

			ClassicAssert.AreEqual(@"INSERT INTO [dbo].[foo]([a])
VALUES('Y')

INSERT INTO [dbo].[foo]([a])
VALUES('Z')

INSERT INTO [dbo].[foo]([a])
VALUES('yabba zabba')

", updateScripts[1].Text);
			ClassicAssert.AreEqual(ScriptType.TableData, updateScripts[1].Type);

			ExecuteScripts(updateScripts);

		}

		[Test]
		public void DateTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE a( b int primary key, c datetime)");
			parser.Parse("INSERT INTO a(b, c) VALUES(1, '2000-01-02 03:04:05.600')");
			ScriptSet initScripts=parser.Database.CreateDiffScripts(new Database());

			initScripts.Sort();
			ClassicAssert.AreEqual(3, initScripts.Count);
			ClassicAssert.AreEqual(ScriptType.Table, initScripts[0].Type);
			ClassicAssert.AreEqual(ScriptType.PrimaryKey, initScripts[1].Type);
			ClassicAssert.AreEqual(ScriptType.TableData, initScripts[2].Type);
			ClassicAssert.AreEqual(@"INSERT INTO [dbo].[a]([b], [c])
VALUES(1, '2000-01-02 03:04:05.600')

", initScripts[2].Text);
			ExecuteScripts(initScripts);

			ScriptParser currentDatabase=ParseDatabase();
			GetData(currentDatabase.Database, parser.Database.GetTablesWithData());
			ScriptSet difference=parser.Database.CreateDiffScripts(currentDatabase.Database);
			ClassicAssert.AreEqual(0, difference.Count);

			ScriptParser updated=new ScriptParser();
			updated.Parse("CREATE TABLE a( b int primary key, c datetime)");
			updated.Parse("INSERT INTO a(b, c) VALUES(1, '2000-01-02')");

			difference=updated.Database.CreateDiffScripts(currentDatabase.Database);
			difference.Sort();
			ClassicAssert.AreEqual(2, difference.Count);
			ClassicAssert.AreEqual(ScriptType.TableRemoveData, difference[0].Type);
			ClassicAssert.AreEqual(@"DELETE FROM [dbo].[a]
WHERE
	[b] = 1
	AND [c] = '2000-01-02 03:04:05.600'

", difference[0].Text);
			ClassicAssert.AreEqual(ScriptType.TableData, difference[1].Type);
			ClassicAssert.AreEqual(@"INSERT INTO [dbo].[a]([b], [c])
VALUES(1, '2000-01-02 00:00:00.000')

", difference[1].Text);

			ExecuteScripts(difference);
			currentDatabase=ParseDatabase();
			GetData(currentDatabase.Database, parser.Database.GetTablesWithData());
			difference=updated.Database.CreateDiffScripts(currentDatabase.Database);
			ClassicAssert.AreEqual(0, difference.Count);
		}

		[Test]
		public void DefaultDataTest()
		{
			ScriptParser parsed=new ScriptParser();
			parsed.Parse("CREATE TABLE foo( a varchar(10), b varchar(20) default 'foo')");
			parsed.Parse(@"INSERT INTO foo(a) VALUES('X')
INSERT INTO foo(a, b) VALUES('A', 'B')");

			ScriptSet scripts=parsed.Database.CreateDiffScripts(new Database());
			scripts.Sort();
			ClassicAssert.AreEqual(3, scripts.Count);
			ClassicAssert.AreEqual(ScriptType.Table, scripts[0].Type);
			ClassicAssert.AreEqual(ScriptType.DefaultConstraint, scripts[1].Type);
			ClassicAssert.AreEqual(ScriptType.TableData, scripts[2].Type);
			ClassicAssert.AreEqual(@"INSERT INTO [dbo].[foo]([a], [b])
VALUES('X', 'foo')

INSERT INTO [dbo].[foo]([a], [b])
VALUES('A', 'B')

",
				scripts[2].Text);

			scripts=parsed.Database.CreateDiffScripts(parsed.Database);
			ClassicAssert.AreEqual(0, scripts.Count);
		}

		[Test]
		public void DeleteTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a int)");
			startingDatabase.Parse("DELETE FROM foo");

			ScriptParser currentDatabase=new ScriptParser();
			ScriptSet scripts=startingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			scripts.Sort();
			ClassicAssert.AreEqual(1, scripts.Count);

			ClassicAssert.AreEqual(scripts[0].Type, ScriptType.Table);
			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=startingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void DeleteWhereTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a int)");
			startingDatabase.Parse("DELETE FROM foo WHERE a=5");

			ScriptParser currentDatabase=new ScriptParser();
			ScriptSet scripts=startingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			scripts.Sort();
			ClassicAssert.AreEqual(1, scripts.Count);

			ClassicAssert.AreEqual(scripts[0].Type, ScriptType.Table);
			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=startingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void DeleteWhereAndOrTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a int)");
			startingDatabase.Parse("DELETE FROM foo WHERE a=5 AND a=5 OR a=6");

			ScriptParser currentDatabase=new ScriptParser();
			ScriptSet scripts=startingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			scripts.Sort();
			ClassicAssert.AreEqual(1, scripts.Count);

			ClassicAssert.AreEqual(scripts[0].Type, ScriptType.Table);
			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=startingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void DeleteWhereInTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a int)");
			startingDatabase.Parse("DELETE FROM foo WHERE a in (5, 6)");

			ScriptParser currentDatabase=new ScriptParser();
			ScriptSet scripts=startingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			scripts.Sort();
			ClassicAssert.AreEqual(1, scripts.Count);

			ClassicAssert.AreEqual(scripts[0].Type, ScriptType.Table);
			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=startingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void ForeignKeyCreateTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo(a int primary key)");
			startingDatabase.Parse("CREATE TABLE bar(a int)");

			ScriptParser endingDatabase=new ScriptParser();
			endingDatabase.Parse("CREATE TABLE foo(a int primary key)");
			endingDatabase.Parse("CREATE TABLE bar(a int foreign key references foo(a))");
			endingDatabase.Parse("INSERT INTO foo(a) values('1')");

			ScriptSet scripts=endingDatabase.Database.CreateDiffScripts(startingDatabase.Database);
			scripts.Sort();
			ClassicAssert.AreEqual(2, scripts.Count);
			ClassicAssert.AreEqual(ScriptType.TableData, scripts[0].Type);
			ClassicAssert.AreEqual(ScriptType.ForeignKey, scripts[1].Type);
        }
        [Test]
        public void QuotedIntegerTest()
        {
            ScriptParser parser = new ScriptParser();
            parser.Parse(@"CREATE TABLE A(a int)");
            parser.Parse(@"INSERT INTO A(a) VALUES('1')");

            ScriptSet initScripts = parser.Database.CreateDiffScripts(new Database());

            initScripts.Sort();
            ClassicAssert.AreEqual(2, initScripts.Count);
            ClassicAssert.AreEqual(ScriptType.Table, initScripts[0].Type);
            ClassicAssert.AreEqual(ScriptType.TableData, initScripts[1].Type);
            ExecuteScripts(initScripts);

            ScriptParser currentDatabase = ParseDatabase();
            GetData(currentDatabase.Database, parser.Database.GetTablesWithData());
            ScriptSet difference = parser.Database.CreateDiffScripts(currentDatabase.Database);

            ClassicAssert.AreEqual(0, difference.Count);
        }

		[Test]
		public void TableUpdateTest()
		{
			ScriptSet initScripts=new ScriptSet();
			initScripts.Add(new Script("CREATE TABLE foo( a varchar(10), b varchar(10))", "Foo", ScriptType.Table));
			initScripts.Add(new Script(@"INSERT INTO foo(a,b) VALUES('X', 'Y')
INSERT INTO foo(a,b) VALUES('A','B')", "FooData", ScriptType.TableData));
			ExecuteScripts(initScripts);

			ScriptSet updateScripts=new ScriptSet();
			updateScripts.Add(new Script("CREATE TABLE foo( a varchar(10))", "Foo", ScriptType.Table));
			updateScripts.Add(new Script(
				@"INSERT INTO foo(a) VALUES('X')
INSERT INTO foo(a) VALUES('Y')
INSERT INTO dbo.foo(a) VALUES('Z')
insert into dbo.foo(a) values('yabba')", "FooData", ScriptType.TableData));

			ScriptParser parsedScripts=new ScriptParser();
			parsedScripts.RetrieveParsableObjects(updateScripts);
			ClassicAssert.AreEqual(0, updateScripts.Count);

			ScriptParser parsedDatabase=ParseDatabase();
			GetData(parsedDatabase.Database, parsedScripts.Database.GetTablesWithData());

			updateScripts.Add(parsedScripts.Database.CreateDiffScripts(parsedDatabase.Database));
			ClassicAssert.AreEqual(5, updateScripts.Count);
			updateScripts.Sort();

			ClassicAssert.AreEqual(@"DELETE FROM [dbo].[foo]
WHERE
	[a] = 'X'
	AND [b] = 'Y'

DELETE FROM [dbo].[foo]
WHERE
	[a] = 'A'
	AND [b] = 'B'

", updateScripts[0].Text);
			ClassicAssert.AreEqual(ScriptType.TableRemoveData, updateScripts[0].Type);

			ClassicAssert.AreEqual(@"EXEC sp_rename '[dbo].[foo]', 'Tmp__foo', 'OBJECT'", updateScripts[1].Text);
			ClassicAssert.AreEqual(ScriptType.TableSaveData, updateScripts[1].Type);

			ClassicAssert.AreEqual(@"CREATE TABLE [dbo].[foo](
	[a] [varchar](10) NULL
)

GO", updateScripts[2].Text);
			ClassicAssert.AreEqual(ScriptType.Table, updateScripts[2].Type);

			ClassicAssert.AreEqual(@"INSERT INTO [dbo].[foo]([a])
VALUES('X')

INSERT INTO [dbo].[foo]([a])
VALUES('Y')

INSERT INTO [dbo].[foo]([a])
VALUES('Z')

INSERT INTO [dbo].[foo]([a])
VALUES('yabba')

", updateScripts[3].Text);
			ClassicAssert.AreEqual(ScriptType.TableData, updateScripts[3].Type);

			ExecuteScripts(updateScripts);

			ScriptParser currentDatabase=ParseDatabase();
			GetData(currentDatabase.Database, parsedScripts.Database.GetTablesWithData());
			ScriptSet difference=parsedScripts.Database.CreateDiffScripts(currentDatabase.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void TextTest()
		{
			ScriptSet initScripts=new ScriptSet();
			initScripts.Add(new Script("CREATE TABLE foo( a text, b ntext, c varchar(max), d nvarchar(max))", "Foo", ScriptType.Table));
			initScripts.Add(new Script(@"INSERT INTO foo(a, b, c, d) VALUES('X', 'X', 'X', 'X')
INSERT INTO foo(a, b, c, d) VALUES('A', 'A', 'A', 'A')", "FooData", ScriptType.TableData));
			ExecuteScripts(initScripts);

			ScriptSet updateScripts=new ScriptSet();
			updateScripts.Add(new Script("CREATE TABLE foo( a text, b ntext, c varchar(max), d nvarchar(max))", "Foo", ScriptType.Table));
			updateScripts.Add(new Script(
				@"INSERT INTO foo(a, b, c, d) VALUES('X', 'X', 'X', 'X')
INSERT INTO foo(a, b, c, d) VALUES('Y', 'Y', 'Y', 'Y')
INSERT INTO dbo.foo(a, b, c, d) VALUES('Z', 'Z', 'Z', 'Z')
insert into dbo.foo(a, b, c, d) values('yabba zabba', 'yabba zabba', 'yabba zabba', 'yabba zabba')", "FooData", ScriptType.TableData));

			ScriptParser parsedScripts=new ScriptParser();
			parsedScripts.RetrieveParsableObjects(updateScripts);
			ClassicAssert.AreEqual(0, updateScripts.Count);

			ScriptParser parsedDatabase=ParseDatabase();
			GetData(parsedDatabase.Database, parsedScripts.Database.GetTablesWithData());

			updateScripts.Add(parsedScripts.Database.CreateDiffScripts(parsedDatabase.Database));
			ClassicAssert.AreEqual(2, updateScripts.Count);
			updateScripts.Sort();

			ClassicAssert.AreEqual(@"DELETE FROM [dbo].[foo]
WHERE
	CAST([a] AS VARCHAR(MAX)) = 'A'
	AND CAST([b] AS VARCHAR(MAX)) = 'A'
	AND [c] = 'A'
	AND [d] = 'A'

", updateScripts[0].Text);
			ClassicAssert.AreEqual(ScriptType.TableRemoveData, updateScripts[0].Type);

			ClassicAssert.AreEqual(@"INSERT INTO [dbo].[foo]([a], [b], [c], [d])
VALUES('Y', 'Y', 'Y', 'Y')

INSERT INTO [dbo].[foo]([a], [b], [c], [d])
VALUES('Z', 'Z', 'Z', 'Z')

INSERT INTO [dbo].[foo]([a], [b], [c], [d])
VALUES('yabba zabba', 'yabba zabba', 'yabba zabba', 'yabba zabba')

", updateScripts[1].Text);
			ClassicAssert.AreEqual(ScriptType.TableData, updateScripts[1].Type);

			ExecuteScripts(updateScripts);
		}

		[Test]
		public void TruncateTest()
		{
			ScriptParser startingDatabase=new ScriptParser();
			startingDatabase.Parse("CREATE TABLE foo( a int)");
			startingDatabase.Parse("TRUNCATE TABLE foo");

			ScriptParser currentDatabase=new ScriptParser();
			ScriptSet scripts=startingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			scripts.Sort();
			ClassicAssert.AreEqual(1, scripts.Count);

			ClassicAssert.AreEqual(scripts[0].Type, ScriptType.Table);
			ExecuteScripts(scripts);

			currentDatabase=ParseDatabase();
			ScriptSet difference=startingDatabase.Database.CreateDiffScripts(currentDatabase.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}
	}
}
