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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SQLUpdater.Lib
{
	/// <summary>
	/// Difference types
	/// </summary>
	public enum DifferenceType
	{
		/// <summary>
		/// Item has been created
		/// </summary>
		Created,

		/// <summary>
		/// Item has new permissions
		/// </summary>
		CreatedPermission,

		/// <summary>
		/// Item needs to be re-created as a dependency of another item
		/// </summary>
		Dependency,

		/// <summary>
		/// Item has been modified
		/// </summary>
		Modified,

		/// <summary>
		/// Item has been removed
		/// </summary>
		Removed,

		/// <summary>
		/// Table data has been added or removed
		/// </summary>
		TableData
	}
}
