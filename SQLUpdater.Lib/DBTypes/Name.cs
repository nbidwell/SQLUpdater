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
	/// An identifier for an object within the database
	/// </summary>
	public class Name
	{
		private string fullName;
		int? hashCode;
		private string unescaped;

		/// <summary>
		/// Gets a temporary, hopefully unique name for backup.
		/// </summary>
		/// <value>A temporary, hopefully unique name for backup.</value>
		public Name BackupName
		{
			get
			{ 
				return (Database==null ? "" : Database+".") 
					+ (Owner==null ? "" : Owner+".") + Object.ToString().Insert(1, "Tmp__");
			}
		}

		/// <summary>
		/// Gets or sets the database section of the name.
		/// </summary>
		/// <value>The database section of the name.</value>
		public SmallName Database { get; private set; }

		/// <summary>
		/// Gets or sets the full name.
		/// </summary>
		/// <value>The full name.</value>
		public string FullName
		{
			get
			{
				if(fullName==null)
				{
					fullName=(Database==null ? "" : Database+".") 
						+ (Owner==null ? "" : Owner+".") + Object;
				}
				return fullName;
			}
		}

		/// <summary>
		/// Gets or sets the object section of the name.
		/// </summary>
		/// <value>The object section of the name.</value>
		public SmallName Object { get; set; }

		/// <summary>
		/// Gets or sets the owner section of the name.
		/// </summary>
		/// <value>The owner section of the name.</value>
		public SmallName Owner { get; set; }

		/// <summary>
		/// Gets or sets the full name without escape characters.
		/// </summary>
		/// <value>The full name without escape characters.</value>
		public string Unescaped
		{
			get
			{
				if(unescaped==null)
				{
					unescaped=(Database==null ? "" : Database.Unescaped+".") 
						+ (Owner==null ? "" : Owner.Unescaped+".") + Object.Unescaped;
				}
				return unescaped;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Name"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		private Name(string name)
		{
			if(name==null || name=="")
				throw new ApplicationException("Empty names are invalid");

			//defaults
			Database=null;
			Owner="dbo";
			Object=null;

			//get the object name
			int lastBracket=name.LastIndexOf("[");
			int objectStart=name.LastIndexOf(".");
			while(lastBracket>0 && objectStart>0 && objectStart>lastBracket)
			{
				objectStart=name.LastIndexOf(".", objectStart-1);
			}
			objectStart++;  //don't include the dot or start before the beginning
			Object=name.Substring(objectStart, name.Length-objectStart).Trim();

			//get any owner
			if(objectStart>1)
			{
				int ownerStart=name.LastIndexOf(".", objectStart-2);
				if(lastBracket>objectStart)
				{
					lastBracket=name.LastIndexOf("[", objectStart-2);
				}
				while(lastBracket>0 && ownerStart>0 && ownerStart>lastBracket)
				{
					ownerStart=name.LastIndexOf(".", ownerStart-1);
				}
				ownerStart++;  //don't include the dot or start before the beginning
				Owner=name.Substring(ownerStart, objectStart-1-ownerStart).Trim();

				//get any database
				if(ownerStart>1)
				{
					Database=name.Substring(0, ownerStart-1).Trim();
				}
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Name"/> class.
		/// </summary>
		/// <param name="database">The database.</param>
		/// <param name="owner">The owner.</param>
		/// <param name="objectName">Name of the object.</param>
		public Name(string database, string owner, string objectName)
		{
			if(string.IsNullOrEmpty(owner))
				throw new ArgumentNullException("owner");
			if(string.IsNullOrEmpty(objectName))
				throw new ArgumentNullException("objectName");

			Database=database;
			Owner=owner;
			Object=objectName;
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="System.String"/> to <see cref="SQLUpdater.Lib.DBTypes.Name"/>.
		/// </summary>
		/// <param name="converting">The string value.</param>
		/// <returns>A name.</returns>
		public static implicit operator Name(string converting)
		{
			return new Name(converting);
		}

		/// <summary>
		/// Performs an explicit conversion from <see cref="SQLUpdater.Lib.DBTypes.Name"/> to <see cref="SQLUpdater.Lib.DBTypes.SmallName"/>.
		/// </summary>
		/// <param name="converting">The Name.</param>
		/// <returns>A Small Name.</returns>
		public static explicit operator SmallName(Name converting)
		{
			return converting.Object;
		}

		/// <summary>
		/// Implements the operator ==.
		/// </summary>
		/// <param name="a">One Name.</param>
		/// <param name="b">Another Name.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator==(Name a, Name b)
		{
			return (object)a==null ? (object)b==null : a.Equals(b);
		}

		/// <summary>
		/// Implements the operator !=.
		/// </summary>
		/// <param name="a">One Name.</param>
		/// <param name="b">Another Name.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator!=(Name a, Name b)
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
			if(obj is Name)
			{
				Name other=obj as Name;

				if(other==null)
				{
					other=new Name(obj as string);
				}

				if(other==null)
					return false;

				return FullName.Equals(other.FullName, StringComparison.CurrentCultureIgnoreCase);
			}
			return false;
		}

		/// <summary>
		/// Serves as a hash function for a particular type.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"/>.
		/// </returns>
		public override int GetHashCode()
		{
			if(hashCode==null)
			{
				hashCode=FullName.ToLower().GetHashCode();
			}
			return hashCode.Value;
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </returns>
		public override string ToString()
		{
			return FullName;
		}
	}
}
