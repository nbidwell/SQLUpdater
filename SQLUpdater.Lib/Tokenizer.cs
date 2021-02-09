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
using System.IO;
using System.Text.RegularExpressions;

namespace SQLUpdater.Lib
{
	/// <summary>
	/// Tokenizer
	/// </summary>
	public class Tokenizer
	{
		private const string OPERATORS="+-*/%&|^=><~!";
		private static int parserFile;

		/// <summary>
		/// Creates a tree from a set of tokens.
		/// </summary>
		/// <param name="tokens">The tokens.</param>
		private static TokenSet CreateTree(TokenSet tokens)
		{
			CreateTree_Grouping(tokens);
			DisplayHTMLParseStep(tokens, "After grouping", false);

			CreateTree_Case(tokens);
			DisplayHTMLParseStep(tokens, "After case statements", false);

			CreateTree_Operator(tokens);
			DisplayHTMLParseStep(tokens, "After operators", false);

			//must be before not
			CreateTree_Exists(tokens);
			DisplayHTMLParseStep(tokens, "After exists", false);

			//must be before and
			CreateTree_Not(tokens);
			DisplayHTMLParseStep(tokens, "After not", false);

			CreateTree_AndOr(tokens);
			DisplayHTMLParseStep(tokens, "After and &amp; or", false);

            return tokens;
		}

		private static void CreateTree_AndOr(TokenSet tokens)
		{
			//treat "and" and "or" as operators too
            TokenEnumerator enumerator=tokens.GetEnumerator();
			while(enumerator.MoveNext())
			{
				//make sure this is the start of a new group
				Token starter=enumerator.Current;
				if(starter.Type!=TokenType.Keyword || starter.Children.Count>1 
					|| (starter.Value.ToLower()!="and" && starter.Value.ToLower()!="or"))
				{
					CreateTree_AndOr(starter.Children);
					continue;
				}

				if(starter.Children.Count==0 && enumerator.Next!=null)
				{
					starter.Children.Add(enumerator.Next);
                    enumerator.RemoveNext();
				}
				if(enumerator.Previous!=null)
				{
					starter.Children.AddToBeginning(enumerator.Previous);
                    enumerator.RemovePrevious();
				}

				CreateTree_AndOr(starter.Children);
			}
		}

		private static void CreateTree_Case(TokenSet tokens)
		{
            //group together case statements
            TokenEnumerator enumerator = tokens.GetEnumerator();
            while (enumerator.MoveNext())
            {
				//make sure this is the start of a new group
				Token starter=enumerator.Current;
				if(starter.Type!=TokenType.CaseStatement || starter.Children.Count>0)
				{
					CreateTree_Case(starter.Children);
					continue;
				}

				int groupdepth=1;
				while(enumerator.Next!=null && groupdepth>0)
				{
					Token child=enumerator.Next;
					starter.Children.Add(child);
                    enumerator.RemoveNext();

					if(child.Value.ToLower()=="end")
					{
						groupdepth--;
					}
					else if(child.Value.ToLower()=="case")
					{
						groupdepth++;
					}
				}

				CreateTree_Case(starter.Children);
			}
		}

		private static void CreateTree_Exists(TokenSet tokens)
		{
            //take care of the exists keyword
            TokenEnumerator enumerator = tokens.GetEnumerator();
            while (enumerator.MoveNext())
			{
				//make sure this is the start of a new group
				Token starter=enumerator.Current;
				if(starter.Type!=TokenType.Keyword || starter.Children.Count>0 || starter.Value.ToLower()!="exists")
				{
					CreateTree_Exists(starter.Children);
					continue;
				}

				starter.Children.Add(enumerator.Next);
				enumerator.RemoveNext();

				//make a tree of those children too
				CreateTree_Exists(starter.Children);
			}
		}

