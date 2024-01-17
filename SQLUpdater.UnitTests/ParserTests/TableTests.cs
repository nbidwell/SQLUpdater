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
using NUnit.Framework.Legacy;
using SQLUpdater.Lib;
using SQLUpdater.Lib.DBTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLUpdater.UnitTests.ParserTests
{
	[TestFixture]
	public class TableTests : BaseUnitTest
	{
		[Test]
		public void BasicTableTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE foo( a int)");
			ClassicAssert.AreEqual(1, parser.Database.Tables.Count);

			Script createScript=parser.Database.Tables[0].GenerateCreateScript();
			ClassicAssert.AreEqual(@"CREATE TABLE [dbo].[foo](
	[a] [int] NULL
)

GO",
				createScript.Text);
			ExecuteScripts(createScript);

			ScriptParser database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void ColumnReplicationTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse(@"CREATE TABLE a( b int identity(1, 1) NOT FOR REPLICATION NOT NULL)");

			Script createScript=parser.Database.Tables[0].GenerateCreateScript();
			ClassicAssert.AreEqual(@"CREATE TABLE [dbo].[a](
	[b] [int] IDENTITY(1, 1) NOT FOR REPLICATION NOT NULL
)

GO",
				createScript.Text);
			ExecuteScripts(createScript);

			ScriptParser database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void ComputedColumnTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse(@"CREATE TABLE a( b int, c AS [b], d AS [b] )");

			Script createScript=parser.Database.Tables[0].GenerateCreateScript();
			ClassicAssert.AreEqual(@"CREATE TABLE [dbo].[a](
	[b] [int] NULL,
	[c] AS [b],
	[d] AS [b]
)

GO",
				createScript.Text);
			ExecuteScripts(createScript);

			ScriptParser database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void CreateTableTwiceTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE foo( a int)");
			ClassicAssert.AreEqual(1, parser.Database.Tables.Count);

			Script createScript=parser.Database.Tables[0].GenerateCreateScript();
			ClassicAssert.AreEqual(@"CREATE TABLE [dbo].[foo](
	[a] [int] NULL
)

GO",
				createScript.Text);

			//second time - by default this should blow up
			Assert.Throws<ApplicationException>(() =>
				{
					parser.Parse("CREATE TABLE foo( a int)");
				},
				"An item with the same key has already been added. ([dbo].[foo])\nCREATE TABLE foo( a int)"
				);
		}

		[Test]
		public void DecimalColumnTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE foo( a decimal, b decimal(10, 2))");
			ClassicAssert.AreEqual(1, parser.Database.Tables.Count);

			Script createScript=parser.Database.Tables[0].GenerateCreateScript();
			ClassicAssert.AreEqual(@"CREATE TABLE [dbo].[foo](
	[a] [decimal](18, 0) NULL,
	[b] [decimal](10, 2) NULL
)

GO",
				createScript.Text);
			ExecuteScripts(createScript);

			ScriptParser database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void FilegroupTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE foo( a int) ON Primary");
			ClassicAssert.AreEqual(1, parser.Database.Tables.Count);

			Script createScript=parser.Database.Tables[0].GenerateCreateScript();
			ClassicAssert.AreEqual(@"CREATE TABLE [dbo].[foo](
	[a] [int] NULL
) ON [Primary]

GO",
				createScript.Text);
			ExecuteScripts(createScript);

			ScriptParser database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void GrantTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE FOO( a int)");
			parser.Parse("GRANT SELECT ON foo TO public");

			ScriptSet difference=parser.Database.CreateDiffScripts(new Database());
			ClassicAssert.AreEqual(2, difference.Count);

			ClassicAssert.AreEqual(difference[0].Type, ScriptType.Table);
			ClassicAssert.AreEqual(@"CREATE TABLE [dbo].[FOO](
	[a] [int] NULL
)

GO",
				difference[0].Text);

			ClassicAssert.AreEqual(difference[1].Type, ScriptType.Permission);
			ClassicAssert.AreEqual(@"GRANT SELECT ON [dbo].[FOO] TO [public]

GO

",
				difference[1].Text);

			ExecuteScripts(difference);

			ScriptParser database=ParseDatabase();
			difference=parser.Database.CreateDiffScripts(database.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void IdentityColumnTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE foo( a int identity)");
			ClassicAssert.AreEqual(1, parser.Database.Tables.Count);

			Script createScript=parser.Database.Tables[0].GenerateCreateScript();
			ClassicAssert.AreEqual(@"CREATE TABLE [dbo].[foo](
	[a] [int] IDENTITY(1, 1) NOT NULL
)

GO",
				createScript.Text);
			ExecuteScripts(createScript);

			ScriptParser database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void IdentityColumnSeedTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE foo( a int identity(0, 1))");
			ClassicAssert.AreEqual(1, parser.Database.Tables.Count);

			Script createScript=parser.Database.Tables[0].GenerateCreateScript();
			ClassicAssert.AreEqual(@"CREATE TABLE [dbo].[foo](
	[a] [int] IDENTITY(0, 1) NOT NULL
)

