// Classic Borders: narrow traditional rule frames. Thick-and-thin rules with one quiet corner treatment.
using static Borders.Geom;

namespace Borders.Styles;

public static class Classic
{
    public static string Draw(Frame frame)
    {
        double W = frame.W, H = frame.H, t = frame.T, u = frame.U;
        var rnd = frame.Rnd;
        double hair = 1.1 * u, thick = 3.2 * u;
        string variant = rnd.Pick("scotch", "indent", "notch", "oxford");
        double o = t * rnd.Range(0.16, 0.24);          // outer (thick) rule
        double d = t * rnd.Range(0.26, 0.34);          // distance to the inner (thin) rule
        bool triple = rnd.Chance(0.35);                // thin - thick - thin
        bool centre = rnd.Chance(0.5);
        string Diamond(double x, double y, double s) => Fill($"M{F(x)} {F(y - s)}L{F(x + s)} {F(y)}L{F(x)} {F(y + s)}L{F(x - s)} {F(y)}Z");
        string svg = "";
        double inner = o + d;                          // inset of the innermost thin rule (before any third rule)

        // Rules: each is a closed outline built from one top-left corner polyline.
        string Rule(IReadOnlyList<Pt> corner, double w) => StrokeSq(PolyD(Frame.CornerRectPts(W, H, corner), true), w);

        if (variant == "scotch")
        {
            svg += StrokeSq(RectD(o, o, W - 2 * o, H - 2 * o), thick);
            svg += StrokeSq(RectD(o + d, o + d, W - 2 * (o + d), H - 2 * (o + d)), hair);
            if (triple) svg += StrokeSq(RectD(o + d * 1.5, o + d * 1.5, W - 2 * (o + d * 1.5), H - 2 * (o + d * 1.5)), hair);
            // a small diamond and bead tucked inside each corner
            double c = o + d * (triple ? 1.5 : 1) + t * 0.38;
            svg += frame.Corners(Diamond(c, c, t * 0.17) + Dot(c + t * 0.34, c + t * 0.34, t * 0.06));
        }
        else if (variant == "indent")
        {
            // concave quarter-circle corners; the inner rule shares the arc centre so the two stay parallel
            double r = t * rnd.Range(0.7, 1.05);
            List<Pt> Corner(double off)
            {
                double rr = r + off, h = Math.Sqrt(rr * rr - off * off);
                return ArcPts(o, o, rr, Math.Atan2(h, off), Math.Atan2(off, h));
            }
            svg += Rule(Corner(0), thick) + Rule(Corner(d), hair);
            if (triple) svg += Rule(Corner(d * 1.5), hair);
            svg += frame.Corners(Dot(o + r * 0.36, o + r * 0.36, t * 0.1));
        }
        else if (variant == "notch")
        {
            // square stepped-in corners
            double n = t * rnd.Range(0.55, 0.85);
            List<Pt> Corner(double off) => new() { new(o + off, o + n + off), new(o + n + off, o + n + off), new(o + n + off, o + off) };
            svg += Rule(Corner(0), thick) + Rule(Corner(d), hair);
            if (triple) svg += Rule(Corner(d * 1.5), hair);
            double s = n * 0.3;
            svg += frame.Corners(Fill(RectD(o + n * 0.5 - s / 2 - thick * 0.25, o + n * 0.5 - s / 2 - thick * 0.25, s, s)));
        }
        else
        {
            // Oxford corners: the thick rules cross and run on past each other
            double a = o + t * rnd.Range(0.22, 0.32), e = t * 0.04;
            svg += StrokeSq($"M{F(e)} {F(a)}H{F(W - e)}M{F(e)} {F(H - a)}H{F(W - e)}M{F(a)} {F(e)}V{F(H - e)}M{F(W - a)} {F(e)}V{F(H - e)}", thick);
            double b = a + d;
            inner = b;
            svg += StrokeSq(RectD(b, b, W - 2 * b, H - 2 * b), hair);
            if (triple) svg += StrokeSq(RectD(b + d * 0.5, b + d * 0.5, W - 2 * (b + d * 0.5), H - 2 * (b + d * 0.5)), hair);
            svg += frame.Corners(Diamond((a + e) / 2 - thick * 0.25, (a + e) / 2 - thick * 0.25, Math.Min(t * 0.13, (a - e) * 0.3)));
        }

        // optional mark at the middle of each side: a diamond between two beads, sitting just inside the rules
        if (centre)
        {
            double y = inner + (triple ? d * 0.5 : 0) + t * 0.36;
            svg += frame.Edges(L => Diamond(L / 2, y, t * 0.15) + Dot(L / 2 - t * 0.42, y, t * 0.055) + Dot(L / 2 + t * 0.42, y, t * 0.055));
        }
        return svg;
    }
}