		private static void CreateTree_Grouping(TokenSet tokens)
		{
            //start with grouping constructs
            TokenEnumerator enumerator = tokens.GetEnumerator();
            while (enumerator.MoveNext())
			{
				//make sure this is the start of a group
                if (enumerator.Current.Type != TokenType.GroupBegin)
                {
                    continue;
                }

				//pull in all children
                Stack<Token> GroupStarters=new Stack<Token>();
                GroupStarters.Push(enumerator.Current);

                //push the group under its predecessor for functions...
                if (enumerator.Previous!=null && enumerator.Previous.Children.Count == 0
                    && (enumerator.Previous.Type == TokenType.Identifier || enumerator.Previous.Type == TokenType.Unknown))
                {
                    enumerator.Previous.Children.Add(enumerator.Current);
                    enumerator.RemoveCurrent();
                }

				while(GroupStarters.Count>0)
				{
                    enumerator.MoveNext();
					if(!enumerator.IsValid)
					{
                        throw new ApplicationException("Unclosed " + GroupStarters.Peek().Value);
					}

					Token child=enumerator.Current;
					if(child==null)
					{
                        throw new ApplicationException("Unclosed " + GroupStarters.Peek().Value);
					}
                    enumerator.RemoveCurrent();

                    Token group=GroupStarters.Peek();
                    Token last=group.Children.Count>0 ? group.Children.Last : null;
                    if (last!=null && last.Children.Count==0
                        && (last.Type==TokenType.Identifier || last.Type==TokenType.Unknown)
                        && (child.Type==TokenType.GroupBegin  || child.Type==TokenType.CaseStatement))
                    {
                        //push the group under its predecessor for functions...
                        last.Children.Add(child);
                    }
                    else
                    {
                        group.Children.Add(child);
                    }

					if(child.Type==TokenType.GroupBegin  || child.Type==TokenType.CaseStatement)
					{
                        GroupStarters.Push(child);
					}
					else if(child.Type==TokenType.GroupEnd)
					{
                        GroupStarters.Pop();
					}
				}
            }
        }

		private static void CreateTree_Not(TokenSet tokens)
		{
            //take care of the not keyword
            TokenEnumerator enumerator = tokens.GetEnumerator();
            while (enumerator.MoveNext())
			{
				//make sure this is the start of a new group
                Token starter = enumerator.Current;
				if(starter.Type!=TokenType.Keyword || starter.Value.ToLower()!="not" || starter.Children.Count>0)
				{
					CreateTree_Not(starter.Children);
					continue;
				}

				starter.Children.Add(enumerator.Next);
				enumerator.RemoveNext();

				//make a tree of those children too
				CreateTree_Not(starter.Children);
			}
		}

		private static void CreateTree_Operator(TokenSet tokens)
		{
            //work on operators
            TokenEnumerator enumerator = tokens.GetEnumerator();
            while (enumerator.MoveNext())
			{
				//make sure this is the start of a new group
                Token starter = enumerator.Current;
				if(starter.Type!=TokenType.Operator || starter.Children.Count>0)
				{
					CreateTree_Operator(starter.Children);
					continue;
				}

				Token previous=null;
				if(enumerator.Previous!=null && starter.Value!="!" && starter.Value!="~")
				{
                    previous = enumerator.Previous;
				}
				Token next=enumerator.Next;

				//don't bury keywords in the tree
				if(previous!=null && (previous.Type==TokenType.Keyword || previous.Type==TokenType.Separator))
				{
					CreateTree_Operator(starter.Children);
					continue;
				}

				//add previous operand if not unary (don't remove - screws up adding of next operand)
				if(previous!=null && previous.Type!=TokenType.Comment && previous.Type!=TokenType.Keyword
					&& previous.Type!=TokenType.StringValue)
				{
					starter.Children.Add(previous);
				}
				else
				{
					previous=null;
				}

				//add next operand
				while(next!=null)
				{
					starter.Children.Add(next);
					enumerator.RemoveNext();

					if(next.Type==TokenType.Operator)
					{
						next=enumerator.Next;
					}
					else
					{
						next=null;
					}
				}

				//remove any previous operand
				if(previous!=null)
				{
					enumerator.RemovePrevious();
				}

				//make a tree of those children too
				CreateTree_Operator(starter.Children);
			}
		}
		
