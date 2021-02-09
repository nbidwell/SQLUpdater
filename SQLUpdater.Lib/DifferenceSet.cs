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
using System.IO;
using System.Text;

namespace SQLUpdater.Lib
{
	/// <summary>
	/// A set of differences found when comparing two databases
	/// </summary>
	public class DifferenceSet : IEnumerable<Difference>
	{
		private Dictionary<Name, Difference> differences=new Dictionary<Name, Difference>();

		/// <summary>
		/// Gets the number of stored differences.
		/// </summary>
		/// <value>The number of stored differences.</value>
		public int Count
		{
			get { return differences.Count; }
		}

		/// <summary>
		/// Adds the specified difference.
		/// </summary>
		/// <param name="difference">The difference.</param>
		/// <param name="dependencies">The dependencies.</param>
		public void Add(Difference difference, DependencyCollection dependencies)
		{
			Add(difference, dependencies, null);
		}

		/// <summary>
		/// Store an item as removed
		/// </summary>
		/// <param name="difference">The difference.</param>
		/// <param name="dependencies">The dependencies.</param>
		/// <param name="otherDependencies">The other dependencies.</param>
		public void Add(Difference difference, DependencyCollection dependencies, DependencyCollection otherDependencies)
		{
			if(differences.ContainsKey(difference.Item))
			{
				//A more accurate difference based on the object itself?
				if(differences[difference.Item].DifferenceType==DifferenceType.Dependency)
					differences[difference.Item]=difference;

				return;
			}

			differences[difference.Item]=difference;

			if(difference.DifferenceType!=DifferenceType.Created)
			{
				if(dependencies!=null)
				{
					foreach(Name child in dependencies[difference.Item])
					{
						CollectDependencies(child, dependencies, difference.Item);
					}
				}

				if(otherDependencies!=null)
				{
					foreach(Name child in dependencies[difference.Item])
					{
						CollectDependencies(child, dependencies, difference.Item);
					}
				}
			}
		}

		/// <summary>
		/// Collect all of an item's dependencies in a set of differences.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="dependencies">The dependencies.</param>
		public void CollectDependencies(Name item, DependencyCollection dependencies)
		{
			if(item==null)
				return;

			foreach(Name dependency in dependencies[item])
			{
				CollectDependencies(dependency, dependencies, item);
			}
		}

		/// <summary>
		/// Collect all of an item's dependencies in a set of differences.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="dependencies">The dependencies.</param>
		/// <param name="parent">The item's parent.</param>
		private void CollectDependencies(Name item, DependencyCollection dependencies, Name parent)
		{
			//Don't overwrite any true differences
			if(differences.ContainsKey(item))
				return;

			differences[item]=new Difference(DifferenceType.Dependency, item, parent);
			foreach(Name child in dependencies[item])
			{
				CollectDependencies(child, dependencies, item);
			}
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
		/// </returns>
		public IEnumerator<Difference> GetEnumerator()
		{
			return differences.Values.GetEnumerator();
		}

		/// <summary>
		/// Returns an enumerator that iterates through a collection.
		/// </summary>
		/// <returns>
		/// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
		/// </returns>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return differences.Values.GetEnumerator();
		}

		/// <summary>
		/// Write out a summary of the differences here
		/// </summary>
		/// <param name="writer">The writer to write to.</param>
		public void Write(TextWriter writer)
		{
			foreach(Difference difference in differences.Values)
			{
				writer.WriteLine(difference.ToString());
			}
		}

	}
}
