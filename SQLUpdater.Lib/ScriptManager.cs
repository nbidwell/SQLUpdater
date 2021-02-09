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

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using SQLUpdater.Lib.DBTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Text;

namespace SQLUpdater.Lib
{
	/// <summary>
	/// Common functionality when dealing with scripts
	/// </summary>
	public class ScriptManager
	{
		private const int COMMAND_TIMEOUT=1500;
        private static Dictionary<ScriptType, string> ScriptExtensions = new Dictionary<ScriptType, string>()
        {
            {ScriptType.ForeignKey, ".fkey.sql"},
            {ScriptType.PrimaryKey, ".pkey.sql"},
            {ScriptType.StoredProc, ".proc.sql"},
            {ScriptType.Table, ".table.sql"},
            {ScriptType.TableData, ".data.sql"},
            {ScriptType.Trigger, ".trigger.sql"},
            {ScriptType.UserDefinedFunction, ".function.sql"},
            {ScriptType.View, ".view.sql"}
        };

		/// <summary>
		/// Initializes a new instance of the <see cref="ScriptManager"/> class.
		/// </summary>
		/// <remarks>Static class</remarks>
		private ScriptManager(){}

		/// <summary>
		/// Complete a batch of updates to the database
		/// </summary>
		/// <param name="success">Was the batch successful?</param>
		/// <param name="command">Command executing batches</param>
		/// <param name="closeConnection">Close the connection after this batch?</param>
		/// <param name="newTransaction">set to <c>true</c> if a new transaction is required.</param>
		public static void CompleteUpdate(bool success, SqlCommand command, bool closeConnection, bool newTransaction)
		{
			try
			{
				if(command.Transaction!=null)
				{
					if(success)
					{
						command.Transaction.Commit();
					}
					else
					{
						command.Transaction.Rollback();
					}
				}

				if(!success || closeConnection)
				{
					command.Connection.Close();
				}
			}
			catch(Exception ex)
			{
				RunOptions.Current.Logger.Log(ex.Message, OutputLevel.Errors);
				success=false;
			}

			if(!success)
			{
				throw new Exception("Update Failed");
			}
			else if(success && !closeConnection && newTransaction && RunOptions.Current.TransactionLevel>TransactionLevel.TableOnly)
			{
				command.Transaction=command.Connection.BeginTransaction();
			}
		}

		/// <summary>
		/// Script out the data for the specified tables
		/// </summary>
		/// <param name="database">The database to script into.</param>
		/// <param name="tables">The tables to script data for.</param>
		/// <param name="connectionString">The source database connection string.</param>
		public static void GetData(SQLUpdater.Lib.DBTypes.Database database, List<Name> tables, string connectionString)
		{
			RunOptions.Current.Logger.Log("Retrieving table data", OutputLevel.Reads);

			SqlCommand command=new SqlCommand();
			command.Connection=new SqlConnection(connectionString);
			command.Connection.Open();
			try
			{
				foreach(Name tableName in tables)
				{
					SQLUpdater.Lib.DBTypes.Table table=database.Tables[tableName];
					if(table==null || table.Data.Count>0)
						continue;

					command.CommandText="SELECT * FROM "+tableName;
					SqlDataReader reader=command.ExecuteReader();
					while(reader.Read())
					{
						TableRow row=new TableRow(table);
						for(int i=0; i<reader.FieldCount; i++)
						{
							if(reader.IsDBNull(i))
							{
								row[reader.GetName(i)]="NULL";
								continue;
							}

							switch(reader.GetDataTypeName(i))
							{
								case "bigint":
									row[reader.GetName(i)]=reader.GetInt64(i).ToString();
									break;

								case "bit":
									row[reader.GetName(i)]=reader.GetBoolean(i) ? "1" : "0";
									break;

								case "char":
									row[reader.GetName(i)]="'"+reader.GetString(i)+"'";
									break;

								case "datetime":
								case "smalldatetime":
									row[reader.GetName(i)]="'"+reader.GetDateTime(i).ToString("yyyy'-'MM'-'dd HH':'mm':'ss'.'fff")+"'";
									break;

								case "decimal":
									row[reader.GetName(i)]=reader.GetDecimal(i).ToString("0.0000");
									break;

                                case "float":
                                    row[reader.GetName(i)] = reader.GetDouble(i).ToString();
                                    break;

								case "int":
									row[reader.GetName(i)]=reader.GetInt32(i).ToString();
									break;

								case "money":
									row[reader.GetName(i)]=reader.GetDecimal(i).ToString();
									break;

								case "ntext":
								case "nvarchar":
								case "text":
								case "varchar":
									row[reader.GetName(i)]="'"+reader.GetString(i).Replace("'", "''")+"'";
									break;

                                case "smallint":
                                    row[reader.GetName(i)] = reader.GetInt16(i).ToString();
                                    break;

                                case "tinyint":
                                    row[reader.GetName(i)] = reader.GetByte(i).ToString();
                                    break;

								default:
									throw new Exception("Unknown type: "+reader.GetDataTypeName(i)+" in table "+tableName);
							}
						}
						table.Data.Add(row);
					}
					reader.Close();
				}
			}
			finally
			{
				command.Connection.Close();
			}
		}

