// Art Nouveau Whiplash Borders: bundles of long tapering lines that undulate along each edge and snap back into curls.
using static Borders.Geom;
using static Borders.Ornament;

namespace Borders.Styles;

public static class Whiplash
{
    public static string Draw(Frame frame)
    {
        double W = frame.W, H = frame.H, t = frame.T, u = frame.U;
        var rnd = frame.Rnd;
        double knot = t * 0.52;
        int n = rnd.Int(3, 4);
        string medallion = rnd.Pick("rose", "rings", "bud");
        double waveK = rnd.Pick(1, 1.5, 2);

        // A bundle of strands leaving the corner medallion along +x. y is measured from the outer edge.
        string Bundle(double Lh)
        {
            string g = "";
            double[] fracs = new[] { 0.98, 0.74, 0.52, 0.36 }.Take(n).ToArray();
            // shuffle which lane gets which length
            for (int i = fracs.Length - 1; i > 0; i--)
            {
                int j = rnd.Int(0, i);
                (fracs[i], fracs[j]) = (fracs[j], fracs[i]);
            }
            for (int i = 0; i < n; i++)
            {
                double lane = t * Lerp(0.24, 0.78, n == 1 ? 0.5 : (double)i / (n - 1));
                double amp = t * rnd.Range(0.07, 0.15);
                double lambda = Math.Max(t * 1.6, Lh / waveK) * rnd.Range(0.85, 1.15);
                double phase = rnd.Range(0, TAU);
                double X = Math.Max(t * 0.7, (Lh - t * 0.42) * fracs[i]);
                double YAt(double x)
                {
                    double b = Lerp(knot, lane, Smoothstep(0, t * 0.95, x));
                    return Math.Min(t * 0.9, Math.Max(t * 0.1, b + amp * Math.Sin((TAU * x) / lambda + phase) * Smoothstep(t * 0.2, t * 1.3, x)));
                }
                var pts = new List<Pt>();
                for (double x = 0; x <= X; x += 2) pts.Add(new Pt(x, YAt(x)));
                double yEnd = YAt(X), aEnd = Math.Atan2(YAt(X) - YAt(X - 2), 2);
                int dir = yEnd > t * 0.5 ? -1 : 1;
                double room = dir < 0 ? yEnd - t * 0.08 : t * 0.92 - yEnd;
                double size = Math.Min(t * rnd.Range(0.22, 0.34), room * 0.6);
                var end = CurlPts(x: X, y: yEnd, h: aEnd, lead: t * 0.22, bend: -dir * Rad(rnd.Range(10, 20)), size: size, dir: dir, turns: rnd.Range(1.15, 1.5), open: 1, tight: 5.2);
                pts.AddRange(end.Skip(1));
                g += Flourish(pts, u * rnd.Range(4.4, 6.4), min: 0.9 * u, peak: rnd.Range(0.14, 0.28), fall: 0.75);
            }
            return g;
        }

        string quad = "";
        quad += $"<g transform=\"translate({F(knot)} 0)\">{Bundle(W / 2 - knot)}</g>";
        quad += $"<g transform=\"matrix(0 1 1 0 0 {F(knot)})\">{Bundle(H / 2 - knot)}</g>";

        // ---- corner medallion hiding where the bundles meet ----
        double R = t * 0.31;
        // a pair of sweeping leaves reaching back toward the outer corner and one pointing into the frame
        quad += Place(knot, knot, Rad(225), Fill(DropD(t * 0.5, t * 0.1)));
        foreach (int d in new[] { 45 - 24, 45 + 24 }) quad += Place(knot, knot, Rad(d), Fill(DropD(t * 0.58, t * 0.075)));
        quad += Place(knot, knot, Rad(45), Fill(DropD(t * 0.84, t * 0.1)));
        quad += Outlined(CircleD(knot, knot, R), 2.6 * u);
        if (medallion == "rose")
        {
            var sp = SpiralPts(knot, knot, R * 0.82, R * 0.08, rnd.Range(0, TAU), rnd.Range(0, TAU) + TAU * 2.4, 0.9);
            quad += Fill(RibbonD(sp, Taper.Out(2.8 * u, 0.6 * u, 0.7)));
        }
        else if (medallion == "rings")
        {
            quad += Fill(CircleD(knot, knot, R * 0.62)) + Fill(CircleD(knot, knot, R * 0.36), "#fff") + Dot(knot, knot, R * 0.16);
        }
        else
        {
            for (int i = 0; i < 5; i++) quad += Place(knot, knot, Rad(45 + i * 72), Fill(DropD(R * 0.84, R * 0.22)));
            quad += Dot(knot, knot, R * 0.2, "#fff") + Dot(knot, knot, R * 0.09);
        }

        string o = "";
        double fr = t * 0.06;
        o += StrokeSq(RectD(fr, fr, W - 2 * fr, H - 2 * fr), 1.5 * u);
        o += frame.Quad(quad);

        // ---- pendant where the bundles from both corners face each other ----
        o += frame.Edges(L =>
        {
            double cx = L / 2;
            return
                Place(cx, t * 0.2, Rad(90), Fill(DropD(t * 0.44, t * 0.07))) +
                Dot(cx, t * 0.74, t * 0.04) +
                Dot(cx, t * 0.86, t * 0.026);
        });
        return o;
    }
}
