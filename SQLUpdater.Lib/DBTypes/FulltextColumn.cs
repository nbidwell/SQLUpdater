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

using System;
using System.Collections.Generic;
using System.Text;

namespace SQLUpdater.Lib.DBTypes
{
	/// <summary>
	/// A parsed column in a full text index
	/// </summary>
	public class FulltextColumn : Item, IComparable	
	{
		/// <summary>
		/// Language
		/// </summary>
		public SmallName Language="English";

		/// <summary>
		/// Gets or sets the name of this item.
		/// </summary>
		/// <value>The name of this item.</value>
		public new SmallName Name
		{
			get { return base.Name.Object; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="FulltextColumn"/> class.
		/// </summary>
		/// <param name="name">The column name.</param>
		public FulltextColumn(string name) : base(name)
		{
		}

		/// <summary>
		/// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
		/// </summary>
		/// <param name="obj">An object to compare with this instance.</param>
		/// <returns>
		/// A 32-bit signed integer that indicates the relative order of the objects being compared. The return value has these meanings:
		/// Value
		/// Meaning
		/// Less than zero
		/// This instance is less than <paramref name="obj"/>.
		/// Zero
		/// This instance is equal to <paramref name="obj"/>.
		/// Greater than zero
		/// This instance is greater than <paramref name="obj"/>.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">
		/// 	<paramref name="obj"/> is not the same type as this instance.
		/// </exception>
		public int CompareTo(object obj)
		{
			FulltextColumn other=obj as FulltextColumn;
			if(other==null)
				throw new Exception("Incomparable objects");

			return Name.ToString().CompareTo(other.Name.ToString());
		}

		/// <summary>
		/// Generates a create script.
		/// </summary>
		/// <returns></returns>
		public override Script GenerateCreateScript()
		{
			//This doesn't actually make sense, but is required
			throw new NotImplementedException();
		}

		/// <summary>
		/// Generates a drop script.
		/// </summary>
		/// <returns></returns>
		public override Script GenerateDropScript()
		{
			//This doesn't actually make sense, but is required
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the differences between this item and another of the same type
		/// </summary>
		/// <param name="other">Another item of the same type</param>
		/// <param name="allDifferences">if set to <c>true</c> collects all differences,
		/// otherwise only the first difference is collected.</param>
		/// <returns>
		/// A Difference if there are differences between the items, otherwise null
		/// </returns>
		public override Difference GetDifferences(Item other, bool allDifferences)
		{
			FulltextColumn otherColumn=other as FulltextColumn;
			if(otherColumn==null)
				return new Difference(DifferenceType.Created, Name);

			Difference difference=new Difference(DifferenceType.Modified, Name);
			if(Name!=otherColumn.Name)
			{
				difference.AddMessage("Name", otherColumn.Name, Name);
				if(!allDifferences)
					return difference;
			}
			if(Language!=otherColumn.Language)
			{
				difference.AddMessage("Language", otherColumn.Language, Language);
				if(!allDifferences)
					return difference;
			}

			return difference.Messages.Count>0 ? difference : null;
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </returns>
		public override string ToString()
		{
			StringBuilder output=new StringBuilder();
			output.Append(Name);

			if(Language!=null)
			{
				output.Append(" LANGUAGE ");
				output.Append(Language);
			}

			return output.ToString();
		}
	}
}
