// Engraved Floral Frames: laurel garlands and rosettes shaded with fine hatch lines, inside milled rules.
using static Borders.Geom;

namespace Borders.Styles;

public static class Engraved
{
    public static string Draw(Frame frame)
    {
        double W = frame.W, H = frame.H, t = frame.T, u = frame.U;
        var rnd = frame.Rnd;
        double lw = 1.25 * u, hair = Math.Max(0.55, 0.62 * u);
        double hatchGap = Math.Max(1.9, 2.05 * u);
        double slim = rnd.Range(0.27, 0.34);
        double pairAngle = Rad(rnd.Range(26, 36));
        double pairGap = t * rnd.Range(0.27, 0.33);
        int outerPetals = rnd.Pick(7, 8, 9);
        bool withBerries = rnd.Chance(0.6);
        bool milled = rnd.Chance(0.65);

        // Leaf from an explicit width profile so hatch lines can be clipped analytically (no clip paths).
        double Prof(double q) => q <= 0 || q >= 1 ? 0 : Math.Pow(Math.Sin(Math.PI * Math.Pow(q, 0.78)), 0.85);
        string HatchedLeaf(double len, double wid, double shadeSide)
        {
            var top = new List<Pt>();
            var bot = new List<Pt>();
            for (int i = 0; i <= 26; i++)
            {
                double q = i / 26.0;
                top.Add(new Pt(q * len, -wid * Prof(q)));
                bot.Add(new Pt(q * len, wid * Prof(q)));
            }
            bot.Reverse();
            top.AddRange(bot);
            string g = Outlined(PolyD(top, true), lw);
            // dense shading on one half, running forward and outward from the midrib
            string h = "";
            double dx = Math.Cos(Rad(38)), dy = Math.Sin(Rad(38)) * shadeSide;
            for (double x0 = len * 0.06; x0 < len * 0.94; x0 += hatchGap)
            {
                double s = 0;
                while (s < wid * 2)
                {
                    double x = x0 + dx * (s + 1), y = dy * (s + 1);
                    if (Math.Abs(y) > wid * Prof(x / len) - lw * 0.5) break;
                    s += 1;
                }
                if (s > 1.5) h += $"M{F(x0)} 0L{F(x0 + dx * s)} {F(dy * s)}";
            }
            // a few open veins on the lit half
            foreach (double q in new[] { 0.22, 0.4, 0.58, 0.74 })
            {
                double reach = wid * Prof(q + 0.1) * 0.6;
                h += $"M{F(len * q)} 0L{F(len * (q + 0.1))} {F(-shadeSide * reach)}";
            }
            g += Stroke(h, hair);
            g += Stroke($"M0 0L{F(len * 0.93)} 0", lw * 0.8);
            return g;
        }

        string Petal(double len, double wid)
        {
            var top = new List<Pt>();
            var bot = new List<Pt>();
            for (int i = 0; i <= 20; i++)
            {
                double q = i / 20.0, w = wid * Math.Pow(Math.Sin(Math.PI * Math.Pow(q, 1.45)), 0.6);
                top.Add(new Pt(q * len, -w));
                bot.Add(new Pt(q * len, w));
            }
            bot.Reverse();
            top.AddRange(bot);
            string g = Outlined(PolyD(top, true), lw);
            string h = "";
            double n = Math.Max(2, Round((wid * 1.5) / hatchGap));
            for (int i = 0; i <= n; i++)
            {
                double b = ((i / n) * 2 - 1) * 0.42;
                double r1 = len * (0.62 - Math.Abs(b) * 0.35);
                h += $"M{F(Math.Cos(b) * len * 0.2)} {F(Math.Sin(b) * len * 0.2)}L{F(Math.Cos(b) * r1)} {F(Math.Sin(b) * r1)}";
            }
            return g + Stroke(h, hair);
        }

        string Rosette(double x, double y, double R, int n, double rot)
        {
            string g = "";
            void RingOf(int count, double len, double wid, double r0)
            {
                for (int i = 0; i < count; i++) g += $"<g transform=\"rotate({F(Deg(r0 + (TAU * i) / count))})\">{Petal(len, wid)}</g>";
            }
            RingOf(n, R, (R * 2.5) / n, rot);
            int inner = Math.Max(5, n - 3);
            RingOf(inner, R * 0.62, (R * 1.75) / inner, rot + Math.PI / inner);
            g += Ring(0, 0, R * 0.2, lw, PAPER);
            for (int i = 0; i < 6; i++) g += Dot(Math.Cos((TAU * i) / 6) * R * 0.105, Math.Sin((TAU * i) / 6) * R * 0.105, Math.Max(0.6, R * 0.028));
            g += Dot(0, 0, Math.Max(0.7, R * 0.035));
            return $"<g transform=\"translate({F(x)} {F(y)})\">{g}</g>";
        }

        string o = "";

        // ---- engraved rules ----
        double e0 = t * 0.05, e1 = t * 0.115, e2 = t * 0.215;
        o += StrokeSq(RectD(e0, e0, W - 2 * e0, H - 2 * e0), 2.3 * u);
        if (milled)
        {
            double mid = (e1 + e2) / 2;
            o += StrokeSq(RectD(mid, mid, W - 2 * mid, H - 2 * mid), e2 - e1 - hair, $" stroke-dasharray=\"{F(Math.Max(0.7, 0.8 * u))} {F(Math.Max(1.7, 2 * u))}\"");
            o += StrokeSq(RectD(e1, e1, W - 2 * e1, H - 2 * e1), hair) + StrokeSq(RectD(e2, e2, W - 2 * e2, H - 2 * e2), hair);
        }
        else
        {
            for (int i = 0; i < 4; i++)
            {
                double e = e1 + ((e2 - e1) * i) / 3;
                o += StrokeSq(RectD(e, e, W - 2 * e, H - 2 * e), hair);
            }
        }
        double ei = t * 0.98;
        o += StrokeSq(RectD(ei, ei, W - 2 * ei, H - 2 * ei), lw);

        // ---- laurel garland: leaf pairs marching from each corner toward the middle of the edge ----
        double yc = t * 0.61;
        double cf = t * 0.64, RR = t * 0.4;
        double start = cf + RR * 0.5;
        string Garland(double len)
        {
            string g = Stroke($"M0 0H{F(len)}", lw);
            double n = Math.Max(1, Math.Floor((len - t * 0.3) / pairGap));
            double step = (len - t * 0.3) / n;
            for (int i = (int)n - 1; i >= 0; i--)
            {
                double x = i * step + t * 0.05;
                double k = 1 - 0.22 * (i / Math.Max(1, n - 1));
                double ll = t * 0.5 * k;
                foreach (int s in new[] { -1, 1 }) g += Place(x, 0, s * pairAngle, HatchedLeaf(ll, ll * slim, -s));
                if (withBerries && i % 2 == 1)
                {
                    foreach (int s in new[] { -1, 1 }) g += Ring(x + step * 0.42, s * t * 0.045, t * 0.036, hair * 1.3, PAPER);
                }
            }
            return g;
        }
        string quad = "";
        double lh = W / 2 - start - t * 0.14, lv = H / 2 - start - t * 0.14;
        quad += $"<g transform=\"translate({F(start)} {F(yc)})\">{Garland(lh)}</g>";
        quad += $"<g transform=\"matrix(0 1 1 0 {F(yc)} {F(start)})\">{Garland(lv)}</g>";

        // corner: three large leaves behind a rosette
        foreach (int d in new[] { 225, 180, 270 })
        {
            double ll = d == 225 ? t * 0.62 : t * 0.5;
            quad += Place(cf, cf, Rad(d), HatchedLeaf(ll, ll * slim * 1.05, d == 270 ? -1 : 1));
        }
        quad += Rosette(cf, cf, RR, outerPetals, rnd.Range(0, TAU));
        o += frame.Quad(quad);

        // ---- small rosettes where the garlands meet ----
        double mr = t * 0.27;
        int mn = Math.Max(6, outerPetals - 2);
        o += Rosette(W / 2, yc, mr, mn, -Math.PI / 2);
        o += Rosette(W / 2, H - yc, mr, mn, Math.PI / 2);
        o += Rosette(yc, H / 2, mr, mn, Math.PI);
        o += Rosette(W - yc, H / 2, mr, mn, 0);
        return o;
    }
}
