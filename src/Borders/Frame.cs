// Frame layout: symmetry helpers and a "perimeter track" coordinate system.
using static Borders.Geom;

namespace Borders;

/// <summary>
/// Layout context handed to every style.
///   W, H  canvas size        T  border band thickness        U  line-weight unit (about 1px at T=64)
/// </summary>
public sealed class Frame
{
    public double W { get; }
    public double H { get; }
    public double T { get; }
    public double U { get; }
    public Rng Rnd { get; }

    /// <param name="thickness">Band thickness as a fraction of the shorter side (null or 0 = 13%).</param>
    /// <param name="bandScale">Lets the simpler styles use a narrower band while keeping the same line weights.</param>
    public Frame(int width, int height, string seed, double? thickness = null, double bandScale = 1)
    {
        W = width;
        H = height;
        double th = thickness is double v && v != 0 && !double.IsNaN(v) ? v : 0.13;
        double full = Clamp(Math.Min(W, H) * th, 16, Math.Min(W, H) * 0.3);
        T = Round(Math.Max(12, full * bandScale));
        U = Math.Max(0.6, Round(full) / 64);
        Rnd = new Rng(seed);
    }

    private static string G(string transform, string inner) =>
        inner.Length != 0 ? $"<g transform=\"{transform}\">{inner}</g>" : "";

    /// <summary>Draw inner (authored for the top-left corner, origin at the outer corner) in all four corners.</summary>
    public string Corners(string inner) =>
        inner +
        G($"translate({F(W)} 0) scale(-1 1)", inner) +
        G($"translate(0 {F(H)}) scale(1 -1)", inner) +
        G($"translate({F(W)} {F(H)}) scale(-1 -1)", inner);

    /// <summary>
    /// Draw an edge motif on all four sides. fn(L) returns SVG for a run of length L with local x
    /// along the edge (0..L) and local y from the outer edge (0) inward. c is how much each corner reserves.
    /// </summary>
    public string Edges(Func<double, string> fn, double c = 0)
    {
        string h = fn(W - 2 * c), v = fn(H - 2 * c);
        return
            G($"translate({F(c)} 0)", h) +
            G($"translate({F(c)} {F(H)}) scale(1 -1)", h) +
            G($"matrix(0 1 1 0 0 {F(c)})", v) +
            G($"matrix(0 1 -1 0 {F(W)} {F(c)})", v);
    }

    /// <summary>Mirror the top-left quadrant into all four.</summary>
    public string Quad(string inner) => Corners(inner);

    /// <summary>Mirror the left half onto the right.</summary>
    public string Lr(string inner) => inner + G($"translate({F(W)} 0) scale(-1 1)", inner);

    /// <summary>inner covers local x in [0, L/2]; returns it plus its mirror about L/2.</summary>
    public static string MirrorX(string inner, double L) => inner + G($"translate({F(L)} 0) scale(-1 1)", inner);

    /// <summary>Mirror across the x=y diagonal (for corner motifs).</summary>
    public static string MirrorDiag(string inner) => inner + G("matrix(0 1 1 0 0 0)", inner);

    /// <summary>
    /// Closed rectangle outline whose four corners all use the same corner polyline.
    /// tl is the top-left corner's points, ordered from the left side to the top side.
    /// </summary>
    public static List<Pt> CornerRectPts(double W, double H, IReadOnlyList<Pt> tl)
    {
        var o = new List<Pt>(tl.Count * 4);
        o.AddRange(tl);
        for (int i = tl.Count - 1; i >= 0; i--) o.Add(new Pt(W - tl[i].X, tl[i].Y));
        for (int i = 0; i < tl.Count; i++) o.Add(new Pt(W - tl[i].X, H - tl[i].Y));
        for (int i = tl.Count - 1; i >= 0; i--) o.Add(new Pt(tl[i].X, H - tl[i].Y));
        return o;
    }
}

public readonly record struct TrackPt(double X, double Y, double A, double Nx, double Ny);

/// <summary>Arc-length positions of the four corner midpoints and the four edge centres of a track.</summary>
public readonly record struct TrackMarks(
    double Bottom, double BottomLeft, double Left, double TopLeft, double Top, double TopRight, double Right, double BottomRight);

