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

namespace SQLUpdater.Lib
{
	/// <summary>
	/// Classes of scripts to be executed
	/// </summary>
	/// <remarks>Ordered for execution, lowest to be executed first</remarks>
	public enum ScriptType
	{
		/// <summary>
		/// Drop a fulltext index
		/// </summary>
		/// <remarks>This must happen outside of a transaction</remarks>
		DropFulltextIndex,

		/// <summary>
		/// Drop a fulltext catalog
		/// </summary>
		DropFulltextCatalog,

		//---------------
		//  Drop scripts
		//---------------

		/// <summary>
		/// Stop ordering a trigger
		/// </summary>
		DropTriggerOrder,

		/// <summary>
		/// Drop a foreign key
		/// </summary>
		DropForeignKey,

		/// <summary>
		/// Drop a primary key
		/// </summary>
		DropPrimaryKey,

		/// <summary>
		/// Drop an index
		/// </summary>
		DropIndex,

		/// <summary>
		/// Drop an index
		/// </summary>
		DropClusteredIndex,

		/// <summary>
		/// Drop a constraint
		/// </summary>
		DropConstraint,

		/// <summary>
		/// Drop a user defined function
		/// </summary>
		DropUserDefinedFunction,

		/// <summary>
		/// Disable a constraint
		/// </summary>
		DisableConstraint,

		/// <summary>
		/// Drop a trigger
		/// </summary>
		DropTrigger,

		/// <summary>
		/// Drop a stored procedure
		/// </summary>
		DropStoredProc,

		/// <summary>
		/// Drop a view
		/// </summary>
		DropView,

		/// <summary>
		/// Remove table data
		/// </summary>
		TableRemoveData,

		/// <summary>
		/// Save table data
		/// </summary>
		TableSaveData,

		/// <summary>
		/// Drop a table
		/// </summary>
		DropTable,

		/// <summary>
		/// Drop a table type
		/// </summary>
		DropTableType,

		//---------------
		//  Indexable items
		//---------------

		/// <summary>
		/// Table type
		/// </summary>
		TableType,

		/// <summary>
		/// Table
		/// </summary>
		Table,

		/// <summary>
		/// User defined function
		/// </summary>
		/// <remarks>Views can depend on this</remarks>
		UserDefinedFunction,

		/// <summary>
		/// View
		/// </summary>
		View,

		//---------------
		//  Indexes and constraints
		//---------------

		/// <summary>
		/// Primary key
		/// </summary>
		PrimaryKey,

		/// <summary>
		/// Unique constraint
		/// </summary>
		UniqueConstraint,

		/// <summary>
		/// Clustered index
		/// </summary>
		ClusteredIndex,

		/// <summary>
		/// Index
		/// </summary>
		Index,

		/// <summary>
		/// Default constraint
		/// </summary>
		DefaultConstraint,

		/// <summary>
		/// Check constraint
		/// </summary>
		CheckConstraint,

		/// <summary>
		/// Enable a constraint
		/// </summary>
		EnableConstraint,

		//---------------
		//  Data
		//---------------

		/// <summary>
		/// New table data
		/// </summary>
		TableData,

		/// <summary>
		/// Restore table data
		/// </summary>
		TableRestoreData,

		//---------------
		//  Data dependant
		//---------------
		
		/// <summary>
		/// Foreign key
		/// </summary>
		/// <remarks>Allow all data in first</remarks>
		ForeignKey,

		/// <summary>
		/// Fulltext catalog
		/// </summary>
		FulltextCatalog,

		/// <summary>
		/// Fulltext index
		/// </summary>
		/// <remarks>Must happen outside of a transaction</remarks>
		FulltextIndex,

		//---------------

		/// <summary>
		/// Trigger
		/// </summary>
		Trigger,

		/// <summary>
		/// Trigger order
		/// </summary>
		TriggerOrder,

		/// <summary>
		/// Stored procedure
		/// </summary>
		StoredProc,

		/// <summary>
		/// Permission
		/// </summary>
		Permission,

		//---------------

		/// <summary>
		/// Unknown
		/// </summary>
		Unknown
	}
}
