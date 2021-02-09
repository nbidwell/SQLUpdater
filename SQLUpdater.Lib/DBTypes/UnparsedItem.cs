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
using System.Linq;
using System.Text;

namespace SQLUpdater.Lib.DBTypes
{
	/// <summary>
	/// An item that we can't parse and then reconstruct
	/// </summary>
	public abstract class UnparsedItem : Item
	{
		/// <summary>
		/// The item definition
		/// </summary>
		public string Body { get; private set; }

		/// <summary>
		/// Gets or sets the list of items referenced by this one.
		/// </summary>
		/// <value>The list of items referenced by this one.</value>
		public List<Name> ReferencedItems { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="UnparsedItem"/> class.
		/// </summary>
		/// <param name="name">The item name.</param>
		/// <param name="body">The item definition.</param>
		public UnparsedItem(string name, string body) : base(name)
		{
			Body=body.Trim();
			ReferencedItems=new List<Name>();
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
			UnparsedItem otherItem=other as UnparsedItem;
			if(otherItem==null || this.GetType()!=other.GetType())
				return new Difference(DifferenceType.Created, Name);

			Difference difference=new Difference(DifferenceType.Modified, Name);
			if(Name!=otherItem.Name)
			{
				difference.AddMessage("Name", otherItem.Name, Name);
				if(!allDifferences)
					return difference;
			}
            if (Body != otherItem.Body
                && Body.Replace("[", "").Replace("]", "") != otherItem.Body.Replace("[", "").Replace("]", ""))
			{
				difference.AddMessage("Body is different");
				if(!allDifferences)
					return difference;
			}

			return difference.Messages.Count>0 ? difference : null;
		}
	}
}
