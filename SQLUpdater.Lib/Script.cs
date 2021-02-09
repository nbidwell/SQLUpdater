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

using SQLUpdater.Lib.DBTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SQLUpdater.Lib
{
	/// <summary>
	/// A SQL script to execute
	/// </summary>
	public class Script: IComparable
	{
		private static string DELIMETERS=" \r\t\n.[]+-*/%&|!()";

		/// <summary>
		/// Gets or sets the name of the file the script was loaded from.
		/// </summary>
		/// <value>The name of the file the script was loaded from.</value>
		public string FileName { get; private set; }

		/// <summary>
		/// Gets or sets script name.
		/// </summary>
		/// <value>The script name.</value>
		public Name Name { get; private set; }

		/// <summary>
		/// Gets or sets the script body.
		/// </summary>
		/// <value>The script body.</value>
		public string Text { get; private set; }

		/// <summary>
		/// Gets or sets the script type.
		/// </summary>
		/// <value>The script type.</value>
		public ScriptType Type { get; private set; }

		/// <summary>
		/// Gets the script split into batches.
		/// </summary>
		/// <value>The script split into batches.</value>
		public List<string> Batches
		{
			get
			{
				//init
				List<string> batches=new List<string>();
				int lastBreak=0;
				int lastFound=0;
				Regex goFinder=new Regex(@"(\s+|^)(go)(\s+|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

				//Mark all of the areas that are escaped and we shouldn't split
				bool[] validMap=new bool[Text.Length];
				for(int x=0; x<validMap.Length; x++)
				{
					validMap[x]=true;
				}
				
				int i=0;
				while(i<Text.Length)
				{
					i=Text.IndexOfAny("/-'".ToCharArray(), i);  //bah, only finds single characters
					if(i<0)
					{
						//we're done!
						break;
					}
					if(!validMap[i])
					{
						throw new Exception("This should never happen");
					}

					//strings
					if(Text[i]=='\'')
					{
						do
						{
							validMap[i]=false;
							i++;
						} while(i<Text.Length && Text[i]!='\'');
						if(i<Text.Length)
						{
							validMap[i]=false;
							i++;
						}
						continue;
					}

					string twoChars="";
					if(i<Text.Length-1)
					{
						twoChars=Text.Substring(i, 2);
					}

					//single line comments
					if(twoChars=="--")
					{
						do
						{
							validMap[i]=false;
							i++;
						} while(i<Text.Length && Text[i]!='\n' && Text[i]!='\r');
						//trailing whitespace should be valid
						continue;
					}

					//multi line comments
					if(twoChars=="/*")
					{
						while(i<Text.Length)
						{
							validMap[i]=false;
							i++;

							if(i>1 && Text[i-2]=='*' && Text[i-1]=='/')
								break;
						}
						continue;
					}

					//prevent loops
					i++;
				}

				while(true)
				{
					//locate an endpoint
					Match found=goFinder.Match(Text, lastFound);
					if(!found.Success)
					{
						break;
					}
					lastFound=found.Groups[2].Index+found.Groups[2].Length;

					if(!validMap[found.Groups[2].Index])
					{
						continue;
					}

					if(found.Index>0)
					{
						string adding=Text.Substring(lastBreak, found.Index-lastBreak).Trim();
						if(adding.Length>0)
						{
							batches.Add(adding);
						}
					}
					lastBreak=lastFound;
				}

				if(lastBreak<Text.Length)
				{
					string adding=Text.Substring(lastBreak, Text.Length-lastBreak).Trim();
					if(adding.Length>0)
					{
						batches.Add(adding);
					}
				}

				return batches;
			}
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="Script"/> class.
        /// </summary>
        /// <param name="file">The script file to read in.</param>
        /// <param name="type">The script type.</param>
        public Script(FileInfo file, ScriptType type)
            : this(file, type, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Script"/> class.
        /// </summary>
        /// <param name="file">The script file to read in.</param>
        /// <param name="type">The script type.</param>
        /// <param name="name">The script name.</param>
		public Script(FileInfo file, ScriptType type, string name)
		{
			FileName=file.FullName;
			Name=name ?? file.Name.Replace(file.Extension, "");			
			using(StreamReader script=new StreamReader(file.FullName))
			{
				Text=script.ReadToEnd();
				Text=Text+"\n\rGO\n\r";
			}

			if(type==ScriptType.Unknown)
			{
				string lowerText=Text;
				if(lowerText.IndexOf("create procedure")>=0)
					Type=ScriptType.StoredProc;
				else if(lowerText.IndexOf("create trigger")>=0)
					Type=ScriptType.Trigger;
				else if(lowerText.IndexOf("create function")>=0)
					Type=ScriptType.UserDefinedFunction;
				else
					Type=ScriptType.Unknown;
			}
			else
			{
				Type=type;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Script"/> class.
		/// </summary>
		/// <param name="text">The script body.</param>
		/// <param name="name">The script name.</param>
		/// <param name="type">The script type.</param>
		public Script(string text, Name name, ScriptType type)
		{
			Name=name;
			Text=text;
			Type=type;
		}

        /// <summary>
        /// Append another script to this one
        /// </summary>
        /// <param name="adding">The script to append</param>
        public void Append(string adding)
        {
            if(adding!=null)
                Text=Text+"\r\n\r\n"+adding;
        }

        /// <summary>
        /// Append another script to this one
        /// </summary>
        /// <param name="adding">The script to append</param>
        public void Append(Script adding)
        {
            if(adding!=null)
                Append(adding.Text);
        }

        /// <summary>
        /// Append another script to this one
        /// </summary>
        /// <param name="adding">The script to append</param>
        public void Append(ScriptSet adding)
        {
            foreach (Script script in adding ?? new ScriptSet())
            {
                Append(script);
            }
        }

		/// <summary>
		/// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
		/// </summary>
		/// <param name="obj">An object to compare with this instance.</param>
		/// <returns>
		/// A 32-bit signed integer that indicates the relative order of the objects being compared. The return value has these meanings:
		/// Value
		/// Meaning
		/// Less than zero
		/// This instance is less than <paramref name="obj"/>.
		/// Zero
		/// This instance is equal to <paramref name="obj"/>.
		/// Greater than zero
		/// This instance is greater than <paramref name="obj"/>.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">
		/// 	<paramref name="obj"/> is not the same type as this instance.
		/// </exception>
		public int CompareTo(object obj)
		{
			Script other=(Script)obj;

			//order by type first
			if(Type!=other.Type)
				return Type.CompareTo(other.Type);

			//Script name should be unique
			if(Name==other.Name)
				return 0;

			//this should be first if the other script references it
			if(other.References(Name))
			{
					return -1;
			}

			//the other script should be first if this script references it
			if(References(other.Name))
			{
				return 1;
			}

			//default to equal
			return 0;
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <param name="obj">The <see cref="T:System.Object"/> to compare with the current <see cref="T:System.Object"/>.</param>
		/// <returns>
		/// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.
		/// </returns>
		/// <exception cref="T:System.NullReferenceException">
		/// The <paramref name="obj"/> parameter is null.
		/// </exception>
		public override bool Equals(object obj)
		{
			Script other=obj as Script;
			if(other==null)
				return false;

			//Name should be identifying
			return Name==other.Name && Type==other.Type;
		}

		/// <summary>
		/// Serves as a hash function for a particular type.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"/>.
		/// </returns>
		public override int GetHashCode()
		{
			return (Name.ToString()+"_"+Type).GetHashCode();
		}

		private bool References(Name name)
		{
			if(References(name.FullName) || References(name.Unescaped))
				return true;

			if(Name.Database!=name.Database || Name.Owner!=name.Owner)
				return false;

			SmallName small=(SmallName)name;
			return References(small) || References(small.Unescaped);
		}

		private bool References(string name)
		{
			int lastFound=-1;
			do
			{
				lastFound=Text.IndexOf(name, lastFound+1);
				if(lastFound>=0)
				{
					//check that the characters before & after the word are delimeters
					if(lastFound>0 && DELIMETERS.IndexOf(Text[lastFound-1])<0)
						continue;
					int nextCharIndex=lastFound+name.Length;
					if(nextCharIndex<Text.Length && DELIMETERS.IndexOf(Text[nextCharIndex])<0)
						continue;

					return true;
				}
			}while(lastFound>=0);

			return false;
		}
	}
}
