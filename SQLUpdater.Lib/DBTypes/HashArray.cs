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
using System.Collections;
using System.Collections.Generic;

namespace SQLUpdater.Lib.DBTypes
{
	/// <summary>
	/// An array that is addressable by keys as well
	/// </summary>
	public class HashArray<T> : IEnumerable where T : Item
	{
		private List<T> array=new List<T>();
		private Dictionary<Name, T> hash=new Dictionary<Name, T>();

		/// <summary>
		/// Gets the number of elements contained.
		/// </summary>
		/// <value>The number of elements contained.</value>
		public int Count
		{
			get { return array.Count; }
		}

		/// <summary>
		/// Gets the item at the specified index.
		/// </summary>
		/// <value></value>
		public T this[int index]
		{
			get { return array[index]; }
		}

		/// <summary>
		/// Gets the item with the specified name.
		/// </summary>
		/// <value></value>
		public T this[Name name]
		{
			get { return hash.ContainsKey(name) ? hash[name] : null; }
		}

		/// <summary>
		/// Gets the item with the specified name.
		/// </summary>
		/// <value></value>
		public T this[string name]
		{
			get { return this[(Name)name]; }
		}

		/// <summary>
		/// Adds the specified item.
		/// </summary>
		/// <param name="val">The item.</param>
		public void Add(T val)
		{
			try
			{
				array.Add(val);
				hash.Add(val.Name, val);
			}
			catch(ArgumentException e)
			{
				throw new ApplicationException(e.Message+" ("+val.Name+")", e);
			}
		}

		/// <summary>
		/// Copies all the elements to another HashArray.
		/// </summary>
		/// <param name="sink">The HashArray to fill.</param>
		public void CopyTo(HashArray<T> sink)
		{
			foreach(T item in array)
				sink.Add(item);
		}

		/// <summary>
		/// Gets the differences between this collection of items and another.
		/// </summary>
		/// <param name="other">The set of items to compare against.</param>
		/// <param name="dependencies">The dependencies for this collection.</param>
		/// <param name="otherDependencies">The dependencies for the other collection.</param>
		/// <param name="differences">The set of differences to fill.</param>
		public void GetDifferences(HashArray<T> other, DependencyCollection dependencies, DependencyCollection otherDependencies,
			DifferenceSet differences)
		{
			foreach(Item item in this)
			{
				Item otherItem=other==null ? null : other[item.Name];
				Difference difference=item.GetDifferences(otherItem, true);
				if(difference!=null)
				{
					differences.Add(difference, dependencies, otherDependencies);
				}
			}

			if(RunOptions.Current.FullyScripted && other!=null)
			{
				foreach(Item otherItem in other)
				{
					if(this[otherItem.Name]==null)
					{
						differences.Add(new Difference(DifferenceType.Removed, otherItem.Name), otherDependencies);
					}
				}
			}
		}

		/// <summary>
		/// Returns an enumerator that iterates through a collection.
		/// </summary>
		/// <returns>
		/// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
		/// </returns>
		public IEnumerator GetEnumerator()
		{
			return array.GetEnumerator();
		}

		/// <summary>
		/// Removes the item with the specified name.
		/// </summary>
		/// <param name="name">The item name to remove.</param>
		public void Remove(Name name)
		{
			T removing=this[name];
			if(removing!=null)
			{
				Remove(removing);
			}
		}

		/// <summary>
		/// Removes the specified item.
		/// </summary>
		/// <param name="val">The item.</param>
		public void Remove(T val)
		{
			array.Remove(val);
			hash.Remove(val.Name);
		}
	}
}
