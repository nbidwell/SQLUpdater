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
	/// Verbosity levels for system output.
	/// </summary>
	public enum OutputLevel
	{
		/// <summary>
		/// Errors
		/// </summary>
		Errors=0,

		/// <summary>
		/// Differences
		/// </summary>
		Differences=1,

		/// <summary>
		/// Updates
		/// </summary>
		Updates=2,

		/// <summary>
		/// Reads
		/// </summary>
		Reads=3
	}
}
