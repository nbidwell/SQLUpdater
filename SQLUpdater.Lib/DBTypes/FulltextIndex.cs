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
	/// Parsed representation of a full text index
	/// </summary>
	public class FulltextIndex : Item
	{
		/// <summary>
		/// Change tracking.
		/// </summary>
		public string ChangeTracking;

		/// <summary>
		/// Gets or sets the set of columns to index.
		/// </summary>
		/// <value>The set of columns to index.</value>
        public List<FulltextColumn> Columns { get; private set; }

        /// <summary>
        /// Full text catalog
        /// </summary>
        public SmallName FulltextCatalog;

        /// <summary>
        /// Full text filegroup
        /// </summary>
        public SmallName FulltextFilegroup;

		/// <summary>
		/// Key index.
		/// </summary>
        public SmallName KeyIndex;

        /// <summary>
        /// Stop List.
        /// </summary>
        public string StopList;

		/// <summary>
		/// Indexed table.
		/// </summary>
		public Name Table;

		/// <summary>
		/// Initializes a new instance of the <see cref="FulltextIndex"/> class.
		/// </summary>
		/// <param name="name">The index name.</param>
		public FulltextIndex(string name) : base(name)
		{
			Columns=new List<FulltextColumn>();
		}

		/// <summary>
		/// Generates a create script.
		/// </summary>
		/// <returns></returns>
		public override Script GenerateCreateScript()
		{
			StringBuilder output=new StringBuilder("CREATE FULLTEXT INDEX ON ");
			output.Append(Table);
			output.Append("(");

			foreach(FulltextColumn column in Columns)
			{
				output.Append("\r\n\t");
				output.Append(column);
				output.Append(",");
			}
			output.Remove(output.Length-1, 1);
			output.Append("\r\n)");

			output.Append("\r\nKEY INDEX ");
			output.Append(KeyIndex);

			if(FulltextCatalog!=null || FulltextFilegroup!=null)
			{
                List<string> on = new List<string>();
                if (FulltextCatalog != null)
                    on.Add(FulltextCatalog);
                if (FulltextFilegroup != null)
                    on.Add("FILEGROUP " + FulltextFilegroup);

				output.Append("\r\nON (");
				output.Append(string.Join(", ", on.ToArray()));
                output.Append(")");
			}

			if(ChangeTracking!=null || StopList!=null)
			{
                List<string> with = new List<string>();
                if(ChangeTracking!=null)
                    with.Add("CHANGE_TRACKING "+ChangeTracking);
                if(StopList!=null)
                    with.Add("STOPLIST "+StopList);

                output.Append("\r\nWITH (");
                output.Append(string.Join(", ", with.ToArray()));
                output.Append(")");
			}

			return new Script(output.ToString(), Name, ScriptType.FulltextIndex);
		}

		/// <summary>
		/// Generates a drop script.
		/// </summary>
		/// <returns></returns>
		public override Script GenerateDropScript()
		{
			StringBuilder output=new StringBuilder();

			output.Append("DROP FULLTEXT INDEX ON ");
			output.Append(Table);
			output.Append("\r\n");

			return new Script(output.ToString(), Name, ScriptType.DropFulltextIndex);
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
			FulltextIndex otherIndex=other as FulltextIndex;
			if(otherIndex==null)
				return new Difference(DifferenceType.Created, Name);

			Difference difference=new Difference(DifferenceType.Modified, Name);
			if(Name!=otherIndex.Name)
			{
				difference.AddMessage("Name", otherIndex.Name, Name);
				if(!allDifferences)
					return difference;
			}
			if(Table!=otherIndex.Table)
			{
				difference.AddMessage("Table", otherIndex.Table, Table);
				if(!allDifferences)
					return difference;
			}
			if(ChangeTracking!=otherIndex.ChangeTracking)
			{
				difference.AddMessage("Change Tracking", otherIndex.ChangeTracking, ChangeTracking);
				if(!allDifferences)
					return difference;
			}
			if(FulltextCatalog!=otherIndex.FulltextCatalog && FulltextCatalog!="" && otherIndex.FulltextCatalog!="")
			{
				difference.AddMessage("Fulltext Catalog", otherIndex.FulltextCatalog, FulltextCatalog);
				if(!allDifferences)
					return difference;
            }
            if (FulltextFilegroup!= otherIndex.FulltextFilegroup && FulltextFilegroup != "" && otherIndex.FulltextFilegroup != "")
            {
                difference.AddMessage("Fulltext Filegroup", otherIndex.FulltextFilegroup, FulltextFilegroup);
                if (!allDifferences)
                    return difference;
            }
			if(KeyIndex!=otherIndex.KeyIndex)
			{
				difference.AddMessage("Key Index", otherIndex.KeyIndex, KeyIndex);
				if(!allDifferences)
					return difference;
            }
            if (!EqualWithDefault(StopList, otherIndex.StopList, "system"))
            {
                difference.AddMessage("Stoplist", otherIndex.StopList, StopList);
                if (!allDifferences)
                    return difference;
            }

			if(Columns.Count!=otherIndex.Columns.Count)
			{
				difference.AddMessage("Column Count", otherIndex.Columns.Count, Columns.Count);
				if(!allDifferences)
					return difference;
			}
			else
			{
				//Order is not important for fulltext indexes
				Columns.Sort();
				otherIndex.Columns.Sort();
				for(int i=0; i<Columns.Count; i++)
				{
					Difference columnDifference=Columns[i].GetDifferences(otherIndex.Columns[i], allDifferences);
					if(columnDifference!=null)
					{
						difference.AddMessage(columnDifference);
						if(!allDifferences)
							return difference;
					}
				}
			}

			return difference.Messages.Count>0 ? difference : null;
		}
	}
}
