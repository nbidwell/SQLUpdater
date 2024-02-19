using NUnit.Framework;
using NUnit.Framework.Legacy;
using SQLUpdater.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLUpdater.UnitTests
{
    [TestFixture]
    public class TokenSetTests
    {
        [Test]
        public void EquivalentToTest()
        {
            ClassicAssert.IsTrue(Tokenizer.Tokenize("").EquivalentTo(Tokenizer.Tokenize("")));

            ClassicAssert.IsTrue(Tokenizer.Tokenize("a").EquivalentTo(Tokenizer.Tokenize("a")));

            ClassicAssert.IsTrue(Tokenizer.Tokenize("a").EquivalentTo(Tokenizer.Tokenize("A")));

            ClassicAssert.IsFalse(Tokenizer.Tokenize("a").EquivalentTo(Tokenizer.Tokenize("b")));

            ClassicAssert.IsTrue(Tokenizer.Tokenize("(a)").EquivalentTo(Tokenizer.Tokenize("a")));

            ClassicAssert.IsTrue(Tokenizer.Tokenize("a").EquivalentTo(Tokenizer.Tokenize("(a)")));

            ClassicAssert.IsTrue(Tokenizer.Tokenize("(a)").EquivalentTo(Tokenizer.Tokenize("(a)")));

            ClassicAssert.IsFalse(Tokenizer.Tokenize("(a)").EquivalentTo(Tokenizer.Tokenize("b")));

            ClassicAssert.IsTrue(Tokenizer.Tokenize("( ( [a] ) + [b] )").EquivalentTo(Tokenizer.Tokenize("a + b")));
        }
    }
}
