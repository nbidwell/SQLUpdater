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
using System.Collections.Generic;
using System.Text;

namespace SQLUpdater.Lib.DBTypes
{
	/// <summary>
	/// The parsed representation of a stored procedure.
	/// </summary>
	public class Procedure : UnparsedItem
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Procedure"/> class.
		/// </summary>
		/// <param name="name">The stored procedure name.</param>
		/// <param name="body">The procedure definition.</param>
		public Procedure(string name, string body) : base(name, body)
		{
		}

		/// <summary>
		/// Generates a create script.
		/// </summary>
		/// <returns></returns>
		public override Script GenerateCreateScript()
		{
			return new Script(Body+"\r\nGO", Name, ScriptType.StoredProc);
		}

		/// <summary>
		/// Generates a drop script.
		/// </summary>
		/// <returns></returns>
		public override Script GenerateDropScript()
		{
			StringBuilder output=new StringBuilder();

			output.Append("DROP PROCEDURE "+Name.FullName);
			output.Append("\r\n\r\nGO");

			return new Script(output.ToString(), Name, ScriptType.DropStoredProc);
		}
	}
}
