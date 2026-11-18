// Geometric Line Borders: uniform-weight line patterns (Greek key, zigzag, diamonds, rings, lattice).
using static Borders.Geom;

namespace Borders.Styles;

public static class Geometric
{
    public static string Draw(Frame frame)
    {
        double t = frame.T, u = frame.U;
        var rnd = frame.Rnd;
        double bold = 2.6 * u, med = 1.8 * u, thin = 1.1 * u;
        string pattern = rnd.Pick("meander", "zigzag", "diamonds", "rings", "lattice", "steps");
        string cornerType = rnd.Pick("nested", "diamond", "target", "cross");
        double r0 = t * 0.07;                  // outer rule
        double r1 = t * 0.93;                  // inner rule
        double a = t * rnd.Range(0.2, 0.24);   // pattern band top
        double b = t - a;                      // pattern band bottom
        double hb = b - a;
        bool doubleRule = rnd.Chance(0.6);
        bool filled = rnd.Chance(0.5);

        string o = "";

        // ---- corner block: a square of side t ----
        string c = "";
        double q0 = r0, q1 = r1, qc = t / 2, qs = q1 - q0;
        c += StrokeSq(RectD(q0, q0, qs, qs), bold);
        if (cornerType == "nested")
        {
            c += StrokeSq(RectD(q0 + qs * 0.17, q0 + qs * 0.17, qs * 0.66, qs * 0.66), thin);
            c += Fill(RectD(q0 + qs * 0.33, q0 + qs * 0.33, qs * 0.34, qs * 0.34));
            if (filled) c += Fill(RectD(qc - qs * 0.07, qc - qs * 0.07, qs * 0.14, qs * 0.14), PAPER);
        }
        else if (cornerType == "diamond")
        {
            double r = qs * 0.5;
            string Dia(double k) => $"M{F(qc)} {F(qc - r * k)}L{F(qc + r * k)} {F(qc)}L{F(qc)} {F(qc + r * k)}L{F(qc - r * k)} {F(qc)}Z";
            c += StrokeSq(Dia(1), med);
            c += filled ? Fill(Dia(0.56)) : StrokeSq(Dia(0.58), med) + Fill(Dia(0.2));
        }
        else if (cornerType == "target")
        {
            c += Stroke(CircleD(qc, qc, qs * 0.4), med);
            c += Stroke(CircleD(qc, qc, qs * 0.25), thin);
            c += Fill(CircleD(qc, qc, qs * 0.11));
        }
        else
        {
            c += StrokeSq($"M{F(q0)} {F(q0)}L{F(q1)} {F(q1)}M{F(q1)} {F(q0)}L{F(q0)} {F(q1)}", thin);
            c += Fill(RectD(qc - qs * 0.2, qc - qs * 0.2, qs * 0.4, qs * 0.4), PAPER) + StrokeSq(RectD(qc - qs * 0.2, qc - qs * 0.2, qs * 0.4, qs * 0.4), med);
            c += Fill(RectD(qc - qs * 0.08, qc - qs * 0.08, qs * 0.16, qs * 0.16));
        }
        o += frame.Corners(c);

        // ---- edges ----
        o += frame.Edges(L =>
        {
            string g = "";
            double ov = t - r1; // reach back to the corner block
            g += StrokeSq($"M{F(-ov)} {F(r0)}H{F(L + ov)}", bold) + StrokeSq($"M{F(-ov)} {F(r1)}H{F(L + ov)}", bold);
            if (doubleRule) g += StrokeSq($"M0 {F(r0 + t * 0.07)}H{F(L)}M0 {F(r1 - t * 0.07)}H{F(L)}", thin);

            if (pattern == "meander")
            {
                double p0 = (hb * 4) / 5, n = Math.Max(1, Round(L / 2 / p0)), p = L / 2 / n;
                double gx = p / 4, gy = hb / 5;
                var hook = new (double x, double y)[] { (0.5, 5), (0.5, 1), (3.5, 1), (3.5, 4), (1.5, 4), (1.5, 2), (2.5, 2), (2.5, 3) };
                string d1 = "";
                for (int i = 0; i < n; i++)
                {
                    d1 += PolyD(hook.Select(h => new Pt(i * p + h.x * gx, a + h.y * gy)).ToList());
                    d1 += PolyD(hook.Select(h => new Pt(L - (i * p + h.x * gx), a + h.y * gy)).ToList());
                }
                g += StrokeSq(d1, med) + StrokeSq($"M0 {F(a)}H{F(L)}M0 {F(b)}H{F(L)}", med);
            }
            else if (pattern == "zigzag")
            {
                double n = Math.Max(2, Round(L / (hb * 1.15) / 2) * 2), p = L / n;
                if (filled)
                {
                    string d1 = "";
                    for (int i = 0; i < n; i += 2) d1 += $"M{F(i * p)} {F(b)}L{F((i + 1) * p)} {F(a)}L{F((i + 2) * p)} {F(b)}Z";
                    g += Fill(d1);
                    g += StrokeSq($"M0 {F(a)}H{F(L)}M0 {F(b)}H{F(L)}", thin);
                }
                else
                {
                    double gap = hb * 0.26;
                    var upper = new List<Pt>();
                    var lower = new List<Pt>();
                    for (int i = 0; i <= n; i++)
                    {
                        bool low = i % 2 == 0; // even points sit on the band bottom
                        upper.Add(new Pt(i * p, low ? b : a + gap));
                        lower.Add(new Pt(i * p, low ? b - gap : a));
                    }
                    g += StrokeSq(PolyD(upper), med);
                    g += StrokeSq(PolyD(lower), med);
                }
            }
            else if (pattern == "diamonds")
            {
                double n = Math.Max(1, Round(L / (hb * 1.5))), p = L / n, mid = (a + b) / 2;
                string d1 = "", d2 = "";
                for (int i = 0; i < n; i++)
                {
                    double cx = (i + 0.5) * p;
                    d1 += $"M{F(cx - p / 2)} {F(mid)}L{F(cx)} {F(a)}L{F(cx + p / 2)} {F(mid)}L{F(cx)} {F(b)}Z";
                    double k = 0.42;
                    d2 += $"M{F(cx - (p / 2) * k)} {F(mid)}L{F(cx)} {F(mid - (hb / 2) * k)}L{F(cx + (p / 2) * k)} {F(mid)}L{F(cx)} {F(mid + (hb / 2) * k)}Z";
                }
                g += StrokeSq(d1, med);
                g += filled ? Fill(d2) : StrokeSq(d2, thin);
                string dots = "";
                for (int i = 0; i <= n; i++)
                {
                    if (i == 0 || i == n) continue;
                    dots += CircleD(i * p, a + hb * 0.08, hb * 0.055) + CircleD(i * p, b - hb * 0.08, hb * 0.055);
                }
                g += Fill(dots);
            }
            else if (pattern == "rings")
            {
                double R = hb / 2, n = Math.Max(1, Round((L - 2 * R) / (R * 1.5))), p = (L - 2 * R) / n, mid = (a + b) / 2;
                string d1 = "", d2 = "";
                for (int i = 0; i <= n; i++)
                {
                    d1 += CircleD(R + i * p, mid, R);
                    if (i < n) d2 += CircleD(R + (i + 0.5) * p, mid, R * 0.16);
                }
                g += Stroke(d1, med);
                g += filled ? Fill(d2) : Stroke(d2, thin);
            }
            else if (pattern == "lattice")
            {
                double n = Math.Max(1, Round(L / hb)), p = L / n;
                string d1 = "", d2 = "";
                for (int i = 0; i < n; i++)
                {
                    d1 += $"M{F(i * p)} {F(a)}L{F((i + 1) * p)} {F(b)}M{F((i + 1) * p)} {F(a)}L{F(i * p)} {F(b)}";
                    if (i > 0) d2 += $"M{F(i * p)} {F(a)}V{F(b)}";
                }
                g += StrokeSq(d1, med) + StrokeSq(d2, thin) + StrokeSq($"M0 {F(a)}H{F(L)}M0 {F(b)}H{F(L)}", med);
                if (filled)
                {
                    string d3 = "";
                    double k = 0.2;
                    for (int i = 0; i < n; i++)
                    {
                        double cx = (i + 0.5) * p, mid = (a + b) / 2;
                        d3 += $"M{F(cx - p * k)} {F(mid)}L{F(cx)} {F(mid - hb * k)}L{F(cx + p * k)} {F(mid)}L{F(cx)} {F(mid + hb * k)}Z";
                    }
                    g += Fill(d3);
                }
            }
            else
            {
                // crenellated square wave with inset blocks
                double n = Math.Max(2, Round(L / (hb * 0.9) / 2) * 2), p = L / n;
                var pts = new List<Pt> { new(0, b) };
                for (int i = 0; i < n; i++)
                {
                    double y = i % 2 != 0 ? b : a;
                    pts.Add(new Pt(i * p, y));
                    pts.Add(new Pt((i + 1) * p, y));
                }
                pts.Add(new Pt(L, b));
                g += StrokeSq(PolyD(pts), med);
                string d3 = "";
                double k = 0.3;
                for (int i = 0; i < n; i++)
                {
                    double cx = (i + 0.5) * p, cy = i % 2 != 0 ? a + hb * 0.36 : b - hb * 0.36;
                    d3 += RectD(cx - p * k, cy - hb * 0.14, p * k * 2, hb * 0.28);
                }
                g += filled ? Fill(d3) : StrokeSq(d3, thin);
            }
            return g;
        }, t);

        return o;
    }
}
