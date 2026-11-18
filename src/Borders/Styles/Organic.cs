// Organic Borders: irregular, hand-drawn natural forms with no mirror symmetry.
// Two families chosen by the seed: packed pebbles/cells, or flowing strata (wood-grain contours with knots).
using System.Text;
using static Borders.Geom;

namespace Borders.Styles;

public static class Organic
{
    private readonly record struct Stone(double X, double Y, double R);
    private readonly record struct Knot(double S, double D, double A, double Sg);

    public static string Draw(Frame frame)
    {
        double W = frame.W, H = frame.H, t = frame.T, u = frame.U;
        var rnd = frame.Rnd;
        var tr = new Track(W, H, t * 0.04, t * 1.0);
        string variant = rnd.Pick("pebbles", "strata");
        var o = new StringBuilder();

        string Blob(double cx, double cy, double r, double wob = 0.16, int n = 9)
        {
            double ph = rnd.Range(0, TAU), k2 = rnd.Range(0.4, 1), k3 = rnd.Range(0.2, 0.8), p2 = rnd.Range(0, TAU), p3 = rnd.Range(0, TAU);
            var pts = new List<Pt>(n);
            for (int i = 0; i < n; i++)
            {
                double a = ph + (TAU * i) / n;
                double rr = r * (1 + wob * (k2 * Math.Sin(2 * a + p2) + k3 * Math.Sin(3 * a + p3)) * 0.8);
                pts.Add(new Pt(cx + rr * Math.Cos(a), cy + rr * Math.Sin(a)));
            }
            return PolyD(Spline(pts, true, 6), true);
        }

        if (variant == "pebbles")
        {
            var stones = new List<Stone>();
            double gap = t * 0.022;
            bool Fits(double x, double y, double r)
            {
                foreach (var s in stones)
                {
                    double dx = s.X - x, dy = s.Y - y, m = s.R + r + gap;
                    if (dx * dx + dy * dy < m * m) return false;
                }
                return true;
            }
            double attempts = Round((tr.Len / t) * 260);
            double fade = rnd.Range(0.75, 0.95);
            double[] passScale = { 1, 0.7, 0.45 };
            for (int pass = 0; pass < 3; pass++)
            {
                for (int i = 0; i < attempts; i++)
                {
                    double d = t * (0.05 + Math.Pow(rnd.Next(), pass == 0 ? 1.6 : 1) * fade);
                    double big = Lerp(0.2, 0.05, d / t) * passScale[pass];
                    double r = t * big * rnd.Range(0.7, 1.1);
                    if (d - r < t * 0.02) continue;
                    var p = tr.At(rnd.Range(0, tr.Len), d);
                    if (Fits(p.X, p.Y, r)) stones.Add(new Stone(p.X, p.Y, r));
                }
            }
            double lw = 1.5 * u;
            foreach (var s in stones)
            {
                double roll = rnd.Next();
                if (s.R < t * 0.035 || roll < 0.14)
                {
                    o.Append(Fill(Blob(s.X, s.Y, s.R, 0.14)));
                }
                else
                {
                    o.Append(Outlined(Blob(s.X, s.Y, s.R, 0.17), lw * rnd.Range(0.85, 1.25)));
                    if (s.R > t * 0.1 && roll > 0.55) o.Append(Stroke(Blob(s.X, s.Y, s.R * 0.58, 0.2), 0.8 * u));
                    else if (s.R > t * 0.075 && roll > 0.4) o.Append(Fill(Blob(s.X + s.R * 0.15, s.Y - s.R * 0.1, s.R * 0.24, 0.2, 7)));
                }
            }
        }
        else
        {
            int m = rnd.Int(6, 8);
            var shared = MakeNoise(rnd, tr.Len, 5);
            var knots = new List<Knot>();
            double kc = Math.Max(5, Round(tr.Len / (t * rnd.Range(2.4, 3.4))));
            for (int i = 0; i < kc; i++)
            {
                double ks = ((i + rnd.Range(0.15, 0.85)) / kc) * tr.Len;
                double kd = t * rnd.Range(0.3, 0.68);
                double ka = t * rnd.Range(0.1, 0.16);
                double ksg = t * rnd.Range(0.45, 0.8);
                knots.Add(new Knot(ks, kd, ka, ksg));
            }
            double Wrap(double ds)
            {
                ds = ((ds % tr.Len) + tr.Len) % tr.Len;
                return ds > tr.Len / 2 ? ds - tr.Len : ds;
            }
            for (int i = 0; i < m; i++)
            {
                double baseOff = t * Lerp(0.1, 0.88, (double)i / (m - 1));
                var own = MakeNoise(rnd, tr.Len, 6);
                var wn = MakeNoise(rnd, tr.Len, 7);
                double Off(double s)
                {
                    double v = baseOff + t * 0.085 * shared(s) + t * 0.03 * own(s);
                    foreach (var k in knots)
                    {
                        double ds = Wrap(s - k.S), dd = baseOff - k.D;
                        v += (dd < 0 ? -1 : 1) * k.A * Math.Exp(-(ds * ds) / (k.Sg * k.Sg)) * Math.Exp(-Math.Abs(dd) / (t * 0.3));
                    }
                    return Math.Max(t * 0.02, v);
                }
                var pts = tr.Run(0, tr.Len, Off, 2.5);
                double wBase = (i == 0 ? 3.4 : i == m - 1 ? 1.2 : rnd.Range(1.3, 2.6)) * u;
                int j = 0;
                o.Append(Fill(RibbonD(pts, (_, _) => wBase * (1 + 0.55 * wn(((double)j++ / pts.Count) * tr.Len)))));
            }
            foreach (var k in knots)
            {
                var p = tr.At(k.S, k.D);
                var pts = new List<Pt>(10);
                double rx = k.Sg * 0.34, ry = k.A * 0.62, ph = rnd.Range(0, TAU);
                for (int q = 0; q < 10; q++)
                {
                    double a = (TAU * q) / 10, wob = 1 + 0.12 * Math.Sin(3 * a + ph);
                    double lx = Math.Cos(a) * rx * wob, ly = Math.Sin(a) * ry * wob;
                    pts.Add(new Pt(p.X + lx * Math.Cos(p.A) + ly * p.Nx, p.Y + lx * Math.Sin(p.A) + ly * p.Ny));
                }
                string d = PolyD(Spline(pts, true, 6), true);
                o.Append(rnd.Chance(0.5) ? Fill(d) : Outlined(d, 1.6 * u) + Fill(Blob(p.X, p.Y, ry * 0.4, 0.2, 7)));
            }
        }
        return o.ToString();
    }
}
