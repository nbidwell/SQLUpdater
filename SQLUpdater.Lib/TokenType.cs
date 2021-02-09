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
	/// Types of tokens that can be generated
	/// </summary>
	public enum TokenType
	{
		/// <summary>
		/// Case Statement
		/// </summary>
		CaseStatement,

		/// <summary>
		/// Comment
		/// </summary>
		Comment,

		/// <summary>
		/// Dot
		/// </summary>
		Dot,

		/// <summary>
		/// Identifier
		/// </summary>
		Identifier,

		/// <summary>
		/// Grouping Beginning
		/// </summary>
		GroupBegin,

		/// <summary>
		/// Grouping End
		/// </summary>
		GroupEnd,

		/// <summary>
		/// Keyword
		/// </summary>
		Keyword,

		/// <summary>
		/// Number
		/// </summary>
		Number,

		/// <summary>
		/// Operator
		/// </summary>
		Operator,

		/// <summary>
		/// Quote
		/// </summary>
		Quote,

        /// <summary>
        /// Semicolon
        /// </summary>
        Semicolon,

		/// <summary>
		/// Separator
		/// </summary>
		Separator,

		/// <summary>
		/// String Value
		/// </summary>
		StringValue,

		/// <summary>
		/// Unknown
		/// </summary>
		Unknown,

		/// <summary>
		/// Variable
		/// </summary>
		Variable
	}
}
