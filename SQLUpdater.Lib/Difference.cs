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

using SQLUpdater.Lib.DBTypes;
using System;
using System.Collections.Generic;

namespace SQLUpdater.Lib
{
	/// <summary>
	/// A difference found between two Items
	/// </summary>
	public class Difference
	{
		/// <summary>
		/// Gets or sets the type of the difference.
		/// </summary>
		/// <value>The type of the difference.</value>
		public DifferenceType DifferenceType { get; private set; }

		/// <summary>
		/// Gets or sets the item with a difference.
		/// </summary>
		/// <value>The item with a difference.</value>
		public string Item { get; private set; }

		/// <summary>
		/// Gets or sets the messages.
		/// </summary>
		/// <value>The messages.</value>
		public List<string> Messages { get; private set; }

		/// <summary>
		/// Gets or sets the parent item that .
		/// </summary>
		/// <value>The parent item.</value>
		public string ParentItem { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="Difference"/> class.
		/// </summary>
		/// <param name="differenceType">Type of the difference.</param>
		/// <param name="item">The item with a difference.</param>
		public Difference(DifferenceType differenceType, Name item)
			: this(differenceType, item.ToString(), null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Difference"/> class.
		/// </summary>
		/// <param name="differenceType">Type of the difference.</param>
		/// <param name="item">The item with a difference.</param>
		public Difference(DifferenceType differenceType, SmallName item)
			: this(differenceType, item.ToString(), null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Difference"/> class.
		/// </summary>
		/// <param name="differenceType">Type of the difference.</param>
		/// <param name="item">The item with a difference.</param>
		/// <param name="parentItem">The parent item.</param>
		public Difference(DifferenceType differenceType, Name item, Name parentItem)
			: this(differenceType, item.ToString(), parentItem.ToString())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Difference"/> class.
		/// </summary>
		/// <param name="differenceType">Type of the difference.</param>
		/// <param name="item">The item with a difference.</param>
		/// <param name="parentItem">The parent item.</param>
		public Difference(DifferenceType differenceType, string item, string parentItem)
		{
			DifferenceType=differenceType;
			Item=item;
			ParentItem=parentItem;

			Messages=new List<string>();
		}

		/// <summary>
		/// Adds the message.
		/// </summary>
		/// <param name="message">The message.</param>
		public void AddMessage(string message)
		{
			Messages.Add(message);
		}

		/// <summary>
		/// Adds the message.
		/// </summary>
		/// <param name="label">The label.</param>
		/// <param name="oldValue">The old value.</param>
		/// <param name="newValue">The new value.</param>
		public void AddMessage(string label, object oldValue, object newValue)
		{
			AddMessage(label, oldValue.ToString(), newValue.ToString());
		}

		/// <summary>
		/// Adds a message.
		/// </summary>
		/// <param name="label">The label.</param>
		/// <param name="oldValue">The old value.</param>
		/// <param name="newValue">The new value.</param>
		public void AddMessage(string label, string oldValue, string newValue)
		{
			Messages.Add(label+": "+oldValue+" -> "+newValue);
		}

		/// <summary>
		/// Adds the messages from another subordinate difference.
		/// </summary>
		/// <param name="difference">A subordinate difference.</param>
		public void AddMessage(Difference difference)
		{
			AddMessage(difference.Item+": ");
			foreach(string message in difference.Messages)
			{
				AddMessage("\t"+message);
			}
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </returns>
		public override string ToString()
		{
			string retVal=Item+": ";
			switch(DifferenceType)
			{
				case DifferenceType.Created:
				case DifferenceType.Modified:
				case DifferenceType.Removed:
					retVal=retVal+DifferenceType;
					break;

				case DifferenceType.CreatedPermission:
					retVal=retVal+"Permission created for "+Item;
					break;

				case DifferenceType.Dependency:
					retVal=retVal+"Recreated for a dependency on "+ParentItem;
					break;

				case DifferenceType.TableData:
					retVal=ParentItem+": Table Data";
					break;

				default:
					throw new Exception(DifferenceType.ToString());
			}
			foreach(string message in Messages)
				retVal=retVal+"\n\t"+message;
			return retVal;
		}
	}
}
