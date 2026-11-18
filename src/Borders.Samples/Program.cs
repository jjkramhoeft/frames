// Renders every style at the three test sizes into <out>/samples, plus one review sheet per style
// (frames composited on a tan background so both black ink and white paper are visible) and an
// overview of all styles.
//   dotnet run --project src/Borders.Samples -- [styleFilter] [--seeds 1,2,3] [--mode bw|ink] [--out dir]
using System.Diagnostics;
using System.Globalization;
using Borders;

(int W, int H)[] sizes = [(512, 512), (800, 600), (540, 960)];
byte[] bg = [222, 205, 172];
const int pad = 24;

try
{
    var rest = args.ToList();
    string Flag(string name, string def)
    {
        int i = rest.IndexOf($"--{name}");
        if (i < 0) return def;
        if (i + 1 >= rest.Count) throw new BorderException($"Missing value for --{name}");
        string value = rest[i + 1];
        rest.RemoveRange(i, 2);
        return value;
    }
    string[] seeds = Flag("seeds", "1").Split(',');
    string mode = Flag("mode", "bw");
    string outDir = Path.GetFullPath(Flag("out", "out"));
    string? filter = rest.FirstOrDefault();
    var styles = BorderGenerator.Styles
        .Where(s => filter is null || s.Id.Contains(filter) || s.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
        .ToList();

    string samplesDir = Path.Combine(outDir, "samples"), sheetsDir = Path.Combine(outDir, "sheets");
    Directory.CreateDirectory(samplesDir);
    Directory.CreateDirectory(sheetsDir);

    foreach (var st in styles)
    {
        int sheetW = sizes.Sum(s => s.W + pad) + pad;
        int rowH = sizes.Max(s => s.H) + pad;
        int sheetH = rowH * seeds.Length + pad;
        byte[] sheet = Blank(sheetW, sheetH);

        for (int row = 0; row < seeds.Length; row++)
        {
            string seed = seeds[row];
            int x0 = pad;
            foreach (var (width, height) in sizes)
            {
                var opts = new BorderOptions { Width = width, Height = height, Style = st.Id, Seed = seed, Mode = mode };
                var clock = Stopwatch.StartNew();
                File.WriteAllBytes(Path.Combine(samplesDir, $"{st.Id}_{width}x{height}_seed{seed}.png"), PngRenderer.RenderPng(opts));
                double ms = clock.Elapsed.TotalMilliseconds;
                Composite(sheet, sheetW, x0, pad + row * rowH, PngRenderer.Rasterise(BorderGenerator.GenerateSvg(opts), width, height), width, height);
                x0 += width + pad;
                Console.WriteLine($"{st.Name} {width}x{height} seed {seed}: {ms.ToString("F0", CultureInfo.InvariantCulture)} ms");
            }
        }
        File.WriteAllBytes(Path.Combine(sheetsDir, $"{st.Id}.png"), PngRenderer.EncodePng(sheetW, sheetH, sheet));
    }
    Console.WriteLine($"\nSamples: {samplesDir}\nSheets:  {sheetsDir}");

    // One overview sheet: all styles at 512x512 for the first seed.
    {
        const int cell = 512, cols = 3;
        var all = BorderGenerator.Styles;
        int rows = (all.Count + cols - 1) / cols;
        int gw = cols * (cell + pad) + pad, gh = rows * (cell + pad) + pad;
        byte[] g = Blank(gw, gh);
        for (int n = 0; n < all.Count; n++)
        {
            var opts = new BorderOptions { Width = cell, Height = cell, Style = all[n].Id, Seed = seeds[0], Mode = mode };
            Composite(g, gw, pad + (n % cols) * (cell + pad), pad + (n / cols) * (cell + pad),
                PngRenderer.Rasterise(BorderGenerator.GenerateSvg(opts), cell, cell), cell, cell);
        }
        string file = Path.Combine(outDir, "gallery.png");
        File.WriteAllBytes(file, PngRenderer.EncodePng(gw, gh, g));
        Console.WriteLine($"Gallery: {file}");
    }
    return 0;
}
catch (Exception err)
{
    Console.Error.WriteLine($"Error: {err.Message}");
    return 1;
}

// An opaque sheet filled with the background colour.
byte[] Blank(int width, int height)
{
    var px = new byte[width * height * 4];
    for (int i = 0; i < px.Length; i += 4) { px[i] = bg[0]; px[i + 1] = bg[1]; px[i + 2] = bg[2]; px[i + 3] = 255; }
    return px;
}

// Alpha-blend a straight-alpha frame onto the sheet with its top-left corner at (x0, y0).
static void Composite(byte[] sheet, int sheetW, int x0, int y0, byte[] pixels, int width, int height)
{
    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            int si = (y * width + x) * 4, di = ((y0 + y) * sheetW + x0 + x) * 4;
            double al = pixels[si + 3] / 255.0;
            if (al == 0) continue;
            for (int c = 0; c < 3; c++) sheet[di + c] = (byte)Math.Floor(pixels[si + c] * al + sheet[di + c] * (1 - al) + 0.5);
        }
    }
}
