using NUnit.Framework;
using SQLUpdater.Lib;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQLUpdater.UnitTests.ParserTests
{
	[TestFixture]
	public class BatchingTests : BaseUnitTest
	{
		[Test]
		public void BasicTest()
		{
			Script script=new Script("A GO B", "Basic", ScriptType.Unknown);
			Assert.AreEqual(2, script.Batches.Count);
			Assert.AreEqual("A", script.Batches[0]);
			Assert.AreEqual("B", script.Batches[1]);
		}

		[Test]
		public void BeginningTest()
		{
			Script script=new Script("GO B", "Basic", ScriptType.Unknown);
			Assert.AreEqual(1, script.Batches.Count);
			Assert.AreEqual("B", script.Batches[0]);
		}

		[Test]
		public void CommentedQuoteTest()
		{
			Script script=new Script(@"A -- '
GO", "Basic", ScriptType.Unknown);
			Assert.AreEqual(1, script.Batches.Count);
			Assert.AreEqual("A -- '", script.Batches[0]);

			script=new Script(@"A /* ' */
GO", "Basic", ScriptType.Unknown);
			Assert.AreEqual(1, script.Batches.Count);
			Assert.AreEqual("A /* ' */", script.Batches[0]);
		}

		[Test]
		public void EndTest()
		{
			Script script=new Script("A GO", "Basic", ScriptType.Unknown);
			Assert.AreEqual(1, script.Batches.Count);
			Assert.AreEqual("A", script.Batches[0]);
		}

		[Test]
		public void EscapedTest()
		{
			Script script=new Script("A [GO] B", "Escaped", ScriptType.Unknown);
			Assert.AreEqual(1, script.Batches.Count);
			Assert.AreEqual("A [GO] B", script.Batches[0]);
		}

		[Test]
		public void MultipleGoTest()
		{
			Script script=new Script(@"A GO GO GO GO", "Basic", ScriptType.Unknown);
			Assert.AreEqual(1, script.Batches.Count);
			Assert.AreEqual("A", script.Batches[0]);
		}

		[Test]
		public void MultipleLineCommentTest()
		{
			Script script=new Script("Blah /* Go */ Blah", "String", ScriptType.Unknown);
			Assert.AreEqual(1, script.Batches.Count);
			Assert.AreEqual("Blah /* Go */ Blah", script.Batches[0]);
		}

		[Test]
		public void SingleLineCommentTest()
		{
			Script script=new Script("Blah -- Go", "String", ScriptType.Unknown);
			Assert.AreEqual(1, script.Batches.Count);
			Assert.AreEqual("Blah -- Go", script.Batches[0]);
		}

		[Test]
		public void StringTest()
		{
			Script script=new Script("Blah 'just GO somewhere' blah", "String", ScriptType.Unknown);
			Assert.AreEqual(1, script.Batches.Count);
			Assert.AreEqual("Blah 'just GO somewhere' blah", script.Batches[0]);
		}

		[Test]
		public void SubstringTest()
		{
			Script script=new Script("Pogo GO Goto", "Substring", ScriptType.Unknown);
			Assert.AreEqual(2, script.Batches.Count);
			Assert.AreEqual("Pogo", script.Batches[0]);
			Assert.AreEqual("Goto", script.Batches[1]);
		}
	}
}
