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

namespace SQLUpdater.Lib
{
	/// <summary>
	/// An enumerator for a token set
	/// </summary>
	public class TokenEnumerator : IEnumerator<Token>
	{
        bool complete = false;
        LinkedListNode<Token> currentNode;
        private LinkedListNode<Token> firstNode;

		/// <summary>
		/// Gets the current token.
		/// </summary>
		/// <value>The current token.</value>
        public Token Current
        {
            get { return currentNode.Value; }
        }

		/// <summary>
		/// Gets the current token.
		/// </summary>
		/// <value>The current token.</value>
        object System.Collections.IEnumerator.Current
        {
            get { return currentNode.Value; }
        }

        /// <summary>
        /// Is the enumerator in a valid state?
        /// </summary>
        public bool IsValid
        {
            get { return currentNode != null; }
        }

		/// <summary>
		/// Gets the next token.
		/// </summary>
		/// <value>The next token.</value>
		public Token Next
		{
			get
			{
                return currentNode.Next == null ? null : currentNode.Next.Value;
			}
		}

        /// <summary>
        /// Gets the previous token.
        /// </summary>
        /// <value>The previous token.</value>
        public Token Previous
		{
			get
			{
                return currentNode.Previous == null ? null : currentNode.Previous.Value;
			}
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenEnumerator"/> class.
        /// </summary>
        /// <param name="firstNode">The first node.</param>
        public TokenEnumerator(LinkedListNode<Token> firstNode)
		{
            this.firstNode = firstNode;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            currentNode = null;
            firstNode = null;
        }

        /// <summary>
        /// Moves to the last Token.
        /// </summary>
        public bool MoveLast()
        {
            currentNode = firstNode==null ? null : firstNode.List.Last;
            return currentNode != null;
        }

		/// <summary>
		/// Moves to the next token.
		/// </summary>
		public bool MoveNext()
		{
            if (currentNode == null)
            {
                if (complete)
                    return false;
                currentNode = firstNode;
            }
            else
            {
                currentNode = currentNode.Next;
            }

            complete = currentNode == null;
            return currentNode != null;
		}

		/// <summary>
		/// Moves to the previous token.
		/// </summary>
		public bool MovePrevious()
		{
            if (currentNode == null)
                return false;

            currentNode = currentNode.Previous;
            return currentNode != null;
		}

        /// <summary>
        /// Removes the current token.
        /// </summary>
        public void RemoveCurrent()
        {
            if (firstNode == currentNode)
                firstNode = currentNode.Next;

            LinkedListNode<Token> current = currentNode;
            currentNode = currentNode.Previous;
            current.List.Remove(current);
        }

        /// <summary>
        /// Removes the next Token.
        /// </summary>
        public void RemoveNext()
        {
            currentNode.List.Remove(currentNode.Next);
        }

        /// <summary>
        /// Removes the previous Token.
        /// </summary>
        public void RemovePrevious()
        {
            if (currentNode.Previous == firstNode)
                firstNode = currentNode;

            currentNode.List.Remove(currentNode.Previous);
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        public void Reset()
        {
            complete = false;
            currentNode = null;
        }
    }
}
