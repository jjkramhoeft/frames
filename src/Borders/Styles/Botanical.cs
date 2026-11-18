// Botanical Floral Frames: wavy vines with alternating leaves, berries and blossoms; flower clusters in the corners.
using static Borders.Geom;
using static Borders.Ornament;

namespace Borders.Styles;

public static class Botanical
{
    public static string Draw(Frame frame)
    {
        double W = frame.W, H = frame.H, t = frame.T, u = frame.U;
        var rnd = frame.Rnd;
        double lw = 1.25 * u;
        string leafKind = rnd.Pick("solid", "outline", "veined");
        string flowerKind = rnd.Pick("round", "daisy", "star");
        int petals = flowerKind == "daisy" ? rnd.Int(9, 12) : rnd.Int(5, 6);
        double belly = rnd.Range(0.32, 0.48);
        double slim = rnd.Range(0.24, 0.36);
        double amp = t * rnd.Range(0.07, 0.13);
        double lambda = t * rnd.Range(1.7, 2.5);
        double spacing = t * rnd.Range(0.36, 0.45);
        bool withBerries = rnd.Chance(0.65);
        double cf = t * 0.64, cr = t * 0.43;
        double start = cf + cr * 0.55;

        // One vine growing from a corner cluster toward the middle of an edge. Local x along the edge, y=0 on the band centre.
        string Vine(double len)
        {
            double phase = rnd.Range(0, TAU);
            double YAt(double x) => amp * Math.Sin((TAU * x) / lambda + phase) * Smoothstep(0, t * 0.5, x) * (1 - 0.5 * Smoothstep(len - t * 0.5, len, x));
            double AngAt(double x) => Math.Atan2(YAt(x + 1) - YAt(x - 1), 2);
            var pts = new List<Pt>();
            for (double x = 0; x <= len; x += 2) pts.Add(new Pt(x, YAt(x)));
            string stems = Fill(RibbonD(pts, Taper.Out(3.6 * u, 1.3 * u, 0.8)));
            string leaves = "";
            int side = rnd.Sign();
            for (double x = t * 0.5; x < len - t * 0.12; x += spacing * rnd.Range(0.9, 1.1))
            {
                double y = YAt(x), a = AngAt(x), q = x / len;
                double spread = Rad(rnd.Range(46, 62));
                double roll = rnd.Next();
                if (withBerries && roll < 0.18)
                {
                    leaves += Berries(x, y, a + side * Rad(62), t * 0.25, t * 0.055, lw);
                }
                else if (roll < 0.3 && q > 0.15 && q < 0.85)
                {
                    double sl = t * 0.22, fa = a + side * Rad(70);
                    double fx = x + Math.Cos(fa) * sl, fy = y + Math.Sin(fa) * sl;
                    stems += Stroke($"M{F(x)} {F(y)}Q{F(x + Math.Cos(a) * sl * 0.5)} {F(y + side * sl * 0.4)} {F(fx)} {F(fy)}", lw);
                    leaves += flowerKind == "star"
                        ? Bud(fx, fy, fa, t * 0.26, lw * 0.9, 0.8)
                        : Flower(fx, fy, t * 0.165, n: petals > 6 ? 8 : 5, rot: rnd.Range(0, TAU), kind: flowerKind, lw: lw * 0.85, centre: 0.3);
                }
                else
                {
                    double ll = t * Lerp(0.54, 0.36, q) * rnd.Range(0.9, 1.08);
                    double room = t * 0.49 - side * y;
                    ll = Math.Min(ll, room / Math.Sin(spread));
                    leaves += Leaf(x, y, a + side * spread, ll, ll * slim, leafKind, lw, belly);
                }
                side = -side;
            }
            // terminal leaf
            double ex = len, ey = YAt(len);
            leaves += Leaf(ex, ey, AngAt(len), t * 0.28, t * 0.28 * slim, leafKind, lw, belly);
            return stems + leaves;
        }

        string quad = "";
        double lh = W / 2 - start - t * 0.3, lv = H / 2 - start - t * 0.3;
        quad += $"<g transform=\"translate({F(start)} {F(t * 0.5)})\">{Vine(lh)}</g>";
        quad += $"<g transform=\"matrix(0 1 1 0 {F(t * 0.5)} {F(start)})\">{Vine(lv)}</g>";

        // corner cluster: leaves fanning toward the outer corner, a big blossom on top
        double big = t * 0.62;
        int[] fan = rnd.Pick(new[] { 225 }, new[] { 205, 245 }, new[] { 190, 225, 260 });
        foreach (int d in new[] { 150, 300 }.Concat(fan))
        {
            double ll = d == 225 ? big * 1.02 : d == 150 || d == 300 ? big * 0.78 : big * 0.92;
            quad += Leaf(cf, cf, Rad(d), ll, ll * slim * 1.1, leafKind, lw, belly);
        }
        quad += Flower(cf, cf, cr, n: petals, rot: rnd.Range(0, TAU), kind: flowerKind, lw: lw);

        string o = "";
        if (rnd.Chance(0.5)) o += StrokeSq(RectD(t, t, W - 2 * t, H - 2 * t), 0.9 * u);
        o += frame.Quad(quad);

        // blossoms tying the vines together at the middle of each edge
        double mr = t * 0.3;
        o += Flower(W / 2, t * 0.5, mr, n: petals, kind: flowerKind, lw: lw, rot: -Math.PI / 2);
        o += Flower(W / 2, H - t * 0.5, mr, n: petals, kind: flowerKind, lw: lw, rot: Math.PI / 2);
        o += Flower(t * 0.5, H / 2, mr, n: petals, kind: flowerKind, lw: lw, rot: Math.PI);
        o += Flower(W - t * 0.5, H / 2, mr, n: petals, kind: flowerKind, lw: lw, rot: 0);
        return o;
    }
}
