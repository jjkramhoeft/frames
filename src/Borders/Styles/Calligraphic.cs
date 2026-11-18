// Calligraphic Flourishes: broad-nib pen strokes that loop like a spring at the corners and edge
// centres, with nib-cut diamonds in between.
using static Borders.Geom;

namespace Borders.Styles;

public static class Calligraphic
{
    public static string Draw(Frame frame)
    {
        double W = frame.W, H = frame.H, t = frame.T, u = frame.U;
        var rnd = frame.Rnd;
        var tr = new Track(W, H, t * 0.5, t * 0.8);
        double nib = Rad(rnd.Range(-50, -32));
        // broad-nib pen: thick across the nib, hairline along it; fade keeps free ends pointed
        Func<double, double, double> Pen(double wmax, double wmin, double fade = 0) => (q, a) =>
        {
            double kk = fade != 0 ? Math.Min(1, Math.Min(q / fade, (1 - q) / fade)) : 1;
            return (wmin + (wmax - wmin) * Math.Abs(Math.Sin(a - nib))) * (0.15 + 0.85 * kk);
        };
        double pitch = t * rnd.Range(1.2, 1.55);       // spacing between successive loops
        double bCorner = t * rnd.Range(0.37, 0.43);
        double bCentre = t * rnd.Range(0.36, 0.42);
        double cornerSpan = t * rnd.Range(1.5, 2.1);
        bool second = rnd.Chance(0.75);
        bool accents = rnd.Chance(0.8);

        double sL = tr.Marks.Left, sC = tr.Marks.TopLeft, sT = tr.Marks.Top;
        double span = sT - sL;
        double k = Math.Max(2, Round((span * 2) / pitch));

        // One quadrant of a looping stroke: track point + rotating offset whose radius follows an envelope.
        List<Pt> Looped(double side, double scale, double baseOff, double phaseTurns)
        {
            var pts = new List<Pt>();
            double steps = Math.Ceiling(span / 1.5);
            for (int i = 0; i <= steps; i++)
            {
                double s = sL + (span * i) / steps;
                double dCentre = Math.Min(s - sL, sT - s);
                double reachE = Math.Min(t * 3.4, (s - sL < sT - s ? sC - sL : sT - sC) * 0.6);
                double env = Math.Max(
                    Math.Max(
                        bCentre * (1 - Smoothstep(0, reachE, dCentre)),
                        bCorner * (1 - Smoothstep(cornerSpan * 0.25, cornerSpan, Math.Abs(s - sC)))),
                    t * 0.03);
                double th = side * (Math.PI / 2 + (k + phaseTurns) * Math.PI * (i / steps));
                var p = tr.At(s, baseOff);
                double ca = Math.Cos(p.A), sa = Math.Sin(p.A);
                double b = env * scale;
                pts.Add(new Pt(p.X + b * (Math.Cos(th) * ca + Math.Sin(th) * p.Nx), p.Y + b * (Math.Cos(th) * sa + Math.Sin(th) * p.Ny)));
            }
            return pts;
        }

        string quad = "";
        if (second) quad += Fill(RibbonD(Looped(-1, 0.6, t * 0.12, 2), Pen(3.6 * u, 0.6 * u)));
        quad += Fill(RibbonD(Looped(1, 1, second ? -t * 0.05 : 0, 0), Pen(8.2 * u, 0.8 * u)));
        string o = frame.Quad(quad);

        // nib-cut diamond
        string Diamond(double x, double y, double s) => Place(x, y, nib, Fill($"M{F(-s)} 0L0 {F(-s * 0.62)}L{F(s)} 0L0 {F(s * 0.62)}Z"));

        // ---- a trail of diamonds stepping from each corner into the frame ----
        if (accents)
        {
            string c = "";
            foreach (var (d, r) in new[] { (1.0, 0.085), (1.15, 0.055), (1.27, 0.034) }) c += Diamond(t * d, t * d, t * r);
            o += frame.Corners(c);
        }

        // ---- pendants under the central loops ----
        o += frame.Edges(L =>
        {
            double cx = L / 2;
            // pendant of diamonds hanging into the frame under the central loop
            return Diamond(cx, t * 1.02, t * 0.095) + Diamond(cx, t * 1.22, t * 0.06) + Diamond(cx, t * 1.37, t * 0.036);
        });
        return o;
    }
}
