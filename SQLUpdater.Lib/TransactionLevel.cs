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

namespace SQLUpdater.Lib
{
	/// <summary>
	/// Transaction levels
	/// </summary>
	public enum TransactionLevel
	{
		/// <summary>
		/// No transaction
		/// </summary>
		None=0,

		/// <summary>
		/// Only a transaction around table updates
		/// </summary>
		TableOnly=1,

		/// <summary>
		/// A transaction around everything
		/// </summary>
		Everything=2
	}
}
