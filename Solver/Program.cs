using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.IO;

namespace Solver
{
    public class Program
    {
        static void Main(string[] args)
        {
            //var r = new Random();
            //var cheatMatrix =
            //    Matrix.Rotate(new RationalNumber(3, 5), new RationalNumber(4, 5)) *
            //    Matrix.Translate(RationalNumber.Random(r) / 1337, RationalNumber.Random(r) / 1337);
            //Console.WriteLine(CreateRandomPuzzle(cheatMatrix));
            //Environment.Exit(0);

            var problemsToSolve = File.ReadAllLines(args[0]);
            Parallel.ForEach(
                problemsToSolve,
                new ParallelOptions() { MaxDegreeOfParallelism = 80 },
                i => {
                var ps = new ProblemSpecification(File.ReadAllText("/icfp2016/work/probs/" + i));
                var ans = Program.Solve(ps, 3);
                Console.WriteLine("{0}: {1}", i, ans.Compare(ps.polys));
                File.WriteAllText("/icfp/work/solutions/" + i, ans.ToString());
            });
        }

        public static Origami CreateRandomPuzzle(Matrix cheatMatrix)
        {
            var r = new Random();
            var o = new Origami();
            for (var i = 0; i < 100; ++i)
            {
                var p1 = Point.Random(r);
                var p2 = Point.Random(r);
                if (p1.Equals(p2))
                {
                    continue;
                }

                // Console.WriteLine(p1.ToString() + "  " + p2.ToString());
                var line = new Line(p1, p2);
                var o2 = o.Fold(line);
                if (o2.ToString(cheatMatrix).Length > 5000)
                {
                    return o;
                }
                o = o2;
                // Console.WriteLine("Area: " + o.Area());
            }

            return o;
        }

        public static Origami Solve(ProblemSpecification ps, int depth = 3, Origami origami = null)
        {
            if (depth == 0)
            {
                return origami;
            }

            if (origami == null)
            {
                origami = new Origami();
            }

            double currentSimilarity = 0.0;
            Origami currentAnswer = null;
            foreach (var segment in ps.segments.Concat(ps.reverseSegments))
            {
                //if (origami.ContainsSegment(segment))
                //{
                //    continue;
                //}

                var t = Solve(ps, depth - 1, origami.Fold(new Line(segment)));
                var ts = t.Compare(ps.polys); 
                if (ts >= currentSimilarity)
                {
                    currentAnswer = t;
                    currentSimilarity = ts;
                }

                if (currentSimilarity == 1.0)
                {
                    return currentAnswer;
                }
            }

            return currentAnswer;
        }


        public static void Assert(bool pred)
        {
            if (!pred)
            {
                throw new Exception("Assertion failed");
            }
        }
    }

    public class Origami
    {
        public Origami()
        {
            var origPoints = new List<Point>() 
                {
                    new Point(0, 0),
                    new Point(0, 1),
                    new Point(1, 1),
                    new Point(1, 0)
                };

            polys = new List<Polygon>()
            {
                new Polygon(origPoints, Matrix.Identity)
            };
        }

        private Origami(List<Polygon> polys_)
        {
            polys = polys_;
        }

        public Origami Fold(Line line)
        {
            return new Origami(polys.SelectMany(i => i.Fold(line)).ToList());
        }

        public override string ToString()
        {
            return ToString(null);
        }

