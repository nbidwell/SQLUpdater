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
	/// The parsed representation of a trigger.
	/// </summary>
	public class Trigger : UnparsedItem
	{
		/// <summary>
		/// Gets or sets the table the trigger is defined on.
		/// </summary>
		/// <value>The table the trigger is defined on.</value>
		public Name ReferencedTable { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="Trigger"/> class.
		/// </summary>
		/// <param name="name">The trigger name.</param>
		/// <param name="body">The trigger definition.</param>
		/// <param name="table">The table the trigger is defined on.</param>
		public Trigger(string name, string body, string table) : base(name, body)
		{
			ReferencedTable=table;
		}

		/// <summary>
		/// Generates a create script.
		/// </summary>
		/// <returns></returns>
		public override Script GenerateCreateScript()
		{
			return new Script(Body+"\r\nGO", Name, ScriptType.Trigger);
		}

		/// <summary>
		/// Generates a drop script.
		/// </summary>
		/// <returns></returns>
		public override Script GenerateDropScript()
		{
			StringBuilder output=new StringBuilder();

			output.Append("if exists(\r\n\tselect 1\r\n\tfrom dbo.sysobjects");
			output.Append("\r\n\twhere type = 'TR' AND id = object_id('"+Name.FullName+"')\r\n)\r\n");
			output.Append("DROP TRIGGER "+Name.FullName);
			output.Append("\r\n\r\nGO");

			return new Script(output.ToString(), Name, ScriptType.DropTrigger);
		}
	}
}
