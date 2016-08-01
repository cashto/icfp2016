using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solver;

namespace Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void CheckPrimes()
        {
            Assert.IsTrue(Program.isPrime(7));
            Assert.IsFalse(Program.isPrime(25));
            Assert.IsFalse(Program.isPrime(42));
            Assert.IsTrue(Program.isPrime(2));
        }
    }
}
