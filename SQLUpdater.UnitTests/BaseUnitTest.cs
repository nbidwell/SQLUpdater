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

using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using NUnit.Framework;
using SQLUpdater.Lib;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace SQLUpdater.UnitTests
{
	public class BaseUnitTest
	{
		public void ExecuteScripts(Script script)
		{
			try
			{
				ScriptManager.ExecuteScripts(script, ConfigurationManager.AppSettings["ConnectionString"]);
			}
			catch
			{
				Console.WriteLine(RunOptions.Current.Logger.ToString());
				throw;
			}
		}

		public void ExecuteScripts(ScriptSet scripts)
		{
			try
			{
				ScriptManager.ExecuteScripts(scripts, ConfigurationManager.AppSettings["ConnectionString"]);
			}
			catch
			{
				Console.WriteLine(RunOptions.Current.Logger.ToString());
				throw;
			}
		}

		public void GetData(SQLUpdater.Lib.DBTypes.Database database, List<SQLUpdater.Lib.DBTypes.Name> tables)
		{
			ScriptManager.GetData(database, tables, ConfigurationManager.AppSettings["ConnectionString"]);
		}

		public SQLUpdater.Lib.DBTypes.ScriptParser ParseDatabase()
		{
			return ScriptManager.ParseDatabase(ConfigurationManager.AppSettings["ConnectionString"]);
		}

		[SetUp]
		public void TestSetup()
		{
			//start with default options
			string connectionString=ConfigurationManager.AppSettings["ConnectionString"];
			string[] options={ connectionString };
			RunOptions.Current.Logger=new TestLogger();
			RunOptions.Current.Init(options);

			SqlConnection connection=new SqlConnection(connectionString);
			ServerConnection serverConnection=new ServerConnection(connection);
			Server server=new Server(serverConnection);
			Microsoft.SqlServer.Management.Smo.Database database=server.Databases[connection.Database];
			if(database==null)
			{
				throw new ApplicationException("Database "+connection.Database+" not found.");
			}

			server.SetDefaultInitFields(typeof(View), true);
			database.PrefetchObjects(typeof(View));
			for(int i=database.Views.Count-1; i>=0; i--)
			{
				View view=database.Views[i];

				if(view.IsSystemObject)
					continue;

				view.Drop();
			}

			server.SetDefaultInitFields(typeof(Table), true);
			database.PrefetchObjects(typeof(Table));
			foreach(Table table in database.Tables)
			{
				if(table.IsSystemObject)
					continue;

				for(int i=table.ForeignKeys.Count-1; i>=0; i--)
				{
					table.ForeignKeys[i].Drop();
				}
			}

			for(int i=database.Tables.Count-1; i>=0; i--)
			{
				Table table=database.Tables[i];

				if(table.IsSystemObject)
					continue;

				table.Drop();
			}

			server.SetDefaultInitFields(typeof(StoredProcedure), true);
			database.PrefetchObjects(typeof(StoredProcedure));
			for(int i=database.StoredProcedures.Count-1; i>=0; i--)
			{
				StoredProcedure procedure=database.StoredProcedures[i];

				if(procedure.IsSystemObject)
					continue;

				procedure.Drop();
			}

			server.SetDefaultInitFields(typeof(UserDefinedFunction), true);
			database.PrefetchObjects(typeof(UserDefinedFunction));
			for(int i=database.UserDefinedFunctions.Count-1; i>=0; i--)
			{
				UserDefinedFunction function=database.UserDefinedFunctions[i];

				if(function.IsSystemObject)
					continue;

				function.Drop();
			}

            server.SetDefaultInitFields(typeof(UserDefinedTableType), true);
            database.PrefetchObjects(typeof(UserDefinedTableType));
            for (int i = database.UserDefinedTableTypes.Count - 1; i >= 0; i--)
            {
                UserDefinedTableType tableType = database.UserDefinedTableTypes[i];

                if (!tableType.IsUserDefined)
                    continue;

                tableType.Drop();
            }

			server.SetDefaultInitFields(typeof(FullTextCatalog), true);
			for(int i=database.FullTextCatalogs.Count-1; i>=0; i--)
			{
				FullTextCatalog catalog=database.FullTextCatalogs[i];

				catalog.Drop();
			}
		}
	}
}
