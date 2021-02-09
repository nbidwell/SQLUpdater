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

using SQLUpdater.Lib;
using SQLUpdater.Lib.DBTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;

namespace SQLUpdater
{
	/// <summary>
	/// Tool to run and generate scripts for a SQL Server DB
	/// </summary>
	public class Updater
	{
		private DateTime startTime=DateTime.Now;

		/// <summary>
		/// Initializes a new instance of the <see cref="Updater"/> class.
		/// </summary>
		public Updater()
		{
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			//show help when no args specified
			if(args.Length==0)
			{
				args=new string[]{"-h"};
			}
			RunOptions.Current.Logger=new ConsoleLogger();
			RunOptions.Current.Init(args);
            if(RunOptions.Current.Connections.References.Count==0)
            {
                new Updater().ScriptTarget();
            }
            else
            {
			    new Updater().DoUpdate();
            }
		}

		/// <summary>
		/// The main functionality of the tool
		/// </summary>
		public void DoUpdate()
		{
			try
			{
				//make sure we can run
				string error=null;
				if(RunOptions.Current.Connections.Targets.Count==0)
				{
					error="Missing a valid target database";
				}
				if(error!=null)
				{
					Exit(error, false);
				}

				//Load any reference database
				ScriptSet referenceScripts=new ScriptSet();
				ScriptParser referenceParser=new ScriptParser();
				foreach(ConnectionInfo connection in RunOptions.Current.Connections.References)
				{
					switch(connection.Type)
					{
						case ConnectionType.Database:
							ScriptManager.ParseDatabase(referenceParser, connection.Path);
                            ScriptManager.GetData(referenceParser.Database, RunOptions.Current.LoadData, connection.Path);
                            break;

						case ConnectionType.Directory:
							referenceScripts.Add(ScriptManager.LoadScripts(connection.Path));
							referenceParser.RetrieveParsableObjects(referenceScripts);
							break;

						case ConnectionType.File:
							referenceScripts.Add(ScriptManager.LoadScript(connection.Path));
							referenceParser.RetrieveParsableObjects(referenceScripts);
							break;

						default:
							throw new Exception(connection.Type.ToString());
					}
				}

				//Load the target database
                ScriptParser targetParser = LoadTargetDatabase(referenceParser);

				//generate new scripts
				RunOptions.Current.Logger.Log("Calculating Differences", OutputLevel.Reads);
				DifferenceSet differences=null;
				if(referenceParser.Database.IsEmpty)
				{
					referenceScripts=targetParser.Database.CreateDiffScripts(new Database());
				}
				else
				{
					differences=referenceParser.Database.GetDifferences(targetParser.Database);
					if(targetParser.Database.IsEmpty && differences.Count>0)
					{
						Console.WriteLine("Creating database schema");
					}
					else
					{
						differences.Write(Console.Out);
					}
					referenceScripts.Add(referenceParser.Database.CreateDiffScripts(targetParser.Database, differences));
				}

				//Make sure everything runs in the right order
				referenceScripts.Sort();

				//store scripts if requested
				if(RunOptions.Current.ScriptOutputDirectory!="")
				{
					ScriptManager.WriteScripts(referenceScripts, differences);
				}
				if(!string.IsNullOrEmpty(RunOptions.Current.DiffFile))
				{
					ScriptManager.WriteDifferenceSummary(referenceParser.Database, targetParser.Database, differences);
				}

				//run the scripts
				if(!RunOptions.Current.Test && RunOptions.Current.Connections.References.Count>0)
				{
					if(RunOptions.Current.Connections.Targets.Count>1)
						throw new ApplicationException("Updating multiple targets is not supported");

					ScriptManager.ExecuteScripts(referenceScripts, RunOptions.Current.Connections.Targets[0].Path);
				}
			}
			catch(Exception ex)
			{
				Exit("Error: "+ex.Message, false);
			}

			Exit("Update Successful", true);
		}

		/// <summary>
		/// Exits the program.
		/// </summary>
		/// <param name="message">A message to display</param>
		/// <param name="successful">Was execution successful?</param>
		private void Exit(string message, bool successful)
		{
			RunOptions.Current.Logger.Log(message, OutputLevel.Errors);
			TimeSpan executionTime=DateTime.Now-startTime;
			RunOptions.Current.Logger.Log("Time: "+Math.Floor(executionTime.TotalMinutes)+"m "+executionTime.Seconds+"s", 
				OutputLevel.Differences);
			Environment.Exit(successful ? 0 : -1);
        }

        /// <summary>
        /// Loads the target database
        /// </summary>
        /// <param name="referenceParser">Reference Database</param>
        /// <returns>The parsed target database</returns>
        private static ScriptParser LoadTargetDatabase(ScriptParser referenceParser)
        {
            ScriptSet targetScripts = new ScriptSet();
            ScriptParser targetParser = new ScriptParser();
            foreach (ConnectionInfo connection in RunOptions.Current.Connections.Targets)
            {
                switch (connection.Type)
                {
                    case ConnectionType.Database:
                        ScriptManager.ParseDatabase(targetParser, connection.Path);

                        if(referenceParser!=null)
                        {
                            List<Name> dataTables = referenceParser.Database.GetTablesWithData();
                            dataTables.AddRange(RunOptions.Current.LoadData);
                            ScriptManager.GetData(targetParser.Database, dataTables, connection.Path);
                        }
                        else
                        {
                            ScriptManager.GetData(targetParser.Database, RunOptions.Current.LoadData, connection.Path);
                        }
                        break;

                    case ConnectionType.Directory:
                        targetScripts.Add(ScriptManager.LoadScripts(connection.Path));
                        targetParser.RetrieveParsableObjects(targetScripts);
                        break;

                    case ConnectionType.File:
                        targetScripts.Add(ScriptManager.LoadScript(connection.Path));
                        targetParser.RetrieveParsableObjects(targetScripts);
                        break;

                    default:
                        throw new Exception(connection.Type.ToString());
                }
            }
            return targetParser;
        }

        /// <summary>
        /// Script out the target database
        /// </summary>
		public void ScriptTarget()
		{
			try
			{
				//make sure we can run
				string error=null;
				if(RunOptions.Current.Connections.Targets.Count==0)
				{
					error="Missing a valid target database";
				}
				if(error!=null)
				{
					Exit(error, false);
				}

				//Load the target database
                ScriptParser targetParser = LoadTargetDatabase(null);

				//generate new scripts
				RunOptions.Current.Logger.Log("Generating Scripts", OutputLevel.Reads);
				DifferenceSet differences=null;
				ScriptSet scripts=targetParser.Database.CreateScripts();

				//store the scripts
				ScriptManager.WriteScripts(scripts, differences);
			}
			catch(Exception ex)
			{
				Exit("Error: "+ex.Message, false);
			}

			Exit("Update Successful", true);
		}
	}
}
