// Command line: generate one border frame as a PNG (or SVG).
using System.Text;
using System.Text.RegularExpressions;
using Borders;

Console.OutputEncoding = Encoding.UTF8;

try
{
    Run(args);
    return 0;
}
catch (Exception err)
{
    Console.Error.WriteLine($"Error: {err.Message}");
    return 1;
}

static string StyleList() =>
    string.Join("\n", BorderGenerator.Styles.Select(s => $"  {s.Id,-13} {s.Name}"));

static string Help() => $"""
    Decorative border generator

    Usage:
      Borders.Cli --style <style> --width <px> --height <px> [--seed <seed>] [--out <file>]

    Options:
      --style, -s      Style name, id or unique fragment (see below)        [required]
      --width, -w      Output width in pixels                               [required]
      --height, -h     Output height in pixels                              [required]
      --size           Shorthand for width and height, e.g. --size 800x600
      --seed           Any number or text; same inputs always give the same frame  [default: 1]
      --out, -o        Output file (.png or .svg)       [default: out/<style>_<w>x<h>_seed<seed>.png]
      --mode           bw    = black ink + white paper fills on transparent (default)
                       ink   = black ink only; white areas become transparent
                       white = white ink only, for tinting the PNG afterwards
                       white-grey = white ink with ornament fills kept opaque in grey
      --grey           Fill tone for white-grey mode, 0 (black) to 255 (white)   [default: 102]
      --thickness      Border band thickness as a fraction of the shorter side  [default: 0.13]
      --list           List the available styles
      --help           Show this help

    Styles:
    {StyleList()}

    Examples:
      Borders.Cli --style "Art Deco Line Borders" --size 512x512 --seed 7
      Borders.Cli -s whiplash -w 540 -h 960 --seed spring --mode white -o frame.png

    """;

static Dictionary<string, string> Parse(string[] argv)
{
    var alias = new Dictionary<string, string> { ["s"] = "style", ["w"] = "width", ["h"] = "height", ["o"] = "out" };
    var opts = new Dictionary<string, string>();
    for (int i = 0; i < argv.Length; i++)
    {
        string a = argv[i];
        if (!a.StartsWith('-')) throw new BorderException($"Unexpected argument \"{a}\"");
        string key = a.TrimStart('-');
        string? value = null;
        int eq = key.IndexOf('=');
        if (eq >= 0) { value = key[(eq + 1)..]; key = key[..eq]; }
        if (alias.TryGetValue(key, out var full)) key = full;
        if (key is "help" or "list") { opts[key] = "true"; continue; }
        if (value is null && i + 1 < argv.Length) value = argv[++i];
        if (value is null) throw new BorderException($"Missing value for --{key}");
        opts[key] = value;
    }
    return opts;
}

static void Run(string[] argv)
{
    var o = Parse(argv);
    if (o.ContainsKey("help") || argv.Length == 0) { Console.WriteLine(Help()); return; }
    if (o.ContainsKey("list")) { Console.WriteLine(string.Join("\n", BorderGenerator.Styles.Select(s => $"{s.Id,-13} {s.Name}"))); return; }

    if (o.TryGetValue("size", out var size))
    {
        var m = Regex.Match(size, @"^(\d+)\s*[x×]\s*(\d+)$", RegexOptions.IgnoreCase);
        if (!m.Success) throw new BorderException($"--size must look like 800x600 (got \"{size}\")");
        o["width"] = m.Groups[1].Value;
        o["height"] = m.Groups[2].Value;
    }
    foreach (string k in new[] { "style", "width", "height" })
        if (!o.ContainsKey(k)) throw new BorderException($"--{k} is required (try --help)");

    var style = BorderGenerator.ResolveStyle(o["style"]);
    var opts = new BorderOptions
    {
        Style = style.Id,
        Width = BorderGenerator.ParseSize("width", o["width"]),
        Height = BorderGenerator.ParseSize("height", o["height"]),
        Seed = o.GetValueOrDefault("seed", "1"),
        Mode = o.GetValueOrDefault("mode", "bw"),
        Grey = BorderGenerator.ParseGrey(o.GetValueOrDefault("grey")),
        Thickness = BorderGenerator.ParseThickness(o.GetValueOrDefault("thickness"), "--thickness"),
    };
    string safeSeed = Regex.Replace(opts.Seed, @"[^\w.-]+", "_", RegexOptions.ECMAScript);
    string suffix = opts.Mode == "bw" ? "" : $"_{opts.Mode}";
    string outPath = o.GetValueOrDefault("out") is { Length: > 0 } given
        ? given
        : Path.Combine("out", $"{style.Id}_{opts.Width}x{opts.Height}_seed{safeSeed}{suffix}.png");
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
    if (outPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
        File.WriteAllText(outPath, BorderGenerator.GenerateSvg(opts), new UTF8Encoding(false));
    else
        File.WriteAllBytes(outPath, PngRenderer.RenderPng(opts));
    Console.WriteLine($"{style.Name} · {opts.Width}×{opts.Height} · seed {opts.Seed} · {opts.Mode} → {outPath}");
}
