/*
 * Copyright 2009 Nathan Bidwell (nbidwell@bidwellfamily.net)
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

namespace SQLUpdater.UnitTests
{
	[TestFixture]
	public class FileTests : BaseUnitTest
	{
		[Test]
		public void DataTest()
		{
			ScriptSet scripts=ScriptManager.LoadScripts("TestData\\DataTest");
			Assert.AreEqual(2, scripts.Count);
			Assert.AreEqual(ScriptType.Table, scripts[0].Type);
			Assert.AreEqual(ScriptType.TableData, scripts[1].Type);

			ScriptParser parser=new ScriptParser();
			parser.RetrieveParsableObjects(scripts);
			Assert.AreEqual(0, scripts.Count);
			Assert.AreEqual(1, parser.Database.Tables.Count);
			Assert.AreEqual(2, parser.Database.Tables[0].Data.Count);

			ScriptSet differences=parser.Database.CreateDiffScripts(new Database());
			differences.Sort();
			Assert.AreEqual(2, differences.Count);
			Assert.AreEqual(ScriptType.Table, differences[0].Type);
			Assert.AreEqual(ScriptType.TableData, differences[1].Type);
		}

		[Test]
		public void TableTest()
		{
			ScriptSet scripts=ScriptManager.LoadScripts("TestData\\TableTest");
			Assert.AreEqual(1, scripts.Count);
			Assert.AreEqual(ScriptType.Table, scripts[0].Type);

			ScriptParser parser=new ScriptParser();
			parser.RetrieveParsableObjects(scripts);
			Assert.AreEqual(0, scripts.Count);
			Assert.AreEqual(1, parser.Database.Tables.Count);

			ScriptSet differences=parser.Database.CreateDiffScripts(new Database());
			differences.Sort();
			Assert.AreEqual(1, differences.Count);
			Assert.AreEqual(ScriptType.Table, differences[0].Type);
		}
	}
}
