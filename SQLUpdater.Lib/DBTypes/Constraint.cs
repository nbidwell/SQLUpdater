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
using System.Text;

namespace SQLUpdater.Lib.DBTypes
{
	/// <summary>
	/// Parsed representation of a constraint creation script
	/// </summary>
	public class Constraint : Item
	{
		/// <summary>
		/// Check constraint definition
		/// </summary>
		public string Check;

		/// <summary>
		/// Create a clustered index?
		/// </summary>
		public bool Clustered;

		/// <summary>
		/// Gets or sets the constrained columns.
		/// </summary>
		/// <value>The constrained columns.</value>
		public List<SmallName> Columns { get; private set; }

		/// <summary>
		/// Table the constraint applies to
		/// </summary>
		public Name ConstrainedTable;

		/// <summary>
		/// Value for a default constraint
		/// </summary>
		public string DefaultValue;

		/// <summary>
		/// Filegroup to store indexes on
		/// </summary>
		public string FileGroup;

		/// <summary>
		/// Does this constraint participate in replication?
		/// </summary>
		public bool NotForReplication;

		/// <summary>
		/// What to do with a foreign key on deletion
		/// </summary>
		public string OnDelete;

		/// <summary>
		/// What to do with a foreign key on an update
		/// </summary>
		public string OnUpdate;

		/// <summary>
		/// Gets or sets the referenced table columns in a foreign key.
		/// </summary>
		/// <value>The referenced table columns in a foreign key.</value>
		public List<SmallName> ReferencedColumns { get; private set; }

		/// <summary>
		/// The referenced table for a foreign key
		/// </summary>
		public Name ReferencedTable;

		/// <summary>
		/// Type of constraint
		/// </summary>
		public ConstraintType Type;

		/// <summary>
		/// With Clause
		/// </summary>
		public string With;

		/// <summary>
		/// Initializes a new instance of the <see cref="Constraint"/> class.
		/// </summary>
		/// <param name="name">The constraint name.</param>
		/// <param name="constrainedTable">The constrained table.</param>
		public Constraint(string name, Name constrainedTable) : base(name)
		{
			Clustered=false;
			Columns=new List<SmallName>();
			ConstrainedTable=constrainedTable;
			NotForReplication=false;
			ReferencedColumns=new List<SmallName>();
			Type=ConstraintType.Unknown;
		}

		/// <summary>
		/// Are the columns of this constrain equal to the columns of another constraint?
		/// </summary>
		/// <param name="other">The other constraint.</param>
		/// <returns></returns>
		public bool ColumnsEqual(Constraint other)
		{
			Difference difference=new Difference(DifferenceType.Modified, Name);
			GetColumnDifferences(other, false, difference);
			return difference.Messages.Count==0;
		}

		/// <summary>
		/// Generates a create script.
		/// </summary>
		/// <returns></returns>
		public override Script GenerateCreateScript()
		{
			StringBuilder output=new StringBuilder();
			ScriptType type=ScriptType.DefaultConstraint;

			output.Append("ALTER TABLE "+ConstrainedTable);

			switch(Type)
			{
				case ConstraintType.Check:
					output.Append("\r\nADD CONSTRAINT "+Name.Object+"\r\n");
					output.Append("CHECK "+Check);
					type=ScriptType.CheckConstraint;
					break;

				case ConstraintType.Default:
					output.Append("\r\nADD CONSTRAINT "+Name.Object+"\r\n");
					output.Append("DEFAULT ( "+DefaultValue+" )");
					if(Columns.Count>0)
					{
						output.Append("\r\nFOR ");
						foreach(SmallName column in Columns)
						{
							output.Append(column+", ");
						}
						output.Remove(output.Length-2, 2);
					}
					if(With!=null && With!="")
					{
						output.Append("\r\nWITH "+With);
					}
					type=ScriptType.DefaultConstraint;
					break;

				case ConstraintType.Disable:
					output.Append("\r\nNOCHECK CONSTRAINT "+Check);
					type=ScriptType.DisableConstraint;
					break;

				case ConstraintType.Enable:
					output.Append("\r\nCHECK CONSTRAINT "+Check);
					type=ScriptType.EnableConstraint;
					break;

				case ConstraintType.ForeignKey:
					if(With!=null && With!="")
					{
						output.Append("\r\nWITH "+With);
					}
					output.Append("\r\nADD CONSTRAINT "+Name.Object+"\r\n");
					output.Append("FOREIGN KEY(");
					foreach(SmallName column in Columns)
					{
						output.Append("\r\n\t"+column+",");
					}
					output.Remove(output.Length-1, 1);
					output.Append("\r\n)\r\nREFERENCES "+ReferencedTable+"(");
					foreach(SmallName column in ReferencedColumns)
					{
						output.Append("\r\n\t"+column+",");
					}
					output.Remove(output.Length-1, 1);
					output.Append("\r\n)");

					if(OnDelete!=null &&OnDelete!="")
					{
						output.Append("\r\nON DELETE "+OnDelete);
					}
					if(OnUpdate!=null &&OnUpdate!="")
					{
						output.Append("\r\nON UPDATE "+OnUpdate);
					}
					if(NotForReplication)
					{
						output.Append("\r\nNOT FOR REPLICATION");
					}
					type=ScriptType.ForeignKey;
					break;

				case ConstraintType.PrimaryKey:
					output.Append("\r\nADD CONSTRAINT "+Name.Object+"\r\n");
					output.Append("PRIMARY KEY");
					if(!Clustered)
					{
						output.Append(" NONCLUSTERED");
                    }
					output.Append("(");
					foreach(SmallName column in Columns)
					{
						output.Append("\r\n\t"+column+",");
					}
					output.Remove(output.Length-1, 1);
					output.Append("\r\n)");
					if(With!=null && With!="")
					{
						output.Append("\r\nWITH "+With);
					}
					if(FileGroup!=null && FileGroup!="")
					{
						output.Append("\r\nON "+FileGroup);
					}
					type=ScriptType.PrimaryKey;
					break;

				case ConstraintType.Unique:
					output.Append("\r\nADD CONSTRAINT "+Name.Object+"\r\n");
					output.Append("UNIQUE");
					if(Clustered)
					{
						output.Append(" CLUSTERED");
					}
					output.Append("(");
					foreach(SmallName column in Columns)
					{
						output.Append("\r\n\t"+column+",");
					}
					output.Remove(output.Length-1, 1);
					output.Append("\r\n)");
					if(With!=null && With!="")
					{
						output.Append("\r\nWITH "+With);
					}
					if(FileGroup!=null && FileGroup!="")
					{
						output.Append("\r\nON "+FileGroup);
					}
					type=ScriptType.UniqueConstraint;
					break;

				default:
					throw new Exception("Trying to generate unknown type of constraint");
			}

			return new Script(output.ToString(), Name, type);
		}

