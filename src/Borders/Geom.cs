// Shared geometry + SVG helpers. Everything here is pure and deterministic.
// A seed's design depends on the exact numbers produced here: reordering an expression or changing
// a rounding rule changes what existing seeds draw.
using System.Globalization;
using System.Text;

namespace Borders;

public readonly record struct Pt(double X, double Y);

/// <summary>Seeded PRNG (mulberry32 on a string hash). Same seed, same sequence, on every machine.</summary>
public sealed class Rng
{
    private uint _a;

    public Rng(string seed) => _a = Hash(seed);

    private static uint Hash(string str)
    {
        unchecked
        {
            uint h = 1779033703u ^ (uint)str.Length;
            foreach (char c in str)
            {
                h = (h ^ c) * 3432918353u;
                h = (h << 13) | (h >> 19);
            }
            h = (h ^ (h >> 16)) * 2246822507u;
            h = (h ^ (h >> 13)) * 3266489909u;
            return h ^ (h >> 16);
        }
    }

    public double Next()
    {
        unchecked
        {
            _a += 0x6d2b79f5u;
            uint t = (_a ^ (_a >> 15)) * (1u | _a);
            t = (t + (t ^ (t >> 7)) * (61u | t)) ^ t;
            return (t ^ (t >> 14)) / 4294967296.0;
        }
    }

    public double Range(double lo, double hi) => lo + (hi - lo) * Next();
    public int Int(int lo, int hi) => (int)Math.Floor(lo + (hi - lo + 1) * Next());
    public T Pick<T>(params T[] arr) => arr[(int)Math.Floor(Next() * arr.Length)];
    public bool Chance(double p) => Next() < p;
    public int Sign() => Next() < 0.5 ? -1 : 1;
    public double Jitter(double v, double frac) => v * (1 + (Next() * 2 - 1) * frac);
}

public static class Geom
{
    public const double TAU = Math.PI * 2;
    public const string INK = "#000";
    public const string PAPER = "#fff";

    /// <summary>Smooth periodic 1D noise: sum of a few seeded sines. period in the same units as x.</summary>
    public static Func<double, double> MakeNoise(Rng rnd, double period, int octaves = 4)
    {
        var parts = new (double f, double ph, double a)[octaves];
        for (int i = 0; i < octaves; i++)
        {
            double f = i + 1 + rnd.Int(0, 1);
            double ph = rnd.Range(0, TAU);
            parts[i] = (f, ph, 1 / (i + 1.5));
        }
        double norm = 0;
        foreach (var p in parts) norm += p.a;
        return x =>
        {
            double s = 0;
            foreach (var p in parts) s += p.a * Math.Sin((TAU * p.f * x) / period + p.ph);
            return s / norm;
        };
    }

    // ---------- numbers & points ----------

    /// <summary>Number for SVG output: two decimals, trailing zeros removed.</summary>
    public static string F(double n)
    {
        if (n == 0) return "0";
        string s;
        // An exact tie (x.xx5) only happens on odd eighths. Those round away from zero; .NET's "F2"
        // would round them to even, so they are handled here.
        double eighths = n * 8;
        if (Math.Abs(eighths) < 1e15 && eighths == Math.Floor(eighths) && ((long)eighths & 1) != 0)
        {
            long cents = (long)Math.Floor(Math.Abs(n) * 100) + 1;
            s = (n < 0 ? "-" : "") + (cents / 100).ToString(CultureInfo.InvariantCulture) + "." + (cents % 100).ToString("00", CultureInfo.InvariantCulture);
        }
        else
        {
            s = n.ToString("F2", CultureInfo.InvariantCulture);
        }
        return s.TrimEnd('0').TrimEnd('.');
    }

    /// <summary>Round to the nearest whole number; halves round toward positive infinity.</summary>
    public static double Round(double x) => Math.Floor(x + 0.5);
    public static double Hypot(double dx, double dy) => Math.Sqrt(dx * dx + dy * dy);
    public static double Lerp(double a, double b, double t) => a + (b - a) * t;
    public static double Clamp(double v, double lo, double hi) => Math.Max(lo, Math.Min(hi, v));
    public static double Smoothstep(double a, double b, double x)
    {
        double t = Clamp((x - a) / (b - a), 0, 1);
        return t * t * (3 - 2 * t);
    }
    public static Pt Polar(double cx, double cy, double r, double a) => new(cx + r * Math.Cos(a), cy + r * Math.Sin(a));
    public static double Deg(double a) => (a * 180) / Math.PI;
    public static double Rad(double d) => (d * Math.PI) / 180;