		/// <summary>
		/// Executes the specified script.
		/// </summary>
		/// <param name="script">The script to execute.</param>
		/// <param name="connectionString">The database connection string.</param>
		public static void ExecuteScripts(Script script, string connectionString)
		{
			ScriptSet scripts=new ScriptSet();
			scripts.Add(script);
			ExecuteScripts(scripts, connectionString);
		}

		/// <summary>
		/// Executes the specified scripts.
		/// </summary>
		/// <param name="scripts">The scripts to execute.</param>
		/// <param name="connectionString">The database connection string.</param>
		public static void ExecuteScripts(ScriptSet scripts, string connectionString)
		{
			scripts.Sort();

			using(SqlConnection connection=new SqlConnection(connectionString))
			{
				//initialize
				connection.Open();
				SqlCommand command=new SqlCommand();
				command.Connection=connection;
				command.CommandTimeout=COMMAND_TIMEOUT;
				bool success=true;
				ScriptType lastProcessed=ScriptType.DropFulltextIndex;
				RunOptions.Current.Logger.Log("Beginning table updates", OutputLevel.Updates);

				//process each script
				foreach(Script script in scripts)
				{
					try
					{
						//Fulltext indexes have to happen outside of transactions
						if(lastProcessed<ScriptType.DropTrigger && script.Type>=ScriptType.DropTrigger)
						{
							CompleteUpdate(success, command, false, true);
						}
						if(lastProcessed<ScriptType.FulltextIndex && script.Type>=ScriptType.FulltextCatalog)
						{
							CompleteUpdate(success, command, false, false);
						}
						//separate table updates and more generic scripts
						if(lastProcessed<ScriptType.Trigger && script.Type>=ScriptType.Trigger)
						{
							CompleteUpdate(success, command, false, true);
							RunOptions.Current.Logger.Log("Table updates complete", OutputLevel.Updates);
						}
						//do the same for data population scripts
						if(lastProcessed<ScriptType.Unknown && script.Type>=ScriptType.Unknown)
						{
							CompleteUpdate(success, command, false, true);
						}

						RunOptions.Current.Logger.Log("Processing script: "+script.Name+" ("+script.Type+")", OutputLevel.Updates);
						lastProcessed=script.Type;
						foreach(string batch in script.Batches)
						{
							if(batch.Trim()=="")
								continue;

							command.CommandText=batch;
							command.ExecuteNonQuery();
						}
					}
					catch(Exception ex)
					{
						success=false;
						RunOptions.Current.Logger.Log("Error: "+ex.Message, OutputLevel.Errors);
					}
				}

				//clean up behind the horse
				CompleteUpdate(success, command, true, false);
			}
		}

