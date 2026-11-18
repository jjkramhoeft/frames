// Contract tests for the generator: every style, at every test size, must produce a PNG of the
// requested size whose middle is transparent and whose pixels are strictly black/white/grey.
// The last tests pin reference values so a change that would alter what existing seeds draw is caught.
using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace Borders.Tests;

public class FrameTests
{
    private static readonly (int W, int H)[] Sizes = [(512, 512), (800, 600), (540, 960)];

    private static readonly string[] Expected =
    [
        "Art Deco Line Borders", "Botanical Floral Frames", "Art Nouveau Whiplash Borders", "Art Nouveau Borders",
        "Victorian Flourishes", "Geometric Line Borders", "Engraved Floral Frames", "Organic Borders", "Calligraphic Flourishes",
        "Modern Borders", "Classic Borders",
    ];

    public static TheoryData<string> StyleIds()
    {
        var data = new TheoryData<string>();
        foreach (var s in BorderGenerator.Styles) data.Add(s.Id);
        return data;
    }

    public static TheoryData<string, int, int> StylesAndSizes()
    {
        var data = new TheoryData<string, int, int>();
        foreach (var s in BorderGenerator.Styles)
            foreach (var (w, h) in Sizes) data.Add(s.Name, w, h);
        return data;
    }

    /// <summary>Decode an 8-bit RGBA PNG (the only kind this project writes).</summary>
    private static (int Width, int Height, byte[] Px) DecodePng(byte[] buf)
    {
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }, buf[..8]);
        int pos = 8, width = 0, height = 0;
        using var idat = new MemoryStream();
        while (pos < buf.Length)
        {
            int len = (int)BinaryPrimitives.ReadUInt32BigEndian(buf.AsSpan(pos));
            string type = Encoding.ASCII.GetString(buf, pos + 4, 4);
            var data = buf.AsSpan(pos + 8, len);
            if (type == "IHDR")
            {
                width = (int)BinaryPrimitives.ReadUInt32BigEndian(data);
                height = (int)BinaryPrimitives.ReadUInt32BigEndian(data[4..]);
                Assert.Equal(8, data[8]);  // bit depth
                Assert.Equal(6, data[9]);  // colour type RGBA
            }
            else if (type == "IDAT") idat.Write(data);
            pos += 12 + len;
        }
        idat.Position = 0;
        using var raw = new MemoryStream();
        using (var z = new ZLibStream(idat, CompressionMode.Decompress)) z.CopyTo(raw);
        var bytes = raw.GetBuffer();
        int stride = width * 4;
        var px = new byte[stride * height];
        for (int y = 0; y < height; y++)
        {
            int ft = bytes[y * (stride + 1)], row = y * (stride + 1) + 1;
            for (int x = 0; x < stride; x++)
            {
                int a = x >= 4 ? px[y * stride + x - 4] : 0;
                int b = y > 0 ? px[(y - 1) * stride + x] : 0;
                int c = x >= 4 && y > 0 ? px[(y - 1) * stride + x - 4] : 0;
                int v = bytes[row + x];
                if (ft == 1) v += a;
                else if (ft == 2) v += b;
                else if (ft == 3) v += (a + b) >> 1;
                else if (ft == 4)
                {
                    int p = a + b - c, pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
                    v += pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
                }
                px[y * stride + x] = (byte)(v & 255);
            }
        }
        return (width, height, px);
    }

    private static BorderOptions Opts(string style, int width, int height, string seed = "1", string mode = "bw", int? grey = null) =>
        new() { Style = style, Width = width, Height = height, Seed = seed, Mode = mode, Grey = grey };

    [Fact]
    public void All_requested_styles_are_available_by_their_full_names()
    {
        Assert.Equal(Expected, BorderGenerator.Styles.Select(s => s.Name));
        foreach (string name in Expected) Assert.Equal(name, BorderGenerator.ResolveStyle(name).Name);
        Assert.Equal("art-deco", BorderGenerator.ResolveStyle("deco").Id);
        Assert.Equal("modern", BorderGenerator.ResolveStyle("Modern").Id);
    }

    [Theory]
    [MemberData(nameof(StylesAndSizes))]
    public void Size_transparent_middle_monochrome_border(string style, int width, int height)
    {
        var (w, h, px) = DecodePng(PngRenderer.RenderPng(Opts(style, width, height, "3")));
        Assert.Equal(width, w);
        Assert.Equal(height, h);

        int inked = 0, black = 0;
        int band = (int)Math.Round(Math.Min(width, height) * 0.13, MidpointRounding.AwayFromZero);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int i = (y * w + x) * 4, a = px[i + 3];
                if (a == 0) continue;
                Assert.True(px[i] == px[i + 1] && px[i + 1] == px[i + 2], $"pixel {x},{y} is not neutral grey");
                inked++;
                if (px[i] < 40 && a > 200) black++;
                // The middle of the image must stay empty: nothing may be drawn deeper than 2.2 band widths.
                int depth = Math.Min(Math.Min(x, y), Math.Min(w - 1 - x, h - 1 - y));
                Assert.True(depth < band * 2.2, $"artwork at {x},{y} intrudes into the transparent middle");
            }
        }
        Assert.Equal(0, px[((h / 2) * w + w / 2) * 4 + 3]); // centre pixel is transparent
        Assert.True(black > w + h, "frame contains a meaningful amount of black ink");
        Assert.True(inked < w * h * 0.45, "most of the image is transparent");

        // every side of the frame carries artwork
        bool SideHasInk(int x0, int y0, int x1, int y1)
        {
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                    if (px[(y * w + x) * 4 + 3] > 128) return true;
            return false;
        }
        int mx = w / 2, my = h / 2, q = (int)Math.Round(band * 1.2, MidpointRounding.AwayFromZero);
        Assert.True(SideHasInk(mx - band * 2, 0, mx + band * 2, q), "top side");
        Assert.True(SideHasInk(mx - band * 2, h - q, mx + band * 2, h), "bottom side");
        Assert.True(SideHasInk(0, my - band * 2, q, my + band * 2), "left side");
        Assert.True(SideHasInk(w - q, my - band * 2, w, my + band * 2), "right side");
    }

    [Theory]
    [MemberData(nameof(StyleIds))]
    public void Seed_is_deterministic_and_changes_the_design(string style)
    {
        Assert.Equal(BorderGenerator.GenerateSvg(Opts(style, 512, 512, "11")), BorderGenerator.GenerateSvg(Opts(style, 512, 512, "11")));
        var variants = new[] { "1", "2", "3", "4", "5" }.Select(seed => BorderGenerator.GenerateSvg(Opts(style, 512, 512, seed))).ToHashSet();
        Assert.Equal(5, variants.Count); // five seeds give five different frames
    }

    [Fact]
    public void Ink_mode_writes_pure_black_with_the_artwork_in_the_alpha_channel()
    {
        var (_, _, px) = DecodePng(PngRenderer.RenderPng(Opts("botanical", 512, 512, "2", "ink")));
        int ink = 0;
        for (int i = 0; i < px.Length; i += 4)
        {
            Assert.Equal(0, px[i] + px[i + 1] + px[i + 2]);
            if (px[i + 3] > 200) ink++;
        }
        Assert.True(ink > 1000);
    }

    [Theory]
    [MemberData(nameof(StyleIds))]
    public void White_mode_writes_pure_white_with_the_same_alpha_as_ink_mode(string style)
    {
        var white = DecodePng(PngRenderer.RenderPng(Opts(style, 540, 960, "5", "white"))).Px;
        var ink = DecodePng(PngRenderer.RenderPng(Opts(style, 540, 960, "5", "ink"))).Px;
        int solid = 0, mismatched = 0, notWhite = 0;
        for (int i = 0; i < white.Length; i += 4)
        {
            if (white[i] + white[i + 1] + white[i + 2] != 765) notWhite++;
            if (white[i + 3] != ink[i + 3]) mismatched++;
            if (white[i + 3] > 200) solid++;
        }
        Assert.Equal(0, notWhite);   // every pixel is pure white
        Assert.Equal(0, mismatched); // alpha matches ink mode
        Assert.True(solid > 1500, "frame is visible");
        Assert.Equal(0, white[(480 * 540 + 270) * 4 + 3]); // centre is transparent
    }

    [Theory]
    [MemberData(nameof(StyleIds))]
    public void White_grey_mode_keeps_fills_opaque(string style)
    {
        var wg = DecodePng(PngRenderer.RenderPng(Opts(style, 800, 600, "1", "white-grey", 90))).Px;
        var bw = DecodePng(PngRenderer.RenderPng(Opts(style, 800, 600, "1"))).Px;
        int white = 0, grey = 0, bad = 0, alphaDiff = 0;
        for (int i = 0; i < wg.Length; i += 4)
        {
            if (wg[i + 3] != bw[i + 3]) alphaDiff++;
            if (wg[i + 3] == 0) continue;
            int v = wg[i];
            if (v != wg[i + 1] || v != wg[i + 2] || v < 90) bad++;
            // lines are the inverse of the black ink, fills the inverse of the white paper
            if (v != (int)Math.Floor(255 - (165 * bw[i]) / 255.0 + 0.5)) bad++;
            if (v == 255) white++;
            if (v == 90) grey++;
        }
        Assert.Equal(0, bad);       // only neutral tones between the fill grey and white
        Assert.Equal(0, alphaDiff); // alpha is unchanged
        Assert.True(white > 1500, "has white line art");
        if (style is "art-deco" or "botanical" or "engraved" or "organic") Assert.True(grey > 1500, "has grey fills");
    }

    [Fact]
    public void White_grey_checks_its_grey_and_accepts_the_american_spelling()
    {
        var ex = Assert.Throws<BorderException>(() => PngRenderer.RenderPng(Opts("organic", 512, 512, mode: "white-grey", grey: 300)));
        Assert.Contains("grey must be", ex.Message);
        Assert.True(PngRenderer.RenderPng(Opts("organic", 512, 512, mode: "white-gray")).Length > 1000);
    }

    [Fact]
    public void Bad_input_is_rejected_with_a_clear_message()
    {
        Assert.Contains("Unknown style", Assert.Throws<BorderException>(() => PngRenderer.RenderPng(Opts("nope", 512, 512))).Message);
        Assert.Contains("Unknown mode", Assert.Throws<BorderException>(() => PngRenderer.RenderPng(Opts("art-deco", 512, 512, mode: "pink"))).Message);
        Assert.Contains("width must be", Assert.Throws<BorderException>(() => PngRenderer.RenderPng(Opts("art-deco", 10, 512))).Message);
        Assert.Contains("height must be", Assert.Throws<BorderException>(() => BorderGenerator.ParseSize("height", "512.5")).Message);
        Assert.Contains("thickness must be", Assert.Throws<BorderException>(() => BorderGenerator.ParseThickness("0.9")).Message);
        Assert.Contains("grey must be", Assert.Throws<BorderException>(() => BorderGenerator.ParseGrey("12.5")).Message);
    }

    // ---- reference values: same seed, same random numbers, same drawing ----

    [Fact]
    public void Random_numbers_match_the_reference_values()
    {
        var r = new Rng("art-deco:1");
        Assert.Equal(0.6382963845971972, r.Next());
        Assert.Equal(0.02670909254811704, r.Next());
        Assert.Equal(0.5451287236064672, r.Next());
        var q = new Rng("spring");
        Assert.Equal(150, q.Int(0, 1000));
        Assert.Equal("a", q.Pick("a", "b", "c", "d"));
    }

    [Fact]
    public void Numbers_are_formatted_with_two_decimals_and_ties_away_from_zero()
    {
        Assert.Equal("0", Geom.F(0));
        Assert.Equal("0", Geom.F(-0.0));
        Assert.Equal("10", Geom.F(10));
        Assert.Equal("1.5", Geom.F(1.5));
        Assert.Equal("-485.98", Geom.F(-485.98));
        Assert.Equal("0.13", Geom.F(0.125));   // exact ties round away from zero
        Assert.Equal("-0.38", Geom.F(-0.375));
        Assert.Equal("12.63", Geom.F(12.625));
        Assert.Equal("1", Geom.F(1.005));      // not a tie in binary: 1.00499...
        Assert.Equal("3", Geom.Round(2.5).ToString());
        Assert.Equal("-2", Geom.Round(-2.5).ToString());
    }

    [Fact]
    public void Svg_is_unchanged_for_a_known_frame()
    {
        const string reference =
            "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"512\" height=\"512\" viewBox=\"0 0 512 512\">" +
            "<path d=\"M5.5 5.5h485.98v485.98h-485.98Z\" fill=\"none\" stroke=\"#000\" stroke-width=\"1.15\" stroke-linecap=\"butt\" stroke-linejoin=\"miter\"/>" +
            "<path d=\"M20.52 20.52h485.98v485.98h-485.98Z\" fill=\"none\" stroke=\"#000\" stroke-width=\"2.3\" stroke-linecap=\"butt\" stroke-linejoin=\"miter\"/></svg>";
        Assert.Equal(reference, BorderGenerator.GenerateSvg(Opts("modern", 512, 512, "1")));
    }
}
