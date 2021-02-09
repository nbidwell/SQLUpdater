/*
 * Copyright 2006 Nathan Bidwell (nbidwell@bidwellfamily.net)
 * 
 * This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License version 20 as published by
 *  the Free Software Foundation.
 * 
 * This software is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 */

using NUnit.Framework;
using SQLUpdater.Lib;
using SQLUpdater.Lib.DBTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLUpdater.UnitTests.ParserTests
{
	[TestFixture]
	public class ConstraintTests : BaseUnitTest
	{
		[Test]
		public void AlterCheckConstraintTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE foo( a int)");
			parser.Parse("ALTER TABLE foo WITH CHECK ADD CHECK(a>0)");
			Assert.AreEqual(1, parser.Database.Tables.Count);
			Assert.AreEqual(1, parser.Database.Constraints.Count);

			ScriptParser database=ParseDatabase();
			ScriptSet scripts=parser.Database.CreateDiffScripts(database.Database);
			Assert.AreEqual(ScriptType.CheckConstraint, scripts[1].Type);
			Assert.AreEqual(@"ALTER TABLE [dbo].[foo]
ADD CONSTRAINT [CK_foo]
CHECK ( [a] > 0 )",
				scripts[1].Text);
			ExecuteScripts(scripts);

            database = ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void AlterDefaultTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE foo( a int)");
			parser.Parse("ALTER TABLE foo ADD CONSTRAINT DF_foo_a DEFAULT 0 FOR a");
			Assert.AreEqual(1, parser.Database.Tables.Count);
			Assert.AreEqual(1, parser.Database.Constraints.Count);

			ScriptParser database=ParseDatabase();
			ScriptSet scripts=parser.Database.CreateDiffScripts(database.Database);
			Assert.AreEqual(2, scripts.Count);
			Assert.AreEqual(ScriptType.Table, scripts[0].Type);

			Assert.AreEqual(ScriptType.DefaultConstraint, scripts[1].Type);
			Assert.AreEqual(@"ALTER TABLE [dbo].[foo]
ADD CONSTRAINT [DF_foo_a]
DEFAULT ( 0 )
FOR [a]",
				scripts[1].Text);
			ExecuteScripts(scripts);

			database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void InlineDefaultTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE foo( a int default 0)");
			Assert.AreEqual(1, parser.Database.Tables.Count);
			Assert.AreEqual(1, parser.Database.Constraints.Count);

			ScriptParser database=ParseDatabase();
			ScriptSet scripts=parser.Database.CreateDiffScripts(database.Database);
			Assert.AreEqual(2, scripts.Count);
			Assert.AreEqual(ScriptType.Table, scripts[0].Type);

			Assert.AreEqual(ScriptType.DefaultConstraint, scripts[1].Type);
			Assert.AreEqual(@"ALTER TABLE [dbo].[foo]
ADD CONSTRAINT [DF_foo_a]
DEFAULT ( 0 )
FOR [a]",
				scripts[1].Text);
			ExecuteScripts(scripts);

			database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void InlineForeignKeyTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE foo( a int primary key)");
			parser.Parse("CREATE TABLE bar( a int foreign key references foo(a))");
			Assert.AreEqual(2, parser.Database.Tables.Count);
			Assert.AreEqual(2, parser.Database.Constraints.Count);

			ScriptParser database=ParseDatabase();
			ScriptSet scripts=parser.Database.CreateDiffScripts(database.Database);
			Assert.AreEqual(4, scripts.Count);
			scripts.Sort();
			Assert.AreEqual(ScriptType.ForeignKey, scripts[3].Type);
			Assert.AreEqual(@"ALTER TABLE [dbo].[bar]
ADD CONSTRAINT [FK_bar_foo]
FOREIGN KEY(
	[a]
)
REFERENCES [dbo].[foo](
	[a]
)",
				scripts[3].Text);
			ExecuteScripts(scripts);

			database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
        }

        [Test]
        public void NullCheckConstraintTest()
        {
            ScriptParser parser = new ScriptParser();
            parser.Parse("CREATE TABLE foo( a int)");
            parser.Parse("ALTER TABLE foo WITH CHECK ADD CHECK((a IS NULL))");
            Assert.AreEqual(1, parser.Database.Tables.Count);
            Assert.AreEqual(1, parser.Database.Constraints.Count);

            ScriptParser database = ParseDatabase();
            ScriptSet scripts = parser.Database.CreateDiffScripts(database.Database);
            Assert.AreEqual(ScriptType.CheckConstraint, scripts[1].Type);
            Assert.AreEqual(@"ALTER TABLE [dbo].[foo]
ADD CONSTRAINT [CK_foo]
CHECK ( ( [a] IS NULL ) )",
                scripts[1].Text);
            ExecuteScripts(scripts);

            database = ParseDatabase();
            ScriptSet difference = parser.Database.CreateDiffScripts(database.Database);
            Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
        }

        [Test]
		public void OnDeleteSetNullTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE a( b int)");
			parser.Parse("CREATE TABLE c( d int primary key)");
			parser.Parse(@"ALTER TABLE a ADD CONSTRAINT z FOREIGN KEY(b)
REFERENCES c(d)
ON UPDATE CASCADE
ON DELETE SET NULL");

			ScriptSet scripts=parser.Database.CreateDiffScripts(new Database());
			Assert.AreEqual(4, scripts.Count);
			ExecuteScripts(scripts);

			ScriptParser database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			Assert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
        }

        [Test]
        public void SchemaTest()
        {
            ScriptParser parser = new ScriptParser();
            parser.Parse("CREATE TABLE blah.foo( a varchar(10) CHECK ( a != 'X'))");

            ScriptSet scripts = parser.Database.CreateDiffScripts(new ScriptParser().Database);
            Assert.AreEqual(2, scripts.Count);

            Assert.AreEqual(@"CREATE TABLE [blah].[foo](
	[a] [varchar](10) NULL
)

GO",
                scripts[0].Text);

            Assert.AreEqual("[blah].[CK_foo]", scripts[1].Name.FullName);
            Assert.AreEqual(@"ALTER TABLE [blah].[foo]
ADD CONSTRAINT [CK_foo]
CHECK ( [a] != 'X' )",
                scripts[1].Text);
        }

        [Test]
        public void UniqueTest()
        {
            ScriptParser parser = new ScriptParser();
            parser.Parse("CREATE TABLE foo( a varchar(10) UNIQUE )");

            ScriptSet scripts = parser.Database.CreateDiffScripts(new ScriptParser().Database);
            Assert.AreEqual(2, scripts.Count);

            Assert.AreEqual(ScriptType.Table, scripts[0].Type);
            Assert.AreEqual(@"CREATE TABLE [dbo].[foo](
	[a] [varchar](10) NULL
)

GO",
                scripts[0].Text);

            Assert.AreEqual(ScriptType.UniqueConstraint, scripts[1].Type);
            Assert.AreEqual("[dbo].[IX_foo_a]", scripts[1].Name.FullName);
            Assert.AreEqual(@"ALTER TABLE [dbo].[foo]
ADD CONSTRAINT [IX_foo_a]
UNIQUE(
	[a]
)",
                scripts[1].Text);
        }
	}
}
