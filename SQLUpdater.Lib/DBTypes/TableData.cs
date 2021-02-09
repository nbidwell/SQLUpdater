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

using System;
using System.Collections.Generic;
using System.Text;

namespace SQLUpdater.Lib.DBTypes
{
	/// <summary>
	/// A set of scripted table data
	/// </summary>
	public class TableData : List<TableRow>
	{
		/// <summary>
		/// Gets or sets the name of this table data.
		/// </summary>
		/// <value>The name of this table data.</value>
		public Name Name { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TableData"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		public TableData(Name name) : base()
		{
			Name=name;
		}

		/// <summary>
		/// Is this set of table data the same as another?
		/// </summary>
		/// <param name="other">The other set of table data.</param>
		/// <returns></returns>
		public bool AreEqual(TableData other)
		{
			return !GetDifferences(other, null, null);
		}

		/// <summary>
		/// Gets difference scripts.
		/// </summary>
		/// <param name="other">The other set of table data.</param>
		/// <param name="tableName">Name of the table.</param>
		/// <returns></returns>
		public ScriptSet GetDifferenceScript(TableData other, Name tableName)
		{
			ScriptSet scripts=new ScriptSet();
			GetDifferences(other, tableName, scripts);
			return scripts;
		}

		/// <summary>
		/// Gets the differences between this set of table data and another.
		/// </summary>
		/// <param name="other">The other set of table data.</param>
		/// <param name="tableName">Name of the table.</param>
		/// <param name="scripts">A set of scripts to fill with the differences.</param>
		/// <returns>True if the two sets are different; otherwise false.</returns>
		private bool GetDifferences(TableData other, Name tableName, ScriptSet scripts)
		{
			if(Count==0)
				return false;

			if(other==null)
				other=new TableData(Name);

			StringBuilder addScript=new StringBuilder();
			StringBuilder deleteScript=new StringBuilder();
			List<TableRow> testing=new List<TableRow>(this);

			//this may not be the most efficient algorithm, but we have no index
			foreach(TableRow existing in other)
			{
				TableRow found=null;
				foreach(TableRow adding in testing)
				{
					if(existing.Equals(adding))
					{
						found=adding;
						break;
					}
				}

				if(found==null)
				{
					if(scripts==null)
					{
						return true;
					}

					existing.GenerateDelete(deleteScript, tableName);
				}
				else
				{
					testing.Remove(found);
				}
			}

			foreach(TableRow adding in testing)
			{
				if(scripts==null)
				{
					return true;
				}

				adding.GenerateInsert(addScript, tableName);
			}

			if(addScript.Length>0)
			{
				scripts.Add(new Script(addScript.ToString(), tableName.Unescaped+"Data", ScriptType.TableData));
			}
			if(deleteScript.Length>0)
			{
				scripts.Add(new Script(deleteScript.ToString(), tableName+"Data", ScriptType.TableRemoveData));
			}
			return scripts!=null && scripts.Count>0;
		}
	}
}
