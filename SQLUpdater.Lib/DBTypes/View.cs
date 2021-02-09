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
	/// The parsed representation of a view.
	/// </summary>
	public class View : UnparsedItem
	{
		/// <summary>
		/// Gets or sets the indexes on this view.
		/// </summary>
		/// <value>The indexes on this view.</value>
		public HashArray<Index> Indexes { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="View"/> class.
		/// </summary>
		/// <param name="name">The view name.</param>
		/// <param name="body">The view definition.</param>
		public View(string name, string body) : base(name, body)
		{
			Indexes=new HashArray<Index>();
		}

		/// <summary>
		/// Generates a create script.
		/// </summary>
		/// <returns></returns>
		public override Script GenerateCreateScript()
		{
			return new Script(Body+"\r\nGO", Name, ScriptType.View);
		}

		/// <summary>
		/// Generates a drop script.
		/// </summary>
		/// <returns></returns>
		public override Script GenerateDropScript()
		{
			StringBuilder output=new StringBuilder();

			output.Append("if exists(\r\n\tselect 1\r\n\tfrom dbo.sysobjects");
			output.Append("\r\n\twhere type = 'V' AND id = object_id('"+Name.FullName+"')\r\n)\r\n");
			output.Append("DROP VIEW "+Name.FullName);
			output.Append("\r\n\r\nGO");

			return new Script(output.ToString(), Name, ScriptType.DropView);
		}
	}
}