        /// <summary>
        /// Load a script from the file system.
        /// </summary>
        /// <param name="file">The file to load.</param>
        /// <param name="scripts">The script set to populate.</param>
        /// <param name="name">The script name.</param>
		public static void LoadScript(FileInfo file, ScriptSet scripts, string name)
		{
			//what we do depends on what type of file we've got
			switch(file.Extension.ToLower())
			{
				//data
				case ".data":
					scripts.Add(new Script(file, ScriptType.TableData, name));
					break;

				//extended table properties
				case ".ext":
					RunOptions.Current.Logger.Log("Ignoring extended property file "+file.FullName, OutputLevel.Reads);
					break;

				//foreign key constraints
				case ".fky":
                    scripts.Add(new Script(file, ScriptType.ForeignKey, name));
					break;

				//generic script?
				case ".sql":
					switch(Path.GetExtension(Path.GetFileNameWithoutExtension(file.FullName)))
					{
						case ".data":
                            scripts.Add(new Script(file, ScriptType.TableData, name));
							break;

						case ".fkey":
                            scripts.Add(new Script(file, ScriptType.ForeignKey, name));
							break;

						case ".function":
                            scripts.Add(new Script(file, ScriptType.UserDefinedFunction, name));
							break;

						case ".pkey":
                            scripts.Add(new Script(file, ScriptType.PrimaryKey, name));
							break;

						case ".proc":
                            scripts.Add(new Script(file, ScriptType.StoredProc, name));
							break;

						case ".table":
                            scripts.Add(new Script(file, ScriptType.Table, name));
							break;

						case ".trigger":
                            scripts.Add(new Script(file, ScriptType.Trigger, name));
							break;

						case ".view":
                            scripts.Add(new Script(file, ScriptType.View, name));
							break;

						default:
                            scripts.Add(new Script(file, ScriptType.Unknown, name));
							break;
					}
					break;

				//known files to ignore
				case ".dbp": //database project
				case ".log": //log file
				case ".scc": //source control file
				case ".vspscc": //source control file
					RunOptions.Current.Logger.Log("Ignoring non script file "+file.FullName, OutputLevel.Reads);
					break;

				//primary key constraints
				case ".kci":
                    scripts.Add(new Script(file, ScriptType.PrimaryKey, name));
					break;

				//stored procs
				case ".prc":
                    scripts.Add(new Script(file, ScriptType.StoredProc, name));
					break;

				//table definitions
				case ".tab": //table definitions
                    scripts.Add(new Script(file, ScriptType.Table, name));
					break;

				//triggers
				case ".trg":
                    scripts.Add(new Script(file, ScriptType.Trigger, name));
					break;

				//user defined functions
				case ".udf":
                    scripts.Add(new Script(file, ScriptType.UserDefinedFunction, name));
					break;

				//views
				case ".viw":
                    scripts.Add(new Script(file, ScriptType.View, name));
					break;

				default:
					RunOptions.Current.Logger.Log("Ignoring unknown file "+file.FullName, OutputLevel.Reads);
					break;
			}
		}

		/// <summary>
		/// Load a single script from the file system.
		/// </summary>
		/// <param name="scriptFile">The script file.</param>
		/// <returns></returns>
		public static ScriptSet LoadScript(string scriptFile)
		{
			ScriptSet scripts=new ScriptSet();
			LoadScript(new FileInfo(scriptFile), scripts, null);
			return scripts;
		}

		/// <summary>
		/// Load scripts from the file system.
		/// </summary>
		/// <param name="scriptDirectory">The script directory.</param>
		/// <returns></returns>
		public static ScriptSet LoadScripts(string scriptDirectory)
		{
			ScriptSet scripts=new ScriptSet();
			LoadScripts(new DirectoryInfo(scriptDirectory), scripts, scriptDirectory);
			return scripts;
		}

        /// <summary>
        /// Load scripts from the file system.
        /// </summary>
        /// <param name="scriptDirectory">The script directory.</param>
        /// <param name="scripts">The set of scripts to populate.</param>
        /// <param name="baseDirectory">The base script directory.</param>
		private static void LoadScripts(DirectoryInfo scriptDirectory, ScriptSet scripts, string baseDirectory)
		{
			if(scriptDirectory.Name==".svn")
			{
				RunOptions.Current.Logger.Log("Ignoring "+scriptDirectory.FullName, OutputLevel.Reads);
				return;
			}

			//load each script in turn
            foreach (FileInfo file in scriptDirectory.GetFiles())
            {
                bool parse = true;
                foreach (string pattern in RunOptions.Current.IgnoreFiles)
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(file.FullName, pattern))
                    {
                        parse = false;
                        break;
                    }
                }

                if (parse)
                {
                    LoadScript(file, scripts, file.FullName.Replace(baseDirectory, "").Replace(file.Extension, ""));
                }
            }

