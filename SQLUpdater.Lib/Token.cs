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
using System.Text;

namespace SQLUpdater.Lib
{
	/// <summary>
	/// Summary description for Token.
	/// </summary>
	public class Token
	{
		/// <summary>
		/// Gets or sets the set of child tokens
		/// </summary>
		/// <value>The child tokens.</value>
		public TokenSet Children {get; private set;}

		/// <summary>
		/// Gets or sets the ending index of this token in the source script.
		/// </summary>
		/// <value>The end index of this token in the source script.</value>
		public int EndIndex
		{
			get
			{
				int endIndex=StartIndex+Value.Length-1;
				if(Children.Count>0)
				{
					endIndex=Math.Max(endIndex, Children.Last.EndIndex);
				}
				return endIndex;
			}
		}

		/// <summary>
		/// Gets or sets the starting index of this token in the source script.
		/// </summary>
		/// <value>The start index of this token in the source script.</value>
		public int StartIndex {get; set;}

		/// <summary>
		/// Gets or sets the token type.
		/// </summary>
		/// <value>The token type.</value>
		public TokenType Type { get; set; }

		/// <summary>
		/// Gets or sets the token value.
		/// </summary>
		/// <value>The token value.</value>
		public string Value { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="Token"/> class.
		/// </summary>
		/// <param name="tokenValue">The token value.</param>
		/// <param name="type">The token type.</param>
		/// <param name="startIndex">The start index in the source script.</param>
		public Token(string tokenValue, TokenType type, int startIndex)
		{
			if(tokenValue==null)
			{
				throw new ArgumentNullException("tokenValue", "Null value passed");
			}

			Children=new TokenSet();
			StartIndex=startIndex+tokenValue.IndexOf(tokenValue.Trim());  //correct for trim
			Type=type;
			Value=tokenValue.Trim();
		}

		/// <summary>
		/// Flattens this token and its children into a single string.
		/// </summary>
		/// <returns></returns>
		public string FlattenTree()
		{
            return FlattenTree(false);
        }

		/// <summary>
		/// Flattens this token and its children into a single string.
		/// </summary>
		/// <returns></returns>
		public string FlattenTree(bool escapeValues)
		{
            string val = Value;
            if (escapeValues && this.Type == TokenType.Identifier)
            {
                val = new DBTypes.SmallName(val).ToString();
            }

			StringBuilder retVal=new StringBuilder();
			if(Type==TokenType.Dot || Type==TokenType.Operator)
			{
                if (Children.Count > 2)
                    throw new Exception("Unexpected child count");

				//if 2 operands, value is middle otherwise value 1st
				if(Children.Count>1)
				{
                    retVal.Append(Children.First.FlattenTree(escapeValues));
					if(Type==TokenType.Operator)
					{
						retVal.Append(" ");
					}
				}
				retVal.Append(val);
				if(Children.Count>1)
				{
					if(Type==TokenType.Operator)
					{
						retVal.Append(" ");
					}
                    retVal.Append(Children.Last.FlattenTree(escapeValues));
				}
				else if(Children.Count>0)
				{
                    retVal.Append(Children.First.FlattenTree(escapeValues));
				}
			}
			else if(Value.ToLower()=="and" || Value.ToLower()=="or")
            {
                if (Children.Count > 2)
                    throw new Exception("Unexpected child count");

				//if 2 operands, value is middle otherwise value 1st
				if(Children.Count>1)
				{
                    retVal.Append(Children.First.FlattenTree(escapeValues) + " ");
				}
				retVal.Append(val);
				if(Children.Count>1)
				{
                    retVal.Append(" " + Children.Last.FlattenTree(escapeValues));
				}
				else if(Children.Count>0)
				{
                    retVal.Append(" " + Children.First.FlattenTree(escapeValues));
				}
			}
			else
			{
				retVal.Append(val);
				foreach(Token child in Children)
				{
					retVal.Append(" "+child.FlattenTree(escapeValues));
				}
			}
			return retVal.ToString();
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </returns>
		public override string ToString()
		{
			return "Token(Value: \""+Value+"\", Type: \""+Type+"\")";
		}

	}
}