        public string ToString(Matrix cheatMatrix = null)
        {
            if (cheatMatrix == null)
            {
                cheatMatrix = Matrix.Identity;
            }

            var sb = new StringBuilder();
            var pointSet = new Dictionary<PointMapping, int>();
            foreach (var poly in polys)
            {
                foreach (var destPoint in poly.vertexes)
                {
                    var invMatrix = poly.matrix.Invert();
                    var pointMapping = new PointMapping(invMatrix.Transform(destPoint), destPoint);
                    if (!pointSet.ContainsKey(pointMapping))
                    {
                        pointSet.Add(pointMapping, 0);
                    }
                    pointSet[pointMapping]++;
                }
            }

            var orderedPoints = pointSet.OrderByDescending(i => i.Value).Select(i => i.Key).ToList();
            for (var i = 0; i < orderedPoints.Count; ++i)
            {
                pointSet[orderedPoints[i]] = i;
            }

            sb.AppendLine(orderedPoints.Count.ToString());
            foreach (var point in orderedPoints)
            {
                sb.AppendFormat("{0},{1}", point.srcPoint.x, point.srcPoint.y);
                sb.AppendLine();
            }

            sb.AppendLine(polys.Count.ToString());
            foreach (var poly in polys)
            {
                sb.Append(poly.vertexes.Count);
                var invMatrix = poly.matrix.Invert();
                foreach (var point in poly.vertexes)
                {
                    sb.Append(' ');
                    sb.Append(pointSet[new PointMapping(invMatrix.Transform(point), null)].ToString());
                }
                sb.AppendLine();
            }

            foreach (var point in orderedPoints)
            {
                sb.AppendFormat("{0},{1}", 
                    cheatMatrix.Transform(point.destPoint).x, 
                    cheatMatrix.Transform(point.destPoint).y);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public string ToSilhouetteString()
        {
            var uniquePolys = GetUniquePolys();
            var sb = new StringBuilder();

            sb.Append(uniquePolys.Count);
            sb.AppendLine();

            foreach (var poly in uniquePolys)
            {
                sb.Append(poly.vertexes.Count);
                sb.AppendLine();
                foreach (var vertex in poly.vertexes)
                {
                    sb.AppendFormat("{0} {1}", vertex.x, vertex.y);
                    sb.AppendLine();
                }
            }

            var lineSegments = new List<Tuple<Point, Point>>();
            foreach (var poly in uniquePolys)
            {
                foreach (var segment in poly.GetLineSegments())
                {
                    if (!lineSegments.Contains(segment) &&
                        !lineSegments.Contains(Tuple.Create(segment.Item2, segment.Item1)))
                    {
                        lineSegments.Add(segment);
                    }
                }
            }

            sb.Append(lineSegments.Count);
            sb.AppendLine();
            foreach (var segment in lineSegments)
            {
                sb.AppendFormat("{0} {1} {2} {3}",
                    segment.Item1.x, segment.Item1.y,
                    segment.Item2.x, segment.Item2.y);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public List<Polygon> GetUniquePolys()
        {
            var ans = new List<Polygon>();
            var polyList = this.polys
                .Select(p => p.GetPositiveAreaPolygon())
                .OrderByDescending(p => p.Area().AsDouble());

            foreach (var poly in polyList)
            {
                var area = poly.Area().AsDouble();
                if (!ans.Any(i => Math.Abs(i.Intersect(poly).Area().AsDouble()) == area))
                {
                    ans.Add(poly);
                }
            }
            return ans;
        }

        class Section
        {
            public Section(int thisCount, int otherCount, Polygon poly)
            {
                ThisCount = thisCount;
                OtherCount = otherCount;
                Poly = poly;
            }

            public int ThisCount { get; set; }
            public int OtherCount { get; set; }
            public Polygon Poly { get; set; }
        }

        public static double Area(
            List<Polygon> thisList,
            List<Polygon> otherList = null)
        {
            var sections = new List<Section>();

            foreach (var poly in thisList)
            {
                var newSections = sections
                    .Select(s => new Section(s.ThisCount + 1, s.OtherCount, poly.Intersect(s.Poly)))
                    .Where(s => s.Poly != null)
                    .Where(s => s.Poly.Area().AsDouble() > 0.01)
                    .ToList();
                sections.AddRange(newSections);
                sections.Add(new Section(1, 0, poly));
            }

            if (otherList != null)
            {
                foreach (var poly in otherList)
                {
                    var newSections = sections
                        .Select(s => new Section(s.ThisCount, s.OtherCount + 1, poly.Intersect(s.Poly)))
                        .Where(s => s.Poly != null)
                        .Where(s => s.Poly.Area().AsDouble() > 0.01)
                        .ToList();
                    sections.AddRange(newSections);
                    sections.Add(new Section(0, 1, poly));
                }
            }

            double area = 0.0;
            foreach (var s in sections)
            {
                int multiplier =
                    otherList != null && (s.ThisCount == 0 || s.OtherCount == 0) ? 0 :
                    otherList != null && ((s.ThisCount & 1) != (s.OtherCount & 1)) ? -1 :
                    otherList == null && ((s.ThisCount & 1) == 0) ? -1 :
                    1;
                area = area + multiplier * Math.Abs(s.Poly.Area().AsDouble());
            }
            return area;
        }

        public double Area()
        {
            return Area(this.GetUniquePolys());
        }

        public double Compare(List<Polygon> otherPolys)
        {
            var thisPolys = this.GetUniquePolys();
            double intersectionArea = Area(thisPolys, otherPolys);
            return intersectionArea / (Area(thisPolys) + Area(otherPolys) - intersectionArea);
        }

        //public double Compare(List<Polygon> otherPolys)
        //{
        //    var thisPolys = this.GetUniquePolys();
        //    var bounds = GetBounds(thisPolys, otherPolys);
        //    var c = 0;
        //    var n = 10;
        //    var dx = (bounds.Item2.x - bounds.Item1.x) / (n - 1);
        //    var dy = (bounds.Item2.y - bounds.Item1.y) / (n - 1);
        //    var positivePolys = otherPolys.Where(p => p.Area().n >= 0).ToList();
        //    var negativePolys = otherPolys.Where(p => p.Area().n < 0).Select(p => p.GetPositiveAreaPolygon()).ToList();
        //    for (var x = 0; x < n; ++x)
        //    {
        //        for (var y = 0; y < n; ++y)
        //        {
        //            var xy = new Point(bounds.Item1.x + x * dx, bounds.Item1.y + y * dy);
        //            var isInsideThis = thisPolys.Any(p => p.ContainsPoint(xy));
        //            var isInsideOther = 
        //                positivePolys.Any(p => p.ContainsPoint(xy)) && 
        //                negativePolys.All(p => !p.ContainsPoint(xy));
        //            if (isInsideThis == isInsideOther)
        //            {
        //                ++c;
        //            }
        //            else
        //            {
        //                c = c + 0;
        //            }
        //        }
        //    }
        //    return c / ((double)n * n);
        //}

        public Tuple<Point, Point> GetBounds(List<Polygon> p1, List<Polygon> p2)
        {
            RationalNumber minX = 0;
            RationalNumber maxX = 0;
            RationalNumber minY = 0;
            RationalNumber maxY = 0;

            bool firstIter = true;

            foreach (var poly in p1.Concat(p2))
            {
                foreach (var v in poly.vertexes)
                {
                    if (firstIter || v.x < minX)
                    {
                        minX = v.x;
                    }

                    if (firstIter || v.y < minY)
                    {
                        minY = v.y;
                    }

                    if (firstIter || v.x > maxX)
                    {
                        maxX = v.x;
                    }

                    if (firstIter || v.y < maxY)
                    {
                        maxY = v.y;
                    }
                }
            }

            return Tuple.Create(new Point(minX, minY), new Point(maxX, maxY));
        }

        public bool ContainsSegment(Tuple<Point, Point> segment)
        {
            return this.polys.Any(i =>
                i.vertexes.Contains(segment.Item1) && i.vertexes.Contains(segment.Item2));
        }

        public List<Polygon> polys { get; private set; }
    }

    public class Polygon
    {
        public Polygon(
            List<Point> vertexes_,
            Matrix matrix_)
        {
            vertexes = vertexes_;
            matrix = matrix_;
        }

        public List<Polygon> Fold(Line line)
        {
            var useAns1 = true;
            var xs = new List<Point>();
            var ans1 = new List<Point>();
            var ans2 = new List<Point>();

            foreach (var segment in this.GetLineSegments())
            {
                var x = line.Intersect(segment.Item1, segment.Item2);
                (useAns1 ? ans1 : ans2).Add(segment.Item1);

                if (x != null && !segment.Item2.Equals(x))
                {
                    if (!segment.Item1.Equals(x))
                    {
                        (useAns1 ? ans1 : ans2).Add(x);
                    }

                    xs.Add(x);
                    useAns1 = !useAns1;
                    (useAns1 ? ans1 : ans2).Add(x);
                }
            }

            if (xs.Count != 2 || xs[0].Equals(xs[1]))
            {
                ans1 = vertexes;
                ans2.Clear();
            }

            var ans = new List<Polygon>();
            Action<List<Point>> add = (i) =>
                {
                    if (i.Count == 0)
                    {
                        return;
                    }

                    Program.Assert(i.Count >= 3);
                    var p = new Polygon(i, matrix);
                    if (i.Any(j => j.IsToRightOfLine(line)))
                    {
                        p = p.Reflect(line);
                    }
                    ans.Add(p);
                };

            add(ans1);
            add(ans2);
            return ans;
        }

        public Polygon Reflect (Line line)
        {
            var ans = new Polygon(
                vertexes.Select(i => line.Reflect(i)).ToList(),
                Matrix.Reflect(line) * matrix);

            var invMatrix = ans.matrix.Invert();
            Program.Assert(Matrix.Identity.Equals(invMatrix * ans.matrix));
            foreach (var v in ans.vertexes)
            {
                var v2 = invMatrix.Transform(v);
                Program.Assert(v2.x >= RationalNumber.Zero);
                Program.Assert(v2.x <= RationalNumber.One);
                Program.Assert(v2.y >= RationalNumber.Zero);
                Program.Assert(v2.y <= RationalNumber.One);
            }

            return ans;
        }

        public RationalNumber Area()
        {
            var area = RationalNumber.Zero;
            foreach (var segment in this.GetLineSegments())
            {
                area = area + (segment.Item2.x + segment.Item1.x) * (segment.Item2.y - segment.Item1.y);
            }

            return area / 2;
        }

        public Polygon GetPositiveAreaPolygon()
        {
            if (this.Area().n >= 0)
            {
                return this;
            }

            return new Polygon(this.vertexes.Reverse<Point>().ToList(), matrix);
        }

        public IEnumerable<Tuple<Point, Point>> GetLineSegments()
        {
            for (var i = 0; i < vertexes.Count; ++i)
            {
                var p1 = vertexes[i];
                var p2 = vertexes[(i + 1) % vertexes.Count];
                yield return Tuple.Create(p1, p2);
            }
        }

        public bool ContainsPoint(Point point)
        {
            return GetLineSegments().All(seg => !point.IsToRightOfLine(new Line(seg)));
        }

        public Polygon Intersect(Polygon other)
        {
            foreach (var srcSegment in this.GetPositiveAreaPolygon().GetLineSegments())
            {
                var line = new Line(srcSegment);
                var c = new List<Point>();
                foreach (var destSegment in other.GetLineSegments())
                {
                    var p1Outside = destSegment.Item1.IsToRightOfLine(line);
                    var p2Outside = destSegment.Item2.IsToRightOfLine(line);

                    if (!p1Outside && p2Outside)
                    {
                        c.Add(destSegment.Item1);
                        var x = line.Intersect(destSegment.Item1, destSegment.Item2);
                        Program.Assert(x != null);
                        c.Add(x);
                    }
                    else if (p1Outside && !p2Outside)
                    {
                        var x = line.Intersect(destSegment.Item1, destSegment.Item2);
                        Program.Assert(x != null);
                        c.Add(x);
                    }
                    else if (!p1Outside && !p2Outside)
                    {
                        c.Add(destSegment.Item1);
                    }
                }

                bool removeDupes = c.Count > other.vertexes.Count;
                other = new Polygon(c, null);
                if (removeDupes)
                {
                    c = other.GetLineSegments()
                        .Where(i => !i.Item1.Equals(i.Item2))
                        .Select(i => i.Item1)
                        .ToList();
                    other = new Polygon(c, null);
                }
            }

            return other;
        }

        public List<Point> vertexes { get; private set; }
        public Matrix matrix { get; private set; }
    }

    // Represents lines of the form ax + by + c = 0
    public class Line
    {
        public Line(Tuple<Point, Point> segment)
            : this (segment.Item1, segment.Item2)
        {
        }

        public Line(Point p1, Point p2)
        {
            var dy = p2.y - p1.y;
            var dx = p2.x - p1.x;
            if (dx.n == 0)
            {
                a = dy.n > 0 ? 1 : -1;
                b = 0;
            }
            else
            {
                var d = dy / dx;
                var s = dx.n > 0 ? -1 : 1;
                a = -d.n * s;
                b = d.d * s;
            }

            c = -(p1.x * a + p1.y * b);
            Program.Assert(this.ContainsPoint(p1));
            Program.Assert(this.ContainsPoint(p2));
        }

        private Line(Point p, BigInteger a_, BigInteger b_)
        {
            a = a_;
            b = b_;
            c = -(p.x * a + p.y * b);
            Program.Assert(this.ContainsPoint(p));
        }

        public Point Intersect(Line other)
        {
            var t = this.a * other.b - this.b * other.a;
            if (t == 0)
            {
                return null;
            }

            var ans = new Point(
                (this.b * other.c - other.b * this.c) / t,
                (other.a * this.c - this.a * other.c) / t);

            Program.Assert(this.ContainsPoint(ans));
            Program.Assert(other.ContainsPoint(ans));

            return ans;
        }

        public Point Intersect(Point p1, Point p2)
        {
            var intersection = this.Intersect(new Line(p1, p2));
            var minX = p1.x < p2.x ? p1.x : p2.x;
            var maxX = p1.x > p2.x ? p1.x : p2.x;
            var minY = p1.y < p2.y ? p1.y : p2.y;
            var maxY = p1.y > p2.y ? p1.y : p2.y;

            if (intersection == null ||
                intersection.x < minX ||
                intersection.x > maxX ||
                intersection.y < minY ||
                intersection.y > maxY)
            {
                return null;
            }

            return intersection;
        }

        public Point Reflect(Point p)
        {
            if (this.ContainsPoint(p))
            {
                return p;
            }

            var perpBisector = new Line(p, -this.b, this.a);
            var intersection = this.Intersect(perpBisector);
            var dx = intersection.x - p.x;
            var dy = intersection.y - p.y;
            return new Point(intersection.x + dx, intersection.y + dy);
        }

        public bool ContainsPoint(Point p)
        {
            return (a * p.x + b * p.y + c).n == 0;
        }

        public BigInteger a { get; private set; }
        public BigInteger b { get; private set; }
        public RationalNumber c { get; private set; }
    }

    public class PointMapping
    {
        public PointMapping(Point src, Point dest)
        {
            srcPoint = src;
            destPoint = dest;
        }

        public Point srcPoint { get; private set; }
        public Point destPoint { get; private set; }

        public override bool Equals(object obj)
        {
            var other = obj as PointMapping;
            if (other == null)
            {
                return false;
            }
            return this.srcPoint.Equals(other.srcPoint);
        }

        public override int GetHashCode()
        {
            return this.srcPoint.GetHashCode();
        }
    }

    public class Point
    {
        public Point(RationalNumber x_, RationalNumber y_)
        {
            x = x_;
            y = y_;
        }

        public RationalNumber x { get; private set; }
        public RationalNumber y { get; private set; }

        public bool IsToRightOfLine(Line l)
        {
            return (l.a * x + l.b * y + l.c).n > 0;
        }

        public override bool Equals(object obj)
        {
            var other = obj as Point;
            if (obj == null)
            {
                return false;
            }
            return this.x.Equals(other.x) && this.y.Equals(other.y);
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ y.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("({0}, {1})", x, y);
        }

        public static Point Parse(string str)
        {
            var s = str.Split(',');
            return new Point(RationalNumber.Parse(s[0]), RationalNumber.Parse(s[1]));
        }

        public static Point Random(Random r)
        {
            return new Point(RationalNumber.Random(r), RationalNumber.Random(r));
        }
    }

    public class RationalNumber
    {
        public RationalNumber(long n_, long d_ = 1)
            : this(new BigInteger(n_), new BigInteger(d_))
        {
        }

        private static BigInteger Abs(BigInteger x)
        {
            if (x.Sign >= 0)
            {
                return x;
            }

            return -x;
        }
        
        public RationalNumber(BigInteger n_, BigInteger d_)
        {
            if (d_ == 0)
            {
                throw new Exception("division by 0");
            }

            
            var t = gcd(Abs(n_), Abs(d_));
            n = n_ / t;
            d = d_ / t;

            if (d < 0)
            {
                d = -d;
                n = -n;
            }
        }

        public static implicit operator RationalNumber(long x)
        {
            return new RationalNumber(x, 1);
        }

        public static implicit operator RationalNumber(BigInteger x)
        {
            return new RationalNumber(x, 1);
        }

        public BigInteger n { get; private set; }
        public BigInteger d { get; private set; }

        public static RationalNumber operator+ (RationalNumber a, RationalNumber b)
        {
            return new RationalNumber(
                a.n * b.d + b.n * a.d,
                a.d * b.d);
        }

        public static RationalNumber operator- (RationalNumber a)
        {
            return new RationalNumber(-a.n, a.d);
        }

        public static RationalNumber operator -(RationalNumber a, RationalNumber b)
        {
            return a + (-b);
        }

        public static RationalNumber operator* (RationalNumber a, RationalNumber b)
        {
            return new RationalNumber(a.n * b.n, a.d * b.d);
        }

        public static RationalNumber operator/ (RationalNumber a, RationalNumber b)
        {
            return new RationalNumber(a.n * b.d, a.d * b.n);
        }

        public static bool operator< (RationalNumber a, RationalNumber b)
        {
            return a.n * b.d < a.d * b.n;
        }

        public static bool operator<= (RationalNumber a, RationalNumber b)
        {
            return a.n * b.d <= a.d * b.n;
        }

        public static bool operator> (RationalNumber a, RationalNumber b)
        {
            return a.n * b.d > a.d * b.n;
        }

        public static bool operator >=(RationalNumber a, RationalNumber b)
        {
            return a.n * b.d >= a.d * b.n;
        }

        public override bool Equals(object obj)
        {
            var other = obj as RationalNumber;
            if (obj == null)
            {
                return false;
            }
            return this.n == other.n && this.d == other.d;
        }

        public override int GetHashCode()
        {
            return n.GetHashCode() ^ d.GetHashCode();
        }


        public double AsDouble()
        {
            return (double)n / (double)d;
        }

        public override string ToString()
        {
            if (d == 1)
            {
                return n.ToString();
            }

            return n.ToString() + "/" + d.ToString();
        }

        public static BigInteger gcd(BigInteger x, BigInteger y)
        {
            if (x < y)
            {
                return gcd(y, x);
            }

            while (y != 0)
            {
                var t = y;
                y = x % y;
                x = t;
            }

            return x;
        }

        public static RationalNumber Parse(string str)
        {
            var s = str.Split('/');
            if (s.Length == 1)
            {
                return new RationalNumber(BigInteger.Parse(s[0]), 1);
            }
            else
            {
                return new RationalNumber(BigInteger.Parse(s[0]), BigInteger.Parse(s[1]));
            }
        }

        public static RationalNumber Random(Random r)
        {
            var d = r.Next(1, 5);
            var n = r.Next(0, d + 1);
            return new RationalNumber(n, d);
        }

        public static RationalNumber Zero = new RationalNumber(0);
        public static RationalNumber One = new RationalNumber(1);

        public RationalNumber Abs()
        {
            if (n >= 0)
            {
                return this;
            }

            return new RationalNumber(-n, d);
        }
    }

    public class Matrix
    {
        private Matrix (RationalNumber[,] a_)
        {
            a = a_;
        }

        public static Matrix Reflect(Line l)
        {
            var a = new RationalNumber[3,3];
            var t = l.a * l.a + l.b * l.b;
            a[0, 0] = new RationalNumber(l.b * l.b - l.a * l.a, t);
            a[1, 0] = a[0, 1] = new RationalNumber(-2 * l.a * l.b, t);
            a[1, 1] = -a[0, 0];
            a[0, 2] = (-2 * l.a * l.c) / t;
            a[1, 2] = (-2 * l.b * l.c) / t;
            a[2, 0] = a[2, 1] = 0;
            a[2, 2] = 1;
            return new Matrix(a);
        }

        public static Matrix Rotate(RationalNumber sin, RationalNumber cos)
        {
            Program.Assert(RationalNumber.One.Equals(sin * sin + cos * cos));
            var a = new RationalNumber[3, 3];
            a[0, 0] = cos;
            a[1, 0] = sin;
            a[0, 1] = -sin;
            a[1, 1] = cos;
            a[0, 2] = a[1, 2] = a[2, 0] = a[2, 1] = 0;
            a[2, 2] = 1;
            return new Matrix(a);
        }

        public static Matrix Translate(RationalNumber dx, RationalNumber dy)
        {
            var a = new RationalNumber[3, 3];
            a[0, 2] = dx;
            a[1, 2] = dy;
            a[0, 0] = a[1, 1] = a[2, 2] = 1; 
            a[0, 1] = a[1, 0] = a[2, 0] = a[2, 1] = 0;
            return new Matrix(a);
        }

        public Point Transform(Point p)
        {
            return new Point(
                p.x * a[0, 0] + p.y * a[0, 1] + a[0, 2],
                p.x * a[1, 0] + p.y * a[1, 1] + a[1, 2]);
        }

        public Matrix Invert()
        {
            var A = a[1, 1] * a[2, 2] - a[1, 2] * a[2, 1];
            var B = a[1, 2] * a[2, 0] - a[1, 0] * a[2, 2];
            var C = a[1, 0] * a[2, 1] - a[1, 1] * a[2, 0];
            var D = a[0, 2] * a[2, 1] - a[0, 1] * a[2, 2];
            var E = a[0, 0] * a[2, 2] - a[0, 2] * a[2, 0];
            var F = a[0, 1] * a[2, 0] - a[0, 0] * a[2, 1];
            var G = a[0, 1] * a[1, 2] - a[0, 2] * a[1, 1];
            var H = a[0, 2] * a[1, 0] - a[0, 0] * a[1, 2];
            var I = a[0, 0] * a[1, 1] - a[0, 1] * a[1, 0];

            var det = a[0, 0] * A + a[0,1] * B + a[0,2] * C;
            var b = new RationalNumber[3, 3];
            b[0, 0] = A / det;
            b[1, 0] = B / det;
            b[2, 0] = C / det;
            b[0, 1] = D / det;
            b[1, 1] = E / det;
            b[2, 1] = F / det;
            b[0, 2] = G / det;
            b[1, 2] = H / det;
            b[2, 2] = I / det;

            return new Matrix(b);
        }

        public static Matrix operator*(Matrix x, Matrix y)
        {
            var a = new RationalNumber[3, 3];
            for (var i = 0; i < 3; ++i)
            {
                for (var j = 0; j < 3; ++j)
                {
                    RationalNumber t = 0;
                    for (var k = 0; k < 3; ++k)
                    {
                        t += x.a[i,k] * y.a[k,j];
                    }
                    a[i,j] = t;
                }
            }
            return new Matrix(a);
        }

        public override bool Equals(object obj)
        {
            var other = obj as Matrix;
            if (other== null)
            {
                return false;
            }

            for (var i = 0; i < 3; ++i)
            {
                for (var j = 0; j < 3; ++j)
                {
                    if (!a[i,j].Equals(other.a[i,j]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        RationalNumber[,] a;

        public static Matrix Identity = new Matrix(new RationalNumber[3, 3] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } });
    }

    public class ProblemSpecification
    {
        public ProblemSpecification(string data)
        {
            this.polys = new List<Polygon>();
            this.segments = new List<Tuple<Point, Point>>();

            var line = data.Replace("\r", "").Split('\n').AsEnumerable<string>().GetEnumerator();

            line.MoveNext();
            var nPolys = int.Parse(line.Current);
            for (var i = 0; i < nPolys; ++i)
            {
                var vertexes = new List<Point>();
                line.MoveNext();
                var nVertexes = int.Parse(line.Current);
                
                for (var j = 0; j < nVertexes; ++j)
                {
                    line.MoveNext();
                    var pt = Point.Parse(line.Current);
                    vertexes.Add(pt);
                }

                polys.Add(new Polygon(vertexes, null));
            }

            line.MoveNext();
            var nSegments = int.Parse(line.Current);
            for (var i = 0; i < nSegments; ++i)
            {
                line.MoveNext();
                var fields = line.Current.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var p1 = Point.Parse(fields[0]);
                var p2 = Point.Parse(fields[1]);
                segments.Add(Tuple.Create(p1, p2));
            }

            reverseSegments = segments.Select(i => Tuple.Create(i.Item2, i.Item1)).ToList();
        }

        public List<Polygon> polys { get; private set; }
        public List<Tuple<Point, Point>> segments { get; private set; }
        public List<Tuple<Point, Point>> reverseSegments { get; private set; }
    }
}
