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
	/// A collection of dependencies between database items
	/// </summary>
	public class DependencyCollection
	{
		Dictionary<Name, List<Name>> dependencies=new Dictionary<Name, List<Name>>();

		/// <summary>
		/// Gets the list of <see cref="T:Item&gt;"/>s that depend on the specified item.
		/// </summary>
		/// <value></value>
		public List<Name> this[Name item]
		{
			get { return dependencies.ContainsKey(item) ? dependencies[item] : new List<Name>(); }
		}

		/// <summary>
		/// Adds the specified item.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="dependantItem">An item that depends on the first item.</param>
		public void Add(Item item, Item dependantItem)
		{

			Add(item.Name, dependantItem);
		}

		/// <summary>
		/// Adds the specified item.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="dependantItem">An item that depends on the first item.</param>
        public void Add(Name item, Item dependantItem)
		{
            if (RunOptions.Current.MinimalDependencies
                && (
                    dependantItem is Function
                    || dependantItem is Procedure
                    || dependantItem is View
                    )
                )
            {
                //Constraint, FulltextIndex, Index, and Trigger need to be regenerated with a table or view
                return;
            }

			Add(item, dependantItem.Name);
		}

		/// <summary>
		/// Adds the specified item.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="dependantItem">An item that depends on the first item.</param>
		private void Add(Name item, Name dependantItem)
		{
			if(item==null)
				throw new ArgumentNullException("item");
			if(dependantItem==null)
				throw new ArgumentNullException("dependantItem");

			if(!dependencies.ContainsKey(item))
			{
				dependencies[item]=new List<Name>();
			}

			if(!dependencies[item].Contains(dependantItem))
			{
				dependencies[item].Add(dependantItem);
			}
		}

	}
}
