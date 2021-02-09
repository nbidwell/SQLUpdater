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

using SQLUpdater.Lib;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLUpdater.UnitTests
{
	public class TestLogger : ILogger
	{
		public List<OutputLevel> levels=new List<OutputLevel>();
		public List<string> messages=new List<string>();

		public void Log(string message, OutputLevel level)
		{
			levels.Add(level);
			messages.Add(message);
		}

		public override string ToString()
		{
			StringBuilder builder=new StringBuilder();

			foreach(string message in messages)
			{
				builder.AppendLine(message);
			}

			return builder.ToString();
		}
	}
}
