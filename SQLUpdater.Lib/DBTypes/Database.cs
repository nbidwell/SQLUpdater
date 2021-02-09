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
	/// A parsed representation of the scripts to create a database
	/// </summary>
	public class Database
	{
		private DependencyCollection dependencies;

		/// <summary>
		/// Gets or sets the defined constraints.
		/// </summary>
		/// <value>The defined constraints.</value>
		public HashArray<Constraint> Constraints { get; private set; }

		/// <summary>
		/// Gets or sets the fulltext indexes.
		/// </summary>
		/// <value>The fulltext indexes.</value>
		public HashArray<FulltextCatalog> FulltextCatalogs { get; private set; }

		/// <summary>
		/// Gets or sets the fulltext indexes.
		/// </summary>
		/// <value>The fulltext indexes.</value>
		public HashArray<FulltextIndex> FulltextIndexes { get; private set; }

		/// <summary>
		/// Gets or sets the functions.
		/// </summary>
		/// <value>The functions.</value>
		public HashArray<Function> Functions { get; private set; }

		/// <summary>
		/// Gets a value indicating whether this <see cref="Database"/> is empty.
		/// </summary>
		/// <value><c>true</c> if empty; otherwise, <c>false</c>.</value>
		public bool IsEmpty
		{
			get
			{
				return
					Constraints.Count==0
                    && FulltextCatalogs.Count == 0
                    && FulltextIndexes.Count == 0
					&& Functions.Count==0
					&& Procedures.Count==0
					&& Tables.Count==0
					&& Triggers.Count==0
					&& TriggerOrder.Count==0
					&& Views.Count==0;
			}
		}

		/// <summary>
		/// Gets or sets the defined permissions.
		/// </summary>
		/// <value>The defined permissions.</value>
		public PermissionSet Permissions { get; private set; }

		/// <summary>
		/// Gets or sets the procedures.
		/// </summary>
		/// <value>The procedures.</value>
		public HashArray<Procedure> Procedures { get; private set; }

		/// <summary>
		/// Gets or sets the tables.
		/// </summary>
		/// <value>The tables.</value>
		public HashArray<Table> Tables { get; private set; }

		/// <summary>
		/// Gets or sets the triggers.
		/// </summary>
		/// <value>The triggers.</value>
		public HashArray<Trigger> Triggers { get; private set; }

		/// <summary>
		/// Gets or sets the trigger ordering declarations.
		/// </summary>
		/// <value>The trigger order declarations.</value>
		public HashArray<TriggerOrder> TriggerOrder { get; private set; }

		/// <summary>
		/// Gets or sets the views.
		/// </summary>
		/// <value>The views.</value>
		public HashArray<View> Views { get; private set; }

		/// <summary>
		/// Gets the <see cref="SQLUpdater.Lib.DBTypes.Item"/> with the specified name.
		/// </summary>
		/// <value></value>
		public Item this[Name name]
		{
			get
			{
				Item found=(Item)Constraints[name]
                    ?? (Item)FulltextCatalogs[name]
                    ?? (Item)FulltextIndexes[name]
					?? (Item)Functions[name]
					?? (Item)Procedures[name]
					?? (Item)Tables[name]
					?? (Item)Triggers[name]
					?? (Item)TriggerOrder[name]
					?? (Item)Views[name];

				if(found==null)
				{
					foreach(Table table in Tables)
					{
						found=table.Indexes[name];
						if(found!=null)
							break;
					}
				}

				if(found==null)
				{
					foreach(View view in Views)
					{
						found=view.Indexes[name];
						if(found!=null)
							break;
					}
				}

				return found;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Database"/> class.
		/// </summary>
		public Database()
		{
			Constraints=new HashArray<Constraint>();
            FulltextCatalogs = new HashArray<FulltextCatalog>();
            FulltextIndexes = new HashArray<FulltextIndex>();
			Functions=new HashArray<Function>();
			Permissions=new PermissionSet();
			Procedures=new HashArray<Procedure>();
			Tables=new HashArray<Table>();
			Triggers=new HashArray<Trigger>();
			TriggerOrder=new HashArray<TriggerOrder>();
			Views=new HashArray<View>();
		}

		/// <summary>
		/// Calculates the dependencies between all the contained items.
		/// </summary>
		private void CalculateDependencies()
		{
			dependencies=new DependencyCollection();

			foreach(Table table in Tables)
			{
				foreach(Index index in table.Indexes)
				{
					dependencies.Add(table, index);
				}
			}

			foreach(View view in Views)
			{
				foreach(Index index in view.Indexes)
				{
					dependencies.Add(view, index);
				}

				foreach(Name identifier in view.ReferencedItems)
				{
					Item referenced=this[identifier];
					if(referenced!=null)
					{
						dependencies.Add(referenced, view);
					}
				}
			}

			foreach(Constraint constraint in Constraints)
			{
				dependencies.Add(Tables[constraint.ConstrainedTable], constraint);
				if(constraint.ReferencedTable!=null)
				{
					Table referencedTable=Tables[constraint.ReferencedTable];

					if(RunOptions.Current.FullyScripted && referencedTable==null)
					{
						throw new Exception("Table "+constraint.ReferencedTable+" not found.");
					}

					if(referencedTable!=null)
					{
						dependencies.Add(referencedTable, constraint);
						dependencies.Add(referencedTable.Data.Name, constraint);

						if(constraint.Type==ConstraintType.ForeignKey)
						{
							foreach(Index index in referencedTable.Indexes)
							{
								if(index.Unique && ListsEqual(constraint.ReferencedColumns, index.Columns))
								{
									dependencies.Add(index, constraint);
								}
							}
						}
					}
				}
				else if(constraint.Type==ConstraintType.ForeignKey)
				{
						throw new Exception("Table "+constraint.ReferencedTable+" not found");
				}

				if(constraint.Type==ConstraintType.ForeignKey)
				{
					foreach(Constraint otherConstraint in Constraints)
					{
						if(otherConstraint.ConstrainedTable==constraint.ReferencedTable)
						{
							if(otherConstraint.Type==ConstraintType.PrimaryKey
								|| (otherConstraint.Type==ConstraintType.Unique
									&& ListsEqual(constraint.ReferencedColumns, otherConstraint.Columns)
								)
							)
							{
								dependencies.Add(otherConstraint, constraint);
							}
						}
					}
				}
			}

			foreach(Trigger trigger in Triggers)
			{
				Item referenced=(Item)Tables[trigger.ReferencedTable] ?? Views[trigger.ReferencedTable];

				if(RunOptions.Current.FullyScripted && referenced==null)
				{
					throw new Exception("Table "+trigger.ReferencedTable+" not found.");
				}

				if(referenced!=null)
				{
					dependencies.Add(referenced, trigger);
				}

				foreach(Name identifier in trigger.ReferencedItems)
				{
					referenced=this[identifier];
					if(referenced!=null)
					{
						dependencies.Add(referenced, trigger);
					}
				}
			}

			foreach(TriggerOrder order in TriggerOrder)
			{
				Item referenced=Triggers[order.Trigger];

				if(referenced!=null)
				{
					dependencies.Add(referenced, referenced);
				}
			}

			foreach(Function function in Functions)
			{
				foreach(Name identifier in function.ReferencedItems)
				{
					Item referenced=this[identifier];
					if(referenced!=null)
					{
						dependencies.Add(referenced, function);
					}
				}
			}

			foreach(FulltextIndex index in FulltextIndexes)
			{
                if (index.FulltextCatalog != null)
                {
                    Item catalog = FulltextCatalogs[index.FulltextCatalog];
                    if (catalog != null)
                    {
                        dependencies.Add(catalog, index);
                    }
                }

				Item referenced=(Item)Tables[index.Table] ?? Views[index.Table];

				if(referenced==null)
				{
					if(RunOptions.Current.FullyScripted)
					{
						throw new Exception("Table or view "+index.Name+" not found");
					}
					else
					{
						continue;
					}
				}

				string indexName=index.Table.Unescaped+"."+index.KeyIndex.Unescaped;
				Index keyIndex=referenced is Table ? ((Table)referenced).Indexes[indexName]
					: ((View)referenced).Indexes[indexName];

                if (keyIndex != null)
                {
                    dependencies.Add(keyIndex, index);
                }
                else
                {
                    Name constraintName = index.Name.Owner + "." + index.KeyIndex;
                    Constraint keyConstraint = Constraints[constraintName];

                    if (keyConstraint != null)
                    {
                        dependencies.Add(keyConstraint, index);
                    }
                    else if (RunOptions.Current.FullyScripted)
                    {
                        throw new Exception("Table or view " + referenced.Name + " does not have index " + index.KeyIndex);
                    }
                }
			}

			foreach(Procedure procedure in Procedures)
			{
				foreach(Name identifier in procedure.ReferencedItems)
				{
					Item referenced=this[identifier];
					if(referenced!=null)
					{
						dependencies.Add(referenced, procedure);
					}
				}
			}
		}

		/// <summary>
		/// Copies the contents of this database to another.
		/// </summary>
		/// <param name="sink">The database to copy to.</param>
		public void CopyTo(Database sink)
		{
			Constraints.CopyTo(sink.Constraints);
            FulltextCatalogs.CopyTo(sink.FulltextCatalogs);
            FulltextIndexes.CopyTo(sink.FulltextIndexes);
			Functions.CopyTo(sink.Functions);
			Permissions.CopyTo(sink.Permissions);
			Procedures.CopyTo(sink.Procedures);
			Tables.CopyTo(sink.Tables);
			Triggers.CopyTo(sink.Triggers);
			TriggerOrder.CopyTo(sink.TriggerOrder);
			Views.CopyTo(sink.Views);
		}

		/// <summary>
		/// Generates the scripts required to turn another <see cref="Database"/> into a copy of this one.
		/// </summary>
		/// <param name="other">Another <see cref="Database"/>.</param>
		/// <returns></returns>
		public ScriptSet CreateDiffScripts(Database other)
		{
			return CreateDiffScripts(other, GetDifferences(other));
		}

		/// <summary>
		/// Generates the scripts required to turn another <see cref="Database"/> into a copy of this one.
		/// </summary>
		/// <param name="other">Another <see cref="Database"/>.</param>
		/// <param name="differences">The differences between these databases.</param>
		/// <returns></returns>
		public ScriptSet CreateDiffScripts(Database other, DifferenceSet differences)
		{
			ScriptSet scripts=new ScriptSet();

			foreach(Difference difference in differences)
			{
				Item adding=null;
				Item removing=null;
				switch(difference.DifferenceType)
				{
					case DifferenceType.Created:
						adding=this[difference.Item];
						scripts.Add(adding.GenerateCreateScript());
						scripts.Add(Permissions.GenerateCreateScript(adding));
						break;

					case DifferenceType.CreatedPermission:
						adding=this[difference.Item];
						scripts.Add(Permissions.GenerateCreateScript(adding==null ? other[difference.Item] : adding));
						break;

					case DifferenceType.Dependency:
					case DifferenceType.Modified:
						adding=this[difference.Item];
						removing=other[difference.Item];
                        if (adding is Table && !((Table)adding).IsType)
                        {
                            scripts.Add(((Table)removing).GenerateSaveScript());
                            scripts.Add(((Table)adding).GenerateCreateScript());
                            scripts.Add(((Table)adding).GenerateRestoreScript(((Table)removing)));
                        }
                        else
                        {
                            scripts.Add(removing.GenerateDropScript());
                            if (adding != null)
                            {
                                scripts.Add(adding.GenerateCreateScript());
                            }
                        }

                        if (adding != null)
                        {
                            scripts.Add(Permissions.GenerateCreateScript(adding));
                        }
						break;

					case DifferenceType.Removed:
						removing=other[difference.Item];
						if(removing is Table)
						{
							if(RunOptions.Current.FullyScripted)
								scripts.Add(removing.GenerateDropScript());
						}
						else
						{
							scripts.Add(removing.GenerateDropScript());
						}
						break;

					case DifferenceType.TableData:
						TableData newData=Tables[difference.ParentItem].Data;
						Table oldTable=other.Tables[difference.ParentItem];
						TableData oldData= oldTable==null ? null : oldTable.Data;
						scripts.Add(newData.GetDifferenceScript(oldData, difference.ParentItem));
						break;

					default:
						throw new ApplicationException(difference.DifferenceType.ToString());
				}
			}

			return scripts;
		}

        /// <summary>
        /// Generate create scripts for all elements in the database
        /// </summary>
        /// <returns></returns>
        public ScriptSet CreateScripts()
		{
            ScriptSet scripts = new ScriptSet();

			foreach(Table table in Tables)
			{
                if (RunOptions.Current.WriteObjects.Count > 0 && !RunOptions.Current.WriteObjects.Contains(table.Name))
                    continue;

                Script tableScript=table.GenerateCreateScript();
                scripts.Add(tableScript);

                foreach (Index index in table.Indexes)
                {
                    tableScript.Append(index.GenerateCreateScript());
                }

                tableScript.Append(table.Permissions.GenerateCreateScript(table));

                foreach(Constraint constraint in Constraints)
                {
                    if(constraint.ConstrainedTable==table.Name)
                    {
                        tableScript.Append(constraint.GenerateCreateScript());
                    }
                }

                foreach(FulltextIndex index in FulltextIndexes)
                {
                    if(index.Table==table.Name)
                    {
                        tableScript.Append(index.GenerateCreateScript());
                    }
                }

                if (table.Data.Count > 0)
                {
                    scripts.Add(table.Data.GetDifferenceScript(null, table.Name));
                }
			}

			foreach(View view in Views)
            {
                if (RunOptions.Current.WriteObjects.Count > 0 && !RunOptions.Current.WriteObjects.Contains(view.Name))
                    continue;

                Script viewScript=view.GenerateCreateScript();
                scripts.Add(viewScript);

                foreach (Index index in view.Indexes)
                {
                    viewScript.Append(index.GenerateCreateScript());
                }

                viewScript.Append(view.Permissions.GenerateCreateScript(view));

                foreach(Constraint constraint in Constraints)
                {
                    if(constraint.ConstrainedTable==view.Name)
                    {
                        viewScript.Append(constraint.GenerateCreateScript());
                    }
                }

                foreach(FulltextIndex index in FulltextIndexes)
                {
                    if(index.Table==view.Name)
                    {
                        viewScript.Append(index.GenerateCreateScript());
                    }
                }
			}

            foreach (Function function in Functions)
            {
                if (RunOptions.Current.WriteObjects.Count > 0 && !RunOptions.Current.WriteObjects.Contains(function.Name))
                    continue;

                Script functionScript = function.GenerateCreateScript();
                scripts.Add(functionScript);

                functionScript.Append(function.Permissions.GenerateCreateScript(function));
            }

            foreach (Procedure procedure in Procedures)
            {
                if (RunOptions.Current.WriteObjects.Count > 0 && !RunOptions.Current.WriteObjects.Contains(procedure.Name))
                    continue;

                Script procedureScript = procedure.GenerateCreateScript();
                scripts.Add(procedureScript);

                procedureScript.Append(procedure.Permissions.GenerateCreateScript(procedure));
            }

            foreach (Trigger trigger in Triggers)
            {
                if (RunOptions.Current.WriteObjects.Count > 0 && !RunOptions.Current.WriteObjects.Contains(trigger.Name))
                    continue;

                Script triggerScript = trigger.GenerateCreateScript();
                scripts.Add(triggerScript);

                triggerScript.Append(trigger.Permissions.GenerateCreateScript(trigger));

                foreach (TriggerOrder order in TriggerOrder)
                {
                    if (order.Trigger == trigger.Name)
                    {
                        triggerScript.Append(order.GenerateCreateScript());
                    }
                }
            }

            foreach (FulltextCatalog catalog in FulltextCatalogs)
            {
                if (RunOptions.Current.WriteObjects.Count > 0 && !RunOptions.Current.WriteObjects.Contains(catalog.Name))
                    continue;

                Script catalogScript = catalog.GenerateCreateScript();
                scripts.Add(catalogScript);
            }

            return scripts;
		}

		/// <summary>
		/// Gets the differences between this database and another.
		/// </summary>
		/// <param name="other">The other database.</param>
		/// <returns></returns>
		public DifferenceSet GetDifferences(Database other)
		{
			CalculateDependencies();
			other.CalculateDependencies();

			DifferenceSet differences=new DifferenceSet();

			//check table differences
			Tables.GetDifferences(other.Tables, dependencies, other.dependencies, differences);

			foreach(Table table in Tables)
			{
				Table otherTable=(Table)other.Tables[table.Name];
				table.Indexes.GetDifferences(otherTable==null ? null : otherTable.Indexes, dependencies, other.dependencies,
					differences);
				if(!table.Data.AreEqual(otherTable==null ? null : otherTable.Data))
				{
					differences.Add(new Difference(DifferenceType.TableData, table.Data.Name, table.Name),
						otherTable==null ? null : dependencies);
				}
			}
            foreach (Table otherTable in other.Tables)
            {
                Table table = (Table)Tables[otherTable.Name];
                if (table == null)
                {
                    foreach (Index index in otherTable.Indexes)
                    {
                        differences.Add(new Difference(DifferenceType.Removed, index.Name, otherTable.Name), other.dependencies);
                    }
                }
            }

			//check constraint differences
			Constraints.GetDifferences(other.Constraints, dependencies, other.dependencies, differences);

			//deal with changing names...
			foreach(Constraint constraint in Constraints)
			{
				Table constrainedTable=other.Tables[constraint.ConstrainedTable];
				if(constrainedTable==null)
					continue;

				if(constraint.Type==ConstraintType.Default)
				{
					foreach(Name dependency in other.dependencies[constrainedTable.Name])
					{
						Constraint otherConstraint=other[dependency] as Constraint;
						if(otherConstraint!=null
							&& otherConstraint.Type==ConstraintType.Default
							&& constraint.ConstrainedTable==otherConstraint.ConstrainedTable
							&& constraint.ColumnsEqual(otherConstraint)
							&& constraint!=otherConstraint
							)
						{
							differences.Add(new Difference(DifferenceType.Removed, otherConstraint.Name), other.dependencies);
							break;
						}
					}
				}
				else if(constraint.Type==ConstraintType.ForeignKey && other.Tables[constraint.ConstrainedTable]!=null)
				{
					foreach(Name dependency in other.dependencies[constrainedTable.Name])
					{
						Constraint otherConstraint=other[dependency] as Constraint;
						if(otherConstraint!=null
							&& otherConstraint.Type==ConstraintType.ForeignKey
							&& constraint.ConstrainedTable==otherConstraint.ConstrainedTable
							&& constraint.ReferencedTable==otherConstraint.ReferencedTable
							&& constraint.ColumnsEqual(otherConstraint)
							&& constraint!=otherConstraint
							)
						{
							differences.Add(new Difference(DifferenceType.Removed, otherConstraint.Name), other.dependencies);
							break;
						}
					}
				}
				else if(constraint.Type==ConstraintType.PrimaryKey && other.Tables[constraint.ConstrainedTable]!=null)
				{
					foreach(Name dependency in other.dependencies[constrainedTable.Name])
					{
						Constraint otherConstraint=other[dependency] as Constraint;
						if(otherConstraint!=null
							&& otherConstraint.Type==ConstraintType.PrimaryKey
							&& constraint.ConstrainedTable==otherConstraint.ConstrainedTable
							&& constraint!=otherConstraint
							)
						{
							differences.Add(new Difference(DifferenceType.Removed, otherConstraint.Name), other.dependencies);
							break;
						}
					}
				}

			}

			//check function differences
			Functions.GetDifferences(other.Functions, dependencies, other.dependencies, differences);

			//check fulltext catalog differences
			FulltextCatalogs.GetDifferences(other.FulltextCatalogs, dependencies, other.dependencies, differences);

			//check fulltext index differences
			FulltextIndexes.GetDifferences(other.FulltextIndexes, dependencies, other.dependencies, differences);

			//check procedure differences
			Procedures.GetDifferences(other.Procedures, dependencies, other.dependencies, differences);

			//check trigger differences
			Triggers.GetDifferences(other.Triggers, dependencies, other.dependencies, differences);

			//check trigger order differences
			TriggerOrder.GetDifferences(other.TriggerOrder, dependencies, other.dependencies, differences);

			//check view differences
			Views.GetDifferences(other.Views, dependencies, other.dependencies, differences);

			foreach(View view in Views)
			{
				View otherView=(View)other.Views[view.Name];
				view.Indexes.GetDifferences(otherView==null ? null : otherView.Indexes, dependencies, other.dependencies,
					differences);
			}

			foreach(View view in Views)
			{
				View otherView=(View)other.Views[view.Name];
				if(otherView!=null)
				{
					view.Indexes.GetDifferences(otherView.Indexes, dependencies, other.dependencies, differences);
				}
			}

            //check for new permissions
            Permissions.GetDifferences(other.Permissions, differences);

			return differences;
		}

		/// <summary>
		/// Gets a list names of all the <see cref="Table"/>s with scripted data.
		/// </summary>
		/// <returns>A list of <see cref="Name"/>s</returns>
		public List<Name> GetTablesWithData()
		{
			List<Name> retVal=new List<Name>();
			foreach(Table table in Tables)
			{
				if(table.Data.Count>0)
					retVal.Add(table.Name);
			}
			return retVal;
		}

		/// <summary>
		/// Do two lists contain the same set of names?
		/// </summary>
		/// <param name="list">One list of names.</param>
		/// <param name="list1">Another list of names.</param>
		/// <returns><c>true</c> if the two lists contain the same set of names; otherwise, <c>false</c>.</returns>
		private bool ListsEqual(List<SmallName> list, List<SmallName> list1)
		{
			if(list.Count!=list1.Count)
				return false;

			foreach(SmallName name in list)
			{
				if(!list1.Contains(name))
					return false;
			}

			return true;
		}
	}
}
