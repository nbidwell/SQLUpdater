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

namespace SQLUpdater
{
	/// <summary>
	/// A logger to display to the console
	/// </summary>
	public class ConsoleLogger : ILogger
	{
		private string hiddenMessage;
		private OutputLevel hiddenOutputLevel;

		/// <summary>
		/// Logs the specified message.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <param name="level">The message level.</param>
		public void Log(string message, OutputLevel level)
		{
			if(level <= RunOptions.Current.OutputLevel)
			{
                if (level == OutputLevel.Errors)
                {
                    if (hiddenMessage != null)
                    {
                        Console.Error.WriteLine(hiddenMessage);
                    }

                    Console.Error.WriteLine(message);
                }
                else
                {
                    Console.WriteLine(message);
                }
			}
			else
			{
				hiddenMessage=message;
				hiddenOutputLevel=level;
			}
		}
	}
}
