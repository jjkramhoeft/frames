// Art Nouveau Borders: stems rise from a bulb at the foot of the frame, climb the sides in parallel
// and droop into blossoms across the top. Mirror-symmetric left/right, deliberately not top/bottom.
using static Borders.Geom;
using static Borders.Ornament;

namespace Borders.Styles;

public static class ArtNouveau
{
    private sealed record Stem(double Lane, double S1, double W, double Head);

    public static string Draw(Frame frame)
    {
        double W = frame.W, H = frame.H, t = frame.T, u = frame.U;
        var rnd = frame.Rnd;
        var tr = new Track(W, H, t * 0.08, t * 0.95);
        double sL = tr.Marks.Left, sC = tr.Marks.TopLeft, sT = tr.Marks.Top;
        string bloom = rnd.Pick("tulip", "tulip", "daisy");
        double lw = 1.5 * u;

        string o = "";
        // outer frame: a bold rounded line with a hairline companion
        o += Stroke(PolyD(tr.Run(0, tr.Len, 0, 3), true), 2.8 * u);
        o += Stroke(PolyD(tr.Run(0, tr.Len, t * 0.085, 3), true), 0.9 * u);

        // ---- stems on the left half ----
        double sideLen = sC - sL;
        var stems = new List<Stem>
        {
            new(t * 0.3, sT - t * rnd.Range(1.1, 1.6), 5.6 * u, t * 0.62),
            new(t * 0.56, sC + t * rnd.Range(0.5, 1.2), 4.6 * u, t * 0.5),
        };
        if (rnd.Chance(0.65)) stems.Add(new Stem(t * 0.78, sL + sideLen * rnd.Range(-0.3, 0.4), 3.6 * u, t * 0.4));

        string half = "", heads = "", leaves = "";
        for (int i = 0; i < stems.Count; i++)
        {
            var st = stems[i];
            double s0 = t * (0.2 + 0.12 * i);
            double lambda = t * rnd.Range(3.5, 6), phase = rnd.Range(0, TAU), amp = t * rnd.Range(0.025, 0.045);
            double droop = t * rnd.Range(0.2, 0.3) * (i == 2 ? 0.6 : 1);
            double Off(double s) =>
                Lerp(t * 0.46, st.Lane, Smoothstep(s0, s0 + t * 1.6, s)) +
                amp * Math.Sin((TAU * s) / lambda + phase) * Smoothstep(s0, s0 + t * 2, s) +
                droop * Math.Pow(Smoothstep(st.S1 - t * 1.0, st.S1, s), 2);
            var pts = tr.Run(s0, st.S1, Off, 2);
            half += Fill(RibbonD(pts, Taper.Out(st.W, st.W * 0.42, 0.8)));

            // long lance leaves peeling away from the stem
            double count = Math.Max(1, Round((st.S1 - s0) / (t * rnd.Range(3, 4))));
            for (int j = 0; j < count; j++)
            {
                double sj = s0 + t * 1.2 + ((st.S1 - s0 - t * 2.6) * (j + rnd.Range(0.2, 0.8))) / count;
                if (sj > st.S1 - t * 1.3) continue;
                int side = i == 0 ? -1 : i == stems.Count - 1 ? 1 : (j % 2 != 0 ? 1 : -1);
                double ll = t * rnd.Range(1.3, 1.9);
                double reach = t * rnd.Range(0.11, 0.16);
                var lpts = tr.Run(sj, sj + ll, s =>
                {
                    double q = (s - sj) / ll;
                    return Math.Max(t * 0.15, Math.Min(t * 0.9, Off(s) + side * reach * (0.75 * Math.Sin(Math.PI * Math.Pow(q, 0.8)) + 0.9 * q)));
                }, 2);
                leaves += Fill(RibbonD(lpts, Taper.Both(t * rnd.Range(0.13, 0.17), 0)));
                int from = (int)Math.Floor(lpts.Count * 0.18), to = (int)Math.Floor(lpts.Count * 0.82);
                leaves += $"<path d=\"{PolyD(lpts.GetRange(from, to - from))}\" fill=\"none\" stroke=\"{PAPER}\" stroke-width=\"{F(1.1 * u)}\" stroke-linecap=\"round\"/>";
            }

            // blossom at the tip, following the stem's final direction
            Pt pa = pts[^4], pb = pts[^1];
            double a = Math.Atan2(pb.Y - pa.Y, pb.X - pa.X);
            if (bloom == "daisy" && i < 2)
            {
                double r = st.Head * 0.56;
                heads += Flower(pb.X + Math.Cos(a) * r * 0.5, pb.Y + Math.Sin(a) * r * 0.5, r, n: 8, rot: a, kind: "daisy", lw: lw, centre: 0.3);
            }
            else
            {
                heads += Bud(pb.X, pb.Y, a, st.Head, lw, i == 0 ? 1 : 0.6);
            }
        }
        half += leaves + heads;
        o += frame.Lr(half);

        // ---- foot: the bulb the stems grow from ----
        double bx = W / 2, by = H - t * 0.1;
        o += Place(bx, by, Rad(-90), Outlined(DropD(t * 0.86, t * 0.3), 2.4 * u) + Fill(DropD(t * 0.6, t * 0.15)));
        foreach (int sx in new[] { -1, 1 })
        {
            o += Place(bx + sx * t * 0.34, by - t * 0.02, Rad(-90 + sx * 24), Outlined(DropD(t * 0.5, t * 0.15), 1.8 * u));
            for (int j = 0; j < 3; j++)
            {
                double q = t * (0.07 - j * 0.012);
                o += Fill($"M{F(bx + sx * t * (0.74 + j * 0.22) - q)} {F(by - t * 0.2 - q)}h{F(q * 2)}v{F(q * 2)}h{F(-q * 2)}Z");
            }
        }

        // ---- crown: pendant between the two leading blossoms ----
        double cy = t * 0.4;
        o += Ring(W / 2, cy, t * 0.13, 2.2 * u, PAPER) + Dot(W / 2, cy, t * 0.05);
        o += Place(W / 2, cy + t * 0.2, Rad(90), Fill(DropD(t * 0.4, t * 0.07)));
        o += Dot(W / 2, cy + t * 0.7, t * 0.035) + Dot(W / 2, cy + t * 0.82, t * 0.024);
        return o;
    }
}
