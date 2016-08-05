using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Solver
{
    public class Program
    {
        static void Main(string[] args)
        {
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
                sb.AppendFormat("{0} {1}", point.srcPoint.x, point.srcPoint.y);
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
                sb.AppendFormat("{0} {1}", point.destPoint.x, point.destPoint.y);
                sb.AppendLine();
            }

            return sb.ToString();
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
            var ans1 = new List<Point>();
            var ans2 = new List<Point>();

            for (var idx = 0; idx < vertexes.Count; ++idx)
            {
                var a = vertexes[idx];
                var b = vertexes[(idx + 1) % vertexes.Count];
                var i = line.Intersect(a, b);

                (useAns1 ? ans1 : ans2).Add(a);

                if (i != null && !b.Equals(i))
                {
                    if (!a.Equals(i))
                    {
                        (useAns1 ? ans1 : ans2).Add(i);
                    }
                    
                    useAns1 = !useAns1;
                    (useAns1 ? ans1 : ans2).Add(i);
                }
            }

            var ans = new List<Polygon>();
            Action<List<Point>> add = (i) =>
                {
                    if (!i.Any())
                    {
                        return;
                    }

                    var p = new Polygon(i, matrix);
                    if (i.Any(j => j.IsAboveLine(line)))
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
            return new Polygon(
                vertexes.Select(i => line.Reflect(i)).ToList(),
                Matrix.Reflect(line) * matrix);
        }

        public List<Point> vertexes { get; private set; }
        public Matrix matrix { get; private set; }
    }

    // Represents lines of the form ax + by + c = 0
    public class Line
    {
        public Line(Point p1, Point p2)
        {
            var dy = p2.y - p1.y;
            var dx = p2.x - p1.x;
            if (dx.n == 0)
            {
                a = 1;
                b = 0;
            }
            else
            {
                var d = dy / dx;
                a = -d.n;
                b = d.d;
            }

            c = -(p1.x * a + p1.y * b);
            Debug.Assert(this.ContainsPoint(p1));
            Debug.Assert(this.ContainsPoint(p2));
        }

        private Line(Point p, long a_, long b_)
        {
            a = a_;
            b = b_;
            c = -(p.x * a + p.y * b);
            Debug.Assert(this.ContainsPoint(p));
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

            Debug.Assert(this.ContainsPoint(ans));
            Debug.Assert(other.ContainsPoint(ans));

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

        public long a { get; private set; }
        public long b { get; private set; }
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

        public bool IsAboveLine(Line l)
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
    }

    public class RationalNumber
    {
        public RationalNumber(long n_, long d_ = 1)
        {
            if (d_ == 0)
            {
                throw new Exception("division by 0");
            }

            long t = gcd(Math.Abs(n_), Math.Abs(d_));
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

        public long n { get; private set; }
        public long d { get; private set; }

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
            return !(b <= a);
        }

        public static bool operator >=(RationalNumber a, RationalNumber b)
        {
            return !(b < a);
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

        public override string ToString()
        {
            if (d == 1)
            {
                return n.ToString();
            }

            return n.ToString() + "/" + d.ToString();
        }

        public static long gcd(long x, long y)
        {
            if (x < y)
            {
                return gcd(y, x);
            }

            while (y != 0)
            {
                long t = y;
                y = x % y;
                x = t;
            }

            return x;
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
}
