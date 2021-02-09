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
	/// A row of scripted data for a table
	/// </summary>
	public class TableRow : IComparable<TableRow>
	{
		private Table table;
		private Dictionary<SmallName, string> values=new Dictionary<SmallName, string>();

		/// <summary>
		/// Gets the number of stored values.
		/// </summary>
		/// <value>The number of stored values.</value>
		public int Count
		{
			get { return values.Count; }
		}

		/// <summary>
		/// Gets or sets the <see cref="System.String"/> with the specified name.
		/// </summary>
		/// <value></value>
		public string this[SmallName name]
		{
			get { return values[name]; }
			set { values[name]=value; }
		}

		/// <summary>
		/// Implements the operator &lt;.
		/// </summary>
		/// <param name="a">One <see cref="TableRow"/>.</param>
		/// <param name="b">Another <see cref="TableRow"/>.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator<(TableRow a, TableRow b)
		{
			return a.CompareTo(b)<0;
		}

		/// <summary>
		/// Implements the operator &gt;.
		/// </summary>
		/// <param name="a">One <see cref="TableRow"/>.</param>
		/// <param name="b">Another <see cref="TableRow"/>.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator>(TableRow a, TableRow b)
		{
			return a.CompareTo(b)>0;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TableRow"/> class.
		/// </summary>
		/// <param name="table">The table.</param>
		public TableRow(Table table)
		{
			this.table=table;
		}

		/// <summary>
		/// Compares the current object with another object of the same type.
		/// </summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns>
		/// A 32-bit signed integer that indicates the relative order of the objects being compared. The return value has the following meanings:
		/// Value
		/// Meaning
		/// Less than zero
		/// This object is less than the <paramref name="other"/> parameter.
		/// Zero
		/// This object is equal to <paramref name="other"/>.
		/// Greater than zero
		/// This object is greater than <paramref name="other"/>.
		/// </returns>
		public int CompareTo(TableRow other)
		{
			throw new Exception("This makes no sense, but an implementation is required");
		}

		/// <summary>
		/// Determines whether the specified column is defined in this row.
		/// </summary>
		/// <param name="name">The column name.</param>
		/// <returns>
		/// 	<c>true</c> if the specified column is defined in this row; otherwise, <c>false</c>.
		/// </returns>
		public bool ContainsColumn(SmallName name)
		{
			return values.ContainsKey(name);
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <param name="obj">The <see cref="T:System.Object"/> to compare with the current <see cref="T:System.Object"/>.</param>
		/// <returns>
		/// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.
		/// </returns>
		/// <exception cref="T:System.NullReferenceException">
		/// The <paramref name="obj"/> parameter is null.
		/// </exception>
		public override bool Equals(object obj)
		{
			TableRow other=obj as TableRow;
			if(other==null)
				return false;

			foreach(SmallName key in values.Keys)
			{
				if(!other.values.ContainsKey(key))
				{
					if(values[key].ToLower()!="null")
					{
						return false;
					}
				}
				else if(values[key]!=other.values[key] 
					& (values[key].ToLower()!="null" || other.values[key].ToLower()!="null"))
				{
					return false;
				}
			}
			foreach(SmallName key in other.values.Keys)
			{
				if(!values.ContainsKey(key) && other.values[key].ToLower()!="null")
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Generates a delete script section.
		/// </summary>
		/// <param name="script">The current script.</param>
		/// <param name="tableName">Name of the table this row belongs to.</param>
		public void GenerateDelete(StringBuilder script, Name tableName)
		{
			script.Append("DELETE FROM ");
			script.AppendLine(tableName.ToString());
			script.AppendLine("WHERE");

			int i=0;
			foreach(SmallName key in values.Keys)
			{
				bool cast = table.Columns[key].Type=="ntext"
					|| table.Columns[key].Type=="text";

				script.Append(i>0 ? "\tAND " : "\t");
				if(cast)
				{
					script.Append("CAST(");
				}
				script.Append(key);
				if(cast)
				{
					script.Append(" AS VARCHAR(MAX))");
				}
				if(values[key].ToLower()=="null")
				{
					script.AppendLine(" IS NULL ");
				}
				else
				{
					script.Append(" = ");
					script.AppendLine(values[key]);
				}

				i++;
			}

			script.AppendLine();
		}

		/// <summary>
		/// Generates an insert script section.
		/// </summary>
		/// <param name="script">The current script.</param>
		/// <param name="tableName">Name of the table this row belongs to.</param>
		public void GenerateInsert(StringBuilder script, Name tableName)
		{
			StringBuilder insert=new StringBuilder("INSERT INTO ");
			insert.Append(tableName);
			insert.Append("(");
			StringBuilder vals=new StringBuilder("VALUES(");

			int i=0;
			foreach(SmallName key in values.Keys)
			{
				insert.Append(key);
				vals.Append(values[key]);

				i++;
				if(i<values.Keys.Count)
				{
					insert.Append(", ");
					vals.Append(", ");
				}
			}

			script.Append(insert);
			script.AppendLine(")");
			script.Append(vals);
			script.AppendLine(")");
			script.AppendLine();
		}

		/// <summary>
		/// Serves as a hash function for a particular type.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"/>.
		/// </returns>
		public override int GetHashCode()
		{
			return values.GetHashCode();
		}
	}
}
