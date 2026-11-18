// Victorian Flourishes: tapered scrollwork with ball terminals, teardrop buds, corner cartouches
// and a running foliate scroll (rinceau) along the longer edges.
using static Borders.Geom;
using static Borders.Ornament;

namespace Borders.Styles;

public static class Victorian
{
    public static string Draw(Frame frame)
    {
        double W = frame.W, H = frame.H, t = frame.T, u = frame.U;
        var rnd = frame.Rnd;
        double wMain = 4.8 * u, wSub = 3.4 * u, hair = 0.9 * u;
        double ball = wMain * 0.66;
        double turnsMain = rnd.Range(1.45, 1.75);
        double sizeMain = t * rnd.Range(0.46, 0.54);
        double leadMain = t * rnd.Range(0.85, 1.15);
        int drops = rnd.Int(3, 4);
        bool nested = rnd.Chance(0.7);
        string centreKind = rnd.Pick("scrolls", "lyre");
        double y0 = t * 0.27;
        const double SpOpen = 1, SpTight = 5.6; // how quickly the spirals wind up

        string Drop(double x, double y, double a, double len, double wid) => Place(x, y, a, Fill(DropD(len, wid)));

        // ---- one half of a corner piece (the half that runs along the top edge) ----
        string Half()
        {
            string g = "";
            var main = CurlPts(x: t * 0.3, y: y0, h: Rad(-8), lead: leadMain, bend: Rad(18), size: sizeMain, dir: 1, turns: turnsMain, open: SpOpen, tight: SpTight);
            // buds riding the outside of the lead-in
            for (int i = 0; i < drops; i++)
            {
                var p = PointAt(main, 0.05 + i * 0.062);
                double len = t * (0.23 - i * 0.035);
                g += Drop(p.X, p.Y, p.A - Rad(50), len, len * 0.3);
            }
            if (nested)
            {
                // a smaller scroll tucked under the lead-in, curling back up toward it
                var p = PointAt(main, 0.075);
                var sub = CurlPts(x: p.X, y: p.Y, h: p.A + Rad(50), lead: t * 0.2, bend: Rad(-8), size: t * 0.27, dir: -1, turns: 1.3, open: SpOpen, tight: SpTight);
                g += Flourish(sub, wSub, ball: wSub * 0.62);
            }
            else
            {
                var p = PointAt(main, 0.1);
                g += Drop(p.X, p.Y, p.A + Rad(58), t * 0.36, t * 0.1) + Drop(p.X + t * 0.2, p.Y + t * 0.02, p.A + Rad(50), t * 0.27, t * 0.075);
            }
            // an answering curl leaving the far side of the big spiral, running on toward the edge centre
            var q = PointAt(main, 0.36);
            var tail = CurlPts(x: q.X, y: q.Y, h: q.A - Rad(14), lead: t * 0.42, bend: Rad(12), size: t * 0.085, dir: -1, turns: 1.05, open: SpOpen, tight: SpTight);
            g += Flourish(tail, wSub * 0.85, ball: wSub * 0.55);
            g += Flourish(main, wMain, ball: ball, peak: 0.2, min: 0.8 * u);
            return g;
        }

        string corner = Frame.MirrorDiag(Half());
        // spear leaf and beads down the diagonal
        double spear = t * rnd.Range(0.7, 0.9);
        corner += Place(t * 0.33, t * 0.33, Rad(45), Fill(DropD(spear, spear * 0.21)) + $"<path d=\"M{F(spear * 0.24)} 0L{F(spear * 0.8)} 0\" stroke=\"{PAPER}\" stroke-width=\"{F(hair * 1.2)}\" stroke-linecap=\"round\" fill=\"none\"/>");
        double e = t * 0.33 + spear / Math.Sqrt(2);
        corner += Dot(e + t * 0.09, e + t * 0.09, t * 0.05) + Dot(e + t * 0.19, e + t * 0.19, t * 0.032);
        corner += Dot(t * 0.3, t * 0.3, t * 0.1) + Dot(t * 0.3, t * 0.3, t * 0.042, PAPER);

        string o = "";
        double fr = t * 0.055;
        o += StrokeSq(RectD(fr, fr, W - 2 * fr, H - 2 * fr), 1.6 * u);
        o += frame.Corners(corner);

        // ---- running scroll: a wavy stem with a spiral tucked into every hollow ----
        double amp = t * 0.17, lambda = t * rnd.Range(1.5, 1.8), ycR = t * 0.47;
        string Rinceau(double len)
        {
            double n = Math.Max(1, Round(len / (lambda / 2)));
            double lam = (len / n) * 2;
            double YAt(double x) => -amp * Math.Sin((TAU * x) / lam);
            var stem = new List<Pt>();
            for (double x = 0; x <= len; x += 2) stem.Add(new Pt(x, YAt(x)));
            stem.Add(new Pt(len, YAt(len)));
            string g = "";
            for (int i = 0; i < n; i++)
            {
                double x = i * (lam / 2);
                bool up = i % 2 == 0;
                double h = Math.Atan2(YAt(x + 1) - YAt(x - 1), 2);
                var sp = CurlPts(x: x, y: YAt(x), h: h, lead: lam * 0.06, bend: 0, size: Math.Min(amp * 1.75, lam * 0.26), dir: up ? 1 : -1, turns: 1.45, open: 1.15, tight: 5.2);
                g += Flourish(sp, wSub * 0.95, ball: wSub * 0.55, peak: 0.12, min: 0.7 * u);
                // leaflet on the outside of each crest
                double cx = x + lam / 4, cy = YAt(cx);
                g += Drop(cx - lam * 0.04, cy + (up ? -1 : 1) * t * 0.02, Rad(up ? -62 : 62), t * 0.17, t * 0.05);
            }
            g += Fill(RibbonD(stem, Taper.Both(wSub * 1.05, 0.7 * u)));
            return g;
        }

        // ---- edge centre cartouche + what joins it to the corners ----
        double cornerReach = t * 0.3 + leadMain + sizeMain * 1.05;
        o += frame.Edges(L =>
        {
            double cx = L / 2;
            string g = "", h = "";
            double yc = t * 0.45;
            double reach = t * 1.12;
            if (centreKind == "scrolls")
            {
                var fwd = CurlPts(x: t * 0.12, y: 0, h: Rad(-24), lead: t * 0.45, bend: Rad(32), size: t * 0.36, dir: 1, turns: 1.4, open: SpOpen, tight: SpTight);
                var back = CurlPts(x: t * 0.12, y: 0, h: Rad(-24) + Math.PI, lead: t * 0.02, bend: 0, size: t * 0.13, dir: 1, turns: 0.9, open: SpOpen, tight: SpTight);
                h += Flourish(fwd, wMain * 0.92, ball: ball * 0.85, peak: 0.25) + Flourish(back, wSub * 0.75, ball: wSub * 0.5, peak: 0.1);
                h += Drop(t * 0.3, -t * 0.12, Rad(-58), t * 0.2, t * 0.06) + Drop(t * 0.46, -t * 0.1, Rad(-50), t * 0.15, t * 0.045);
            }
            else
            {
                // lyre: tall scrolls leaning away from the centre with a second pair sweeping outward
                var up = CurlPts(x: t * 0.08, y: t * 0.4, h: Rad(-78), lead: t * 0.34, bend: Rad(26), size: t * 0.27, dir: 1, turns: 1.3, open: SpOpen, tight: SpTight);
                var out2 = CurlPts(x: t * 0.08, y: t * 0.4, h: Rad(-14), lead: t * 0.5, bend: Rad(-16), size: t * 0.22, dir: -1, turns: 1.3, open: SpOpen, tight: SpTight);
                h += Flourish(up, wSub, ball: wSub * 0.62) + Flourish(out2, wMain * 0.9, ball: ball * 0.82);
                h += Drop(t * 0.36, t * 0.42, Rad(40), t * 0.2, t * 0.055);
            }
            g += $"<g transform=\"translate({F(cx)} {F(yc)})\">{h}</g><g transform=\"translate({F(cx)} {F(yc)}) scale(-1 1)\">{h}</g>";
            // centre jewel, pendant drop pointing into the frame, beads
            double dq = t * 0.1;
            g += Fill($"M{F(cx)} {F(yc - dq)}L{F(cx + dq)} {F(yc)}L{F(cx)} {F(yc + dq)}L{F(cx - dq)} {F(yc)}Z");
            g += Drop(cx, yc + dq * 1.5, Rad(90), t * 0.5, t * 0.1);
            g += Dot(cx, yc + dq * 1.5 + t * 0.61, t * 0.04) + Dot(cx, yc + dq * 1.5 + t * 0.72, t * 0.026);
            g += Dot(cx, yc - dq * 2, t * 0.045);

            double x0 = cornerReach + t * 0.1, x1 = cx - reach;
            double room = x1 - x0;
            if (room > lambda * 0.8)
            {
                g += Frame.MirrorX($"<g transform=\"translate({F(x0)} {F(ycR)})\">{Rinceau(room)}</g>", L);
            }
            else if (room > t * 0.25)
            {
                double mid = (x0 + x1) / 2, k = Math.Min(t * 0.09, room * 0.2);
                string jewel = Fill($"M{F(mid)} {F(yc - k)}L{F(mid + k * 1.7)} {F(yc)}L{F(mid)} {F(yc + k)}L{F(mid - k * 1.7)} {F(yc)}Z") + Dot(mid - k * 2.6, yc, k * 0.36) + Dot(mid + k * 2.6, yc, k * 0.36);
                g += Frame.MirrorX(jewel, L);
            }
            return g;
        });

        return o;
    }
}
