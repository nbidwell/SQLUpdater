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
	/// A section of an identifier for an object within the database
	/// </summary>
	public class SmallName
	{
		private string objectName;

		/// <summary>
		/// Gets the name without escape characters.
		/// </summary>
		/// <value>The name without escape characters.</value>
		public string Unescaped
		{
			get
			{
				string item=objectName;
				if(item.StartsWith("["))
				{
					item=item.Remove(0, 1);
				}
				if(item.EndsWith("]"))
				{
					item=item.Remove(item.Length-1, 1);
				}
				return item;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SmallName"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		public SmallName(string name)
		{
			if(name==null || name=="")
				throw new ApplicationException("Empty names are invalid");

			objectName=name;
			while(objectName.StartsWith("."))
			{
				objectName=objectName.Substring(1, objectName.Length-1);
			}
			while(objectName.EndsWith("."))
			{
				objectName=objectName.Substring(0, objectName.Length-1);
			}
			if(!objectName.StartsWith("["))
			{
				objectName="["+objectName+"]";
			}
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="SQLUpdater.Lib.DBTypes.SmallName"/> to <see cref="System.String"/>.
		/// </summary>
		/// <param name="converting">The small name.</param>
		/// <returns>The string value.</returns>
		public static implicit operator string(SmallName converting)
		{
			if(converting==null)
				return null;

			return converting.objectName;
		}

		/// <summary>
		/// Performs an implicit conversion from <see cref="System.String"/> to <see cref="SQLUpdater.Lib.DBTypes.SmallName"/>.
		/// </summary>
		/// <param name="converting">The string value.</param>
		/// <returns>The small name.</returns>
		public static implicit operator SmallName(string converting)
		{
			if(converting==null || converting=="")
				return null;

			return new SmallName(converting);
		}

		/// <summary>
		/// Implements the operator ==.
		/// </summary>
		/// <param name="a">One small name.</param>
		/// <param name="b">Another small name.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator==(SmallName a, SmallName b)
		{
			return (object)a==null ? (object)b==null : a.Equals(b);
		}

		/// <summary>
		/// Implements the operator !=.
		/// </summary>
		/// <param name="a">One small name.</param>
		/// <param name="b">Another small name.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator!=(SmallName a, SmallName b)
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
			if(obj is SmallName)
			{
				SmallName other=obj as SmallName;

				if(other==null)
				{
					other=new SmallName(obj as string);
				}

				if(other==null)
					return false;

				return objectName.ToLower()==other.objectName.ToLower();
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
			return objectName.ToLower().GetHashCode();
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </returns>
		public override string ToString()
		{
			return objectName;
		}

		/*
		private string Unescape(string item)
		{
			if(item.StartsWith("["))
			{
				item=item.Remove(0, 1);
			}
			if(item.EndsWith("]"))
			{
				item=item.Remove(item.Length-1, 1);
			}

			return item;
		}*/
	}
}