			//process subdirs
			foreach(DirectoryInfo subDir in scriptDirectory.GetDirectories())
			{
				LoadScripts(subDir, scripts, baseDirectory);
			}
		}

		/// <summary>
		/// Parses all entities in a physical database.
		/// </summary>
		/// <param name="connectionString">The database connection string.</param>
		/// <returns></returns>
		public static SQLUpdater.Lib.DBTypes.ScriptParser ParseDatabase(string connectionString)
		{
			SQLUpdater.Lib.DBTypes.ScriptParser parsedDatabase=new SQLUpdater.Lib.DBTypes.ScriptParser();
			ParseDatabase(parsedDatabase, connectionString);
			return parsedDatabase;
		}

		/// <summary>
		/// Parses all entities in a physical database.
		/// </summary>
		/// <param name="parsedDatabase">The parsed database.</param>
		/// <param name="connectionString">The database connection string.</param>
		public static void ParseDatabase(ScriptParser parsedDatabase, string connectionString)
		{
			DatabaseScripter scripter=new DatabaseScripter(parsedDatabase, connectionString);
			scripter.ParseDatabase();
		}

		/// <summary>
		/// Writes a difference summary to the file system.
		/// </summary>
		/// <param name="differences">The differences.</param>
        /// <param name="referenceDatabase">Reference database definition</param>
        /// <param name="targetDatabase">Target database to update</param>
		public static void WriteDifferenceSummary(DBTypes.Database referenceDatabase, DBTypes.Database targetDatabase, DifferenceSet differences)
		{
            StreamWriter diffWriter = new StreamWriter(Path.Combine(RunOptions.Current.ScriptOutputDirectory, RunOptions.Current.DiffFile), false);

            var patchGenerator=new DiffMatchPatch.diff_match_patch();
            foreach (var difference in differences)
            {
                if (difference.DifferenceType != DifferenceType.Modified)
                    continue;

                diffWriter.WriteLine(difference.ToString());
                diffWriter.WriteLine();

                var referenceDefinition = referenceDatabase[difference.Item].GenerateCreateScript();
                var targetDefinition = targetDatabase[difference.Item].GenerateCreateScript();
                var diff = patchGenerator.diff_main(targetDefinition.Text, referenceDefinition.Text);
                //var patch = patchGenerator.diff_prettyHtml(diff);
                //patch = System.Web.HttpUtility.UrlDecode(patch);
                //diffWriter.WriteLine(patch);

                //Diffs aren't full lines...  we need to fix that for readability
                var coalesced = new List<DiffMatchPatch.Diff>();
                DiffMatchPatch.Diff lastEqual = null;
                DiffMatchPatch.Diff lastInsert = null;
                DiffMatchPatch.Diff lastDelete = null;
                foreach (var entry in diff)
                {
                    if (entry.operation == DiffMatchPatch.Operation.INSERT)
                    {
                        lastInsert = lastInsert ?? new DiffMatchPatch.Diff(entry.operation, "");
                        lastInsert.text += entry.text;
                    }
                    else if (entry.operation == DiffMatchPatch.Operation.DELETE)
                    {
                        lastDelete = lastDelete ?? new DiffMatchPatch.Diff(entry.operation, "");
                        lastDelete.text += entry.text;
                    }
                    else if (entry.operation == DiffMatchPatch.Operation.EQUAL)
                    {
                        string content = entry.text;
                        int linebreak = content.IndexOf("\n");
                        string donate = linebreak < 0 ? content : content.Substring(0, linebreak + 1);

                        //see if there's a change that needs this to finish its line
                        bool used = false;
                        if (lastInsert != null)
                        {
                            lastInsert.text += donate;
                            used = true;
                        }
                        if (lastDelete != null)
                        {
                            lastDelete.text += donate;
                            used = true;
                        }

                        if (used)
                        {
                            content = content.Remove(0, donate.Length);
                        }

                        //stash anything that's left
                        if (content.Length > 0)
                        {
                            //time to flush everything we've coalesced

                            //see if there are any mods that need to grab a line start
                            if (lastEqual != null && !lastEqual.text.EndsWith("\n") && (lastInsert != null || lastDelete != null))
                            {
                                linebreak = lastEqual.text.LastIndexOf("\n");
                                donate = lastEqual.text.Substring(linebreak + 1);
                                lastEqual.text = lastEqual.text.Remove(linebreak + 1);

                                if (lastInsert != null)
                                {
                                    lastInsert.text = donate + lastInsert.text;
                                }
                                if (lastDelete!= null)
                                {
                                    lastDelete.text = donate + lastDelete.text;
                                }
                            }

                            //do the flush
                            if (lastEqual != null)
                            {
                                coalesced.Add(lastEqual);
                                lastEqual = null;
                            }
                            if (lastInsert != null)
                            {
                                coalesced.Add(lastInsert);
                                lastInsert = null;
                            }
                            if (lastDelete != null)
                            {
                                coalesced.Add(lastDelete);
                                lastDelete = null;
                            }

                            lastEqual = new DiffMatchPatch.Diff(DiffMatchPatch.Operation.EQUAL, content);
                        }
                    }
                    else
                    {
                        throw new Exception("Unknown operation: " + entry.operation);
                    }
                }
                if (lastEqual != null)
                {
                    coalesced.Add(lastEqual);
                }
                if (lastInsert != null)
                {
                    coalesced.Add(lastInsert);
                }
                if (lastDelete != null)
                {
                    coalesced.Add(lastDelete);
                }

                //Finally spit out a nice diff
                for (int i = 0; i < coalesced.Count; i++)
                {
                    var entry=coalesced[i];
                    if (entry.operation == DiffMatchPatch.Operation.EQUAL)
                    {
                        //(ab)use a stream to partition the content into lines
                        using (var text = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(entry.text))))
                        {
                            //write out the first two lines
                            if (i > 0)
                            {
                                for (int j = 0; j < 2 && !text.EndOfStream; j++)
                                {
                                    string line = text.ReadLine();
                                    if (string.IsNullOrEmpty(line))
                                    {
                                        j--;
                                        continue;
                                    }

                                    diffWriter.WriteLine(line);
                                }
                            }

                            //write out the last two lines
                            if (i < coalesced.Count - 1 && !text.EndOfStream)
                            {
                                var lines = new string[3];
                                while (!text.EndOfStream)
                                {
                                    lines[0] = lines[1];
                                    lines[1] = lines[2];
                                    lines[2] = text.ReadLine();
                                }

                                if (i > 0 && lines[0] != null)
                                {
                                    diffWriter.WriteLine();
                                    diffWriter.WriteLine(">> ... <<");
                                    diffWriter.WriteLine();
                                }

                                if (lines[1] != null)
                                {
                                    diffWriter.WriteLine(lines[1]);
                                }
                                diffWriter.WriteLine(lines[2]);
                            }
                        }
                    }
                    else
                    {
                        string marker = entry.operation == DiffMatchPatch.Operation.INSERT ? "> > > >" : "< < < <";
                        diffWriter.WriteLine(marker);
                        diffWriter.Write(entry.text);
                        if (!entry.text.EndsWith("\n"))
                        {
                            diffWriter.WriteLine();
                        }
                        diffWriter.WriteLine(marker);
                    }
                }
                diffWriter.WriteLine();
            }

            diffWriter.Close();
		}

		/// <summary>
		/// Writes a set of scripts to the file system.
		/// </summary>
		/// <param name="scripts">The scripts to write out.</param>
		/// <param name="differences">The differences the scripts are based on.</param>
		public static void WriteScripts(ScriptSet scripts, DifferenceSet differences)
		{
			if(!Directory.Exists(RunOptions.Current.ScriptOutputDirectory))
				Directory.CreateDirectory(RunOptions.Current.ScriptOutputDirectory);

			StreamWriter writer=null;
			if(RunOptions.Current.ScriptFile!=null)
			{
				writer=new StreamWriter(
					Path.Combine(RunOptions.Current.ScriptOutputDirectory, RunOptions.Current.ScriptFile), false);

				if(differences!=null)
				{
					writer.WriteLine("/*");
					writer.WriteLine("Changes:");
					differences.Write(writer);
					writer.WriteLine("*/");
					writer.WriteLine();
				}

				writer.Write("SET NOCOUNT ON\n\nGO\n\n");
				writer.Write("SET QUOTED_IDENTIFIER ON\nGO\n\n");
			}

            StreamWriter diffWriter=null;
            if (RunOptions.Current.DiffFile != null)
            {
                writer = new StreamWriter(
                    Path.Combine(RunOptions.Current.ScriptOutputDirectory, RunOptions.Current.DiffFile), false);
            }

			for(int i=0; i<scripts.Count; i++)
			{
				Script script=(Script)scripts[i];
				if(writer==null)
				{
                    string extension = ScriptExtensions.ContainsKey(script.Type) ? ScriptExtensions[script.Type] : ".sql";
					writer=new StreamWriter(
						Path.Combine(RunOptions.Current.ScriptOutputDirectory, script.Name.Unescaped+extension), false);
				}
				else
				{
					writer.Write("-- "+i+" - "+script.Name+"\r\nGO\r\n");
				}
				writer.Write(script.Text);
				writer.Flush();
				if(RunOptions.Current.ScriptFile==null)
				{
					writer.Close();
					writer=null;
				}
				else
				{
					writer.Write("\r\n\r\n");
				}
			}

            if (writer != null)
            {
                writer.Close();
            }
            if (diffWriter != null)
            {
                diffWriter.Close();
            }
		}
	}
}