/// <summary>
/// Perimeter track: a rounded rectangle (inset b, corner radius r) parameterised by arc length.
/// s = 0 at the bottom centre, running left, up the left side, across the top, down the right side.
/// </summary>
public sealed class Track
{
    private readonly record struct Seg(bool IsLine, double X0, double Y0, double X1, double Y1, double Cx, double Cy, double A0, double Len);

    private readonly List<Seg> _segs = new();
    private readonly double _r;

    public double Len { get; }
    public TrackMarks Marks { get; }

    public Track(double W, double H, double b, double r)
    {
        r = Math.Max(0.01, Math.Min(r, (Math.Min(W, H) - 2 * b) / 2 - 0.01));
        _r = r;
        void Line(double ax, double ay, double bx, double by) =>
            _segs.Add(new Seg(true, ax, ay, bx, by, 0, 0, 0, Hypot(bx - ax, by - ay)));
        void Arc(double cx, double cy, double a0) =>
            _segs.Add(new Seg(false, 0, 0, 0, 0, cx, cy, a0, (r * Math.PI) / 2));

        double x0 = b, y0 = b, x1 = W - b, y1 = H - b;
        Line(W / 2, y1, x0 + r, y1);
        Arc(x0 + r, y1 - r, Math.PI / 2);
        Line(x0, y1 - r, x0, y0 + r);
        Arc(x0 + r, y0 + r, Math.PI);
        Line(x0 + r, y0, x1 - r, y0);
        Arc(x1 - r, y0 + r, Math.PI * 1.5);
        Line(x1, y0 + r, x1, y1 - r);
        Arc(x1 - r, y1 - r, 0);
        Line(x1 - r, y1, W / 2, y1);
        double len = 0;
        foreach (var q in _segs) len += q.Len;
        Len = len;

        double hx = W / 2 - x0 - r, vy = y1 - y0 - 2 * r, qa = (r * Math.PI) / 2;
        Marks = new TrackMarks(
            Bottom: 0,
            BottomLeft: hx + qa / 2,
            Left: hx + qa + vy / 2,
            TopLeft: hx + qa + vy + qa / 2,
            Top: hx + qa + vy + qa + hx,
            TopRight: hx + qa + vy + qa + 2 * hx + qa / 2,
            Right: hx + 2 * qa + vy + 2 * hx + qa + vy / 2,
            BottomRight: len - hx - qa / 2);
    }

    /// <summary>The point off pixels inward of the base path at arc length s, plus the heading and inward normal there.</summary>
    public TrackPt At(double s, double off = 0)
    {
        s = ((s % Len) + Len) % Len;
        Seg q = _segs[^1];
        foreach (var cand in _segs)
        {
            if (s <= cand.Len) { q = cand; break; }
            s -= cand.Len;
        }
        double x, y, a;
        if (q.IsLine)
        {
            double tt = q.Len != 0 ? s / q.Len : 0;
            x = q.X0 + (q.X1 - q.X0) * tt;
            y = q.Y0 + (q.Y1 - q.Y0) * tt;
            a = Math.Atan2(q.Y1 - q.Y0, q.X1 - q.X0);
        }
        else
        {
            double ang = q.A0 + s / _r;
            x = q.Cx + _r * Math.Cos(ang);
            y = q.Cy + _r * Math.Sin(ang);
            a = ang + Math.PI / 2;
        }
        double nx = -Math.Sin(a), ny = Math.Cos(a);
        return new TrackPt(x + nx * off, y + ny * off, a, nx, ny);
    }

    /// <summary>Polyline following the track from s0 to s1 at offset offFn(s).</summary>
    public List<Pt> Run(double s0, double s1, Func<double, double> offFn, double step = 2)
    {
        int n = (int)Math.Max(2, Math.Ceiling(Math.Abs(s1 - s0) / step));
        var pts = new List<Pt>(n + 1);
        for (int i = 0; i <= n; i++)
        {
            double s = s0 + ((s1 - s0) * i) / n;
            var p = At(s, offFn(s));
            pts.Add(new Pt(p.X, p.Y));
        }
        return pts;
    }

    public List<Pt> Run(double s0, double s1, double off, double step = 2) => Run(s0, s1, _ => off, step);
}
