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

namespace SQLUpdater.Lib.DBTypes
{
	/// <summary>
	/// Parses SQL scripts
	/// </summary>
	public class ScriptParser
	{
		/// <summary>
		/// Gets or sets the database being parsed.
		/// </summary>
		/// <value>The database being parsed.</value>
		public Database Database { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ScriptParser"/> class.
		/// </summary>
		public ScriptParser()
		{
			Database=new Database();
		}

		/// <summary>
		/// Asserts an expected token value.
		/// </summary>
		/// <param name="tokenEnumerator">The token enumerator.</param>
		/// <param name="expected">The expected token value.</param>
		private void AssertToken(TokenEnumerator tokenEnumerator, string expected)
		{
			AssertToken(tokenEnumerator.Current, expected);
		}

		/// <summary>
		/// Asserts an expected token value.
		/// </summary>
		/// <param name="token">The token.</param>
		/// <param name="expected">The expected token value.</param>
		private void AssertToken(Token token, string expected)
		{
            if (token.Value != (Name)expected)
            {
                throw new ApplicationException("Unexpected token: " + token + " expected: " + expected);
            }
		}

		private string CleanQuotes(string val)
		{
			while (val.StartsWith("'") && val.EndsWith("'"))
			{
				val = val.Substring(1, val.Length - 2);
			}
			return val;
		}

		/// <summary>
		/// Ensure that the scripted database is consistent.
		/// </summary>
		private void EnsureConsistancy()
		{
			//primary keys can never be null
			foreach(Constraint constraint in Database.Constraints)
			{
				if(constraint.Type!=ConstraintType.PrimaryKey)
					continue;

				Table table=(Table)Database.Tables[constraint.ConstrainedTable];
				foreach(SmallName constrainedName in constraint.Columns)
				{
					Column column=(Column)table.Columns[constrainedName];
					if (column == null)
						throw new ApplicationException("Column " + constrainedName + " does not exist");
					column.Nullable=false;
				}
			}
		}

		/// <summary>
		/// Parses the specified script.
		/// </summary>
		/// <param name="script">The script.</param>
		public void Parse(Script script)
		{
			foreach(string toParse in script.Batches)
			{
				Parse(toParse);
			}
		}

		/// <summary>
		/// Parses the specified script.
		/// </summary>
		/// <param name="script">The script.</param>
		public void Parse(string script)
		{
			try
			{
				if(script==null || script.Trim()=="")
					return;

				//tokenize and parse, separating on go for objects where the original code is necessary
				TokenSet processing=new TokenSet();
				foreach(Token token in Tokenizer.Tokenize(script))
				{
					if(token.Type==TokenType.Keyword && token.Value.ToLower()=="go")
					{
						Parse(processing, script);
						processing=new TokenSet();
						continue;
					}

					processing.Add(token);
				}

				//parse it up
				Parse(processing, script);

				EnsureConsistancy();
			}
			catch(Exception e)
			{
				throw new ApplicationException(e.Message+"\n"+script, e);
			}
		}

		/// <summary>
		/// Parses a set of tokens.
		/// </summary>
		/// <param name="processing">The set of tokens to parse.</param>
		/// <param name="script">The original script.</param>
		private void Parse(TokenSet processing, string script)
		{
			if(processing.Count==0)
				return;

			int startIndex=processing.First.StartIndex;
			int endIndex=processing.Last.EndIndex;
			string scriptSection=script.Substring(startIndex, endIndex-startIndex+1);
			ParseTree(processing, scriptSection);
		}

		/// <summary>
		/// Parses the alter statement.
		/// </summary>
		/// <param name="tokenEnumerator">The token enumerator.</param>
		private void ParseAlterStatement(TokenEnumerator tokenEnumerator)
		{
			tokenEnumerator.MoveNext();
			switch(tokenEnumerator.Current.Value.ToLower())
			{
                case "index":
                    {
                        tokenEnumerator.MoveNext();
                        SmallName indexName = tokenEnumerator.Current.Value;

                        tokenEnumerator.MoveNext();
                        AssertToken(tokenEnumerator.Current, "on");

                        tokenEnumerator.MoveNext();
                        Name tableName = tokenEnumerator.Current.Value;

                        Table table = Database.Tables[tableName];
                        if (table == null)
                            throw new Exception("Table " + tableName + " not found");
                        Index index = table.Indexes[tableName.FullName + "." + indexName];
                        if (index == null)
                            throw new Exception("Index " + indexName + " not found on table " + tableName);

                        tokenEnumerator.MoveNext();
                        switch (tokenEnumerator.Current.Value.ToLower())
                        {
                            case "disable":
                                index.Enabled = false;
                                break;

                            case "enable":
                                index.Enabled = true;
                                break;
                        }

                    }

                    break;

				case "table":
					{
						tokenEnumerator.MoveNext();
						Token tableNameToken=tokenEnumerator.Current;

						string with=null;
						bool repeat=true;
						while(repeat)
						{
							tokenEnumerator.MoveNext();
							repeat=false;
							switch(tokenEnumerator.Current.Value.ToLower())
							{
								case "add":
									do
									{
										ParseAlterTableAddStatement(tokenEnumerator, tableNameToken, with, null, null);

										//handle multiple constraints in one statement
										if(tokenEnumerator.Next!=null && tokenEnumerator.Next.Value==",")
											tokenEnumerator.MoveNext();
									} while(tokenEnumerator.Next!=null && tokenEnumerator.Current.Value==",");

									with=null;
									break;

								case "check":
									ParseAlterTableCheckStatement(tokenEnumerator, tableNameToken);
									break;

								case "enable":
									ParseAlterTableEnableStatement(tokenEnumerator, tableNameToken);
									break;

								case "nocheck":
									ParseAlterTableNocheckStatement(tokenEnumerator, tableNameToken);
									break;

								case "drop":
									ParseAlterTableDropStatement(tokenEnumerator);
									break;

								case "set":
									ParseAlterTableSetStatement(tokenEnumerator, tableNameToken);
									break;

								case "with":
									tokenEnumerator.MoveNext();
									with=tokenEnumerator.Current.Value;
									repeat=true;
									break;

								default:
									throw new ApplicationException("Unexpected token: "+tokenEnumerator.Current);
							}
						}

						break;
					}

				default:
					throw new ApplicationException("Unexpected token: "+tokenEnumerator.Current);
			}
		}

		/// <summary>
		/// Parses the add clause of an alter table statement.
		/// </summary>
		/// <param name="tokenEnumerator">The token enumerator.</param>
		/// <param name="tableNameToken">The table name.</param>
		/// <param name="with">Any with clause.</param>
		/// <param name="columnName">Name of the column.</param>
		/// <param name="expectedName">The expected name.</param>
		private void ParseAlterTableAddStatement(TokenEnumerator tokenEnumerator, Token tableNameToken,
			string with, SmallName columnName, string expectedName)
		{
			string constraintName=expectedName;
			if(tokenEnumerator.Next.Value.ToLower()=="check")
			{
				ParseConstraintStatement(tokenEnumerator, null, tableNameToken, with, columnName);
			}
			else if(tokenEnumerator.Next.Value.ToLower()=="constraint")
			{
				tokenEnumerator.MoveNext(); //pass constraint
				tokenEnumerator.MoveNext();  //to constraint name
				constraintName=tokenEnumerator.Current.FlattenTree();
				ParseConstraintStatement(tokenEnumerator, constraintName, tableNameToken, with, columnName);
			}
			else if(tokenEnumerator.Next.Value.ToLower()=="foreign")
			{
				ParseConstraintStatement(tokenEnumerator, constraintName, tableNameToken, with, columnName);
			}
			else if(tokenEnumerator.Next.Value.ToLower()=="primary")
			{
				ParseConstraintStatement(tokenEnumerator, constraintName, tableNameToken, with, columnName);
			}
			else
			{
				throw new ApplicationException("Unknown token: "+tokenEnumerator.Next.Value);
			}
		}

		/// <summary>
		/// Parses the check clause of an alter table statement.
		/// </summary>
		/// <param name="tokenEnumerator">The token enumerator.</param>
		/// <param name="tableNameToken">The table name token.</param>
		private void ParseAlterTableCheckStatement(TokenEnumerator tokenEnumerator, Token tableNameToken)
		{
			tokenEnumerator.MoveNext();
			if(tokenEnumerator.Current.Value.ToLower()!="constraint")
			{
				throw new ApplicationException("Unknown token: "+tokenEnumerator.Current.Value);
			}
			tokenEnumerator.MoveNext();
			Name tableName=tableNameToken.FlattenTree();
			Constraint adding=new Constraint(tokenEnumerator.Current.FlattenTree(),
				new Name(tableName.Database, tableName.Owner, "EI_"+tableNameToken.Value+"_"+tableName.Object.Unescaped));
			adding.Check=tableName.Object.Unescaped;
			adding.Type=ConstraintType.Enable;

			/* This isn't really a database object
			if(mDatabase.Constraints[adding.Name]==null)
			{
				mDatabase.Constraints.Add(adding);
			}
			 */
		}

		/// <summary>
		/// Parses the drop clause of an alter table statement.
		/// </summary>
		/// <param name="tokenEnumerator">The token enumerator.</param>
		private void ParseAlterTableDropStatement(TokenEnumerator tokenEnumerator)
		{
			tokenEnumerator.MoveNext();
			switch(tokenEnumerator.Current.Value.ToLower())
			{
				case "constraint":
					{
						tokenEnumerator.MoveNext();
						string constraintName=tokenEnumerator.Current.FlattenTree();
						Database.Constraints.Remove(constraintName);

						break;
					}

				default:
					throw new ApplicationException("Unexpected token: "+tokenEnumerator.Current);
			}
        }

        /// <summary>
        /// Parses the enable clause of an alter table statement.
        /// </summary>
        /// <param name="tokenEnumerator">The token enumerator.</param>
        /// <param name="tableNameToken">The table name token.</param>
        private void ParseAlterTableEnableStatement(TokenEnumerator tokenEnumerator, Token tableNameToken)
        {
            tokenEnumerator.MoveNext();
            if (tokenEnumerator.Current.Value.ToLower() != "trigger")
            {
                throw new ApplicationException("Unknown token: " + tokenEnumerator.Current.Value);
            }

            tokenEnumerator.MoveNext();
            Name tableName = tableNameToken.FlattenTree();
			Name triggerName = tokenEnumerator.Current.FlattenTree();

			//Not handling disabled triggers right now, so just verify that it exists
			Trigger trigger = Database.Triggers[triggerName];
			if (trigger == null)
			{
				Name qualifiedName = new Name(tableName.Database, tableName.Owner, triggerName.Object);
                trigger = Database.Triggers[qualifiedName];
            }
			if (trigger == null)
            {
                throw new Exception("Trigger " + triggerName + " not found");
            }
        }

        /// <summary>
        /// Parses the fake set clause of an alter table statement.
        /// </summary>
        /// <param name="tokenEnumerator">The token enumerator.</param>
        /// <param name="tableNameToken">The table name token.</param>
        private void ParseAlterTableSetStatement(TokenEnumerator tokenEnumerator, Token tableNameToken)
		{
			tokenEnumerator.MoveNext();
			switch(tokenEnumerator.Current.Value.ToLower())
			{
				case "textimage_on":
					{
						Name tableName=tableNameToken.FlattenTree();
						Table table=Database.Tables[tableName];
						if(table==null)
						{
							throw new Exception("Table "+tableName+" not found.");
						}

						tokenEnumerator.MoveNext();
						table.TextFileGroup=tokenEnumerator.Current.FlattenTree();

						break;
					}

				default:
					throw new ApplicationException("Unexpected token: "+tokenEnumerator.Current);
			}
		}

		/// <summary>
		/// Parses the nocheck clause of an alter table statement.
		/// </summary>
		/// <param name="tokenEnumerator">The token enumerator.</param>
		/// <param name="tableNameToken">The table name.</param>
		private void ParseAlterTableNocheckStatement(TokenEnumerator tokenEnumerator, Token tableNameToken)
		{
			tokenEnumerator.MoveNext();
			if(tokenEnumerator.Current.Value.ToLower()!="constraint")
			{
				throw new ApplicationException("Unknown token: "+tokenEnumerator.Current.Value);
			}
			tokenEnumerator.MoveNext();
			Name tableName=tableNameToken.FlattenTree();
			Constraint adding=new Constraint(tokenEnumerator.Current.FlattenTree(), 
				new Name(tableName.Database, tableName.Owner, "DI_"+tableNameToken.Value+"_"+tableName.Object.Unescaped));
			adding.Check=tableName.Object.Unescaped;
			adding.Type=ConstraintType.Disable;

			/* This isn't really a database object
			if(mDatabase.Constraints[adding.Name]==null)
			{
				mDatabase.Constraints.Add(adding);
			}
			 */
		}

		/// <summary>
		/// Parses the create statement.
		/// </summary>
		/// <param name="tokenEnumerator">The token enumerator.</param>
		private void ParseCreateStatement(TokenEnumerator tokenEnumerator)
		{
			bool clustered=false;
			bool unique=false;

			bool working=true;
			while(working)
			{
				working=false;
				tokenEnumerator.MoveNext();
				switch(tokenEnumerator.Current.Value.ToLower())
				{
					case "clustered":
						clustered=true;
						working=true;
						break;

					case "fulltext":
                        tokenEnumerator.MoveNext();
                        switch (tokenEnumerator.Current.Value.ToLower())
                        {
                            case "index":
                                ParseCreateFulltextIndexStatement(tokenEnumerator);
                                break;

                            case "catalog":
                                ParseCreateFulltextCatalogStatement(tokenEnumerator);
                                break;

                            default:
                                throw new ApplicationException("Unexpected token: " + tokenEnumerator.Current);
                        }

						break;

					case "index":
						ParseCreateIndexStatement(tokenEnumerator, clustered, unique);
						break;

					case "nonclustered":
						clustered=false;
						working=true;
						break;

                    case "table":
                        tokenEnumerator.MoveNext();
                        Token tableNameToken = tokenEnumerator.Current;
                        Table adding = new Table(tableNameToken.Value);

						ParseCreateTableStatement(tokenEnumerator, adding, tableNameToken);
						break;

                    case "type":
                        tokenEnumerator.MoveNext();
                        Token nameToken = tokenEnumerator.Current;

                        tokenEnumerator.MoveNext();
			            AssertToken(tokenEnumerator.Current, "as");

                        tokenEnumerator.MoveNext();
			            AssertToken(tokenEnumerator.Current, "table");

                        var table = new Table(nameToken.Value);
                        table.IsType = true;
                        ParseCreateTableStatement(tokenEnumerator, table, nameToken);
                        break;

					case "unique":
						unique=true;
						working=true;
						break;

					default:
						throw new ApplicationException("Unexpected token: "+tokenEnumerator.Current);
				}
			}
		}

		/// <summary>
		/// Parses a check constraint statement.
		/// </summary>
		/// <param name="tokenEnumerator">The token enumerator.</param>
		/// <param name="adding">The constraint being parsed.</param>
		private void ParseCheckStatement(TokenEnumerator tokenEnumerator, Constraint adding)
		{
			//Inline constraints come in with columns, but no others do...
			adding.Columns.Clear();

			//make sure the name is right
			if(adding.Name.Object.Unescaped.StartsWith("PK_"))
			{
				adding.Name=new Name(adding.Name.Database, adding.Name.Owner,
					adding.Name.Object.Unescaped.Replace("PK_", "CK_"));
			}

			adding.Type=ConstraintType.Check;
			if(tokenEnumerator.Current.Children.Count>0)
			{
				adding.Check=ParseCheckConstraint(tokenEnumerator.Current.Children.First);
			}
			else
			{
				tokenEnumerator.MoveNext();
				adding.Check=ParseCheckConstraint(tokenEnumerator.Current);
			}
		}

        private string ParseCheckConstraint(Token token)
        {
            token.Children.CleanGrouping(token);

            //something like (a in (b, c, c)) needs to be changed to (a=b OR a=c OR a=d)
            //SQL Server does that to us under the hood and screws up our differences if we don't
            if (token.Type == TokenType.GroupBegin && token.Children.Count > 2
                && token.Children.Second.Value.ToLower() == "in"
                && token.Children.Third.Type == TokenType.GroupBegin)
            {
                SmallName constrained = token.Children.First.Value;
                List<string> optionList = new List<string>();
                foreach (Token option in token.Children.Third.Children)
                {
                    if (option.Type == TokenType.Separator || option.Type == TokenType.GroupEnd)
                        continue;

                    optionList.Add(constrained + " = " + option.Value);
                }
                return "( " + string.Join(" OR ", optionList.ToArray()) + " )";
            }

            return token.FlattenTree(true);
        }

		/// <summary>
		/// Parses a constraint statement.
		/// </summary>
		/// <param name="tokenEnumerator">The token enumerator.</param>
		/// <param name="constraintName">Name of the constraint.</param>
		/// <param name="tableNameToken">The table name.</param>
		/// <param name="with">Any with clause.</param>
		/// <param name="columnName">Name of the column.</param>
		private Constraint ParseConstraintStatement(TokenEnumerator tokenEnumerator, string constraintName,
			Token tableNameToken, string with, SmallName columnName)
		{
			//make sure the constraint has a name
			if(constraintName==null)
			{
				constraintName=tableNameToken.Value;
				int dot=constraintName.LastIndexOf(".");
				if(dot>0)
				{
					constraintName=constraintName.Substring(dot+1, constraintName.Length-dot-1);
				}
				constraintName="PK_"+constraintName.Replace("[", "").Replace("]", "");
			}

			//get basic constraint
			Constraint adding=new Constraint(constraintName, tableNameToken.Value);
			adding.With=with;
			if(columnName!=null)
			{
				adding.Columns.Add(columnName);
			}

            //Make sure it has a reasonable owner
            if (tableNameToken != null)
            {
                Name tableName = tableNameToken.Value;
                if (tableName.Owner != adding.Name.Owner)
                {
                    adding.Name = tableName.Owner + "." + adding.Name.Object;
                }
            }

			//type of constraint
			tokenEnumerator.MoveNext();
			switch(tokenEnumerator.Current.Value.ToLower())
			{
				case "check":
					ParseCheckStatement(tokenEnumerator, adding);
					break;

				case "default":
					ParseDefaultStatement(tokenEnumerator, adding, with);
					break;

				case "foreign":
					ParseForignKeyStatement(tokenEnumerator, adding);
					break;

				case "primary":
					ParsePrimaryKeyStatement(tokenEnumerator, adding);
					break;

				case "unique":
					ParseUniqueStatement(tokenEnumerator, adding);
					break;

				default:
					throw new ApplicationException("Unexpected token: "+tokenEnumerator.Current);
			}

			//deal with name collisions
			string originalName=adding.Name.Object;
			for(int i=1; Database.Constraints[adding.Name]!=null; i++)
			{
				adding.Name=new Name(adding.Name.Database, adding.Name.Owner,
					originalName.Insert(originalName.Length-1, i.ToString()));
			}

			//don't add until name is set in stone
			var constrainedTable = Database.Tables[adding.ConstrainedTable];
			if (constrainedTable != null && constrainedTable.IsType)
			{
				constrainedTable.Constraints.Add(adding);
			}
			else
			{
				Database.Constraints.Add(adding);
			}

            return adding;
		}

		/// <summary>
		/// Parses the create fulltext catalog statement.
		/// </summary>
		/// <param name="tokenEnumerator">The token enumerator.</param>
        private void ParseCreateFulltextCatalogStatement(TokenEnumerator tokenEnumerator)
        {
            tokenEnumerator.MoveNext();
            FulltextCatalog catalog=new FulltextCatalog(((Name)tokenEnumerator.Current.Value).Unescaped);
            Database.FulltextCatalogs.Add(catalog);

            if (tokenEnumerator.Next != null && tokenEnumerator.Next.Value.ToLower() == "with")
            {
                tokenEnumerator.MoveNext();
                tokenEnumerator.MoveNext();
                AssertToken(tokenEnumerator, "=");
                AssertToken(tokenEnumerator.Current.Children.First, "accent_sensitivity");

                catalog.AccentSensitivity = tokenEnumerator.Current.Children.Second.Value;
            }

            if (tokenEnumerator.Next != null && tokenEnumerator.Next.Value.ToLower() == "as")
            {
                tokenEnumerator.MoveNext();
                tokenEnumerator.MoveNext();
                AssertToken(tokenEnumerator, "default");

                catalog.Default = true;
            }

            if (tokenEnumerator.Next != null && tokenEnumerator.Next.Value.ToLower() == "authorization")
            {
                tokenEnumerator.MoveNext();
                tokenEnumerator.MoveNext();

                catalog.Authorization = tokenEnumerator.Current.Value;
            }
        }

		/// <summary>
		/// Parses the create fulltext index statement.
		/// </summary>
		/// <param name="tokenEnumerator">The token enumerator.</param>
		private void ParseCreateFulltextIndexStatement(TokenEnumerator tokenEnumerator)
		{
			//start the index
			tokenEnumerator.MoveNext();
			AssertToken(tokenEnumerator, "on");
			tokenEnumerator.MoveNext();
			FulltextIndex adding=new FulltextIndex(((Name)tokenEnumerator.Current.Value).Unescaped+"_index");
			adding.Table=tokenEnumerator.Current.Value;
			Database.FulltextIndexes.Add(adding);

			//get columns
			if(tokenEnumerator.Current.Children.Count<1)
			{
				throw new ApplicationException("Index columns are missing");
			}
			AssertToken(tokenEnumerator.Current.Children.First, "(");
			TokenEnumerator columnEnumerator=tokenEnumerator.Current.Children.First.Children.GetEnumerator();
			while(columnEnumerator.MoveNext() && columnEnumerator.Current.Type!=TokenType.GroupEnd)
			{
				//get the column name
				FulltextColumn creating=new FulltextColumn(columnEnumerator.Current.FlattenTree());
				adding.Columns.Add(creating);

				//and its language
				if(columnEnumerator.Next!=null && columnEnumerator.Next.Value.ToLower()=="language")
				{
					columnEnumerator.MoveNext();
					columnEnumerator.MoveNext();
					creating.Language=CleanQuotes(columnEnumerator.Current.Value);
				}

				if(columnEnumerator.Next!=null && columnEnumerator.Next.Type==TokenType.Separator)
					columnEnumerator.MoveNext();
			}

			//get metadata
			tokenEnumerator.MoveNext();
			AssertToken(tokenEnumerator, "key");
			tokenEnumerator.MoveNext();
			AssertToken(tokenEnumerator, "index");

			tokenEnumerator.MoveNext();
			adding.KeyIndex=tokenEnumerator.Current.FlattenTree();

			if(tokenEnumerator.Next!=null && tokenEnumerator.Next.Value.ToLower()=="on")
			{
				tokenEnumerator.MoveNext();
				tokenEnumerator.MoveNext();

                if (tokenEnumerator.Current.Value == "(")
                {
                    TokenEnumerator enumerator = tokenEnumerator.Current.Children.GetEnumerator();
                    enumerator.MoveNext();

                    while (enumerator.Current.Value != ")")
                    {
                        switch (enumerator.Current.Value.ToLower())
                        {
                            case "filegroup":
                                enumerator.MoveNext();
                                adding.FulltextFilegroup = enumerator.Current.Value;
                                break;

                            case ",":
                                break;

                            default:
                                adding.FulltextCatalog = enumerator.Current.Value;
                                break;
                        }
                        enumerator.MoveNext();
                    }
                }
                else
                {
                    adding.FulltextCatalog = tokenEnumerator.Current.Value;
                }
			}

			if(tokenEnumerator.Next!=null && tokenEnumerator.Next.Value.ToLower()=="with")
			{
                tokenEnumerator.MoveNext();
                tokenEnumerator.MoveNext();

                TokenEnumerator enumerator = tokenEnumerator;
                bool complete = false;
                while (!complete && enumerator.IsValid)
                {
                    switch(enumerator.Current.Value.ToLower()){
                        case "change_tracking":
                            enumerator.MoveNext();
                            adding.ChangeTracking = enumerator.Current.Value;
                            enumerator.MoveNext();
                            break;

                        case "stoplist":
                            adding.StopList = enumerator.Current.Value;
                            enumerator.MoveNext();
                            break;

                        case "(":
                            enumerator = enumerator.Current.Children.GetEnumerator();
                            enumerator.MoveNext();
                            break;

                        case "=":
                            switch (enumerator.Current.Children.First.Value.ToLower())
                            {
                                case "change_tracking":
                                    adding.ChangeTracking = enumerator.Current.Children.Second.Value;
                                    break;

                                case "stoplist":
                                    adding.StopList = enumerator.Current.Children.Second.Value;
                                    break;

                                default:
                                    throw new ApplicationException("Unexpected token: " + enumerator.Current.Children.First.Value);
                            }
                            enumerator.MoveNext();
                            break;

                        case ",":
                            enumerator.MoveNext();
                            break;

                        default:
                            complete = true;
                            enumerator.MovePrevious();
                            break;
                    }
                }
			}
		}

		/// <summary>
		/// Parses the create index statement.
		/// </summary>
		/// <param name="tokenEnumerator">The token enumerator.</param>
		/// <param name="clustered">A clustered index if set to <c>true</c>.</param>
		/// <param name="unique">A unique index if set to <c>true</c>.</param>
		private void ParseCreateIndexStatement(TokenEnumerator tokenEnumerator, bool clustered, bool unique)
		{
			//start the index
			tokenEnumerator.MoveNext();
			string indexName=tokenEnumerator.Current.Value;
			tokenEnumerator.MoveNext();
			AssertToken(tokenEnumerator, "on");
			tokenEnumerator.MoveNext();
			Index adding=new Index(indexName, tokenEnumerator.Current.Value, clustered, unique);
			if(Database.Tables[adding.Table]!=null)
			{
				Database.Tables[adding.Table].Indexes.Add(adding);
			}
			else if(Database.Views[adding.Table]!=null)
			{
				Database.Views[adding.Table].Indexes.Add(adding);
			}
			else
			{
				throw new ApplicationException("Indexing nonexistant table or view: "+adding.Table);
			}

			//get columns
			if(tokenEnumerator.Current.Children.Count<1)
			{
				throw new ApplicationException("Index columns are missing");
			}
			AssertToken(tokenEnumerator.Current.Children.First, "(");
			TokenEnumerator columnEnumerator=tokenEnumerator.Current.Children.First.Children.GetEnumerator();
			while(columnEnumerator.MoveNext())
			{
				//get the column name
				adding.Columns.Add(columnEnumerator.Current.FlattenTree());
				columnEnumerator.MoveNext();

				//and its direction
				SmallName direction="ASC";
				if(columnEnumerator.Current.Value!="," && columnEnumerator.Current.Value!=")")
				{
					direction=columnEnumerator.Current.Value;
					columnEnumerator.MoveNext();
				}
				adding.ColumnDirections.Add(direction);

				//syntax validation
				if(columnEnumerator.Current.Value!="," && columnEnumerator.Current.Value!=")")
				{
					throw new ApplicationException("Unexpected token "+columnEnumerator.Current);
				}
			}

            if (tokenEnumerator.Next != null && tokenEnumerator.Next.Value.ToLower() == "include")
            {
                tokenEnumerator.MoveNext();
			    if(tokenEnumerator.Current.Children.Count<1)
			    {
				    throw new ApplicationException("Included columns are missing");
			    }
                AssertToken(tokenEnumerator.Current.Children.First, "(");

                columnEnumerator = tokenEnumerator.Current.Children.First.Children.GetEnumerator();
                while (columnEnumerator.MoveNext())
                {
                    //get the column name
                    adding.Include.Add(columnEnumerator.Current.FlattenTree());
                    columnEnumerator.MoveNext();

                    //syntax validation
                    if (columnEnumerator.Current.Value != "," && columnEnumerator.Current.Value != ")")
                    {
                        throw new ApplicationException("Unexpected token " + columnEnumerator.Current);
                    }
                }
            }

			//get metadata
			bool finished=tokenEnumerator.Next==null;
			while(!finished)
			{
				//only read in things we understand
				finished=true;

				if(tokenEnumerator.Next.Value.ToLower()=="on")
				{
					tokenEnumerator.MoveNext();  //past "on"
					tokenEnumerator.MoveNext();  //to the filegroup
					adding.FileGroup = tokenEnumerator.Current.FlattenTree().Replace("[", "").Replace("]", "");  //Hack for partitioned tables

					//check the next thing
					finished=tokenEnumerator.Next==null;
				}
				else if(tokenEnumerator.Next.Value.ToLower()=="where")
				{
					tokenEnumerator.MoveNext();  //past "where"

                    while (tokenEnumerator.Next != null && tokenEnumerator.Next.Type != TokenType.Semicolon)
                    {
                        tokenEnumerator.MoveNext();
                        adding.Where = adding.Where + " " + tokenEnumerator.Current.FlattenTree();
                    }
                    adding.Where = adding.Where.Trim();

					//check the next thing
					finished=tokenEnumerator.Next==null;
				}
				else if(tokenEnumerator.Next.Value.ToLower()=="with")
				{
					tokenEnumerator.MoveNext();  //past "with"
					tokenEnumerator.MoveNext();  //to the value
					adding.With=tokenEnumerator.Current.FlattenTree();

					//check the next thing
					finished=tokenEnumerator.Next==null;
				}
			}
		}

		/// <summary>
		/// Parses the create table statement.
		/// </summary>
		/// <param name="tokenEnumerator">The token enumerator.</param>
        /// <param name="adding">The table object to fill and add to the collection</param>
        /// <param name="tableNameToken">A token with the table's name</param>
		private void ParseCreateTableStatement(TokenEnumerator tokenEnumerator, Table adding, Token tableNameToken)
		{
			//start the table
			Database.Tables.Add(adding);
            var tableStartToken = tokenEnumerator.Current.Children.First;
            if (tableStartToken == null)
            {
                tokenEnumerator.MoveNext();
                tableStartToken = tokenEnumerator.Current;
            }
            AssertToken(tableStartToken, "(");

			//get the columns
            TokenEnumerator columnEnumerator = tableStartToken.Children.GetEnumerator();
			while(columnEnumerator.MoveNext())
			{
				if(columnEnumerator.Current.Type==TokenType.GroupEnd)
				{
					continue;
				}

				//pull out constraints
				if(columnEnumerator.Current.Value.ToLower()=="constraint")
				{
					columnEnumerator.MovePrevious();
					ParseAlterTableAddStatement(columnEnumerator, tableNameToken, null, null, null);
					columnEnumerator.MoveNext(); //pass , or )
					continue;
				}
				if(columnEnumerator.Current.Value.ToLower()=="primary" 
					|| columnEnumerator.Current.Value.ToLower()=="foreign"
					|| columnEnumerator.Current.Value.ToLower()=="unique")
				{
					columnEnumerator.MovePrevious();
					ParseConstraintStatement(columnEnumerator, null, tableNameToken, null, null);
					columnEnumerator.MoveNext(); //pass , or )
					continue;
				}

				//get basic info
				string columnName=columnEnumerator.Current.Value;
				columnEnumerator.MoveNext();

				string columnType=columnEnumerator.Current.Value;
				Column column=new Column(columnName, columnType);
				adding.Columns.Add(column);

				//computed column
				if(columnType.ToLower()=="as")
				{
					column.Type=null;
					if(columnEnumerator.Current.Children.Count>0)
					{
						column.As=columnEnumerator.Current.Children.First.FlattenTree().Trim();
						column.As=column.As.Remove(0, 1);
						column.As=column.As.Substring(0, column.As.Length-1).Trim();
					}
					else
					{
						columnEnumerator.MoveNext();
						column.As=columnEnumerator.Current.FlattenTree();
					}
					columnEnumerator.MoveNext();

					if (columnEnumerator.Current.Value.ToLower() == "persisted")
					{
						column.Persisted = true;
						columnEnumerator.MoveNext();
					}


                    continue;
				}

				//get the column's size
				if(columnEnumerator.Current.Children.Count>0 
					&& columnEnumerator.Current.Children.First.Type==TokenType.GroupBegin)
				{
					if(columnEnumerator.Current.Children.First.Children.Count<2)
					{
						throw new ApplicationException("Invalid column size");
					}

					column.Size="";
					foreach(Token token in columnEnumerator.Current.Children.First.Children)
					{
						if(token.Type!=TokenType.GroupEnd)
						{
							column.Size=column.Size+token.FlattenTree()+" ";
						}
					}
					column.Size=column.Size.Trim().Replace(" ,", ",");
				}
				else if(column.Type=="decimal")
				{
					column.Size="18, 0";
				}

				//slurp in the rest
				columnEnumerator.MoveNext();
				while(columnEnumerator.Current.Value!="," && columnEnumerator.Current.Value!=")")
				{
					switch(columnEnumerator.Current.Value.ToLower())
					{
						case "collate":
							columnEnumerator.MoveNext();
							column.Collate=columnEnumerator.Current.Value;
							break;

						case "constraint":
							columnEnumerator.MovePrevious();
							ParseAlterTableAddStatement(columnEnumerator, tableNameToken, null, column.Name, null);
							break;

						case "check":
						case "foreign":
						case "primary":
						case "unique":
							columnEnumerator.MovePrevious();
							ParseConstraintStatement(columnEnumerator, null, tableNameToken, null, column.Name);
							break;

						case "default":
							columnEnumerator.MovePrevious();
							Constraint constraint = ParseConstraintStatement(columnEnumerator, null, tableNameToken, null, column.Name);
                            column.Default = constraint.DefaultValue;
							break;

						case "identity":
							column.Identity=true;
							column.Nullable=false;
							columnEnumerator.MoveNext();
							if(columnEnumerator.Current.Value=="(")
							{
								column.IdentitySeed=columnEnumerator.Current.Children.First.Value;
								column.IdentityIncrement=columnEnumerator.Current.Children.Third.Value;
							}
							if(columnEnumerator.Next!=null 
								&& columnEnumerator.Next.Value.ToLower()=="not"
								&& columnEnumerator.Next.Children.First.Value.ToLower()=="for")
							{
								columnEnumerator.MoveNext();
								AssertToken(columnEnumerator.Next, "replication");
								column.IdentityReplication=false;
								columnEnumerator.MoveNext();
							}

							break;

						case "not":
							AssertToken(columnEnumerator.Current.Children.First, "null");
							column.Nullable=false;
							break;

						case "null":
							column.Nullable=true;
							break;

						default:
							throw new ApplicationException("Unexpected token: "+columnEnumerator.Current);
					}
					if(columnEnumerator.Current.Value!="," && columnEnumerator.Current.Value!=")")
					{
						columnEnumerator.MoveNext();
					}
				}
			}

			//get filegroup info
			while(tokenEnumerator.Next!=null && 
				(tokenEnumerator.Next.Value.ToLower()=="on" || tokenEnumerator.Next.Value.ToLower()=="textimage_on"))
			{
				if(tokenEnumerator.Next.Value.ToLower()=="on")
				{
					tokenEnumerator.MoveNext();  //past "on"
					tokenEnumerator.MoveNext();  //to the filegroup
					adding.FileGroup=tokenEnumerator.Current.Value;
				}
				else if(tokenEnumerator.Next.Value.ToLower()=="textimage_on")
				{
					tokenEnumerator.MoveNext();  //past "textimage_on"
					tokenEnumerator.MoveNext();  //to the filegroup
					adding.TextFileGroup=tokenEnumerator.Current.Value;
				}
			}
		}

		/// <summary>
		/// Parses the default statement.
		/// </summary>
		/// <param name="tokenEnumerator">The token enumerator.</param>
		/// <param name="adding">The constraint being parsed.</param>
		/// <param name="with">Any with clause.</param>
		private void ParseDefaultStatement(TokenEnumerator tokenEnumerator, Constraint adding, string with)
		{
			//make sure the name is right
			if(adding.Name.Object.Unescaped.StartsWith("PK_"))
			{
				string name=adding.Name.Object.Unescaped.Replace("PK_", "DF_");
				foreach(SmallName column in adding.Columns)
				{
					name=name+"_"+column.Unescaped.Replace("PK_", "DF_");
				}
				adding.Name=new Name(adding.Name.Database, adding.Name.Owner, name);
			}

			adding.Type=ConstraintType.Default;
			if(tokenEnumerator.Current.Children.Count>0)
			{
				adding.DefaultValue=tokenEnumerator.Current.Children.First.FlattenTree();
			}
			else
			{
				tokenEnumerator.MoveNext();
				adding.DefaultValue=tokenEnumerator.Current.FlattenTree();
			}
			while(adding.DefaultValue.StartsWith("(") && adding.DefaultValue.EndsWith(")"))
			{
				adding.DefaultValue=adding.DefaultValue.Remove(adding.DefaultValue.Length-1, 1).Remove(0, 1).Trim();
			}

			while(tokenEnumerator.Next!=null && (tokenEnumerator.Next.Value.ToLower()=="for" || tokenEnumerator.Next.Value.ToLower()==with))
			{
				tokenEnumerator.MoveNext();
				if(tokenEnumerator.Current.Value.ToLower()=="for")
				{
					tokenEnumerator.MoveNext();
					adding.Columns.Add(tokenEnumerator.Current.FlattenTree());
				}
				else if(tokenEnumerator.Current.Value.ToLower()=="with")
				{
					tokenEnumerator.MoveNext();
					adding.With=tokenEnumerator.Current.Value;
				}
			}
		}

		/// <summary>
		/// Parses the delete statement.
		/// </summary>
		/// <param name="tokenEnumerator">The token enumerator.</param>
		private void ParseDeleteStatement(TokenEnumerator tokenEnumerator)
		{
			//Only handles basic truncation for now, that's all we need

			tokenEnumerator.MoveNext();

			//from is optional
			if(tokenEnumerator.Current.Value.ToLower()=="from")
			{
				tokenEnumerator.MoveNext();
			}

			Table table=Database.Tables[tokenEnumerator.Current.Value];
			if(table==null)
			{
				throw new ApplicationException("Deleting from nonexistant table: "+tokenEnumerator.Current.Value);
			}

			if(tokenEnumerator.Next!=null && tokenEnumerator.Next.Value.ToLower()=="where")
			{
				RunOptions.Current.Logger.Log("Ignoring DELETE with WHERE (this is usually safe)", OutputLevel.Updates);

				tokenEnumerator.MoveNext();  //where
				tokenEnumerator.MoveNext();  //condition

				bool searching=tokenEnumerator.Next!=null;
				while(searching)
				{
					if(tokenEnumerator.Next.Value.ToLower()=="and"
						|| tokenEnumerator.Next.Value.ToLower()=="or"
						|| tokenEnumerator.Next.Value.ToLower()=="in")
					{
						tokenEnumerator.MoveNext();  //keyword
						tokenEnumerator.MoveNext();  //condition
						searching=tokenEnumerator.Next!=null;
						continue;
					}

					searching=false;
				}
			}
			else
			{
				table.Data.Clear();
			}
		}

		/// <summary>
		/// Parses the deny statement.
		/// </summary>
		/// <param name="tokenEnumerator">The token enumerator.</param>
		private void ParseDenyStatement(TokenEnumerator tokenEnumerator)
		{
			List<string> permissions=new List<string>();
			Name grantedObject;
			SmallName grantedAccount;

			do
			{
				tokenEnumerator.MoveNext();
				permissions.Add(tokenEnumerator.Current.Value);
				tokenEnumerator.MoveNext();
			} while(tokenEnumerator.Current.Value==",");

			AssertToken(tokenEnumerator, "on");

			tokenEnumerator.MoveNext();
			grantedObject=tokenEnumerator.Current.Value;

			tokenEnumerator.MoveNext();
			AssertToken(tokenEnumerator, "to");

			tokenEnumerator.MoveNext();
			grantedAccount=tokenEnumerator.Current.Value;

			Item item=Database[grantedObject];
			if(item==null)
			{
				throw new ApplicationException("Granting on nonexistant object: "+grantedObject);
			}
			foreach(string granting in permissions)
			{
				Database.Permissions.Deny(granting, grantedAccount, grantedObject);
			}

			//drop cascade on the floor for now
			if(tokenEnumerator.Next!=null && tokenEnumerator.Next.Value.ToLower()=="cascade")
			{
				tokenEnumerator.MoveNext();
			}
		}

		/// <summary>
		/// Parses the drop statement.
		/// </summary>
		/// <param name="tokenEnumerator">The token enumerator.</param>
		private void ParseDropStatement(TokenEnumerator tokenEnumerator)
		{
			tokenEnumerator.MoveNext();
			switch(tokenEnumerator.Current.Value.ToLower())
			{
				case "fulltext":
					tokenEnumerator.MoveNext();
					AssertToken(tokenEnumerator, "index");

					tokenEnumerator.MoveNext();
					AssertToken(tokenEnumerator, "on");

					tokenEnumerator.MoveNext();
					Database.FulltextIndexes.Remove(tokenEnumerator.Current.FlattenTree());
					break;

				case "function":
					tokenEnumerator.MoveNext();
					Database.Functions.Remove(tokenEnumerator.Current.FlattenTree());
					break;

				case "index":
					tokenEnumerator.MoveNext();
					string flatValue=tokenEnumerator.Current.FlattenTree();
					int splitter=flatValue.LastIndexOf(".");
					Table indexedTable=Database.Tables[flatValue.Substring(0, splitter)];
					if(indexedTable!=null)
					{
						indexedTable.Indexes.Remove(flatValue.Substring(splitter+1, flatValue.Length-splitter-1));
					}
					break;

				case "procedure":
					tokenEnumerator.MoveNext();
					Database.Procedures.Remove(tokenEnumerator.Current.FlattenTree());
					break;

				case "table":
					tokenEnumerator.MoveNext();
					Database.Tables.Remove(tokenEnumerator.Current.FlattenTree());
					break;

				case "trigger":
					tokenEnumerator.MoveNext();
					Database.Triggers.Remove(tokenEnumerator.Current.FlattenTree());
                    break;

                case "type":
                    //We only support table types right now
                    tokenEnumerator.MoveNext();
                    Database.Tables.Remove(tokenEnumerator.Current.FlattenTree());
                    break;

				case "view":
					tokenEnumerator.MoveNext();
					Database.Views.Remove(tokenEnumerator.Current.FlattenTree());
					break;

				default:
					throw new ApplicationException("Unexpected token: "+tokenEnumerator.Current);
			}
		}

		/// <summary>
		/// Parses the forign key statement.
		/// </summary>
		/// <param name="tokenEnumerator">The token enumerator.</param>
		/// <param name="adding">The constraint being parsed.</param>
		private void ParseForignKeyStatement(TokenEnumerator tokenEnumerator, Constraint adding)
		{
			//basic declaration
			tokenEnumerator.MoveNext();
			AssertToken(tokenEnumerator, "key");
			adding.Type=ConstraintType.ForeignKey;

			if(adding.Columns.Count==0)
			{
				//constrained columns
				tokenEnumerator.MoveNext();
				AssertToken(tokenEnumerator, "(");
				ReadColumns(tokenEnumerator.Current.Children, adding.Columns);
			}

			//referenced table
			tokenEnumerator.MoveNext();
			AssertToken(tokenEnumerator, "references");
			tokenEnumerator.MoveNext();
			adding.ReferencedTable=tokenEnumerator.Current.Value;

			//make sure the name is right
			if(adding.Name.Object.Unescaped.StartsWith("PK_"))
			{
				adding.Name=new Name(adding.Name.Database, adding.Name.Owner,
					adding.Name.Object.Unescaped.Replace("PK_", "FK_")+"_"+adding.ReferencedTable.Object.Unescaped);
			}

			//referenced columns
			if(tokenEnumerator.Current.Children.Count<1)
			{
				throw new ApplicationException("Foreign key reference is missing columns");
			}
			AssertToken(tokenEnumerator.Current.Children.First, "(");
			foreach(Token token in tokenEnumerator.Current.Children.First.Children)
			{
				if(token.Value==")" || token.Value==",")
					continue;

				adding.ReferencedColumns.Add(token.FlattenTree());
			}

			//other attributes
			while(tokenEnumerator.Next!=null && ( tokenEnumerator.Next.Value.ToLower()=="on" || tokenEnumerator.Next.Value.ToLower()=="not"))
			{
				tokenEnumerator.MoveNext();
				if(tokenEnumerator.Current.Value.ToLower()=="on")
				{
					tokenEnumerator.MoveNext();
					if(tokenEnumerator.Current.Value.ToLower()=="delete")
					{
						tokenEnumerator.MoveNext();
						adding.OnDelete=tokenEnumerator.Current.Value;
						if(adding.OnDelete.ToLower()=="set")
						{
							tokenEnumerator.MoveNext();
							adding.OnDelete=adding.OnDelete+" "+tokenEnumerator.Current.Value;
						}
					}
					else if(tokenEnumerator.Current.Value.ToLower()=="update")
					{
						tokenEnumerator.MoveNext();
						adding.OnUpdate=tokenEnumerator.Current.Value;
						if(adding.OnUpdate.ToLower()=="set")
						{
							tokenEnumerator.MoveNext();
							adding.OnUpdate=adding.OnUpdate+" "+tokenEnumerator.Current.Value;
						}
					}
					else
					{
						throw new ApplicationException("Unexpected token: "+tokenEnumerator.Current);
					}
				}
				else if(tokenEnumerator.Current.Value.ToLower()=="not")
				{
					//strange structure as not becomes the parent of its next token
					AssertToken(tokenEnumerator.Current.Children.First, "for");
					tokenEnumerator.MoveNext();
					AssertToken(tokenEnumerator, "replication");
					adding.NotForReplication=true;
				}
			}
		}

		/// <summary>
		/// Parses a function definition.
		/// </summary>
		/// <param name="tokens">The tokens.</param>
		/// <param name="text">The function body.</param>
		private void ParseFunction(TokenSet tokens, string text)
		{
			TokenEnumerator tokenEnumerator=tokens.GetEnumerator();

			//take off "create function"
			tokenEnumerator.MoveNext();
			tokenEnumerator.MoveNext();
			tokenEnumerator.MoveNext();

			//get the name
			string name=tokenEnumerator.Current.FlattenTree();
			int paren=name.IndexOf("(", 0);
			if(paren>0)
			{
				name=name.Substring(0, paren);
			}
			Function function=new Function(name, text);
			Database.Functions.Add(function);

			//Find everything referenced
			RetrieveIdentifiers(function.ReferencedItems, tokens);
		}

		/// <summary>
		/// Parses the grant statement.
		/// </summary>
		/// <param name="tokenEnumerator">The token enumerator.</param>
		private void ParseGrantStatement(TokenEnumerator tokenEnumerator)
		{
			List<string> permissions=new List<string>();
			Name grantedObject;
			SmallName grantedAccount;

			if(tokenEnumerator.Next.Value.ToLower() == "take")
			{
				//throw away 'take ownership on __ to __' for now
				tokenEnumerator.MoveNext();  //take
				tokenEnumerator.MoveNext();  //ownership
				tokenEnumerator.MoveNext();  //on
				tokenEnumerator.MoveNext();
				tokenEnumerator.MoveNext();  //to
				tokenEnumerator.MoveNext();
				return;
			}

			do
			{
				tokenEnumerator.MoveNext();
				permissions.Add(tokenEnumerator.Current.Value);
				tokenEnumerator.MoveNext();
			} while(tokenEnumerator.Current.Value==",");

			AssertToken(tokenEnumerator, "on");

			tokenEnumerator.MoveNext();
			grantedObject=tokenEnumerator.Current.Value;

            if (grantedObject.Object.Unescaped.EndsWith("::"))
            {
                tokenEnumerator.MoveNext();
                grantedObject = grantedObject.Object.Unescaped + tokenEnumerator.Current.Value;
            }

			tokenEnumerator.MoveNext();
			AssertToken(tokenEnumerator, "to");

			tokenEnumerator.MoveNext();
			grantedAccount=tokenEnumerator.Current.Value;

            if (tokenEnumerator.Next!=null && tokenEnumerator.Next.Value == (Name)"as")
            {
                tokenEnumerator.MoveNext();
                tokenEnumerator.MoveNext();
                AssertToken(tokenEnumerator, "dbo");
            }

            if (grantedObject.Owner.Unescaped.ToLower().StartsWith("type::"))
            {
                //We're going to know it's a type, so for now we can just throw it away
                grantedObject.Owner = grantedObject.Owner.Unescaped.Remove(0, 6);
            }

			Item item=Database[grantedObject];
			if(item==null)
			{
				throw new ApplicationException("Granting on nonexistant object: "+grantedObject);
			}
			foreach(string granting in permissions)
			{
				Database.Permissions.Grant(granting, grantedAccount, item.Name);
			}
		}

		/// <summary>
		/// Parses the insert statement.
		/// </summary>
		/// <param name="tokenEnumerator">The token enumerator.</param>
		private void ParseInsertStatement(TokenEnumerator tokenEnumerator)
		{
			tokenEnumerator.MoveNext();
			AssertToken(tokenEnumerator, "into");

			tokenEnumerator.MoveNext();
			Table table=Database.Tables[tokenEnumerator.Current.Value];
			if(table==null)
			{
				throw new ApplicationException("Inserting into nonexistant table: "+tokenEnumerator.Current.Value);
			}
			AssertToken(tokenEnumerator.Current.Children.First, "(");
			TokenSet columns=tokenEnumerator.Current.Children.First.Children;

			tokenEnumerator.MoveNext();
			AssertToken(tokenEnumerator, "values");
			AssertToken(tokenEnumerator.Current.Children.First, "(");
			TokenSet values=tokenEnumerator.Current.Children.First.Children;

            //Condense things that got split
            TokenEnumerator enumerator = values.GetEnumerator();
            while (enumerator.MoveNext())
			{
				if(enumerator.Current.Type==TokenType.Operator)
				{
                    enumerator.Current.Type = enumerator.Next.Type;
                    enumerator.Current.Value = enumerator.Current.Value + enumerator.Next.Value;
                    enumerator.RemoveNext();
				}
			}

			if(columns.Count!=values.Count)
			{
				throw new ApplicationException("Bad column count");
			}
			TableRow row=new TableRow(table);
            TokenEnumerator valueEnumerator = values.GetEnumerator();
            foreach(Token column in columns)
			{
                valueEnumerator.MoveNext();
				if(column.Type==TokenType.Separator || column.Type==TokenType.GroupEnd)
				{
					continue;
				}

                if (valueEnumerator.Current.Value.ToLower() == "null")
				{
                    row[column.Value] = valueEnumerator.Current.Value;
					continue;
				}

                SetTableRowValue(table.Columns[column.Value], valueEnumerator.Current.Value, row, table.Name);
			}

			//fill in defaults
			if(table.Columns.Count!=row.Count)
			{
				List<Constraint> defaults=new List<Constraint>();
				foreach(Constraint constraint in Database.Constraints)
				{
					if(constraint.Type==ConstraintType.Default && constraint.ConstrainedTable==table.Name)
					{
						defaults.Add(constraint);
						if(!row.ContainsColumn(constraint.Columns[0]))
						{
							try
							{
								SetTableRowValue(table.Columns[constraint.Columns[0]], constraint.DefaultValue, row, table.Name);
							}
							catch(Exception)
							{
								//failures here are ok as values aren't always constants
								//real syntax errors will be caught when the constraint is created
							}
						}
					}
				}
			}

			table.Data.Add(row);
		}

		/// <summary>
		/// Parses the primary key statement.
		/// </summary>
		/// <param name="tokenEnumerator">The token enumerator.</param>
		/// <param name="adding">The constraint being parsed.</param>
		private void ParsePrimaryKeyStatement(TokenEnumerator tokenEnumerator, Constraint adding)
		{
			tokenEnumerator.MoveNext();
			AssertToken(tokenEnumerator, "key");
			adding.Type=ConstraintType.PrimaryKey;

			//clustering
			if(tokenEnumerator.Next.Value.ToLower()=="clustered")
			{
				tokenEnumerator.MoveNext();
				adding.Clustered=true;
			}
			else if(tokenEnumerator.Next.Value.ToLower()=="nonclustered")
			{
				tokenEnumerator.MoveNext();
				adding.Clustered=false;
			}
			else
			{
				adding.Clustered=true;
			}

			//get columns
			if(adding.Columns.Count==0)
			{
				tokenEnumerator.MoveNext();
				AssertToken(tokenEnumerator, "(");
				ReadColumns(tokenEnumerator.Current.Children, adding.Columns);
			}

			//get metadata
			bool finished=tokenEnumerator.Next==null;
			while(!finished)
			{
				//only read in things we understand
				finished=true;

				if(tokenEnumerator.Next.Value.ToLower()=="on")
				{
					tokenEnumerator.MoveNext();  //past "on"
					tokenEnumerator.MoveNext();  //to the filegroup
					adding.FileGroup=tokenEnumerator.Current.FlattenTree();

					//check the next thing
					finished=tokenEnumerator.Next==null;
				}
				else if(tokenEnumerator.Next.Value.ToLower()=="with")
				{
					tokenEnumerator.MoveNext();  //past "with"
					tokenEnumerator.MoveNext();  //to the value
					adding.With=tokenEnumerator.Current.FlattenTree();

					//check the next thing
					finished=tokenEnumerator.Next==null;
				}
			}
		}

		/// <summary>
		/// Parses the truncate statement.
		/// </summary>
		/// <param name="tokenEnumerator">The token enumerator.</param>
		private void ParseTruncateStatement(TokenEnumerator tokenEnumerator)
		{
			//Only handles basic truncation for now, that's all we need

			tokenEnumerator.MoveNext();

			//from is optional
			AssertToken(tokenEnumerator.Current, "table");
			tokenEnumerator.MoveNext();

			Table table=Database.Tables[tokenEnumerator.Current.Value];
			if(table==null)
			{
				throw new ApplicationException("Truncating nonexistant table: "+tokenEnumerator.Current.Value);
			}

			table.Data.Clear();
		}

		/// <summary>
		/// Parses the unique statement.
		/// </summary>
		/// <param name="tokenEnumerator">The token enumerator.</param>
		/// <param name="adding">The constraint being parsed.</param>
		private void ParseUniqueStatement(TokenEnumerator tokenEnumerator, Constraint adding)
		{
			tokenEnumerator.MoveNext();
			adding.Type=ConstraintType.Unique;

            bool settingName = adding.Name.Object.Unescaped.Contains("PK_");
            if (settingName)
            {
                adding.Name.Object = adding.Name.Object.Unescaped.Replace("PK_", "IX_");
            }

			//HACK - it would be better to know we're parsing something that needs to remain inline
			if (tokenEnumerator.Current.Value.ToLower() == "clustered" || tokenEnumerator.Current.Type==TokenType.GroupBegin)
			{
				tokenEnumerator.MovePrevious();
			}

			//clustering
			if(tokenEnumerator.Next!=null && tokenEnumerator.Next.Value.ToLower()=="clustered")
			{
				tokenEnumerator.MoveNext();
				adding.Clustered=true;
			}
			else if(tokenEnumerator.Next!=null && tokenEnumerator.Next.Value.ToLower()=="nonclustered")
			{
				tokenEnumerator.MoveNext();
				adding.Clustered=false;
			}
			else
			{
				adding.Clustered=false;
			}

			//get columns
			if(adding.Columns.Count==0)
			{
				if(tokenEnumerator.Current.Value!="(")
				{
					tokenEnumerator.MoveNext();
				}
				AssertToken(tokenEnumerator, "(");
				ReadColumns(tokenEnumerator.Current.Children, adding.Columns);
			}

            if (settingName)
            {
                foreach (SmallName column in adding.Columns)
                {
                    adding.Name.Object = adding.Name.Object.Unescaped + "_" + column.Unescaped;
                }
            }

			//get metadata
			bool finished=tokenEnumerator.Next==null;
			while(!finished)
			{
				//only read in things we understand
				finished=true;

				if(tokenEnumerator.Next!=null && tokenEnumerator.Next.Value.ToLower()=="on")
				{
					tokenEnumerator.MoveNext();  //past "on"
					tokenEnumerator.MoveNext();  //to the filegroup
					adding.FileGroup=tokenEnumerator.Current.FlattenTree();

					//check the next thing
					finished=tokenEnumerator.Next==null;
				}
				else if(tokenEnumerator.Next!=null && tokenEnumerator.Next.Value.ToLower()=="with")
				{
					tokenEnumerator.MoveNext();  //past "with"
					tokenEnumerator.MoveNext();  //to the value
					adding.With=tokenEnumerator.Current.FlattenTree();

					//check the next thing
					finished=tokenEnumerator.Next==null;
				}
			}
		}

		/// <summary>
		/// Parses the set trigger order statement.
		/// </summary>
		/// <param name="tokenEnumerator">The token enumerator.</param>
		private void ParseSetTriggerOrder(TokenEnumerator tokenEnumerator)
		{
			tokenEnumerator.MoveNext();
			tokenEnumerator.MoveNext();

			AssertToken(tokenEnumerator.Current, "=");
			AssertToken(tokenEnumerator.Current.Children.First, "@triggername");
			Trigger trigger=Database.Triggers[tokenEnumerator.Current.Children.Second.Value.Replace("'", "")];
			if(trigger==null)
			{
				throw new ApplicationException("Trigger "+tokenEnumerator.Current.Children.Second.Value
					+" not found.");
			}
			tokenEnumerator.MoveNext();

			AssertToken(tokenEnumerator.Current, ",");
			tokenEnumerator.MoveNext();

			AssertToken(tokenEnumerator.Current, "=");
			AssertToken(tokenEnumerator.Current.Children.First, "@order");
			SmallName order=tokenEnumerator.Current.Children.Second.Value;
			tokenEnumerator.MoveNext();

			AssertToken(tokenEnumerator.Current, ",");
			tokenEnumerator.MoveNext();

			AssertToken(tokenEnumerator.Current, "=");
			AssertToken(tokenEnumerator.Current.Children.First, "@stmttype");
			SmallName statementType=tokenEnumerator.Current.Children.Second.Value;

			Database.TriggerOrder.Add(new TriggerOrder(trigger.ReferencedTable, statementType,
				order, trigger.Name));

		}

		/// <summary>
		/// Parses a stored procedure.
		/// </summary>
		/// <param name="tokens">The tokens.</param>
		/// <param name="text">The stored procedure body.</param>
		private void ParseStoredProcedure(TokenSet tokens, string text)
		{
			TokenEnumerator tokenEnumerator=tokens.GetEnumerator();

			//take off "create procedure"
			tokenEnumerator.MoveNext();
			tokenEnumerator.MoveNext();
			tokenEnumerator.MoveNext();

			//get the name
			string name=tokenEnumerator.Current.FlattenTree();
			int paren=name.IndexOf("(", 0);
			if(paren>0)
			{
				name=name.Substring(0, paren);
			}
			Procedure procedure=new Procedure(name, text);
			Database.Procedures.Add(procedure);

			//Find everything referenced
			RetrieveIdentifiers(procedure.ReferencedItems, tokens);
		}

		/// <summary>
		/// Parses the tree.
		/// </summary>
		/// <param name="tokens">The set of tokens to parse.</param>
		/// <param name="script">The original script.</param>
		private void ParseTree(TokenSet tokens, string script)
		{
			if(tokens==null || tokens.Count<1)
			{
				return;
			}

			//These have to be parsed as a whole
			if(script!=null && tokens.Count>2 && tokens.First.Type==TokenType.Keyword
				&& tokens.Second.Type==TokenType.Keyword && tokens.First.Value.ToLower()=="create")
			{
				if(tokens.Second.Value.ToLower().StartsWith("function"))
				{
					ParseFunction(tokens, script);
					return;
				}
				if(tokens.Second.Value.ToLower().StartsWith("proc"))
				{
					ParseStoredProcedure(tokens, script);
					return;
				}
				if(tokens.Second.Value.ToLower()=="trigger")
				{
					ParseTrigger(tokens, script);
					return;
				}
				if(tokens.Second.Value.ToLower()=="view")
				{
					ParseView(tokens, script);
					return;
				}
			}

			TokenEnumerator tokenEnumerator=tokens.GetEnumerator();
			while(tokenEnumerator.MoveNext())
			{
				switch(tokenEnumerator.Current.Value.ToLower())
				{
                    case ";":
                        //This means we're done.
                        break;

					case "alter":
						ParseAlterStatement(tokenEnumerator);
						break;

					case "begin":
						//parse up the subsection
						ParseTree(tokenEnumerator.Current.Children, null);
						break;

					case "create":
						ParseCreateStatement(tokenEnumerator);
						break;

					case "delete":
						ParseDeleteStatement(tokenEnumerator);
						break;

					case "deny":
						ParseDenyStatement(tokenEnumerator);
						break;

					case "drop":
						ParseDropStatement(tokenEnumerator);
						break;

					case "exec":
						if(tokenEnumerator.Current.Children.Count==1
							&& tokenEnumerator.Current.Children.First.Children.Count==2
							&& tokenEnumerator.Current.Children.First.Children.First.Value.Trim().ToLower().StartsWith("'create statistics")
							)
						{
							break;
						}
						else if(tokenEnumerator.Next.Value.ToLower()=="sp_settriggerorder")
						{
							ParseSetTriggerOrder(tokenEnumerator);
							break;
						}
						throw new ApplicationException("Unexpected EXEC "+tokenEnumerator.Next.FlattenTree());

					case "end":
						//can safely ignore the end of a section
						break;

					case "grant":
						ParseGrantStatement(tokenEnumerator);
						break;

					case "insert":
						ParseInsertStatement(tokenEnumerator);
						break;

					case "if":
						//can't do anything with the if, ignore it
						tokenEnumerator.MoveNext();  //move past the conditional tree
                        if (tokenEnumerator.Next.Value.ToLower() == "is")
                        {
                            //Move past something like
                            //if foo() is not null
                            tokenEnumerator.MoveNext();
                            tokenEnumerator.MoveNext();
                        }
						//leave the next thing for another pass - we'll blindly do it
						break;

					case "print":
						//prints have no lasting effect and can be ignored
						tokenEnumerator.MoveNext(); //move past the string to print
						break;

					case "set":
						//ignore execution options, anything relevant will be set later
						tokenEnumerator.MoveNext();
						tokenEnumerator.MoveNext();
						if(tokenEnumerator.Next!=null && (tokenEnumerator.Next.Value.ToLower()=="on" || tokenEnumerator.Next.Value.ToLower()=="off"))
						{
							tokenEnumerator.MoveNext();
						}
						break;

					case "truncate":
						ParseTruncateStatement(tokenEnumerator);
						break;

                    case "use":
                        //we'll assume everything is in the right database already
                        tokenEnumerator.MoveNext();
                        break;

					default:
						throw new ApplicationException("Unexpected token: "+tokenEnumerator.Current);
				}
			}
		}

		/// <summary>
		/// Parses a trigger defition.
		/// </summary>
		/// <param name="tokens">The set of tokens to parse.</param>
		/// <param name="text">The trigger body.</param>
		private void ParseTrigger(TokenSet tokens, string text)
		{
			TokenEnumerator tokenEnumerator=tokens.GetEnumerator();

			//take off "create trigger"
			tokenEnumerator.MoveNext();
			tokenEnumerator.MoveNext();
			tokenEnumerator.MoveNext();

			//get the name
			string name=tokenEnumerator.Current.FlattenTree();
			tokenEnumerator.MoveNext();

			//take off "on"
			tokenEnumerator.MoveNext();

			//get the referenced table
			string table=tokenEnumerator.Current.FlattenTree();

			Trigger trigger=new Trigger(name, text, table);
			Database.Triggers.Add(trigger);

			//Find everything referenced
			RetrieveIdentifiers(trigger.ReferencedItems, tokens);
		}

		/// <summary>
		/// Parses a view defintion.
		/// </summary>
		/// <param name="tokens">The set of tokens to parse.</param>
		/// <param name="text">The trigger body.</param>
		private void ParseView(TokenSet tokens, string text)
		{
			TokenEnumerator tokenEnumerator=tokens.GetEnumerator();

			//take off "create view"
			tokenEnumerator.MoveNext();
			tokenEnumerator.MoveNext();
			tokenEnumerator.MoveNext();

			//get the name
			string name=tokenEnumerator.Current.FlattenTree();
			int paren=name.IndexOf("(", 0);
			if(paren>0)
			{
				name=name.Substring(0, paren);
			}
			View view=new View(name, text);
			Database.Views.Add(view);

			//Find everything referenced
			RetrieveIdentifiers(view.ReferencedItems, tokens);
		}

		/// <summary>
		/// Reads the columns of a table definition.
		/// </summary>
		/// <param name="tokens">The set of tokens to parse.</param>
		/// <param name="filling">A collection of column names to fill.</param>
		private void ReadColumns(TokenSet tokens, List<SmallName> filling)
		{
			SmallName asc="asc";
			SmallName desc="desc";

			foreach(Token token in tokens)
			{
				if(token.FlattenTree()==asc)
					continue;
				if(token.FlattenTree()==desc)
					throw new Exception("Unimplemented");

				if(token.Type!=TokenType.GroupEnd && token.Type!=TokenType.Separator)
				{
					filling.Add(token.FlattenTree());
				}
			}
		}

		/// <summary>
		/// Retrieves all database identifiers within a set of tokens.
		/// </summary>
		/// <param name="identifiers">The list identifiers found.</param>
		/// <param name="tokens">The set of tokens to parse.</param>
		private void RetrieveIdentifiers(List<Name> identifiers, TokenSet tokens)
		{
			foreach(Token token in tokens)
			{
				if(token.Type==TokenType.Identifier)
					identifiers.Add(token.Value);

				RetrieveIdentifiers(identifiers, token.Children);
			}
		}

		/// <summary>
		/// Remove and parse all identifiable scripts in a set.
		/// </summary>
		/// <param name="scripts">The set of scripts to search.</param>
		public void RetrieveParsableObjects(ScriptSet scripts)
		{
			scripts.Sort();

			for(int i=0; i<scripts.Count; i++)
			{
				Script script=(Script)scripts[i];
				if(script.Type==ScriptType.TableData || script.Type==ScriptType.Table 
						|| script.Type==ScriptType.PrimaryKey || script.Type==ScriptType.ForeignKey)
				{
					RunOptions.Current.Logger.Log("Reading file "+script.FileName, OutputLevel.Reads);
					Parse(script.Text);
					scripts.Remove(script);
					i--;  //don't skip any
				}
				else if(script.Type==ScriptType.StoredProc || script.Type==ScriptType.Trigger
						|| script.Type==ScriptType.UserDefinedFunction || script.Type==ScriptType.View)
				{
					//these items need to be separated by batches
					RunOptions.Current.Logger.Log("Reading file "+script.FileName, OutputLevel.Reads);
					Parse(script);
					scripts.Remove(script);
					i--;  //don't skip any
				}
				else
				{
					try
					{
						ScriptParser tester=new ScriptParser();
						tester.Parse(script); //speculatively parse the script
						tester.Database.CopyTo(Database);  //looks like it worked
						scripts.Remove(script);
						i--;  //don't skip any
					}
					catch
					{
						//Ignore the error, the script will be saved as is
					}
				}
			}
		}

		/// <summary>
		/// Sets a value in a table row.
		/// </summary>
		/// <param name="column">The table column.</param>
		/// <param name="value">The value.</param>
		/// <param name="row">The table row.</param>
		/// <param name="tableName">Name of the table.</param>
		private static void SetTableRowValue(Column column, string value, TableRow row, Name tableName)
		{
			switch(column.Type.Unescaped.ToLower())
			{
				case "bigint":
				case "bit":
                case "float":
				case "int":
                case "smallint":
                case "tinyint":
                    if(value.StartsWith("'") && value.EndsWith("'"))
                        value=value.Substring(1, value.Length-2);
					row[column.Name]=value;
					break;

				case "char":
				case "ntext":
				case "nvarchar":
				case "text":
				case "varchar":
                    if (!value.StartsWith("'"))
                    {
                        value = "'" + value;
                    }
                    if (!value.EndsWith("'"))
                    {
                        value = value + "'";
                    }
					row[column.Name]=value;
					break;

                case "decimal":
				case "numeric":
                case "money":
                    if (value.StartsWith("'") && value.EndsWith("'"))
                        value = value.Substring(1, value.Length - 2);
					row[column.Name]=decimal.Parse(value).ToString("0.0000");
					break;

				case "datetime":
				case "smalldatetime":
					row[column.Name]="'"+DateTime.Parse(value.Replace("'", "")).ToString("yyyy'-'MM'-'dd HH':'mm':'ss'.'fff")+"'";
					break;

				default:
					throw new Exception("Unknown type: "+column.Type+" in table "+tableName);
			}
		}

	}
}

