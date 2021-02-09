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

namespace SQLUpdater.Lib
{
	/// <summary>
	/// A set of database scripts
	/// </summary>
	public class ScriptSet
	{
		List<Script> items=new List<Script>();

		/// <summary>
		/// Gets the number of contained scripts.
		/// </summary>
		/// <value>The number of contained scripts.</value>
		public int Count
		{
			get{ return items.Count; }
		}

		/// <summary>
		/// Gets the <see cref="SQLUpdater.Lib.Script"/> at the specified index.
		/// </summary>
		/// <value></value>
		public Script this [int index]
		{
			get{ return items[index]; }
		}

		/// <summary>
		/// Adds the specified script.
		/// </summary>
		/// <param name="adding">The script.</param>
		public void Add(Script adding)
		{
			if(adding==null)
				return;

			if(items.Contains(adding))
			{
				throw new ApplicationException("Adding duplicate script: "+adding.Name+" ["+adding.Type+"]");
			}

			items.Add(adding);
		}

		/// <summary>
		/// Adds the scripts in the specified set.
		/// </summary>
		/// <param name="adding">The set of scripts.</param>
		public void Add(ScriptSet adding)
		{
			if(adding==null)
				return;

			foreach(Script script in adding)
			{
				Add(script);
			}
		}

		/// <summary>
		/// Gets the enumerator.
		/// </summary>
		/// <returns></returns>
		public IEnumerator GetEnumerator()
		{
			return items.GetEnumerator();
		}

		/// <summary>
		/// Removes the specified script.
		/// </summary>
		/// <param name="removing">The removing.</param>
		public void Remove(Script removing)
		{
			items.Remove(removing);
		}

		/// <summary>
		/// Sorts the scripts in this set.
		/// </summary>
		public void Sort()
		{
			items.Sort(); 

			//the current SQL script ordering mechanism doesn't allow arbitrary comparisons
			//between members of the same script type
			//this method is evil, but should work
			Hashtable cycleBreaker=new Hashtable();
			for(int i=0; i<items.Count; i++)
			{
				for(int j=i+1; j<items.Count && this[i].Type==this[j].Type; j++)
				{
					if(this[j].CompareTo(this[i])<0)
					{
						if(this[i].CompareTo(this[j])<0)
						{
                            RunOptions.Current.Logger.Log("Warning: "+(this[j].FileName ?? this[j].Name)
								+" and "+(this[i].FileName ?? this[i].Name)+" both seem to refer to each other",
                                OutputLevel.Differences);
                            continue;
						}

						string key=this[j].Name.ToString()+"_"+this[i].Name.ToString();
						if(cycleBreaker[key]!=null && ((int)cycleBreaker[key])==i)
						{
							RunOptions.Current.Logger.Log("Script ordering cycle detected on "
								+this[j].Name.ToString()+"("+this[j].Type+") and "
                                +this[i].Name.ToString()+"("+this[i].Type+")", OutputLevel.Errors);
                            return;
						}
						cycleBreaker[key]=i;

						Script tmp=this[j];
						items[j]=items[i];
						items[i]=tmp;
						i--;
						break;
					}
				}
			}
		}
	}
}
