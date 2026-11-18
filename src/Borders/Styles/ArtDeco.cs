// Art Deco Line Borders: ruled parallel lines, stepped (ziggurat) corners, sunbursts, keystones.
using static Borders.Geom;

namespace Borders.Styles;

public static class ArtDeco
{
    public static string Draw(Frame frame)
    {
        double W = frame.W, H = frame.H, t = frame.T, u = frame.U;
        var rnd = frame.Rnd;
        double m = t * 0.07;
        double bold = 3.4 * u, med = 2 * u, thin = 1.1 * u;

        string cornerType = rnd.Pick("stepped", "fan", "blocks");
        string centreType = rnd.Pick("sunburst", "keystone", "chevron");

        // Parallel rules, outer to inner.
        var lines = new List<(double d, double w)> { (m + bold / 2, bold) };
        double d = m + bold + t * rnd.Range(0.07, 0.1);
        lines.Add((d, thin));
        if (rnd.Chance(0.6)) { d += t * 0.065; lines.Add((d, thin)); }
        d = t * rnd.Range(0.56, 0.64);
        lines.Add((d, med));
        lines.Add((d + t * 0.075, thin));
        if (rnd.Chance(0.5)) lines.Add((d + t * 0.14, thin));
        double dMax = lines[^1].d;

        string o = "";

        // ---- rules ----
        int k = rnd.Int(2, 3);
        double s = t * (k == 2 ? rnd.Range(0.26, 0.32) : rnd.Range(0.2, 0.24));
        if (cornerType == "stepped")
        {
            foreach (var ln in lines)
            {
                var tl = new List<Pt> { new(ln.d, ln.d + k * s) };
                for (int i = 1; i <= k; i++)
                {
                    tl.Add(new Pt(ln.d + i * s, ln.d + (k - i + 1) * s));
                    tl.Add(new Pt(ln.d + i * s, ln.d + (k - i) * s));
                }
                o += StrokeSq(PolyD(Frame.CornerRectPts(W, H, tl), true), ln.w);
            }
        }
        else
        {
            foreach (var ln in lines) o += StrokeSq(RectD(ln.d, ln.d, W - 2 * ln.d, H - 2 * ln.d), ln.w);
        }

        // ---- a row of dashes riding in the gap before the inner rules ----
        if (rnd.Chance(0.55))
        {
            int medIdx = lines.FindIndex(ln => ln.w == med);
            double band = Lerp(lines[medIdx - 1].d, lines[medIdx].d, 0.5);
            double dashLen = t * 0.16, gap = t * 0.1, hgt = Math.Max(2 * u, t * 0.05);
            o += frame.Edges(L =>
            {
                string g = "";
                double start = L * 0.5 + t * 1.25, end = L - (cornerType == "stepped" ? k * s + dMax : t * 1.7) - t * 0.2;
                double n = Math.Floor((end - start) / (dashLen + gap));
                for (int i = 0; i < n; i++)
                {
                    double x = start + i * (dashLen + gap);
                    g += Fill(RectD(x, band - hgt / 2, dashLen, hgt));
                    g += Fill(RectD(L - x - dashLen, band - hgt / 2, dashLen, hgt));
                }
                return g;
            });
        }

        // ---- corners ----
        string corner = "";
        if (cornerType == "stepped")
        {
            // Fan of rays filling the cut-away triangle outside the stairs.
            double oo = lines[0].d, reach = k * s;
            int rays = rnd.Pick(5, 7, 9);
            string dd = "";
            for (int i = 0; i < rays; i++)
            {
                double a = (Math.PI / 2) * ((double)i / (rays - 1));
                double r1 = (reach * 0.86) / (Math.Cos(a) + Math.Sin(a));
                Pt p0 = Polar(oo, oo, reach * 0.2, a), p1 = Polar(oo, oo, r1, a);
                dd += $"M{F(p0.X)} {F(p0.Y)}L{F(p1.X)} {F(p1.Y)}";
            }
            corner += StrokeSq(dd, rays >= 9 ? thin : med);
            double r0 = reach * 0.24;
            corner += Fill($"M{F(oo - bold / 2)} {F(oo - bold / 2)}H{F(oo + r0)}A{F(r0)} {F(r0)} 0 0 1 {F(oo - bold / 2)} {F(oo + r0)}Z");
        }
        else if (cornerType == "fan")
        {
            double oo = m, R = t * rnd.Range(1.3, 1.5);
            string Sector(double r0, double r1, double a0, double a1)
            {
                Pt a = Polar(oo, oo, r0, a0), b = Polar(oo, oo, r1, a0);
                Pt c = Polar(oo, oo, r1, a1), e = Polar(oo, oo, r0, a1);
                return $"M{F(a.X)} {F(a.Y)}L{F(b.X)} {F(b.Y)}A{F(r1)} {F(r1)} 0 0 1 {F(c.X)} {F(c.Y)}L{F(e.X)} {F(e.Y)}A{F(r0)} {F(r0)} 0 0 0 {F(a.X)} {F(a.Y)}Z";
            }
            corner += OutlinedSq($"M{F(oo)} {F(oo)}H{F(oo + R)}A{F(R)} {F(R)} 0 0 1 {F(oo)} {F(oo + R)}Z", bold);
            int n = rnd.Pick(6, 8, 10);
            if (rnd.Chance(0.5))
            {
                for (int i = 0; i < n; i += 2)
                    corner += Fill(Sector(R * 0.34, R * 0.84, (Math.PI / 2) * ((double)i / n), (Math.PI / 2) * ((i + 1.0) / n)));
            }
            else
            {
                string dd = "";
                for (int i = 1; i < n; i++)
                {
                    double a = (Math.PI / 2) * ((double)i / n);
                    Pt p0 = Polar(oo, oo, R * 0.34, a), p1 = Polar(oo, oo, R * 0.84, a);
                    dd += $"M{F(p0.X)} {F(p0.Y)}L{F(p1.X)} {F(p1.Y)}";
                }
                corner += StrokeSq(dd, med);
            }
            corner += Fill($"M{F(oo)} {F(oo)}H{F(oo + R * 0.26)}A{F(R * 0.26)} {F(R * 0.26)} 0 0 1 {F(oo)} {F(oo + R * 0.26)}Z");
            foreach (double rr in new[] { 0.34, 0.84, 0.91 })
            {
                Pt p0 = Polar(oo, oo, R * rr, 0), p1 = Polar(oo, oo, R * rr, Math.PI / 2);
                corner += StrokeSq($"M{F(p0.X)} {F(p0.Y)}A{F(R * rr)} {F(R * rr)} 0 0 1 {F(p1.X)} {F(p1.Y)}", rr == 0.84 ? med : thin);
            }
        }
        else
        {
            // Nested corner blocks.
            double q = t * rnd.Range(0.98, 1.12), oo = m;
            corner += OutlinedSq(RectD(oo + bold / 2, oo + bold / 2, q, q), bold);
            corner += StrokeSq(RectD(oo + q * 0.14, oo + q * 0.14, q * 0.78, q * 0.78), thin);
            double b0 = oo + q * 0.27, bs = q * 0.52;
            corner += Fill(RectD(b0, b0, bs, bs));
            double cx = b0 + bs / 2, r = bs * 0.33;
            if (rnd.Chance(0.5)) corner += Fill($"M{F(cx)} {F(cx - r)}L{F(cx + r)} {F(cx)}L{F(cx)} {F(cx + r)}L{F(cx - r)} {F(cx)}Z", PAPER);
            else corner += Fill(RectD(cx - r * 0.75, cx - r * 0.75, r * 1.5, r * 1.5), PAPER) + Fill(RectD(cx - r * 0.3, cx - r * 0.3, r * 0.6, r * 0.6));
            // small satellite squares stepping toward the edges
            double ss = q * 0.2;
            corner += OutlinedSq(RectD(oo + q + ss * 0.5, oo + bold / 2, ss, ss), med);
            corner += OutlinedSq(RectD(oo + bold / 2, oo + q + ss * 0.5, ss, ss), med);
        }
        o += frame.Corners(corner);

        // ---- edge centre ornaments ----
        o += frame.Edges(L =>
        {
            double cx = L / 2, oo = lines[0].d;
            string g = "";
            if (centreType == "sunburst")
            {
                double R = t * 0.92;
                g += OutlinedSq($"M{F(cx - R)} {F(oo)}A{F(R)} {F(R)} 0 0 0 {F(cx + R)} {F(oo)}Z", bold);
                int n = 12;
                for (int i = 0; i < n; i += 2)
                {
                    double a0 = Math.PI * ((double)i / n) + Math.PI / n / 2, a1 = a0 + Math.PI / n;
                    Pt a = Polar(cx, oo, R * 0.36, a0), b = Polar(cx, oo, R * 0.86, a0);
                    Pt e = Polar(cx, oo, R * 0.86, a1), gg = Polar(cx, oo, R * 0.36, a1);
                    g += Fill($"M{F(a.X)} {F(a.Y)}L{F(b.X)} {F(b.Y)}A{F(R * 0.86)} {F(R * 0.86)} 0 0 1 {F(e.X)} {F(e.Y)}L{F(gg.X)} {F(gg.Y)}A{F(R * 0.36)} {F(R * 0.36)} 0 0 0 {F(a.X)} {F(a.Y)}Z");
                }
                double r0 = R * 0.27;
                g += Fill($"M{F(cx - r0)} {F(oo)}A{F(r0)} {F(r0)} 0 0 0 {F(cx + r0)} {F(oo)}Z");
                g += StrokeSq($"M{F(cx - R * 0.86)} {F(oo)}A{F(R * 0.86)} {F(R * 0.86)} 0 0 0 {F(cx + R * 0.86)} {F(oo)}", thin);
                // flanking wings
                foreach (int sx in new[] { -1, 1 })
                {
                    for (int j = 0; j < 3; j++)
                    {
                        double x0 = cx + sx * (R + t * 0.1 + j * t * 0.13), hh = t * (0.62 - j * 0.14);
                        g += Fill(RectD(Math.Min(x0, x0 + sx * t * 0.07), oo, t * 0.07, hh));
                    }
                }
            }
            else if (centreType == "keystone")
            {
                int levels = 4;
                double w0 = t * 1.9, hh = (t * 1.12 - oo) / levels;
                var pts = new List<Pt>();
                for (int j = 0; j < levels; j++)
                {
                    double hw = (w0 / 2) * (1 - j * 0.23);
                    pts.Add(new Pt(cx - hw, oo + j * hh));
                    pts.Add(new Pt(cx - hw, oo + (j + 1) * hh));
                }
                for (int j = levels - 1; j >= 0; j--)
                {
                    double hw = (w0 / 2) * (1 - j * 0.23);
                    pts.Add(new Pt(cx + hw, oo + (j + 1) * hh));
                    pts.Add(new Pt(cx + hw, oo + j * hh));
                }
                g += OutlinedSq(PolyD(pts, true), bold);
                double topHw = (w0 / 2) * (1 - (levels - 1) * 0.23);
                string flutes = "";
                foreach (double fx in new[] { -0.55, 0, 0.55 }) flutes += $"M{F(cx + fx * topHw)} {F(oo + hh * 0.5)}V{F(oo + levels * hh - hh * 0.45)}";
                g += StrokeSq(flutes, med);
                for (int j = 1; j < levels - 1; j++)
                {
                    double hw = (w0 / 2) * (1 - j * 0.23), nx = (w0 / 2) * (1 - (j + 1) * 0.23);
                    double xm = (hw + nx) / 2;
                    g += StrokeSq($"M{F(cx - xm)} {F(oo + hh * 0.5)}V{F(oo + (j + 1) * hh - hh * 0.4)}M{F(cx + xm)} {F(oo + hh * 0.5)}V{F(oo + (j + 1) * hh - hh * 0.4)}", thin);
                }
            }
            else
            {
                double hw = t * 1.05, dep = t * 1.02;
                g += OutlinedSq($"M{F(cx - hw)} {F(oo)}L{F(cx)} {F(oo + dep)}L{F(cx + hw)} {F(oo)}Z", bold);
                for (int j = 1; j <= 3; j++)
                {
                    double q = 1 - j * 0.24;
                    if (j == 3) g += Fill($"M{F(cx - hw * q)} {F(oo)}L{F(cx)} {F(oo + dep * q)}L{F(cx + hw * q)} {F(oo)}Z");
                    else g += StrokeSq($"M{F(cx - hw * q)} {F(oo)}L{F(cx)} {F(oo + dep * q)}L{F(cx + hw * q)} {F(oo)}", j == 1 ? thin : med);
                }
                foreach (int sx in new[] { -1, 1 })
                {
                    for (int j = 0; j < 2; j++)
                    {
                        double x0 = cx + sx * (hw + t * (0.3 + j * 0.46)), q = 0.2 - j * 0.07;
                        g += Fill($"M{F(x0 - t * q)} {F(oo)}L{F(x0)} {F(oo + t * q)}L{F(x0 + t * q)} {F(oo)}Z");
                    }
                }
            }
            return g;
        });

        return o;
    }
}