    // ---------- polylines ----------

    /// <summary>Catmull-Rom spline through points, sampled to a polyline.</summary>
    public static List<Pt> Spline(IReadOnlyList<Pt> pts, bool closed = false, int per = 14)
    {
        int n = pts.Count;
        if (n < 3) return new List<Pt>(pts);
        var o = new List<Pt>();
        Pt Get(int i) => closed ? pts[((i % n) + n) % n] : pts[Math.Clamp(i, 0, n - 1)];
        int segs = closed ? n : n - 1;
        for (int i = 0; i < segs; i++)
        {
            Pt p0 = Get(i - 1), p1 = Get(i), p2 = Get(i + 1), p3 = Get(i + 2);
            for (int j = 0; j < per; j++)
            {
                double t = (double)j / per, t2 = t * t, t3 = t2 * t;
                o.Add(new Pt(
                    0.5 * (2 * p1.X + (-p0.X + p2.X) * t + (2 * p0.X - 5 * p1.X + 4 * p2.X - p3.X) * t2 + (-p0.X + 3 * p1.X - 3 * p2.X + p3.X) * t3),
                    0.5 * (2 * p1.Y + (-p0.Y + p2.Y) * t + (2 * p0.Y - 5 * p1.Y + 4 * p2.Y - p3.Y) * t2 + (-p0.Y + 3 * p1.Y - 3 * p2.Y + p3.Y) * t3)));
            }
        }
        if (!closed) o.Add(pts[n - 1]);
        return o;
    }

    public static List<Pt> ArcPts(double cx, double cy, double r, double a0, double a1, int n = 0)
    {
        int steps = n != 0 ? n : (int)Math.Max(6, Math.Ceiling((Math.Abs(a1 - a0) * Math.Max(r, 2)) / 2.5));
        var o = new List<Pt>(steps + 1);
        for (int i = 0; i <= steps; i++) o.Add(Polar(cx, cy, r, Lerp(a0, a1, (double)i / steps)));
        return o;
    }

    /// <summary>Spiral from radius r0 (angle a0) to r1 (angle a1). ease &gt; 1 tightens faster near the end.</summary>
    public static List<Pt> SpiralPts(double cx, double cy, double r0, double r1, double a0, double a1, double ease = 1)
    {
        int steps = (int)Math.Max(16, Math.Ceiling(Math.Abs(a1 - a0) * 10));
        var o = new List<Pt>(steps + 1);
        for (int i = 0; i <= steps; i++)
        {
            double u = (double)i / steps;
            o.Add(Polar(cx, cy, Lerp(r0, r1, Math.Pow(u, ease)), Lerp(a0, a1, u)));
        }
        return o;
    }

    /// <summary>
    /// Turtle integration: walk len from (x,y) with heading h (radians) and curvature kfn(s, u).
    /// Returns the points and the end state so strokes can be chained.
    /// </summary>
    public static (List<Pt> Pts, double X, double Y, double H) Turtle(double x, double y, double h, double len, Func<double, double, double> kfn, double ds = 1)
    {
        int steps = (int)Math.Max(2, Math.Ceiling(len / ds));
        double step = len / steps;
        var pts = new List<Pt>(steps + 1) { new(x, y) };
        for (int i = 0; i < steps; i++)
        {
            double s = (i + 0.5) * step;
            double k = kfn(s, s / len);
            h += k * step;
            x += Math.Cos(h) * step;
            y += Math.Sin(h) * step;
            pts.Add(new Pt(x, y));
        }
        return (pts, x, y, h);
    }

    public static double PathLength(IReadOnlyList<Pt> pts)
    {
        double l = 0;
        for (int i = 1; i < pts.Count; i++) l += Hypot(pts[i].X - pts[i - 1].X, pts[i].Y - pts[i - 1].Y);
        return l;
    }

    /// <summary>Point and heading at arc-length fraction u of a polyline.</summary>
    public static (double X, double Y, double A) PointAt(IReadOnlyList<Pt> pts, double u)
    {
        double total = PathLength(pts);
        double target = Clamp(u, 0, 1) * total;
        for (int i = 1; i < pts.Count; i++)
        {
            double dx = pts[i].X - pts[i - 1].X, dy = pts[i].Y - pts[i - 1].Y;
            double l = Hypot(dx, dy);
            if (target <= l || i == pts.Count - 1)
            {
                double t = l != 0 ? Math.Min(1, target / l) : 0;
                return (pts[i - 1].X + dx * t, pts[i - 1].Y + dy * t, Math.Atan2(dy, dx));
            }
            target -= l;
        }
        return (pts[0].X, pts[0].Y, 0);
    }

