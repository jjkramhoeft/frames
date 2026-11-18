// Ornament building blocks shared by the curve-based styles.
using static Borders.Geom;

namespace Borders;

public static class Ornament
{
    /// <summary>
    /// Scroll: an optional gently bending lead-in followed by a clothoid curl that tightens into a spiral.
    /// dir = +1 curls clockwise on screen, -1 anticlockwise. Returns the polyline.
    /// </summary>
    public static List<Pt> CurlPts(double x, double y, double h, double size, double lead = 0, double bend = 0,
        double dir = 1, double turns = 1.4, double open = 0.9, double tight = 8)
    {
        var pts = new List<Pt> { new(x, y) };
        if (lead > 0)
        {
            var a = Turtle(x, y, h, lead, (_, _) => bend / lead);
            pts = a.Pts;
            x = a.X; y = a.Y; h = a.H;
        }
        double len = (turns * TAU * size) / ((open + tight) / 2);
        var b = Turtle(x, y, h, len, (_, u) => (dir * (open + (tight - open) * u)) / size);
        pts.AddRange(b.Pts.Skip(1));
        return pts;
    }

    /// <summary>Tapered flourish stroke with an optional ball terminal at its end.</summary>
    public static string Flourish(IReadOnlyList<Pt> pts, double w, double ball = 0, double min = 0.5, double peak = 0.3, double fall = 1.35)
    {
        string o = Fill(RibbonD(pts, Taper.Whip(w, min, peak, fall)));
        if (ball != 0)
        {
            var p = pts[^1];
            o += Dot(p.X, p.Y, ball);
        }
        return o;
    }

    /// <summary>Leaf placed at (x,y) pointing along a. kind: "solid" | "outline" | "veined".</summary>
    public static string Leaf(double x, double y, double a, double len, double wid, string kind, double lw, double belly = 0.4)
    {
        string d = LeafD(len, wid, belly);
        string inner;
        if (kind == "solid")
        {
            inner = Fill(d) + $"<path d=\"M{F(len * 0.12)} 0L{F(len * 0.8)} 0\" stroke=\"{PAPER}\" stroke-width=\"{F(lw * 0.8)}\" stroke-linecap=\"round\" fill=\"none\"/>";
        }
        else
        {
            inner = Outlined(d, lw) + Stroke($"M0 0L{F(len * 0.86)} 0", lw * 0.7);
            if (kind == "veined")
            {
                string v = "";
                foreach (double q in new[] { 0.25, 0.45, 0.63 })
                {
                    double reach = wid * (q < 0.5 ? 0.62 : 0.45);
                    v += $"M{F(len * q)} 0L{F(len * (q + 0.14))} {F(-reach)}M{F(len * q)} 0L{F(len * (q + 0.14))} {F(reach)}";
                }
                inner += Stroke(v, lw * 0.55);
            }
        }
        return Place(x, y, a, inner);
    }

    /// <summary>Open flower seen from above. kind: "round" (5-6 broad petals) | "daisy" (many slim petals) | "star".</summary>
    public static string Flower(double x, double y, double r, int n = 5, double rot = 0, string kind = "round", double lw = 1.2, double centre = 0.24)
    {
        string inner = "";
        double wid = kind == "daisy" ? (r * 2.4) / n : kind == "star" ? (r * 1.9) / n : (r * 2.9) / n;
        for (int i = 0; i < n; i++)
        {
            double a = rot + (TAU * i) / n;
            string shape = kind == "star" ? LeafD(r, wid, 0.45) : PetalD(r, wid, kind == "round" ? r * 0.1 : 0);
            string p = Outlined(shape, lw);
            // a short crease line running out from the centre
            p += Stroke($"M{F(r * centre * 1.05)} 0L{F(r * 0.56)} 0", lw * 0.6);
            inner += $"<g transform=\"rotate({F(Deg(a))})\">{p}</g>";
        }
        inner += Dot(0, 0, r * centre);
        inner += Dot(0, 0, r * centre * 0.42, PAPER);
        return $"<g transform=\"translate({F(x)} {F(y)})\">{inner}</g>";
    }

    /// <summary>Bud / tulip seen from the side, growing along a from (x,y).</summary>
    public static string Bud(double x, double y, double a, double len, double lw, double open = 0.5)
    {
        double w = len * 0.42;
        string inner = "";
        foreach (int s in new[] { -1, 1 })
        {
            inner += $"<g transform=\"rotate({F(s * 26 * open)})\">{Outlined(LeafD(len, w * 0.62, 0.42, s * len * 0.1), lw)}</g>";
        }
        inner += Outlined(LeafD(len * 1.05, w * 0.6, 0.45), lw);
        inner += Fill(DropD(len * 0.34, w * 0.42));
        return Place(x, y, a, inner);
    }

    /// <summary>Small cluster of berries on short stalks.</summary>
    public static string Berries(double x, double y, double a, double len, double r, double lw)
    {
        string inner = "";
        foreach (var (da, k) in new[] { (-0.5, 0.85), (0.0, 1.0), (0.5, 0.85) })
        {
            double ex = Math.Cos(da) * len * k, ey = Math.Sin(da) * len * k;
            inner += Stroke($"M0 0L{F(ex)} {F(ey)}", lw * 0.7) + Dot(ex, ey, r);
        }
        return Place(x, y, a, inner);
    }
}