		/// <summary>
		/// Generates a drop script.
		/// </summary>
		/// <returns></returns>
		public override Script GenerateDropScript()
		{
			StringBuilder output=new StringBuilder();

			output.Append("ALTER TABLE "+ConstrainedTable+" DROP CONSTRAINT "+Name.Object);

			ScriptType scriptType=ScriptType.DropConstraint;
			switch(Type)
			{
				case ConstraintType.Enable:
				case ConstraintType.Disable:
					return null;

				case ConstraintType.ForeignKey:
					scriptType=ScriptType.DropForeignKey;
					break;

				case ConstraintType.PrimaryKey:
					scriptType=ScriptType.DropPrimaryKey;
					break;

				default:
					scriptType=ScriptType.DropConstraint;
					break;
			}
			return new Script(output.ToString(), Name, scriptType);
		}

		private Difference GetColumnDifferences(Constraint otherConstraint, bool allDifferences, Difference difference)
		{
			if(Columns.Count!=otherConstraint.Columns.Count)
			{
				difference.AddMessage("Column Count", otherConstraint.Columns.Count, Columns.Count);
				if(!allDifferences)
					return difference;
			}
			else
			{
				for(int i=0; i<Columns.Count; i++)
				{
					if(Columns[i]!=otherConstraint.Columns[i])
					{
						difference.AddMessage("Column Difference", otherConstraint.Columns[i], Columns[i]);
						if(!allDifferences)
							return difference;
					}
				}
			}

			if(ReferencedColumns.Count!=otherConstraint.ReferencedColumns.Count)
			{
				difference.AddMessage("Referenced column count", otherConstraint.ReferencedColumns.Count, ReferencedColumns.Count);
				if(!allDifferences)
					return difference;
			}
			else
			{
				for(int i=0; i<ReferencedColumns.Count; i++)
				{
					if(ReferencedColumns[i]!=otherConstraint.ReferencedColumns[i])
					{
						difference.AddMessage("Column Difference", otherConstraint.ReferencedColumns[i], ReferencedColumns[i]);
						if(!allDifferences)
							return difference;
					}
				}
			}

			return difference.Messages.Count>0 ? difference : null;
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
			Constraint otherConstraint=other as Constraint;
			if(otherConstraint==null)
				return new Difference(DifferenceType.Created, Name);

			Difference difference=new Difference(DifferenceType.Modified, Name);
			if(Name!=otherConstraint.Name)
			{
				difference.AddMessage("Name", otherConstraint.Name.Object, Name.Object);
				if(!allDifferences)
					return difference;
			}
			if(Clustered!=otherConstraint.Clustered)
			{
				difference.AddMessage("Clustering", otherConstraint.Clustered, Clustered);
				if(!allDifferences)
					return difference;
			}
			if(Check!=otherConstraint.Check)
			{
				difference.AddMessage("Check", otherConstraint.Check, Check);
				if(!allDifferences)
					return difference;
			}
			if(ConstrainedTable!=otherConstraint.ConstrainedTable)
			{
				difference.AddMessage("Constrained table", otherConstraint.ConstrainedTable, ConstrainedTable);
				if(!allDifferences)
					return difference;
			}
			if((DefaultValue==null ? null : DefaultValue.ToLower())!=(otherConstraint.DefaultValue==null ? null : otherConstraint.DefaultValue.ToLower()))
			{
				difference.AddMessage("Default Value", otherConstraint.DefaultValue, DefaultValue);
				if(!allDifferences)
					return difference;
			}
			if(Type!=otherConstraint.Type)
			{
				difference.AddMessage("Type", otherConstraint.Type.ToString(), Type.ToString());
				if(!allDifferences)
					return difference;
			}
			if(NotForReplication!=otherConstraint.NotForReplication)
			{
				difference.AddMessage("Not for replication", otherConstraint.NotForReplication, NotForReplication);
				if(!allDifferences)
					return difference;
			}

			return GetColumnDifferences(otherConstraint, allDifferences, difference);
		}
	}
}