    // ---------- SVG path data ----------

    public static string PolyD(IReadOnlyList<Pt> pts, bool close = false)
    {
        if (pts.Count == 0) return "";
        var d = new StringBuilder(pts.Count * 14);
        d.Append('M').Append(F(pts[0].X)).Append(' ').Append(F(pts[0].Y));
        for (int i = 1; i < pts.Count; i++) d.Append('L').Append(F(pts[i].X)).Append(' ').Append(F(pts[i].Y));
        if (close) d.Append('Z');
        return d.ToString();
    }

    public static string CircleD(double cx, double cy, double r) =>
        $"M{F(cx - r)} {F(cy)}A{F(r)} {F(r)} 0 1 0 {F(cx + r)} {F(cy)}A{F(r)} {F(r)} 0 1 0 {F(cx - r)} {F(cy)}Z";

    public static string RectD(double x, double y, double w, double h) => $"M{F(x)} {F(y)}h{F(w)}v{F(h)}h{F(-w)}Z";

    /// <summary>
    /// Variable-width stroke as a filled outline. wfn(u, heading) returns the full width
    /// at arc-length fraction u; heading lets callers emulate a broad-nib pen.
    /// </summary>
    public static string RibbonD(IReadOnlyList<Pt> pts, Func<double, double, double> wfn)
    {
        int n = pts.Count;
        if (n < 2) return "";
        var cum = new double[n];
        for (int i = 1; i < n; i++) cum[i] = cum[i - 1] + Hypot(pts[i].X - pts[i - 1].X, pts[i].Y - pts[i - 1].Y);
        double total = cum[n - 1] != 0 ? cum[n - 1] : 1;
        var left = new List<Pt>(n * 2);
        var right = new List<Pt>(n);
        for (int i = 0; i < n; i++)
        {
            Pt a = pts[Math.Max(0, i - 1)], b = pts[Math.Min(n - 1, i + 1)];
            double dx = b.X - a.X, dy = b.Y - a.Y;
            double l = Hypot(dx, dy);
            if (l == 0) l = 1;
            dx /= l;
            dy /= l;
            double w = Math.Max(0, wfn(cum[i] / total, Math.Atan2(dy, dx))) / 2;
            left.Add(new Pt(pts[i].X - dy * w, pts[i].Y + dx * w));
            right.Add(new Pt(pts[i].X + dy * w, pts[i].Y - dx * w));
        }
        right.Reverse();
        left.AddRange(right);
        return PolyD(left, true);
    }

    /// <summary>Common width profiles for RibbonD.</summary>
    public static class Taper
    {
        /// <summary>Fat in the middle, pointed at both ends.</summary>
        public static Func<double, double, double> Both(double w, double min = 0) =>
            (u, _) => min + (w - min) * Math.Pow(Math.Sin(Math.PI * u), 0.7);

        /// <summary>Fat at the start, pointed at the end.</summary>
        public static Func<double, double, double> Out(double w, double min = 0, double p = 1) =>
            (u, _) => min + (w - min) * Math.Pow(1 - u, p);

        /// <summary>Pointed at the start, fat at the end.</summary>
        public static Func<double, double, double> In(double w, double min = 0, double p = 1) =>
            (u, _) => min + (w - min) * Math.Pow(u, p);

        /// <summary>Swells early then thins to a hairline (whiplash).</summary>
        public static Func<double, double, double> Whip(double w, double min = 0, double peak = 0.3, double fall = 1.35) =>
            (u, _) =>
            {
                double v = u < peak ? Math.Sin((Math.PI / 2) * (u / peak)) : Math.Pow(1 - (u - peak) / (1 - peak), fall);
                return min + (w - min) * v;
            };

        /// <summary>Broad-nib pen held at nib radians.</summary>
        public static Func<double, double, double> Pen(double wmax, double wmin, double nib, double endFade = 0.06) =>
            (u, a) =>
            {
                double fade = Math.Min(1, Math.Min(u / endFade, (1 - u) / endFade));
                return (wmin + (wmax - wmin) * Math.Abs(Math.Sin(a - nib))) * (0.25 + 0.75 * fade);
            };
    }

    // ---------- SVG elements ----------

    public static string Stroke(string d, double w, string extra = "") =>
        d.Length != 0 ? $"<path d=\"{d}\" fill=\"none\" stroke=\"{INK}\" stroke-width=\"{F(w)}\" stroke-linecap=\"round\" stroke-linejoin=\"round\"{extra}/>" : "";