		private static void DisplayHTMLParseStep(TokenSet tokens, string message, bool start)
		{
			if(RunOptions.Current.ParserOutput==null)
				return;

			StreamWriter writer=null;
			try
			{
				if(start)
				{
					parserFile++;
					string fileName=Path.Combine(RunOptions.Current.ParserOutput, parserFile.ToString())+".html";
					writer=new StreamWriter(fileName, false);
					writer.Write(@"<head>
<style type='text/css'>
.caseStatement{ border: solid 1px maroon; color: maroon; margin: 2px; }
.dot{ border: solid 1px purple; color: purple; margin: 2px; }
.escapedName{ border: solid 1px green; color: green; margin: 2px; }
.groupBegin{ border: solid 1px blue; color: blue; margin: 2px; }
.groupEnd{ border: solid 1px blue; color: blue; margin: 2px; }
.keyword{ border: solid 1px teal; color: teal; margin: 2px; }
.message{ font-weight: bolder; padding-top: 20px; }
.missing{ border: solid 1px red; color: red; margin: 2px; }
.operator{ border: solid 1px maroon; color: maroon; margin: 2px; }
.outer{ border: solid 1px black; width: 100% }
.quote{ border: solid 1px orange; width: 100% }
.separator{ border: solid 1px gray; color: gray; margin: 2px; }
.string{ border: solid 1px brown; color: brown; margin: 2px; }
.unknown{ border: solid 1px black; color: black; margin: 2px; }
.token{ background-color: linen; padding-left: 2px; }
div.token:hover{ border-width: 3px; }
</style>
</head>
<body>");
				}
				else
				{
					string fileName=Path.Combine(RunOptions.Current.ParserOutput, parserFile.ToString())+".html";
					writer=new StreamWriter(fileName, true);
				}

				writer.Write("<div class='message'>"+message+"</div>");
				writer.Write("<div class='outer'>\n");
				DisplayTokenHTML(writer, tokens);
				writer.Write("</div>\n");
			}
			finally
			{
				if(writer!=null)
					writer.Close();
			}
		}

		private static void DisplayTokenHTML(StreamWriter writer, TokenSet tokens)
		{
			foreach(Token token in tokens)
			{
				DisplayTokenHTML(writer, token);
			}
		}

		private static void DisplayTokenHTML(StreamWriter writer, Token token)
		{
			switch(token.Type)
			{
				case TokenType.CaseStatement:
					writer.Write("<div class='caseStatement token' title='case statement'>");
					writer.Write(HTMLEscape(token.Value));
					DisplayTokenHTML(writer, token.Children);
					writer.Write("</div>");
					break;

				case TokenType.Dot:
					writer.Write("<div class='dot token' title='dot'>");
					writer.Write(HTMLEscape(token.Value));
					DisplayTokenHTML(writer, token.Children);
					writer.Write("</div>");
					break;

				case TokenType.Identifier:
					writer.Write("<div class='escapedName token' title='escaped name'>");
					writer.Write(HTMLEscape(token.Value));
					DisplayTokenHTML(writer, token.Children);
					writer.Write("</div>");
					break;

				case TokenType.GroupBegin:
					writer.Write("<div class='groupBegin token' title='group begin'>");
					writer.Write(HTMLEscape(token.Value));
					DisplayTokenHTML(writer, token.Children);
					writer.Write("</div>");
					break;

				case TokenType.GroupEnd:
					writer.Write("<div class='groupEnd token' title='group end'>");
					writer.Write(HTMLEscape(token.Value));
					DisplayTokenHTML(writer, token.Children);
					writer.Write("</div>");
					break;

				case TokenType.Keyword:
					writer.Write("<div class='keyword token' title='keyword'>");
					writer.Write(HTMLEscape(token.Value));
					DisplayTokenHTML(writer, token.Children);
					writer.Write("</div>");
					break;

				case TokenType.Operator:
					writer.Write("<div class='operator token' title='operator'>");
					if(token.Children.Count<2)
					{
						writer.Write(HTMLEscape(token.Value));
						DisplayTokenHTML(writer, token.Children);
					}
					else
					{
						DisplayTokenHTML(writer, token.Children.First);
						writer.Write(HTMLEscape(token.Value));
                        bool first = true;
                        foreach (Token displaying in token.Children)
                        {
                            if (first)
                            {
                                first = false;
                                continue;
                            }
                            DisplayTokenHTML(writer, displaying);
                        }
					}
					writer.Write("</div>");
					break;

				case TokenType.Quote:
					writer.Write("<div class='quote token' title='quote'>");
					writer.Write(HTMLEscape(token.Value));
					DisplayTokenHTML(writer, token.Children);
					writer.Write("</div>");
					break;

				case TokenType.Separator:
					writer.Write("<div class='separator token' title='separator'>");
					writer.Write(HTMLEscape(token.Value));
					DisplayTokenHTML(writer, token.Children);
					writer.Write("</div>");
					break;

				case TokenType.StringValue:
					writer.Write("<div class='string token' title='string'>");
					writer.Write(HTMLEscape(token.Value));
					DisplayTokenHTML(writer, token.Children);
					writer.Write("</div>");
					break;

				case TokenType.Unknown:
					writer.Write("<div class='unknown token' title='unknown'>");
					writer.Write(HTMLEscape(token.Value));
					DisplayTokenHTML(writer, token.Children);
					writer.Write("</div>");
					break;

				default:
					writer.Write("<div class='missing token' title='missing type'>");
					writer.Write(HTMLEscape(token.Value));
					DisplayTokenHTML(writer, token.Children);
					writer.Write("</div>");
					break;
			}
		}

		private static string HTMLEscape(string val)
		{
			return val.Replace(" ", "&nbsp;").Replace("\n", "<br>");
		}

		private static void IdentifyRemainingTokens(TokenSet tokens)
		{
			//identify the tokens
			foreach(Token token in tokens)
			{
				if(token.Type==TokenType.Dot)
				{
					token.Type=TokenType.Identifier;
					continue;
				}

				//only work on unidentified tokens
				if(token.Type!=TokenType.Unknown)
					continue;

				if(Regex.IsMatch(token.Value, "^[0-9\\.]+$", RegexOptions.Compiled))
				{
					token.Type=TokenType.Number;
					continue;
				}

				token.Type=TokenType.Identifier;
			}
		}

		private static void IdentifySpecialTokens(TokenSet tokens)
		{
			//identify the tokens
			foreach(Token token in tokens)
			{
				//only work on unidentified tokens
				if(token.Type!=TokenType.Unknown)
					continue;

				//identify the operators
				if(token.Value.Length==1 && OPERATORS.IndexOf(token.Value)>-1)
				{
					token.Type=TokenType.Operator;
					continue;
				}

				//identify variables
				if(token.Value.StartsWith("@"))
				{
					token.Type=TokenType.Variable;
					continue;
				}

				//pull other types
				switch(token.Value.ToLower())
				{
					case ".":
						token.Type=TokenType.Dot;
						break;

					case ";":
						token.Type=TokenType.Semicolon;
						break;

					case "begin":
					case "(":
						token.Type=TokenType.GroupBegin;
						break;

					case "case":
						token.Type=TokenType.CaseStatement;
						break;

					case "end":
					case ")":
						token.Type=TokenType.GroupEnd;
						break;

					case "add":
					case "alter":
					case "and":
					case "clustered":
					case "collate":
					case "constraint":
					case "create":
					case "default":
					case "drop":
					case "else":
					case "exists":
					case "for":
					case "from":
					case "function":
					case "go":
					case "identity":
					case "if":
					case "in":
					case "index":
					case "is":
					case "key":
					case "nocheck":
					case "nonclustered":
					case "not":
					case "null":
					case "on":
					case "or":
					case "proc":
					case "procedure":
					case "primary":
					case "select":
					case "table":
					case "tran":
					case "transaction":
					case "trigger":
					case "then":
					case "unique":
					case "view":
					case "when":
					case "where":
					case "with":
						token.Type=TokenType.Keyword;
						break;

					case ",":
						token.Type=TokenType.Separator;
						break;

					case "'":
					case "\"":
						token.Type=TokenType.Quote;
						break;
				}
			}

            //fix misidentified tokens
            TokenEnumerator enumerator = tokens.GetEnumerator();
            while (enumerator.MoveNext())
			{
                Token previous = enumerator.Previous;
                Token current = enumerator.Current;
                Token next = enumerator.Next;

                if (current.Type == TokenType.GroupBegin && next.Type == TokenType.Keyword
                    && current.Value.ToLower() == "begin" && next.Value.ToLower().StartsWith("tran"))
				{
                    current.Type = TokenType.Keyword;
				}
                else if (previous!=null && current.Type == TokenType.GroupBegin
					&& previous.Type==TokenType.Quote && next.Type==TokenType.Quote)
				{
                    current.Type = TokenType.StringValue;
				}
			}

			//coalese operators and things with dots
            enumerator = tokens.GetEnumerator();
            enumerator.MoveLast();
            while (enumerator.MovePrevious())
			{
                Token previous = enumerator.Previous;
                Token current = enumerator.Current;
                Token next = enumerator.Next;

				//do the coalesce but don't screw up +5 * -2
				if(next!=null && current.Type==TokenType.Operator && next.Type==TokenType.Operator
					&& next.Value!="-" && next.Value!="+")
				{
                    current.Value = current.Value + next.Value;
                    enumerator.RemoveNext();

                    continue;
				}
                else if(current.Type==TokenType.Dot && previous!=null && next!=null
					&& (previous.Type==TokenType.Unknown || previous.Type==TokenType.Identifier || previous.Type==TokenType.Dot)
					&& (next.Type==TokenType.Unknown || next.Type==TokenType.Identifier || next.Type==TokenType.Dot))
				{
					current.StartIndex=previous.StartIndex;
					current.Value=previous.FlattenTree()+"."+next.FlattenTree();
                    enumerator.RemovePrevious();
                    enumerator.RemoveNext();

                    continue;
				}
			}
		}

		private static void RemoveComments(TokenSet tokens)
        {
            TokenEnumerator enumerator = tokens.GetEnumerator();
            while (enumerator.MoveNext())
			{
				if(enumerator.Current.Type==TokenType.Comment)
				{
                    enumerator.RemoveCurrent();
				}
				else
				{
					RemoveComments(enumerator.Current.Children);
				}
			}
		}

		private static int ShouldSkip(Token source, int found, string noMatch)
		{
			if(noMatch==null)
				return 0;

			if(source.Value.Length>=found+noMatch.Length && source.Value.Substring(found, noMatch.Length)==noMatch)
			{
				return noMatch.Length;
			}
			return 0;
		}

		/// <summary>
		/// Tokenizes the specified script.
		/// </summary>
		/// <param name="script">The script.</param>
		/// <returns></returns>
		public static TokenSet Tokenize(string script)
		{
			parserFile=-1;

			Token seed=new Token(script, TokenType.Unknown, 0);
			TokenSet start=new TokenSet();
			start.Add(seed);
			DisplayHTMLParseStep(start, "Starting set", true);

			//pull out tokens for comments, strings, escaped names, etc
			TokenSet escapedTokens=TokenizeDelimited(seed);

			//pull apart everything else on whitespace
			TokenSet whitespaceTokens=new TokenSet();
			foreach(Token token in escapedTokens)
			{
				if(token.Type==TokenType.Unknown)
				{
					//make life easier by creating optional whitespace
					string spread=token.Value;
					foreach(char op in (OPERATORS+",.();").ToCharArray())
					{
						spread=spread.Replace(op.ToString(), " "+op+" ");
					}
					int offset=0;
					foreach(string piece in spread.Split(" \t\r\n".ToCharArray()))
					{
						if(piece.Length==0)
							continue;

						offset=token.Value.IndexOf(piece, offset);
						whitespaceTokens.Add(new Token(piece, TokenType.Unknown, token.StartIndex+offset));
						offset+=piece.Length;  //don't find the same text twice if it's repeated
					}
				}
				else
				{
					whitespaceTokens.Add(token);
				}
			}

			//remove bogus tokens
			TokenSet finalTokens=new TokenSet();
			TokenEnumerator enumerator=whitespaceTokens.GetEnumerator();
			while(enumerator.MoveNext())
			{
				//empty tokens
				if(enumerator.Current.Value=="")
					continue;

				//bogus unicode string markings
				if(enumerator.Current.Value=="N" && enumerator.Next!=null && enumerator.Next.Type==TokenType.StringValue)
					continue;

				finalTokens.Add(enumerator.Current);
			}
			DisplayHTMLParseStep(finalTokens, "After empty tokens removed", false);

			//comments gum things up
			RemoveComments(finalTokens);
			DisplayHTMLParseStep(finalTokens, "After comments removed", false);

			//Categorization
			IdentifySpecialTokens(finalTokens);
			DisplayHTMLParseStep(finalTokens, "After identifying special tokens", false);

			IdentifyRemainingTokens(finalTokens);
			DisplayHTMLParseStep(finalTokens, "After identifying remaining tokens", false);

			//associate the tokens with each other
			finalTokens = CreateTree(finalTokens);
			DisplayHTMLParseStep(finalTokens, "After tree creation", false);

			return finalTokens;
		}

		private static TokenSet TokenizeDelimited(Token source)
		{
			string[] startDelimiters={"/*", "--", "'", "["};
			string[] endDelimiters={"*/", "\n", "'", "]"};
			string[] noMatches={null, null, "''", null};
			TokenType[] tokenTypes={TokenType.Comment, TokenType.Comment, TokenType.StringValue, TokenType.Identifier};

			string startDelimiter="START";
            int[] startFound = new int[startDelimiters.Length];
			string endDelimiter="END";
			string noMatch=null;
			TokenType tokenType=TokenType.Unknown;

            for (int i = 0; i < startDelimiters.Length; i++)
            {
                startFound[i] = source.Value.IndexOf(startDelimiters[i]);
            }

			TokenSet tokens=new TokenSet();
			int offset=0;
			int skipped=0;
			int start=0;
			bool tokenStarted=false;
			while(start<source.Value.Length)
			{
				int found=-1;
				if(tokenStarted)
				{
					found=source.Value.IndexOf(endDelimiter, start+skipped);
				}
				else
				{
                    for (int i = 0; i < startDelimiters.Length; i++)
                    {
                        if (startFound[i] != -1 && startFound[i] < start)
                        {
                            startFound[i] = source.Value.IndexOf(startDelimiters[i], start);
                        }
                    }

					//These things all escape whatever is found within them, so find the first one
					for(int i=0; i<startDelimiters.Length; i++)
					{
                        int justFound=startFound[i];
						if(justFound>=0 && (found<0 || justFound<found))
						{
							found=justFound;
							startDelimiter=startDelimiters[i];
							endDelimiter=endDelimiters[i];
							noMatch=noMatches[i];
							tokenType=tokenTypes[i];
						}
					}
				}

				string token;
				TokenType addingType;
				if(found<0)
				{
					if(endDelimiter=="\n")
					{
						//newlines are actually optional
						addingType=tokenStarted ? tokenType : TokenType.Unknown;
						tokenStarted=false;
					}
					else
					{
						addingType=TokenType.Unknown;
					}
					token=source.Value.Substring(start, source.Value.Length-start);
					start=source.Value.Length;
				}
				else if(tokenStarted)
				{
					//don't split on things we should leave in the middle
					int skipLength=ShouldSkip(source, found, noMatch);
					if(skipLength>0)
					{
						skipped=found-start+skipLength;
						continue;
					}

					token=source.Value.Substring(start-1,
						found-start+1+endDelimiter.Length);
					addingType=tokenType;
					start=found+endDelimiter.Length;
					tokenStarted=false;

					//do some validation
					if(!token.StartsWith(startDelimiter) || !token.EndsWith(endDelimiter))
					{
						throw new ApplicationException("Bad tokenizing!");
					}
				}
				else
				{
					token=source.Value.Substring(start, found-start);
					addingType=TokenType.Unknown;
					start=found+1;
					tokenStarted=true;
				}
				offset=source.Value.IndexOf(token, offset);
				tokens.Add(new Token(token, addingType, source.StartIndex+offset));
				offset+=token.Length; //don't find the same text twice if it's repeated
				skipped=0;
			}
			if(tokenStarted)
			{
				throw new ApplicationException("Tokenizer error, found \""+startDelimiter
					+"\" without \""+endDelimiter+"\".");
			}

			return tokens;
		}
	}
}
