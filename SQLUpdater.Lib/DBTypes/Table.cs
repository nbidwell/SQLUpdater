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
	/// Parsed representation of a table
	/// </summary>
	public class Table : Item
	{
		/// <summary>
		/// Gets or sets the table columns.
		/// </summary>
		/// <value>The table columns.</value>
		public HashArray<Column> Columns { get; private set; }

		/// <summary>
		/// Gets or sets the inline costraints.
		/// </summary>
		/// <value>The constraints.</value>
		public HashArray<Constraint> Constraints { get; private set; }

		/// <summary>
		/// Gets or sets the scripted data.
		/// </summary>
		/// <value>The scripted data.</value>
		public TableData Data {get; private set;}

		/// <summary>
		/// The file group.
		/// </summary>
		public SmallName FileGroup;

		/// <summary>
		/// Gets or sets the indexes on this view.
		/// </summary>
		/// <value>The indexes on this view.</value>
		public HashArray<Index> Indexes { get; private set; }

        /// <summary>
        /// A table type instead of a direct table
        /// </summary>
        public bool IsType { get; set; }

		/// <summary>
		/// The text file group.
		/// </summary>
		public SmallName TextFileGroup;

		/// <summary>
		/// Initializes a new instance of the <see cref="Table"/> class.
		/// </summary>
		/// <param name="name">The table name.</param>
		public Table(string name) : base(name)
		{
			Columns=new HashArray<Column>();
			Constraints=new HashArray<Constraint>();
			Data=new TableData(((Name)name).Unescaped+"___Data");
			Indexes=new HashArray<Index>();
		}

		/// <summary>
		/// Generates a create script.
		/// </summary>
		/// <returns></returns>
		public override Script GenerateCreateScript()
		{
			StringBuilder output=new StringBuilder();

            output.Append("CREATE " + (IsType ? "TYPE " : "TABLE ") + Name + (IsType ? " AS TABLE" : "") + "(\r\n");
			foreach(Column column in Columns)
			{
				output.Append("\t"+column.GenerateCreateScript().Text);
				if(column!=Columns[Columns.Count-1] || Constraints.Count>0)
				{
					output.Append(",");
				}
				output.Append("\r\n");
			}
			foreach (Constraint constraint in Constraints)
			{
				output.Append(constraint.GenerateInlineCreateScript());
				if (constraint != Constraints[Constraints.Count - 1])
				{
					output.Append(",");
				}
				output.Append("\r\n");
			}
			output.Append(")");
			if(FileGroup!=null)
			{
				output.Append(" ON "+FileGroup);
			}
			if(TextFileGroup!=null)
			{
				output.Append(" TEXTIMAGE_ON "+TextFileGroup);
			}

			output.Append("\r\n\r\nGO");

			return new Script(output.ToString(), Name, IsType ? ScriptType.TableType : ScriptType.Table);
		}

		/// <summary>
		/// Generates a drop script.
		/// </summary>
		/// <returns></returns>
        public override Script GenerateDropScript()
        {
            StringBuilder output = new StringBuilder();
            output.Append("DROP " + (IsType ? "TYPE " : "TABLE ") + Name);
            return new Script(output.ToString(), Name, IsType ? ScriptType.DropTableType : ScriptType.DropTable);
        }

		/// <summary>
		/// Generates a restore script.
		/// </summary>
		/// <param name="oldVersion">The older version of the table.</param>
		/// <returns></returns>
		public Script GenerateRestoreScript(Table oldVersion)
		{
			StringBuilder output=new StringBuilder();

			bool identity=false;
			Dictionary<SmallName, int?> columnMap=new Dictionary<SmallName, int?>(Columns.Count);

			//Match up columns of the same name
			foreach(Column column in Columns)
			{
				if(oldVersion.Columns[column.Name]==null)
					continue;

				if(column.Identity)
					identity=true;

				for(int i=0; i<oldVersion.Columns.Count; i++)
				{
					if(column.Name==oldVersion.Columns[i].Name)
					{
						columnMap[column.Name]=i;
					}
				}
			}

			//Try and handle renamed columns
			//This isn't perfect, but is should at least preserve as much data as possible
			foreach(Column column in Columns)
			{
				if(columnMap.ContainsKey(column.Name))
					continue;

				//Match unmatched columns up with the first column of the same type
				for(int i=0; i<oldVersion.Columns.Count; i++)
				{
					if(columnMap.ContainsValue(i) || column.Type!=oldVersion.Columns[i].Type)
						continue;

					if(column.Identity)
						identity=true;

					columnMap[column.Name]=i;
				}
			}

			if(identity)
			{
				output.Append("SET IDENTITY_INSERT "+Name.FullName+" ON\r\nGO\r\n\r\n");
			}

			StringBuilder insertList=new StringBuilder();
			StringBuilder selectList=new StringBuilder();
            for (int i = 0; i < Columns.Count; i++)
            {
                Column column = Columns[i];
                int? index = columnMap.ContainsKey(column.Name) ? columnMap[column.Name] : null;
                Column oldColumn = index == null ? null : oldVersion.Columns[index.Value];

                if (oldColumn == null || column.Type == "timestamp")
                    continue;

                insertList.Append("\r\n\t" + column.Name + ",");
                string valueText = oldColumn.Name;
                if (column.Type != oldColumn.Type)
                {
                    valueText = "CAST(" + valueText + " AS " + column.Type;
                    if (column.Size != null && column.Size != "")
                    {
                        valueText = valueText + "(" + column.Size + ")";
                    }
                    valueText = valueText + ")";
                }
                if (!string.IsNullOrEmpty(column.Default))
                {
                    valueText = "ISNULL(" + valueText + ", " + column.Default + ")";
                }
                selectList.Append("\r\n\t" + valueText + ",");
            }

			//SQL to fill the new table
			if(insertList.Length>0)
			{
				//remove extra commas
				insertList.Remove(insertList.Length-1, 1);
				selectList.Remove(selectList.Length-1, 1);

				//generate select statement
				output.Append("INSERT INTO "+Name.FullName+" (");
				output.Append(insertList.ToString());
				output.Append("\r\n)\r\nSELECT");
				output.Append(selectList.ToString());
				output.Append("\r\nFROM "+oldVersion.Name.BackupName+" WITH (HOLDLOCK TABLOCKX)");
                output.Append("\r\n\r\n");
			}

			//Drop the old copy
			output.Append("DROP TABLE "+oldVersion.Name.BackupName);
            output.Append("\r\n\r\nGO\r\n\r\n");

			if(identity)
			{
				output.Append("SET IDENTITY_INSERT "+Name.FullName+" OFF\r\nGO\r\n\r\n");
			}

			return new Script(output.ToString(), Name, ScriptType.TableRestoreData);
		}

		/// <summary>
		/// Generates a save script.
		/// </summary>
		/// <returns></returns>
		public Script GenerateSaveScript()
		{
			StringBuilder output=new StringBuilder();

			//don't make dbo literal...  stupid SQL Server bug in sp_rename
			string newName=Name.BackupName.Object.Unescaped;

			output.Append("EXEC sp_rename '");
			output.Append(Name.FullName);
			output.Append("', '");
			output.Append(newName);
			output.Append("', 'OBJECT'");

			return new Script(output.ToString(), Name, ScriptType.TableSaveData);
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
			Table otherTable=other as Table;
			if(otherTable==null)
				return new Difference(DifferenceType.Created, Name);

			Difference difference=new Difference(DifferenceType.Modified, Name);
			if(Name!=otherTable.Name)
			{
				difference.AddMessage("Name", otherTable.Name, Name);
				if(!allDifferences)
					return difference;
			}
			if(FileGroup!=null && FileGroup!=otherTable.FileGroup 
				&& !PrimaryFileGroups(FileGroup, otherTable.FileGroup))
			{
				difference.AddMessage("File group", otherTable.FileGroup, FileGroup);
				if(!allDifferences)
					return difference;
			}
			if(TextFileGroup!=null && TextFileGroup!=otherTable.TextFileGroup 
				&& !PrimaryFileGroups(TextFileGroup, otherTable.TextFileGroup))
			{
				difference.AddMessage("Text File Group",
						otherTable.TextFileGroup==null ? "none" : (string)otherTable.TextFileGroup,
						TextFileGroup);
				if(!allDifferences)
					return difference;
			}

			if(Columns.Count!=otherTable.Columns.Count)
			{
				difference.AddMessage("Column Count", otherTable.Columns.Count, Columns.Count);
				if(!allDifferences)
					return difference;
			}
			else
			{
				for(int i=0; i<Columns.Count; i++)
				{
					Difference columnDifference=Columns[i].GetDifferences(otherTable.Columns[i], allDifferences);
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
