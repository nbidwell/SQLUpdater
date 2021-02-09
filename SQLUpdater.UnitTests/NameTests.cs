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
using SQLUpdater.Lib.DBTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SQLUpdater.UnitTests
{
	[TestFixture]
	public class NameTests
	{
		[Test]
		public void ComparisonTest()
		{
			Name a="dbo.a.b";
			Assert.IsTrue(a=="dbo.a.b");
			Assert.IsFalse(a=="dbo.a.c");
			Assert.IsFalse(a=="dbo.c.b");
			Assert.IsFalse(a=="foo.a.b");
			Assert.IsFalse(a==null);
			Assert.IsFalse(((Name)null)==a);
			Assert.IsTrue(((Name)null)==((Name)null));
		}

		[Test]
		public void NameTest()
		{
			Name name="dbo.a.b";
			Assert.AreEqual("[dbo].[a].[b]", name.ToString());
			Assert.AreEqual("[dbo]", name.Database.ToString());
			Assert.AreEqual("[a]", name.Owner.ToString());
			Assert.AreEqual("[b]", name.Object.ToString());
		}
	}
}
