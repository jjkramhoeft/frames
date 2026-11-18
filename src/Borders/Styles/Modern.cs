// Modern Borders: narrow, minimal line frames. A few clean lines, one deliberate accent, no ornament.
using static Borders.Geom;

namespace Borders.Styles;

public static class Modern
{
    public static string Draw(Frame frame)
    {
        double W = frame.W, H = frame.H, t = frame.T, u = frame.U;
        var rnd = frame.Rnd;
        double hair = 1.1 * u, med = 2.2 * u, bold = 4.6 * u;
        string variant = rnd.Pick("overshoot", "brackets", "offset", "breaks", "rounded");
        string o = "";

        if (variant == "overshoot")
        {
            // four lines that run past each other at the corners
            double a = t * rnd.Range(0.5, 0.62), e = t * rnd.Range(0.04, 0.2);
            o += StrokeSq(
                $"M{F(e)} {F(a)}H{F(W - e)}M{F(e)} {F(H - a)}H{F(W - e)}M{F(a)} {F(e)}V{F(H - e)}M{F(W - a)} {F(e)}V{F(H - e)}",
                med);
            if (rnd.Chance(0.6))
            {
                double b = a + t * rnd.Range(0.24, 0.32);
                o += StrokeSq(RectD(b, b, W - 2 * b, H - 2 * b), hair);
            }
            else
            {
                double s = t * 0.09;
                o += frame.Corners(Fill(RectD(a - s, a - s, s * 2, s * 2)));
            }
        }
        else if (variant == "brackets")
        {
            // heavy corner brackets around a hairline frame
            double a = t * rnd.Range(0.16, 0.24), arm = t * rnd.Range(1.3, 2.4);
            double b = a + t * rnd.Range(0.36, 0.5);
            o += StrokeSq(RectD(b, b, W - 2 * b, H - 2 * b), hair);
            o += frame.Corners(StrokeSq($"M{F(a)} {F(a + arm)}V{F(a)}H{F(a + arm)}", bold));
            if (rnd.Chance(0.5)) o += frame.Edges(L => Fill(RectD(L / 2 - t * 0.5, a - bold / 2, t, bold)));
        }
        else if (variant == "offset")
        {
            // two equal rectangles slipped diagonally against each other
            double a = t * rnd.Range(0.14, 0.24), d = t * rnd.Range(0.4, 0.55);
            bool flip = rnd.Chance(0.5);
            double ax = flip ? a + d : a, bx = flip ? a : a + d;
            o += StrokeSq(RectD(ax, a, W - 2 * a - d, H - 2 * a - d), hair);
            o += StrokeSq(RectD(bx, a + d, W - 2 * a - d, H - 2 * a - d), med);
        }
        else if (variant == "breaks")
        {
            // one frame line, opened at the middle of each side for a short accent
            double a = t * rnd.Range(0.42, 0.56), gap = t * rnd.Range(1.5, 2.4);
            string accent = rnd.Pick("bar", "dots", "none");
            o += frame.Quad(StrokeSq($"M{F(W / 2 - gap / 2)} {F(a)}H{F(a)}V{F(H / 2 - gap / 2)}", med));
            o += frame.Edges(L =>
            {
                double cx = L / 2;
                if (accent == "bar") return Fill(RectD(cx - gap * 0.22, a - bold / 2, gap * 0.44, bold));
                if (accent == "dots") return string.Concat(new[] { -1, 0, 1 }.Select(i => Dot(cx + i * gap * 0.2, a, med * 1.05)));
                return "";
            });
            if (accent == "none" || rnd.Chance(0.4))
            {
                double b = a + t * rnd.Range(0.26, 0.34);
                o += StrokeSq(RectD(b, b, W - 2 * b, H - 2 * b), hair);
            }
        }
        else
        {
            // soft rounded frame with a hairline companion
            double a = t * rnd.Range(0.2, 0.3), r = t * rnd.Range(0.7, 1.5);
            string Rr(double inset, double rad) =>
                $"<rect x=\"{F(inset)}\" y=\"{F(inset)}\" width=\"{F(W - 2 * inset)}\" height=\"{F(H - 2 * inset)}\" rx=\"{F(rad)}\" fill=\"none\" stroke=\"#000\" stroke-width=\"";
            o += Rr(a, r) + $"{F(med)}\"/>";
            double gapIn = t * rnd.Range(0.3, 0.42);
            o += Rr(a + gapIn, Math.Max(1, r - gapIn)) + $"{F(hair)}\"/>";
            if (rnd.Chance(0.5)) o += frame.Edges(L => Dot(L / 2, a + gapIn / 2, med * 0.95));
        }
        return o;
    }
}
