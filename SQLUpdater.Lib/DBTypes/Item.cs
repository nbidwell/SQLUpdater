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

namespace SQLUpdater.Lib.DBTypes
{
	/// <summary>
	/// A parsed database object
	/// </summary>
	public abstract class Item
	{
		/// <summary>
		/// Gets or sets the name of this item.
		/// </summary>
		/// <value>The name of this item.</value>
		public Name Name { get; set; }

		/// <summary>
		/// Permissions defined on this item
		/// </summary>
		/// <value>The permissions.</value>
		public PermissionSet Permissions { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="Item"/> class.
		/// </summary>
		/// <param name="name">The item name.</param>
		public Item(Name name)
		{
			Name=name;
			Permissions=new PermissionSet();
		}

		/// <summary>
		/// Implements the operator ==.
		/// </summary>
		/// <param name="a">A.</param>
		/// <param name="b">The b.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator==(Item a, Item b)
		{
			return (object)a==null ? (object)b==null : a.Equals(b);
		}

		/// <summary>
		/// Implements the operator !=.
		/// </summary>
		/// <param name="a">A.</param>
		/// <param name="b">The b.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator!=(Item a, Item b)
		{
			return (object)a==null ? (object)b!=null : !a.Equals(b);
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
			return GetDifferences(obj as Item, false)==null;
		}

        /// <summary>
        /// Are two strings equal, substituting a default?
        /// </summary>
        /// <param name="a">one value</param>
        /// <param name="b">the other value</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        protected bool EqualWithDefault(string a, string b, string defaultValue)
        {
            return a==b || (Name)(string.IsNullOrEmpty(a) ? defaultValue : a) == (Name)(string.IsNullOrEmpty(b) ? defaultValue : b); 
        }

		/// <summary>
		/// Generates a create script.
		/// </summary>
		/// <returns></returns>
		public abstract Script GenerateCreateScript();

		/// <summary>
		/// Generates a drop script.
		/// </summary>
		/// <returns></returns>
		public abstract Script GenerateDropScript();

		/// <summary>
		/// Gets the differences between this item and another of the same type
		/// </summary>
		/// <param name="other">Another item of the same type</param>
		/// <param name="allDifferences">if set to <c>true</c> collects all differences,
		/// otherwise only the first difference is collected.</param>
		/// <returns>
		/// A Difference if there are differences between the items, otherwise null
		/// </returns>
		public abstract Difference GetDifferences(Item other, bool allDifferences);

		/// <summary>
		/// Serves as a hash function for a particular type.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"/>.
		/// </returns>
		public override int GetHashCode()
		{
			return Name.GetHashCode ();
		}

		/// <summary>
		/// Check if two identifiers are primary file groups
		/// </summary>
		/// <param name="group0">The group0.</param>
		/// <param name="group1">The group1.</param>
		/// <returns>True if both names match as primary filegroups</returns>
		protected bool PrimaryFileGroups(SmallName group0, SmallName group1)
		{
			return (group0==null || group0.Unescaped.ToLower().Trim()=="primary")
				&& (group1==null || group1.Unescaped.ToLower().Trim()=="primary");
		}
	}
}
