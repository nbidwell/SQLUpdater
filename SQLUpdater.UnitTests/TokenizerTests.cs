using NUnit.Framework;
using NUnit.Framework.Legacy;
using SQLUpdater.Lib;
using SQLUpdater.Lib.DBTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SQLUpdater.UnitTests
{
	[TestFixture]
	public class TokenizerTests : BaseUnitTest
	{
		[Test]
		public void CommentTest()
		{
			List<Token> tokens=Tokenizer.Tokenize("--Blah").ToList();
			ClassicAssert.AreEqual(0, tokens.Count);

            tokens = Tokenizer.Tokenize("/* Blah */").ToList();
			ClassicAssert.AreEqual(0, tokens.Count);

            tokens = Tokenizer.Tokenize("/* ' Blah */").ToList();
			ClassicAssert.AreEqual(0, tokens.Count);

            tokens = Tokenizer.Tokenize("/* ' Blah ' */").ToList();
			ClassicAssert.AreEqual(0, tokens.Count);

			tokens=Tokenizer.Tokenize(@"BEGIN BAR --oldest records first
END FOO").ToList();
			ClassicAssert.AreEqual(2, tokens.Count);

			ClassicAssert.AreEqual(0, tokens[0].StartIndex);
			ClassicAssert.AreEqual(36, tokens[0].EndIndex);
			ClassicAssert.AreEqual(TokenType.GroupBegin, tokens[0].Type);
			ClassicAssert.AreEqual("BEGIN", tokens[0].Value);
			ClassicAssert.AreEqual(2, tokens[0].Children.Count);

            ClassicAssert.AreEqual(6, tokens[0].Children.ToList()[0].StartIndex);
            ClassicAssert.AreEqual(8, tokens[0].Children.ToList()[0].EndIndex);
            ClassicAssert.AreEqual(TokenType.Identifier, tokens[0].Children.ToList()[0].Type);
            ClassicAssert.AreEqual("BAR", tokens[0].Children.ToList()[0].Value);

            ClassicAssert.AreEqual(34, tokens[0].Children.ToList()[1].StartIndex);
            ClassicAssert.AreEqual(36, tokens[0].Children.ToList()[1].EndIndex);
            ClassicAssert.AreEqual(TokenType.GroupEnd, tokens[0].Children.ToList()[1].Type);
            ClassicAssert.AreEqual("END", tokens[0].Children.ToList()[1].Value);

			ClassicAssert.AreEqual(38, tokens[1].StartIndex);
			ClassicAssert.AreEqual(40, tokens[1].EndIndex);
			ClassicAssert.AreEqual(TokenType.Identifier, tokens[1].Type);
			ClassicAssert.AreEqual("FOO", tokens[1].Value);
		}

        [Test]
        public void ParenthesesTest()
        {
            List<Token> tokens = Tokenizer.Tokenize("CREATE TABLE foo( a int, b char(1))").ToList();
            ClassicAssert.AreEqual(3, tokens.Count);

            ClassicAssert.AreEqual("CREATE", tokens[0].Value);
            ClassicAssert.AreEqual(0, tokens[0].Children.Count);
            ClassicAssert.AreEqual("TABLE", tokens[1].Value);
            ClassicAssert.AreEqual(0, tokens[1].Children.Count);
            ClassicAssert.AreEqual("foo", tokens[2].Value);
            ClassicAssert.AreEqual(1, tokens[2].Children.Count);

            ClassicAssert.AreEqual("(", tokens[2].Children.ToList()[0].Value);
            ClassicAssert.AreEqual(6, tokens[2].Children.ToList()[0].Children.Count);

            ClassicAssert.AreEqual("a", tokens[2].Children.ToList()[0].Children.ToList()[0].Value);
            ClassicAssert.AreEqual(0, tokens[2].Children.ToList()[0].Children.ToList()[0].Children.Count);
            ClassicAssert.AreEqual("int", tokens[2].Children.ToList()[0].Children.ToList()[1].Value);
            ClassicAssert.AreEqual(0, tokens[2].Children.ToList()[0].Children.ToList()[1].Children.Count);
            ClassicAssert.AreEqual(",", tokens[2].Children.ToList()[0].Children.ToList()[2].Value);
            ClassicAssert.AreEqual(0, tokens[2].Children.ToList()[0].Children.ToList()[2].Children.Count);
            ClassicAssert.AreEqual("b", tokens[2].Children.ToList()[0].Children.ToList()[3].Value);
            ClassicAssert.AreEqual(0, tokens[2].Children.ToList()[0].Children.ToList()[3].Children.Count);
            ClassicAssert.AreEqual("char", tokens[2].Children.ToList()[0].Children.ToList()[4].Value);
            ClassicAssert.AreEqual(1, tokens[2].Children.ToList()[0].Children.ToList()[4].Children.Count);
            ClassicAssert.AreEqual(")", tokens[2].Children.ToList()[0].Children.ToList()[5].Value);
            ClassicAssert.AreEqual(0, tokens[2].Children.ToList()[0].Children.ToList()[5].Children.Count);

            ClassicAssert.AreEqual("(", tokens[2].Children.ToList()[0].Children.ToList()[4].Children.ToList()[0].Value);
            ClassicAssert.AreEqual(2, tokens[2].Children.ToList()[0].Children.ToList()[4].Children.ToList()[0].Children.Count);

            ClassicAssert.AreEqual("1", tokens[2].Children.ToList()[0].Children.ToList()[4].Children.ToList()[0].Children.ToList()[0].Value);
            ClassicAssert.AreEqual(0, tokens[2].Children.ToList()[0].Children.ToList()[4].Children.ToList()[0].Children.ToList()[0].Children.Count);
            ClassicAssert.AreEqual(")", tokens[2].Children.ToList()[0].Children.ToList()[4].Children.ToList()[0].Children.ToList()[1].Value);
            ClassicAssert.AreEqual(0, tokens[2].Children.ToList()[0].Children.ToList()[4].Children.ToList()[0].Children.ToList()[1].Children.Count);

            tokens = Tokenizer.Tokenize("BEGIN SELECT 1 END").ToList();
            ClassicAssert.AreEqual(1, tokens.Count);

            ClassicAssert.AreEqual("BEGIN", tokens[0].Value);
            ClassicAssert.AreEqual(3, tokens[0].Children.Count);
            ClassicAssert.AreEqual("SELECT", tokens[0].Children.ToList()[0].Value);
            ClassicAssert.AreEqual(0, tokens[0].Children.ToList()[0].Children.Count);
            ClassicAssert.AreEqual("1", tokens[0].Children.ToList()[1].Value);
            ClassicAssert.AreEqual(0, tokens[0].Children.ToList()[1].Children.Count);
            ClassicAssert.AreEqual("END", tokens[0].Children.ToList()[2].Value);
            ClassicAssert.AreEqual(0, tokens[0].Children.ToList()[2].Children.Count);

            tokens = Tokenizer.Tokenize("FOO((1))").ToList();
            ClassicAssert.AreEqual(1, tokens.Count);

            ClassicAssert.AreEqual("FOO", tokens[0].Value);
            ClassicAssert.AreEqual(1, tokens[0].Children.Count);

            ClassicAssert.AreEqual("(", tokens[0].Children.ToList()[0].Value);
            ClassicAssert.AreEqual(2, tokens[0].Children.ToList()[0].Children.Count);

            ClassicAssert.AreEqual("(", tokens[0].Children.ToList()[0].Children.ToList()[0].Value);
            ClassicAssert.AreEqual(2, tokens[0].Children.ToList()[0].Children.ToList()[0].Children.Count);
            ClassicAssert.AreEqual(")", tokens[0].Children.ToList()[0].Children.ToList()[1].Value);
            ClassicAssert.AreEqual(0, tokens[0].Children.ToList()[0].Children.ToList()[1].Children.Count);

            ClassicAssert.AreEqual("1", tokens[0].Children.ToList()[0].Children.ToList()[0].Children.ToList()[0].Value);
            ClassicAssert.AreEqual(0, tokens[0].Children.ToList()[0].Children.ToList()[0].Children.ToList()[0].Children.Count);
            ClassicAssert.AreEqual(")", tokens[0].Children.ToList()[0].Children.ToList()[0].Children.ToList()[1].Value);
            ClassicAssert.AreEqual(0, tokens[0].Children.ToList()[0].Children.ToList()[0].Children.ToList()[1].Children.Count);

        }

		[Test]
		public void QuoteTest()
		{
            List<Token> tokens = Tokenizer.Tokenize("'Quoted token''s content '''").ToList();
			ClassicAssert.AreEqual(1, tokens.Count);
			ClassicAssert.AreEqual(0, tokens[0].StartIndex);
			ClassicAssert.AreEqual(27, tokens[0].EndIndex);
			ClassicAssert.AreEqual(TokenType.StringValue, tokens[0].Type);
			ClassicAssert.AreEqual("'Quoted token''s content '''", tokens[0].Value);

            tokens = Tokenizer.Tokenize("''''").ToList();
			ClassicAssert.AreEqual(1, tokens.Count);
			ClassicAssert.AreEqual(0, tokens[0].StartIndex);
			ClassicAssert.AreEqual(3, tokens[0].EndIndex);
			ClassicAssert.AreEqual(TokenType.StringValue, tokens[0].Type);
			ClassicAssert.AreEqual("''''", tokens[0].Value);

            tokens = Tokenizer.Tokenize("blah '''' blah").ToList();
			ClassicAssert.AreEqual(3, tokens.Count);

			ClassicAssert.AreEqual(0, tokens[0].StartIndex);
			ClassicAssert.AreEqual(3, tokens[0].EndIndex);
			ClassicAssert.AreEqual(TokenType.Identifier, tokens[0].Type);
			ClassicAssert.AreEqual("blah", tokens[0].Value);

			ClassicAssert.AreEqual(5, tokens[1].StartIndex);
			ClassicAssert.AreEqual(8, tokens[1].EndIndex);
			ClassicAssert.AreEqual(TokenType.StringValue, tokens[1].Type);
			ClassicAssert.AreEqual("''''", tokens[1].Value);

			ClassicAssert.AreEqual(10, tokens[2].StartIndex);
			ClassicAssert.AreEqual(13, tokens[2].EndIndex);
			ClassicAssert.AreEqual(TokenType.Identifier, tokens[2].Type);
			ClassicAssert.AreEqual("blah", tokens[2].Value);

            tokens = Tokenizer.Tokenize("'--'").ToList();
			ClassicAssert.AreEqual(1, tokens.Count);

			ClassicAssert.AreEqual(0, tokens[0].StartIndex);
			ClassicAssert.AreEqual(3, tokens[0].EndIndex);
			ClassicAssert.AreEqual(TokenType.StringValue, tokens[0].Type);
			ClassicAssert.AreEqual("'--'", tokens[0].Value);

            tokens = Tokenizer.Tokenize("'[a]', 'b'").ToList();
			ClassicAssert.AreEqual(3, tokens.Count);

			ClassicAssert.AreEqual(0, tokens[0].StartIndex);
			ClassicAssert.AreEqual(4, tokens[0].EndIndex);
			ClassicAssert.AreEqual(TokenType.StringValue, tokens[0].Type);
			ClassicAssert.AreEqual("'[a]'", tokens[0].Value);

			ClassicAssert.AreEqual(5, tokens[1].StartIndex);
			ClassicAssert.AreEqual(5, tokens[1].EndIndex);
			ClassicAssert.AreEqual(TokenType.Separator, tokens[1].Type);
			ClassicAssert.AreEqual(",", tokens[1].Value);

			ClassicAssert.AreEqual(7, tokens[2].StartIndex);
			ClassicAssert.AreEqual(9, tokens[2].EndIndex);
			ClassicAssert.AreEqual(TokenType.StringValue, tokens[2].Type);
			ClassicAssert.AreEqual("'b'", tokens[2].Value);
		}

        [Test]
        public void SemicolonTests()
        {
            List<Token> tokens = Tokenizer.Tokenize("BEGIN SELECT 1 END;").ToList();
			ClassicAssert.AreEqual(2, tokens.Count);

            ClassicAssert.AreEqual("BEGIN", tokens[0].Value);
			ClassicAssert.AreEqual(TokenType.GroupBegin, tokens[0].Type);
			ClassicAssert.AreEqual(3, tokens[0].Children.Count);

			ClassicAssert.AreEqual("SELECT", tokens[0].Children.ToList()[0].Value);
            ClassicAssert.AreEqual(TokenType.Keyword, tokens[0].Children.ToList()[0].Type);
            ClassicAssert.AreEqual(0, tokens[0].Children.ToList()[0].Children.Count);

			ClassicAssert.AreEqual("1", tokens[0].Children.ToList()[1].Value);
            ClassicAssert.AreEqual(TokenType.Number, tokens[0].Children.ToList()[1].Type);
            ClassicAssert.AreEqual(0, tokens[0].Children.ToList()[1].Children.Count);

			ClassicAssert.AreEqual("END", tokens[0].Children.ToList()[2].Value);
            ClassicAssert.AreEqual(TokenType.GroupEnd, tokens[0].Children.ToList()[2].Type);
            ClassicAssert.AreEqual(0, tokens[0].Children.ToList()[2].Children.Count);

			ClassicAssert.AreEqual(";", tokens[1].Value);
			ClassicAssert.AreEqual(TokenType.Semicolon, tokens[1].Type);
			ClassicAssert.AreEqual(0, tokens[1].Children.Count);
        }

		[Test]
		public void TokenizeSelectTest()
		{
            List<Token> tokens = Tokenizer.Tokenize("SELECT a+1 FROM b.c").ToList();
			ClassicAssert.AreEqual(4, tokens.Count);

			ClassicAssert.AreEqual("SELECT", tokens[0].Value);
			ClassicAssert.AreEqual(TokenType.Keyword, tokens[0].Type);
			ClassicAssert.AreEqual(0, tokens[0].Children.Count);

			ClassicAssert.AreEqual("+", tokens[1].Value);
			ClassicAssert.AreEqual(TokenType.Operator, tokens[1].Type);
			ClassicAssert.AreEqual(2, tokens[1].Children.Count);

            ClassicAssert.AreEqual("a", tokens[1].Children.ToList()[0].Value);
            ClassicAssert.AreEqual(TokenType.Identifier, tokens[1].Children.ToList()[0].Type);
            ClassicAssert.AreEqual(0, tokens[1].Children.ToList()[0].Children.Count);

            ClassicAssert.AreEqual("1", tokens[1].Children.ToList()[1].Value);
            ClassicAssert.AreEqual(TokenType.Number, tokens[1].Children.ToList()[1].Type);
            ClassicAssert.AreEqual(0, tokens[1].Children.ToList()[1].Children.Count);

			ClassicAssert.AreEqual("FROM", tokens[2].Value);
			ClassicAssert.AreEqual(TokenType.Keyword, tokens[2].Type);
			ClassicAssert.AreEqual(0, tokens[2].Children.Count);

			ClassicAssert.AreEqual("b.c", tokens[3].Value);
			ClassicAssert.AreEqual(TokenType.Identifier, tokens[3].Type);
			ClassicAssert.AreEqual(0, tokens[3].Children.Count);
		}

		[Test]
		public void UnicodeTest()
		{
			TokenSet tokens=Tokenizer.Tokenize("N'NOON'");
			ClassicAssert.AreEqual(1, tokens.Count);
			ClassicAssert.AreEqual("'NOON'", tokens.First.Value);
		}
	}
}
