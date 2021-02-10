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

namespace SQLUpdater.Lib
{
	/// <summary>
	/// A list of Tokens
	/// </summary>
	public class TokenSet : IEnumerable<Token>
	{
        private LinkedList<Token> tokens;

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>The count.</value>
        public int Count
        {
            get { return tokens.Count; }
        }

        /// <summary>
        /// Gets the first Token in the set.
        /// </summary>
        public Token First
        {
            get { return tokens.First==null ? null : tokens.First.Value; }
        }

        /// <summary>
        /// Gets the last Token in the set.
        /// </summary>
        public Token Last
        {
            get { return tokens.Last == null ? null : tokens.Last.Value; }
        }

        /// <summary>
        /// Gets the second Token in the set.
        /// </summary>
        public Token Second
        {
            get { return tokens.Count < 2 ? null : tokens.First.Next.Value; }
        }

        /// <summary>
        /// Gets the third Token in the set.
        /// </summary>
        public Token Third
        {
            get { return tokens.Count < 3 ? null : tokens.First.Next.Next.Value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenSet"/> class.
        /// </summary>
        public TokenSet()
        {
            tokens = new LinkedList<Token>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenSet"/> class.
        /// </summary>
        /// <param name="values">The values.</param>
        public TokenSet(IEnumerable<Token> values)
        {
            tokens = new LinkedList<Token>(values);
        }

        /// <summary>
        /// Adds the specified token.
        /// </summary>
        /// <param name="token">The token.</param>
        public void Add(Token token)
        {
            tokens.AddLast(token);
        }

        /// <summary>
        /// Adds the specified token at the beginning of the set.
        /// </summary>
        /// <param name="token">The token.</param>
        public void AddToBeginning(Token token)
        {
            tokens.AddFirst(token);
        }

        /// <summary>
        /// Removes any redundant grouping constructs
        /// </summary>
        public void CleanGrouping(Token parent)
        {
            foreach (Token token in tokens)
            {
                //Make sure all of this token's children are clean
                token.Children.CleanGrouping(token);

                //Find redundant nesting and eliminate it
                if (token.Value == "(" && token.Children.Count == 2 && parent.Type != TokenType.Identifier)
                {
                    Token target = token.Children.First;
                    token.Children.Clear();

                    token.StartIndex = target.StartIndex;
                    token.Type = target.Type;
                    token.Value = target.Value;

                    foreach (Token child in target.Children)
                    {
                        token.Children.Add(child);
                    }
                }
            }
        }

        /// <summary>
        /// Removes all tokens from the set.
        /// </summary>
        public void Clear()
        {
            tokens.Clear();
        }

		/// <summary>
		/// Flattens this token set and its children into a single string.
		/// </summary>
		/// <returns></returns>
		public string FlattenTree()
		{
			StringBuilder retVal=new StringBuilder();

			foreach(Token token in tokens)
			{
				retVal.Append(" "+token.FlattenTree());
			}

			return retVal.ToString().Trim();
		}

		/// <summary>
		/// Returns an enumerator that iterates through a collection.
		/// </summary>
		/// <returns>
		/// An <see cref="TokenEnumerator"/> object that can be used to iterate through the collection.
		/// </returns>
		public TokenEnumerator GetEnumerator()
		{
			return new TokenEnumerator(tokens.First);
        }

        IEnumerator<Token> IEnumerable<Token>.GetEnumerator()
        {
            return new TokenEnumerator(tokens.First);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new TokenEnumerator(tokens.First);
        }

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </returns>
		public override string ToString()
		{
			if(tokens.Count==0)
				return "";

			StringBuilder retVal=new StringBuilder("(");
			foreach(Token token in tokens)
			{
				retVal.Append(token.Value+" "+token.Children.ToString());
			}
			retVal.Append(") ");
			return retVal.ToString();
		}
    }
}
