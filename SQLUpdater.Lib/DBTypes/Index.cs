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
	/// Parsed representation of an index creation script
	/// </summary>
	public class Index: Item
	{
		/// <summary>
		/// Gets or sets the directions of the indexed columns.
		/// </summary>
		/// <value>The directions of the indexed columns.</value>
		public List<SmallName> ColumnDirections { get; private set; }

		/// <summary>
		/// Gets or sets the indexed columns.
		/// </summary>
		/// <value>The indexed columns.</value>
		public List<SmallName> Columns { get; private set; }

		/// <summary>
		/// Is this a clustered index?
		/// </summary>
		public bool Clustered;

        /// <summary>
        /// Is the index enabled?
        /// </summary>
        public bool Enabled;

		/// <summary>
		/// File group.
		/// </summary>
		public SmallName FileGroup;

		/// <summary>
		/// Gets or sets the non key columns.
		/// </summary>
		/// <value>The non key columns.</value>
		public List<SmallName> Include { get; private set; }

		/// <summary>
		/// The (non-unique) name of the index.
		/// </summary>
		public SmallName IndexName;

		/// <summary>
		/// Indexed table.
		/// </summary>
		public Name Table;

		/// <summary>
		/// Is this a unique index?
		/// </summary>
		public bool Unique;

		/// <summary>
		/// Where clause.
		/// </summary>
		public string Where;

		/// <summary>
		/// With clause.
		/// </summary>
		public string With;

		/// <summary>
		/// Initializes a new instance of the <see cref="Index"/> class.
		/// </summary>
		/// <param name="name">The index name.</param>
		/// <param name="table">The indexed table.</param>
		/// <param name="clustered">if set to <c>true</c> [clustered].</param>
		/// <param name="unique">if set to <c>true</c> [unique].</param>
		public Index(string name, Name table, bool clustered, bool unique)
			: base(table.Unescaped+"."+((SmallName)name).Unescaped)
		{
			Clustered=clustered;
			ColumnDirections=new List<SmallName>();
			Columns=new List<SmallName>();
            Enabled = true;
			Include=new List<SmallName>();
			IndexName=name;
			Table=table;
			Unique=unique;
		}

		/// <summary>
		/// Generates a create script.
		/// </summary>
		/// <returns></returns>
		public override Script GenerateCreateScript()
		{
			StringBuilder output=new StringBuilder();

			output.Append("CREATE");
			if(Unique)
			{
				output.Append(" UNIQUE");
			}
			if(Clustered)
			{
				output.Append(" CLUSTERED");
			}
			output.Append(" INDEX "+IndexName);
			output.Append("\r\nON "+Table+"(");
			for(int i=0; i<Columns.Count; i++)
			{
				SmallName column=Columns[i];
				SmallName direction=(SmallName)ColumnDirections[i];
				output.Append("\r\n\t"+column);
				if(direction!=null)
				{
					output.Append(" "+direction.Unescaped);
				}
				output.Append(",");
			}
			output.Remove(output.Length-1, 1);
			output.Append("\r\n)\r\n");
            if (Include.Count > 0)
            {
			    output.Append("INCLUDE(");
			    for(int i=0; i<Include.Count; i++)
			    {
				    SmallName column=Include[i];
				    output.Append("\r\n\t"+column);
				    output.Append(",");
			    }
			    output.Remove(output.Length-1, 1);
			    output.Append("\r\n)\r\n");
            }
			if(Where!=null && Where!="")
			{
				output.Append("WHERE "+Where);
				output.Append("\r\n");
			}
			if(With!=null && With!="")
			{
				output.Append("WITH "+With);
				output.Append("\r\n");
			}
			if(FileGroup!=null && ""!=FileGroup.Unescaped)
			{
				if(FileGroup.Unescaped.Contains("("))
					output.Append("ON "+FileGroup.Unescaped);  //HACK
				else
					output.Append("ON "+FileGroup);
                output.Append("\r\n");
			}
            if (!Enabled)
            {
                output.AppendLine();
                output.Append("ALTER INDEX ");
                output.Append(IndexName);
                output.Append(" ON ");
                output.Append(Table);
                output.Append(" DISABLE");
				output.Append("\r\n");
            }

			return new Script(output.ToString(), Name, Clustered ? ScriptType.ClusteredIndex : ScriptType.Index);
		}

		/// <summary>
		/// Generates a drop script.
		/// </summary>
		/// <returns></returns>
		public override Script GenerateDropScript()
		{
			StringBuilder output=new StringBuilder();

			output.Append("DROP INDEX "+IndexName+" ON "+Table+"\r\n");

			return new Script(output.ToString(), Name, Clustered ? ScriptType.DropClusteredIndex : ScriptType.DropIndex);
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
			Index otherIndex=other as Index;
			if(otherIndex==null)
				return new Difference(DifferenceType.Created, Name);

			Difference difference=new Difference(DifferenceType.Modified, Name);
			if(Name!=otherIndex.Name)
			{
				difference.AddMessage("Name", otherIndex.Name, Name);
				if(!allDifferences)
					return difference;
			}
			if(Clustered!=otherIndex.Clustered)
			{
				difference.AddMessage("Clustered", otherIndex.Clustered, Clustered);
				if(!allDifferences)
					return difference;
			}
			if(Enabled!=otherIndex.Enabled)
			{
				difference.AddMessage("Enabled", otherIndex.Enabled, Enabled);
				if(!allDifferences)
					return difference;
			}
			if(FileGroup!=null && FileGroup!=otherIndex.FileGroup 
				&& !PrimaryFileGroups(FileGroup, otherIndex.FileGroup))
			{
				difference.AddMessage("File group", otherIndex.FileGroup, FileGroup);
				if(!allDifferences)
					return difference;
			}
			if(Table!=otherIndex.Table)
			{
				difference.AddMessage("Table", otherIndex.Table, Table);
				if(!allDifferences)
					return difference;
			}
			if(Unique!=otherIndex.Unique)
			{
				difference.AddMessage("Unique", otherIndex.Unique, Unique);
				if(!allDifferences)
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
				for(int i=0; i<Columns.Count; i++)
				{
					if(Columns[i]!=otherIndex.Columns[i])
					{
						difference.AddMessage("Column Difference", otherIndex.Columns[i], Columns[i]);
						if(!allDifferences)
							return difference;
					}
					if(ColumnDirections[i]!=otherIndex.ColumnDirections[i])
					{
						difference.AddMessage("Column Direction Difference",
								otherIndex.Columns[i]+" ("+otherIndex.ColumnDirections[i]+")",
								Columns[i]+" ("+ColumnDirections[i]+")");
						if(!allDifferences)
							return difference;
					}
				}
			}

			if(Include.Count!=otherIndex.Include.Count)
			{
				difference.AddMessage("Include Count", otherIndex.Include.Count, Include.Count);
				if(!allDifferences)
					return difference;
			}
			else
			{
				for(int i=0; i<Include.Count; i++)
				{
					if(Include[i]!=otherIndex.Include[i])
					{
						difference.AddMessage("Include Difference", otherIndex.Include[i], Include[i]);
						if(!allDifferences)
							return difference;
					}
				}
			}

			return difference.Messages.Count>0 ? difference : null;
		}
	}
}