GO",
				createScript.Text);
			ExecuteScripts(createScript);

			ScriptParser database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void MultipleSchemaTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE foo( a int primary key)");
			parser.Parse("CREATE TABLE blah.foo( a int primary key)");

			ScriptSet scripts=parser.Database.CreateDiffScripts(new ScriptParser().Database);
			ClassicAssert.AreEqual(4, scripts.Count);
			scripts.Sort();

			ClassicAssert.AreEqual(@"CREATE TABLE [blah].[foo](
	[a] [int] NOT NULL
)

GO",
				scripts[0].Text);

			ClassicAssert.AreEqual(@"CREATE TABLE [dbo].[foo](
	[a] [int] NOT NULL
)

GO",
				scripts[1].Text);

			ClassicAssert.AreEqual(@"ALTER TABLE [blah].[foo]
ADD CONSTRAINT [PK_foo]
PRIMARY KEY(
	[a]
)",
				scripts[2].Text);

			ClassicAssert.AreEqual(@"ALTER TABLE [dbo].[foo]
ADD CONSTRAINT [PK_foo]
PRIMARY KEY(
	[a]
)",
				scripts[3].Text);
		}

        [Test]
        public void NonclusteredPrimaryKeyTest()
        {
			ScriptParser parser=new ScriptParser();
            parser.Parse(@"CREATE TABLE A(
  b int NOT NULL,
  PRIMARY KEY NONCLUSTERED (b)
)");

			ScriptSet scripts=parser.Database.CreateDiffScripts(new Database());
			ClassicAssert.AreEqual(2, scripts.Count);
			ExecuteScripts(scripts);

			ScriptParser database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			ClassicAssert.AreEqual(0, difference.Count);
        }

		[Test]
		public void PrimaryKeyTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse(@"CREATE TABLE a( b int identity(1,1) PRIMARY KEY )");

			ScriptSet scripts=parser.Database.CreateDiffScripts(new Database());
			ClassicAssert.AreEqual(2, scripts.Count);
			ExecuteScripts(scripts);

			ScriptParser database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void SchemaTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE blah.foo( a int)");

			ScriptSet scripts=parser.Database.CreateDiffScripts(new ScriptParser().Database);
			ClassicAssert.AreEqual(1, scripts.Count);

			ClassicAssert.AreEqual(@"CREATE TABLE [blah].[foo](
	[a] [int] NULL
)

GO",
				scripts[0].Text);
		}

		[Test]
		public void TextFilegroupTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE foo( a int, b varchar(max)) TEXTIMAGE_ON Primary");
			ClassicAssert.AreEqual(1, parser.Database.Tables.Count);

			Script createScript=parser.Database.Tables[0].GenerateCreateScript();
			ClassicAssert.AreEqual(@"CREATE TABLE [dbo].[foo](
	[a] [int] NULL,
	[b] [varchar](max) NULL
) TEXTIMAGE_ON [Primary]

GO",
				createScript.Text);
			ExecuteScripts(createScript);

			ScriptParser database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}

		[Test]
		public void VarcharColumnTest()
		{
			ScriptParser parser=new ScriptParser();
			parser.Parse("CREATE TABLE foo( a varchar(20) COLLATE SQL_Latin1_General_CP1_CI_AS)");
			ClassicAssert.AreEqual(1, parser.Database.Tables.Count);

			Script createScript=parser.Database.Tables[0].GenerateCreateScript();
			ClassicAssert.AreEqual(@"CREATE TABLE [dbo].[foo](
	[a] [varchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL
)

GO",
				createScript.Text);
			ExecuteScripts(createScript);

			ScriptParser database=ParseDatabase();
			ScriptSet difference=parser.Database.CreateDiffScripts(database.Database);
			ClassicAssert.AreEqual(0, difference.Count, RunOptions.Current.Logger.ToString());
		}
	}
}
