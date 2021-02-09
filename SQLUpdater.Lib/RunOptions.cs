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

using SQLUpdater.Lib.DBTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace SQLUpdater.Lib
{
	/// <summary>
	/// Settings the app is run with
	/// </summary>
	public class RunOptions
	{
		private static RunOptions current;

		/// <summary>
		/// Gets or sets the database connections to work with.
		/// </summary>
		/// <value>The database connections to work with.</value>
		public ConnectionSet Connections { get; set; }

		/// <summary>
		/// Gets or sets the run options.
		/// </summary>
		/// <value>The run options.</value>
		public static RunOptions Current
		{
			get
			{
				if(current==null)
					current=new RunOptions();
				return current;
			}
			set{ current=value; }
		}

        /// <summary>
        /// Gets or sets the file to store a diff summary in.
        /// </summary>
        /// <value>The file to diff to.</value>
        public string DiffFile { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the database is fully scripted.
		/// </summary>
		/// <value><c>true</c> if  the database is fully scripted; otherwise, <c>false</c>.</value>
		public bool FullyScripted{ get; set; }

        /// <summary>
        /// Files to ignore parsing
        /// </summary>
        public List<string> IgnoreFiles { get; set; }

        /// <summary>
        /// Tables to load data from in the database
        /// </summary>
        public List<Name> LoadData { get; set; }

		/// <summary>
		/// Gets or sets the logger.
		/// </summary>
		/// <value>The logger.</value>
		[XmlIgnore]
		public ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether only minimal dependencies should be recreated.
        /// </summary>
        /// <value><c>true</c> if only the bare minimum of dependencies should be recreated; otherwise, <c>false</c>.</value>
        public bool MinimalDependencies { get; set; }

		/// <summary>
		/// Gets or sets the output level.
		/// </summary>
		/// <value>The output level.</value>
		public OutputLevel OutputLevel{ get; set; }

		/// <summary>
		/// Gets or sets the parser output directory.
		/// </summary>
		/// <value>The parser output directory.</value>
		public string ParserOutput{ get; set; }

		/// <summary>
		/// Gets or sets the file to script to.
		/// </summary>
		/// <value>The file to script to.</value>
		public string ScriptFile{ get; set; }

		/// <summary>
		/// Gets or sets the script output directory.
		/// </summary>
		/// <value>The script output directory.</value>
		public string ScriptOutputDirectory{ get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="RunOptions"/> is a test.
		/// </summary>
		/// <value><c>true</c> if a test; otherwise, <c>false</c>.</value>
		public bool Test{ get; set; }

		/// <summary>
		/// Gets or sets the transaction level.
		/// </summary>
		/// <value>The transaction level.</value>
        public TransactionLevel TransactionLevel { get; set; }

        /// <summary>
        /// Only write out changes for these objects.
        /// </summary>
        public List<Name> WriteObjects { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="RunOptions"/> class.
		/// </summary>
		/// <remarks>static class</remarks>
		private RunOptions()
		{
		}

		/// <summary>
		/// Initializes the run options.
		/// </summary>
		public void Init()
		{
			//defaults
			Connections=new ConnectionSet();
			FullyScripted=false;
            IgnoreFiles=new List<string>();
            LoadData=new List<Name>();
            MinimalDependencies = false;
			OutputLevel=OutputLevel.Reads;
			ParserOutput=null;
			ScriptOutputDirectory="";
			Test=false;
			TransactionLevel=TransactionLevel.Everything;
            WriteObjects=new List<Name>();
		}

		/// <summary>
		/// Initializes the run options.
		/// </summary>
		/// <param name="args">The command line args.</param>
		/// <returns></returns>
		public void Init(string[] args)
		{
			//defaults
			Init();

			//parse up args
			for(int i=0; i<args.Length; i++)
			{
				switch(args[i])
				{
                    case "-d":
                    case "--data":
                        foreach(string table in args[++i].Split(','))
                        {
                            LoadData.Add(table);
                        }
                        break;

                    case "--diffFile":
                        DiffFile = args[++i];
                        break;

					case "-f":
					case "--fullyScripted":
						FullyScripted=true;
						break;

					case "-h":
					case "--help":
						Logger.Log("A tool to run scripts on a Microsoft SQL Server 2000 server", OutputLevel.Errors);
						Logger.Log("Usage: SQLUpdater <args> [-t] <target> [-r] [reference]", OutputLevel.Errors);
                        Logger.Log("\t-d <name[,name]>: Tables to load data from", OutputLevel.Errors);
                        Logger.Log("\t-f: the database is fully scripted", OutputLevel.Errors);
						Logger.Log("\t-h: display this help message and exit", OutputLevel.Errors);
						Logger.Log("\t-l <level>: transaction level (None, TableOnly, Everything)", OutputLevel.Errors);
						Logger.Log("\t-o <level>: output level (Errors, Differences, Updates, Reads)", OutputLevel.Errors);
						Logger.Log("\t-s <directory>: directory to store scripts in before execution", OutputLevel.Errors);
                        Logger.Log("\t-w <name[,name]>: objects to write out changes to", OutputLevel.Errors);
						Logger.Log("\t--diffFile <filename>: name of a single file to store a difference summary in", OutputLevel.Errors);
                        Logger.Log("\t--ignore <name[,name]>: script files to ignore", OutputLevel.Errors);
                        Logger.Log("\t--minimalDependencies: only recreate otherwise dropped objects", OutputLevel.Errors);
                        Logger.Log("\t--parserOutput <directory>: directory for debugging parser output", OutputLevel.Errors);
						Logger.Log("\t--scriptFile <filename>: name of a single file to store scripts in in conjunction with -s", OutputLevel.Errors);
						Logger.Log("\t--test: run in test mode without updating the database", OutputLevel.Errors);
						Environment.Exit(0);
						break;

                    case "--ignore":
                        foreach (string name in args[++i].Split(','))
                        {
                            IgnoreFiles.Add(name);
                        }
                        break;

					case "-l":
						TransactionLevel=(TransactionLevel)Enum.Parse(typeof(TransactionLevel), args[++i]);
						break;

                    case "-m":
                    case "--minimalDependencies":
                        MinimalDependencies = true;
                        break;

					case "-o":
					case "--outputlevel":
						OutputLevel=(OutputLevel)Enum.Parse(typeof(OutputLevel), args[++i]);
						break;

					case "--parserOutput":
						ParserOutput=args[++i];
						break;

					case "-r":
						AddConnection(args[++i], Connections.References);
						break;

					case "-s":
						ScriptOutputDirectory=args[++i];
						if(!ScriptOutputDirectory.EndsWith("\\"))
						{
							ScriptOutputDirectory=ScriptOutputDirectory+"\\";
						}
						break;

					case "--scriptFile":
						ScriptFile=args[++i];
						break;

					case "-t":
						AddConnection(args[++i], Connections.Targets);
						break;

					case "--test":
						Test=true;
                        break;

                    case "-w":
                    case "--write":
                        foreach (string name in args[++i].Split(','))
                        {
                            WriteObjects.Add(name);
                        }
                        break;

					default:
						if(args[i][0]=='-' || (Connections.References.Count>0 && Connections.Targets.Count>0))
						{
							Logger.Log("Unknown argument: "+args[i], OutputLevel.Errors);
							Environment.Exit(-1);
						}
						else
						{
							if(Connections.Targets.Count==0)
							{
								AddConnection(args[i], Connections.Targets);
							}
							else
							{
								AddConnection(args[i], Connections.References);
							}
						}
						break;
				}
			}

		}

		private void AddConnection(string connection, List<ConnectionInfo> collection)
		{
			ConnectionType type=ConnectionType.Database;
			if(Directory.Exists(connection))
			{
				type=ConnectionType.Directory;
			}
			else if(File.Exists(connection))
			{
				type=ConnectionType.File;
			}

			collection.Add(new ConnectionInfo(connection, type));
		}
	}
}
