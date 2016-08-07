using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solver;
using System.Collections.Generic;
using System.Linq;

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
            Assert.AreEqual(-1, l1.a);
            Assert.AreEqual(-2, l1.b);
            Assert.AreEqual(new RationalNumber(3), l1.c);

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
            var o1 = o0.Fold(new Line(new Point(1, 0), new Point(0, 1)));
            var o2 = o1.Fold(new Line(
                new Point(1, new RationalNumber(1, 2)),
                new Point(0, new RationalNumber(1, 2))));

            ProblemSpecification ps = new ProblemSpecification(ExampleProblemSpec);
            Assert.IsTrue(o2.IsExactMatch(ps.polys, Matrix.Identity));

            Console.Out.WriteLine("ToString:");
            Console.Out.WriteLine(o2.ToString());
            Console.Out.WriteLine("ToSilhouette:");
            Console.Out.WriteLine(o2.ToSilhouetteString());
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

        [TestMethod]
        public void TestAboveLine()
        {
            TestAboveLineIter(1, 0, false);
            TestAboveLineIter(1, 1, true);
            TestAboveLineIter(0, 1, true);
            TestAboveLineIter(-1, 1, true);
            TestAboveLineIter(-1, 0, true);
            TestAboveLineIter(-1, -1, false);
            TestAboveLineIter(0, -1, false);
            TestAboveLineIter(1, -1, false);
        }

        private void TestAboveLineIter(int x, int y, bool expected)
        {
            var line = new Line(new Point(0, 0), new Point(x, y));
            Assert.AreEqual(expected, new Point(10, 1).IsToRightOfLine(line));
        }

        [TestMethod]
        public void TestProblemSpecification()
        {
            ProblemSpecification ps = new ProblemSpecification(ExampleProblemSpec);
            Assert.AreEqual(1, ps.polys.Count);
            Assert.AreEqual(4, ps.polys[0].vertexes.Count);
            Assert.AreEqual(5, ps.segments.Count);
        }

        public const string ExampleProblemSpec = 
            "1\n" +
            "4\n" +
            "0,0\n" +
            "1,0\n" +
            "1/2,1/2\n" +
            "0,1/2\n" +
            "5\n" +
            "0,0 1,0\n" +
            "1,0 1/2,1/2\n" +
            "1/2,1/2 0,1/2\n" +
            "0,1/2 0,0\n" +
            "0,0 1/2,1/2\n";

        [TestMethod]
        public void TestRandomOrigami()
        {
            var r = new Random();
            var cheatMatrix =
                Matrix.Rotate(new RationalNumber(3, 5), new RationalNumber(4, 5)) *
                Matrix.Translate(RationalNumber.Random(r) / 1337, RationalNumber.Random(r) / 1337);
            var o = Program.CreateRandomPuzzle(cheatMatrix);
            Console.WriteLine(o.ToString());
        }

        [TestMethod]
        public void TestComparison()
        {
            Assert.IsTrue(RationalNumber.One >= RationalNumber.Zero);
            Assert.IsTrue(RationalNumber.Zero < RationalNumber.One);
        }

        [TestMethod]
        public void TestMoreOrigami()
        {
            var o = new Origami();
            var o2 = o.Fold(new Line(
                new Point(new RationalNumber(1, 2), 0),
                new Point(new RationalNumber(1, 4), 0)));
            Assert.AreEqual(1, o2.polys.Count);

            o = new Origami();
            //o2 = o.Fold(new Line(
            //    new Point(new RationalNumber(1, 2), 1),
            //    new Point(new RationalNumber(2, 3), 0)));
            //var o3 = o2.Fold(new Line(
            //    new Point(new RationalNumber(1, 3), new RationalNumber(1, 3)),
            //    new Point(new RationalNumber(1, 2), 1)));
            //var o4 = o3.Fold(new Line(
            //    new Point(0, 0),
            //    new Point(new RationalNumber(2, 3), 0)));
                o2 = o. Fold(new Line(Point.Parse("3/4,1"), Point.Parse("0,0")));
            var o3 = o2.Fold(new Line(Point.Parse("1,1"), Point.Parse("0,1")));
            var o4 = o3.Fold(new Line(Point.Parse("0,1/3"), Point.Parse("1,1/2")));
            var o5 = o4.Fold(new Line(Point.Parse("3/4,1"), Point.Parse("0,0")));
        }

        [TestMethod]
        public void TestEmptyOrigami()
        {
            Console.WriteLine(new Origami().ToString());
        }

        [TestMethod]
        public void TestEmptyIntersection()
        {
            var poly1 = new Polygon(new List<Point>()
            {
                Point.Parse("0,0"),
                Point.Parse("0,1"),
                Point.Parse("1,1"),
                Point.Parse("1,0")
            });

            var poly2 = new Polygon(new List<Point>()
            {
                Point.Parse("2,0"),
                Point.Parse("2,1"),
                Point.Parse("3,1"),
                Point.Parse("3,0")
            });

            var poly3 = poly1.Intersect(poly2);
            Assert.AreEqual(0.0, poly3.Area().AsDouble());

            var poly4 = new Polygon(new List<Point>()
            {
                Point.Parse("1,0"),
                Point.Parse("1,1"),
                Point.Parse("2,1"),
                Point.Parse("2,0")
            });

            var poly5 = poly1.Intersect(poly4);
            Assert.AreEqual(0.0, poly5.Area().AsDouble());
        }

        [TestMethod]
        public void TestSolver()
        {
              //"id": 48,
              //"problem_spec_hash": "345f24dff004a8f605b61e2425ce55d503c95348",
              //"id": 51,
              //"problem_spec_hash": "8a87aa3143d1ec1156393cc6447ee982990a06f6",
              //"id": 54,
              //"problem_spec_hash": "9dfeb3751d840e96af35c7d865dfe0a015808e04",
              //"id": 55,
              //"problem_spec_hash": "c5c40205033d8d041178fda11d3d32a746852a4c",
              //"id": 56,
              //"problem_spec_hash": "10d540827a99831b09e8a2a1839b6f41089a9b3e",
            Program.SolveMain(new string[] { "ac0ba1b46641b7bea12e471c7e4923162f20a7d8", "100000" });
        }

        [TestMethod]
        public void TestConvexHull()
        {
            var polys = new List<Polygon>()
            {
                new Polygon(new List<Point>()
                {
                    Point.Parse("2,0"),
                    Point.Parse("4,0"),
                    Point.Parse("4,2"),
                    Point.Parse("6,2"),
                    Point.Parse("6,4"),
                    Point.Parse("9/2,9/2"),
                    Point.Parse("11/2,9/2"),
                    Point.Parse("4,6"),
                    Point.Parse("2,6"),
                    Point.Parse("3,3"),
                    Point.Parse("0,4"),
                    Point.Parse("0,2"),
                    Point.Parse("2,2"),
                })
            };

            var hull = Polygon.GetConvexHull(polys);
            Assert.AreEqual(28, hull.Area());
            Assert.AreEqual(8, hull.vertexes.Count);
        }

        [TestMethod]
        public void TestMatchHull()
        {
            var poly = new Polygon(
                new List<Point>() 
                {
                    Point.Parse("0,0"),
                    Point.Parse("5,0"),
                    Point.Parse("3,2"),
                    Point.Parse("0,2")
                });

            var rotatedPoly = poly.Transform(
                Matrix.Translate(RationalNumber.Parse("1/2"), RationalNumber.Parse("1/2")) *
                Matrix.Rotate(RationalNumber.Parse("3/5"), RationalNumber.Parse("4/5")));

            var matrix = poly.MatchHull(rotatedPoly);
            Assert.AreNotEqual(null, matrix);
            Assert.AreEqual(poly.Area(), poly.Transform(matrix).Intersect(rotatedPoly).Area());

            var flippedPoly = rotatedPoly.Transform(Matrix.Flip);
            matrix = poly.MatchHull(flippedPoly);
            Assert.AreNotEqual(null, matrix);
            Assert.AreEqual(poly.Area(), poly.Transform(matrix).Intersect(flippedPoly).Area().Abs());
        }

        //[TestMethod]
        public void TestBoxFold()
        {
            var o = new Origami();
            o = o.Fold(new Line(Point.Parse("0,0"), Point.Parse("1/2,1/2")));
            o = o.Fold(new Line(Point.Parse("1/2,1/2"), Point.Parse("1/2,1")));
            var boxFolds = o.GetBoxFolds(new Line(Point.Parse("1/2,1/2"), Point.Parse("1/2,1"))).ToList();
            
        }
    }
}