    public static string StrokeSq(string d, double w, string extra = "") =>
        d.Length != 0 ? $"<path d=\"{d}\" fill=\"none\" stroke=\"{INK}\" stroke-width=\"{F(w)}\" stroke-linecap=\"butt\" stroke-linejoin=\"miter\"{extra}/>" : "";

    public static string Fill(string d, string color = INK, string extra = "") =>
        d.Length != 0 ? $"<path d=\"{d}\" fill=\"{color}\"{extra}/>" : "";

    /// <summary>Paper-filled shape with an ink outline: knocks out whatever is underneath.</summary>
    public static string Outlined(string d, double w, string extra = "") =>
        d.Length != 0 ? $"<path d=\"{d}\" fill=\"{PAPER}\" stroke=\"{INK}\" stroke-width=\"{F(w)}\" stroke-linejoin=\"round\" stroke-linecap=\"round\"{extra}/>" : "";

    /// <summary>Paper-filled knockout with a mitred ink outline (for rectilinear ornaments).</summary>
    public static string OutlinedSq(string d, double w) =>
        d.Length != 0 ? $"<path d=\"{d}\" fill=\"{PAPER}\" stroke=\"{INK}\" stroke-width=\"{F(w)}\" stroke-linejoin=\"miter\" stroke-linecap=\"butt\"/>" : "";

    public static string Group(string transform, string inner) =>
        inner.Length != 0 ? $"<g transform=\"{transform}\">{inner}</g>" : "";

    public static string Place(double x, double y, double angle, string inner, double s = 1) =>
        Group($"translate({F(x)} {F(y)}) rotate({F(Deg(angle))}){(s != 1 ? $" scale({F(s)})" : "")}", inner);

    public static string Dot(double x, double y, double r, string color = INK) =>
        $"<circle cx=\"{F(x)}\" cy=\"{F(y)}\" r=\"{F(r)}\" fill=\"{color}\"/>";

    public static string Ring(double x, double y, double r, double w, string fillColor = "none") =>
        $"<circle cx=\"{F(x)}\" cy=\"{F(y)}\" r=\"{F(r)}\" fill=\"{fillColor}\" stroke=\"{INK}\" stroke-width=\"{F(w)}\"/>";

    // ---------- reusable botanical shapes (local coords: base at origin, pointing +x) ----------

    /// <summary>Leaf outline from base (0,0) to tip (len,0). belly 0..1 moves the widest point.</summary>
    public static string LeafD(double len, double wid, double belly = 0.4, double curl = 0)
    {
        double b = len * belly;
        return
            $"M0 0C{F(b * 0.5)} {F(-wid * 0.9)} {F(b + (len - b) * 0.45)} {F(-wid * 0.85 + curl)} {F(len)} {F(curl)}" +
            $"C{F(b + (len - b) * 0.45)} {F(wid * 0.85 + curl)} {F(b * 0.5)} {F(wid * 0.9)} 0 0Z";
    }

    /// <summary>Rounded petal from (0,0) to (len,0).</summary>
    public static string PetalD(double len, double wid, double notch = 0)
    {
        double tip = len - notch;
        return
            $"M0 0C{F(len * 0.15)} {F(-wid)} {F(len * 0.85)} {F(-wid * 1.15)} {F(len)} {F(-wid * 0.3)}" +
            (notch != 0
                ? $"Q{F(len * 1.02)} {F(-wid * 0.08)} {F(tip)} 0Q{F(len * 1.02)} {F(wid * 0.08)} {F(len)} {F(wid * 0.3)}"
                : $"Q{F(len * 1.06)} 0 {F(len)} {F(wid * 0.3)}") +
            $"C{F(len * 0.85)} {F(wid * 1.15)} {F(len * 0.15)} {F(wid)} 0 0Z";
    }

    /// <summary>Teardrop pointing +x: round head at the far end, point at origin.</summary>
    public static string DropD(double len, double wid) =>
        $"M0 0C{F(len * 0.45)} {F(-wid * 0.25)} {F(len * 0.6)} {F(-wid)} {F(len * 0.82)} {F(-wid * 0.62)}C{F(len * 1.08)} {F(-wid * 0.3)} {F(len * 1.08)} {F(wid * 0.3)} {F(len * 0.82)} {F(wid * 0.62)}C{F(len * 0.6)} {F(wid)} {F(len * 0.45)} {F(wid * 0.25)} 0 0Z";
}
