/*
 * Copyright 2009 Nathan Bidwell (nbidwell@bidwellfamily.net)
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
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace SQLUpdater.Lib
{
	/// <summary>
	/// An entity to script out a complete database
	/// </summary>
	public class DatabaseScripter
	{
		private Microsoft.SqlServer.Management.Smo.Database database;
		private ScriptParser parsedDatabase;
		ScriptingOptions scriptOptions;
		private Server server;

		/// <summary>
		/// Initializes a new instance of the <see cref="DatabaseScripter"/> class.
		/// </summary>
		/// <param name="parsedDatabase">The parsed database.</param>
		/// <param name="connectionString">The connection string.</param>
		public DatabaseScripter(ScriptParser parsedDatabase, string connectionString)
		{
			this.parsedDatabase=parsedDatabase;
            SqlConnection connection=null;
            try
            {
                connection = new SqlConnection(connectionString);
                ServerConnection serverConnection = new ServerConnection(connection);
                server = new Server(serverConnection);
			    database=server.Databases[connection.Database];
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Unable to connect to database at " + connectionString, ex);
            }
			if(database==null)
			{
				throw new ApplicationException("Database "+connection.Database+" not found.");
			}

			scriptOptions=new ScriptingOptions();
			scriptOptions.DriAll=true;
			scriptOptions.DriAllConstraints=true;
			scriptOptions.DriIncludeSystemNames=true;
			scriptOptions.DriIndexes=true;
			scriptOptions.FullTextIndexes=true;
			scriptOptions.Indexes=true;
			scriptOptions.Permissions=true;
			scriptOptions.SchemaQualify=true;
			scriptOptions.SchemaQualifyForeignKeysReferences=true;
			scriptOptions.Statistics=false;
			scriptOptions.Triggers=true;
		}

		/// <summary>
		/// Parses all entities in a physical database.
		/// </summary>
		public void ParseDatabase()
		{
            ParseDatabase(database.FullTextCatalogs, typeof(Microsoft.SqlServer.Management.Smo.FullTextCatalog));
			ParseDatabase(database.Tables, typeof(Microsoft.SqlServer.Management.Smo.Table));
            ParseDatabase(database.UserDefinedFunctions, typeof(Microsoft.SqlServer.Management.Smo.UserDefinedFunction));
            ParseDatabase(database.UserDefinedTableTypes, typeof(Microsoft.SqlServer.Management.Smo.UserDefinedTableType));
			ParseDatabase(database.StoredProcedures, typeof(Microsoft.SqlServer.Management.Smo.StoredProcedure));
			ParseDatabase(database.Views, typeof(Microsoft.SqlServer.Management.Smo.View));
		}

		private void ParseDatabase(SortedListCollectionBase items, Type itemType)
		{
			RunOptions.Current.Logger.Log("Getting "+itemType.Name+"s", OutputLevel.Reads);

			//Preload database schema
			try
			{
                if (itemType != typeof(Microsoft.SqlServer.Management.Smo.FullTextCatalog))
                {
                    database.PrefetchObjects(itemType, scriptOptions);
                }
			}
			catch(Exception e)
			{
				RunOptions.Current.Logger.Log(e.Message, OutputLevel.Errors);
			}
			
			//Find the objects
			List<SqlSmoObject> scripting=new List<SqlSmoObject>();
			bool table= itemType==typeof(Microsoft.SqlServer.Management.Smo.Table);
			List<string> hackScripts=new List<string>();
			foreach(ScriptNameObjectBase item in items)
			{
                if (item.Properties.Contains("IsSystemObject") && (bool)item.Properties["IsSystemObject"].Value)
					continue;
                if (!table && item.Properties.Contains("IsEncrypted") && (bool)item.Properties["IsEncrypted"].Value)
				{
					RunOptions.Current.Logger.Log("Cannot script encrypted object "+item.Name, OutputLevel.Reads);
					continue;
				}

				//Deal with the fact that we can't script out the text filegroup of tables
				//This isn't real SQL, but it'll work internally
				if(table)
				{
					string textFileGroup=(string)item.Properties["TextFileGroup"].Value;
					if(textFileGroup!="")
					{
						hackScripts.Add("ALTER TABLE "+item.ToString()+"SET TEXTIMAGE_ON "+textFileGroup);
					}
				}

				scripting.Add(item);
			}

			//Generate scripts
			Scripter scripter=new Scripter(server);
			scripter.Options=scriptOptions;
			System.Collections.Specialized.StringCollection scripted=scripter.Script(scripting.ToArray());
			scripted.AddRange(hackScripts.ToArray());

			//Parse everything up
			foreach(string script in scripted)
			{
				parsedDatabase.Parse(script);
			}
		}
	}
}
