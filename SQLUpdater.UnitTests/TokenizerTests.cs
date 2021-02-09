using NUnit.Framework;
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
			Assert.AreEqual(0, tokens.Count);

            tokens = Tokenizer.Tokenize("/* Blah */").ToList();
			Assert.AreEqual(0, tokens.Count);

            tokens = Tokenizer.Tokenize("/* ' Blah */").ToList();
			Assert.AreEqual(0, tokens.Count);

            tokens = Tokenizer.Tokenize("/* ' Blah ' */").ToList();
			Assert.AreEqual(0, tokens.Count);

			tokens=Tokenizer.Tokenize(@"BEGIN BAR --oldest records first
END FOO").ToList();
			Assert.AreEqual(2, tokens.Count);

			Assert.AreEqual(0, tokens[0].StartIndex);
			Assert.AreEqual(36, tokens[0].EndIndex);
			Assert.AreEqual(TokenType.GroupBegin, tokens[0].Type);
			Assert.AreEqual("BEGIN", tokens[0].Value);
			Assert.AreEqual(2, tokens[0].Children.Count);

            Assert.AreEqual(6, tokens[0].Children.ToList()[0].StartIndex);
            Assert.AreEqual(8, tokens[0].Children.ToList()[0].EndIndex);
            Assert.AreEqual(TokenType.Identifier, tokens[0].Children.ToList()[0].Type);
            Assert.AreEqual("BAR", tokens[0].Children.ToList()[0].Value);

            Assert.AreEqual(34, tokens[0].Children.ToList()[1].StartIndex);
            Assert.AreEqual(36, tokens[0].Children.ToList()[1].EndIndex);
            Assert.AreEqual(TokenType.GroupEnd, tokens[0].Children.ToList()[1].Type);
            Assert.AreEqual("END", tokens[0].Children.ToList()[1].Value);

			Assert.AreEqual(38, tokens[1].StartIndex);
			Assert.AreEqual(40, tokens[1].EndIndex);
			Assert.AreEqual(TokenType.Identifier, tokens[1].Type);
			Assert.AreEqual("FOO", tokens[1].Value);
		}

        [Test]
        public void ParenthesesTest()
        {
            List<Token> tokens = Tokenizer.Tokenize("CREATE TABLE foo( a int, b char(1))").ToList();
            Assert.AreEqual(3, tokens.Count);

            Assert.AreEqual("CREATE", tokens[0].Value);
            Assert.AreEqual(0, tokens[0].Children.Count);
            Assert.AreEqual("TABLE", tokens[1].Value);
            Assert.AreEqual(0, tokens[1].Children.Count);
            Assert.AreEqual("foo", tokens[2].Value);
            Assert.AreEqual(1, tokens[2].Children.Count);

            Assert.AreEqual("(", tokens[2].Children.ToList()[0].Value);
            Assert.AreEqual(6, tokens[2].Children.ToList()[0].Children.Count);

            Assert.AreEqual("a", tokens[2].Children.ToList()[0].Children.ToList()[0].Value);
            Assert.AreEqual(0, tokens[2].Children.ToList()[0].Children.ToList()[0].Children.Count);
            Assert.AreEqual("int", tokens[2].Children.ToList()[0].Children.ToList()[1].Value);
            Assert.AreEqual(0, tokens[2].Children.ToList()[0].Children.ToList()[1].Children.Count);
            Assert.AreEqual(",", tokens[2].Children.ToList()[0].Children.ToList()[2].Value);
            Assert.AreEqual(0, tokens[2].Children.ToList()[0].Children.ToList()[2].Children.Count);
            Assert.AreEqual("b", tokens[2].Children.ToList()[0].Children.ToList()[3].Value);
            Assert.AreEqual(0, tokens[2].Children.ToList()[0].Children.ToList()[3].Children.Count);
            Assert.AreEqual("char", tokens[2].Children.ToList()[0].Children.ToList()[4].Value);
            Assert.AreEqual(1, tokens[2].Children.ToList()[0].Children.ToList()[4].Children.Count);
            Assert.AreEqual(")", tokens[2].Children.ToList()[0].Children.ToList()[5].Value);
            Assert.AreEqual(0, tokens[2].Children.ToList()[0].Children.ToList()[5].Children.Count);

            Assert.AreEqual("(", tokens[2].Children.ToList()[0].Children.ToList()[4].Children.ToList()[0].Value);
            Assert.AreEqual(2, tokens[2].Children.ToList()[0].Children.ToList()[4].Children.ToList()[0].Children.Count);

            Assert.AreEqual("1", tokens[2].Children.ToList()[0].Children.ToList()[4].Children.ToList()[0].Children.ToList()[0].Value);
            Assert.AreEqual(0, tokens[2].Children.ToList()[0].Children.ToList()[4].Children.ToList()[0].Children.ToList()[0].Children.Count);
            Assert.AreEqual(")", tokens[2].Children.ToList()[0].Children.ToList()[4].Children.ToList()[0].Children.ToList()[1].Value);
            Assert.AreEqual(0, tokens[2].Children.ToList()[0].Children.ToList()[4].Children.ToList()[0].Children.ToList()[1].Children.Count);

            tokens = Tokenizer.Tokenize("BEGIN SELECT 1 END").ToList();
            Assert.AreEqual(1, tokens.Count);

            Assert.AreEqual("BEGIN", tokens[0].Value);
            Assert.AreEqual(3, tokens[0].Children.Count);
            Assert.AreEqual("SELECT", tokens[0].Children.ToList()[0].Value);
            Assert.AreEqual(0, tokens[0].Children.ToList()[0].Children.Count);
            Assert.AreEqual("1", tokens[0].Children.ToList()[1].Value);
            Assert.AreEqual(0, tokens[0].Children.ToList()[1].Children.Count);
            Assert.AreEqual("END", tokens[0].Children.ToList()[2].Value);
            Assert.AreEqual(0, tokens[0].Children.ToList()[2].Children.Count);

            tokens = Tokenizer.Tokenize("FOO((1))").ToList();
            Assert.AreEqual(1, tokens.Count);

            Assert.AreEqual("FOO", tokens[0].Value);
            Assert.AreEqual(1, tokens[0].Children.Count);

            Assert.AreEqual("(", tokens[0].Children.ToList()[0].Value);
            Assert.AreEqual(2, tokens[0].Children.ToList()[0].Children.Count);

            Assert.AreEqual("(", tokens[0].Children.ToList()[0].Children.ToList()[0].Value);
            Assert.AreEqual(2, tokens[0].Children.ToList()[0].Children.ToList()[0].Children.Count);
            Assert.AreEqual(")", tokens[0].Children.ToList()[0].Children.ToList()[1].Value);
            Assert.AreEqual(0, tokens[0].Children.ToList()[0].Children.ToList()[1].Children.Count);

            Assert.AreEqual("1", tokens[0].Children.ToList()[0].Children.ToList()[0].Children.ToList()[0].Value);
            Assert.AreEqual(0, tokens[0].Children.ToList()[0].Children.ToList()[0].Children.ToList()[0].Children.Count);
            Assert.AreEqual(")", tokens[0].Children.ToList()[0].Children.ToList()[0].Children.ToList()[1].Value);
            Assert.AreEqual(0, tokens[0].Children.ToList()[0].Children.ToList()[0].Children.ToList()[1].Children.Count);

        }

		[Test]
		public void QuoteTest()
		{
            List<Token> tokens = Tokenizer.Tokenize("'Quoted token''s content '''").ToList();
			Assert.AreEqual(1, tokens.Count);
			Assert.AreEqual(0, tokens[0].StartIndex);
			Assert.AreEqual(27, tokens[0].EndIndex);
			Assert.AreEqual(TokenType.StringValue, tokens[0].Type);
			Assert.AreEqual("'Quoted token''s content '''", tokens[0].Value);

            tokens = Tokenizer.Tokenize("''''").ToList();
			Assert.AreEqual(1, tokens.Count);
			Assert.AreEqual(0, tokens[0].StartIndex);
			Assert.AreEqual(3, tokens[0].EndIndex);
			Assert.AreEqual(TokenType.StringValue, tokens[0].Type);
			Assert.AreEqual("''''", tokens[0].Value);

            tokens = Tokenizer.Tokenize("blah '''' blah").ToList();
			Assert.AreEqual(3, tokens.Count);

			Assert.AreEqual(0, tokens[0].StartIndex);
			Assert.AreEqual(3, tokens[0].EndIndex);
			Assert.AreEqual(TokenType.Identifier, tokens[0].Type);
			Assert.AreEqual("blah", tokens[0].Value);

			Assert.AreEqual(5, tokens[1].StartIndex);
			Assert.AreEqual(8, tokens[1].EndIndex);
			Assert.AreEqual(TokenType.StringValue, tokens[1].Type);
			Assert.AreEqual("''''", tokens[1].Value);

			Assert.AreEqual(10, tokens[2].StartIndex);
			Assert.AreEqual(13, tokens[2].EndIndex);
			Assert.AreEqual(TokenType.Identifier, tokens[2].Type);
			Assert.AreEqual("blah", tokens[2].Value);

            tokens = Tokenizer.Tokenize("'--'").ToList();
			Assert.AreEqual(1, tokens.Count);

			Assert.AreEqual(0, tokens[0].StartIndex);
			Assert.AreEqual(3, tokens[0].EndIndex);
			Assert.AreEqual(TokenType.StringValue, tokens[0].Type);
			Assert.AreEqual("'--'", tokens[0].Value);

            tokens = Tokenizer.Tokenize("'[a]', 'b'").ToList();
			Assert.AreEqual(3, tokens.Count);

			Assert.AreEqual(0, tokens[0].StartIndex);
			Assert.AreEqual(4, tokens[0].EndIndex);
			Assert.AreEqual(TokenType.StringValue, tokens[0].Type);
			Assert.AreEqual("'[a]'", tokens[0].Value);

			Assert.AreEqual(5, tokens[1].StartIndex);
			Assert.AreEqual(5, tokens[1].EndIndex);
			Assert.AreEqual(TokenType.Separator, tokens[1].Type);
			Assert.AreEqual(",", tokens[1].Value);

			Assert.AreEqual(7, tokens[2].StartIndex);
			Assert.AreEqual(9, tokens[2].EndIndex);
			Assert.AreEqual(TokenType.StringValue, tokens[2].Type);
			Assert.AreEqual("'b'", tokens[2].Value);
		}

        [Test]
        public void SemicolonTests()
        {
            List<Token> tokens = Tokenizer.Tokenize("BEGIN SELECT 1 END;").ToList();
			Assert.AreEqual(2, tokens.Count);

            Assert.AreEqual("BEGIN", tokens[0].Value);
			Assert.AreEqual(TokenType.GroupBegin, tokens[0].Type);
			Assert.AreEqual(3, tokens[0].Children.Count);

			Assert.AreEqual("SELECT", tokens[0].Children.ToList()[0].Value);
            Assert.AreEqual(TokenType.Keyword, tokens[0].Children.ToList()[0].Type);
            Assert.AreEqual(0, tokens[0].Children.ToList()[0].Children.Count);

			Assert.AreEqual("1", tokens[0].Children.ToList()[1].Value);
            Assert.AreEqual(TokenType.Number, tokens[0].Children.ToList()[1].Type);
            Assert.AreEqual(0, tokens[0].Children.ToList()[1].Children.Count);

			Assert.AreEqual("END", tokens[0].Children.ToList()[2].Value);
            Assert.AreEqual(TokenType.GroupEnd, tokens[0].Children.ToList()[2].Type);
            Assert.AreEqual(0, tokens[0].Children.ToList()[2].Children.Count);

			Assert.AreEqual(";", tokens[1].Value);
			Assert.AreEqual(TokenType.Semicolon, tokens[1].Type);
			Assert.AreEqual(0, tokens[1].Children.Count);
        }

		[Test]
		public void TokenizeSelectTest()
		{
            List<Token> tokens = Tokenizer.Tokenize("SELECT a+1 FROM b.c").ToList();
			Assert.AreEqual(4, tokens.Count);

			Assert.AreEqual("SELECT", tokens[0].Value);
			Assert.AreEqual(TokenType.Keyword, tokens[0].Type);
			Assert.AreEqual(0, tokens[0].Children.Count);

			Assert.AreEqual("+", tokens[1].Value);
			Assert.AreEqual(TokenType.Operator, tokens[1].Type);
			Assert.AreEqual(2, tokens[1].Children.Count);

            Assert.AreEqual("a", tokens[1].Children.ToList()[0].Value);
            Assert.AreEqual(TokenType.Identifier, tokens[1].Children.ToList()[0].Type);
            Assert.AreEqual(0, tokens[1].Children.ToList()[0].Children.Count);

            Assert.AreEqual("1", tokens[1].Children.ToList()[1].Value);
            Assert.AreEqual(TokenType.Number, tokens[1].Children.ToList()[1].Type);
            Assert.AreEqual(0, tokens[1].Children.ToList()[1].Children.Count);

			Assert.AreEqual("FROM", tokens[2].Value);
			Assert.AreEqual(TokenType.Keyword, tokens[2].Type);
			Assert.AreEqual(0, tokens[2].Children.Count);

			Assert.AreEqual("b.c", tokens[3].Value);
			Assert.AreEqual(TokenType.Identifier, tokens[3].Type);
			Assert.AreEqual(0, tokens[3].Children.Count);
		}

		[Test]
		public void UnicodeTest()
		{
			TokenSet tokens=Tokenizer.Tokenize("N'NOON'");
			Assert.AreEqual(1, tokens.Count);
			Assert.AreEqual("'NOON'", tokens.First.Value);
		}
	}
}
