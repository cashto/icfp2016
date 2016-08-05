using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solver;

namespace Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestRationalNumbers()
        {
            var x = new RationalNumber(4, 6);
            var y = new RationalNumber(2, 3);
            Assert.IsTrue(x.Equals(y));

            var z = x * new RationalNumber(-3, 4);
            Assert.IsTrue(z.Equals(new RationalNumber(-1, 2)));
        }

        [TestMethod]
        public void TestLines()
        {
            var p1 = new Point(
                0,
                new RationalNumber(3, 2));
            var p2 = new Point(3, 0);

            var l1 = new Line(p1, p2);
            Assert.IsTrue(l1.ContainsPoint(p1));
            Assert.IsTrue(l1.ContainsPoint(p2));
            Assert.AreEqual(1, l1.a);
            Assert.AreEqual(2, l1.b);
            Assert.AreEqual(new RationalNumber(-3), l1.c);

            var l2 = new Line(
                new Point(0, new RationalNumber(1, 2)),
                new Point(2, 2));
            var i = l1.Intersect(l2);
            Assert.IsTrue(l1.ContainsPoint(i));
            Assert.IsTrue(l2.ContainsPoint(i));
        }

        [TestMethod]
        public void TestOrigami()
        {
            var o0 = new Origami();
            var o1 = o0.Fold(new Line(new Point(0, 1), new Point(1, 0)));
            var o2 = o1.Fold(new Line(
                new Point(0, new RationalNumber(1, 2)),
                new Point(1, new RationalNumber(1, 2))));
            Console.Out.WriteLine(o2.ToString());
        }

        [TestMethod]
        public void TestMatrix()
        {
            var p0 = new Point(0, 0);
            var p1 = new Point(0, new RationalNumber(3, 2));
            var p2 = new Point(3, new RationalNumber(0));

            var line = new Line(p1, p2);

            var m = Matrix.Reflect(line);
            Assert.AreEqual(p1, m.Transform(p1));
            Assert.AreEqual(p2, m.Transform(p2));
            Assert.AreEqual(line.Reflect(p0), m.Transform(p0));

            var I = m * m.Invert();
            Assert.AreEqual(Matrix.Identity, I);
        }
    }
}
