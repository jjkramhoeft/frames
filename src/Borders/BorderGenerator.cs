// Pure generator: (width, height, style, seed) -> SVG string.
using System.Globalization;
using Borders.Styles;

namespace Borders;

/// <summary>A border style. Band narrows the border to that fraction of the usual thickness.</summary>
public sealed record BorderStyle(string Id, string Name, Func<Frame, string> Draw, double Band = 1);

/// <summary>Everything that describes one frame. Mode and Grey only matter when rendering a PNG.</summary>
public sealed record BorderOptions
{
    public required string Style { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public string Seed { get; init; } = "1";
    /// <summary>Border band thickness as a fraction of the shorter side (default 0.13).</summary>
    public double? Thickness { get; init; }
    /// <summary>bw | ink | white | white-grey</summary>
    public string Mode { get; init; } = "bw";
    /// <summary>Fill tone for white-grey mode, 0 (black) to 255 (white). Default 102.</summary>
    public int? Grey { get; init; }
}

/// <summary>Thrown for bad input; the message is meant to be shown to the user as is.</summary>
public sealed class BorderException(string message) : Exception(message);

public static class BorderGenerator
{
    public static readonly IReadOnlyList<BorderStyle> Styles =
    [
        new("art-deco", "Art Deco Line Borders", ArtDeco.Draw),
        new("botanical", "Botanical Floral Frames", Botanical.Draw),
        new("whiplash", "Art Nouveau Whiplash Borders", Whiplash.Draw),
        new("art-nouveau", "Art Nouveau Borders", ArtNouveau.Draw),
        new("victorian", "Victorian Flourishes", Victorian.Draw),
        new("geometric", "Geometric Line Borders", Geometric.Draw),
        new("engraved", "Engraved Floral Frames", Engraved.Draw),
        new("organic", "Organic Borders", Organic.Draw),
        new("calligraphic", "Calligraphic Flourishes", Calligraphic.Draw),
        // Simpler styles: Band narrows the border to that fraction of the usual thickness.
        new("modern", "Modern Borders", Modern.Draw, 0.5),
        new("classic", "Classic Borders", Classic.Draw, 0.5),
    ];

    public const int MinSize = 96;
    public const int MaxSize = 4096;

    private static string Norm(string s) =>
        new(s.ToLowerInvariant().Where(c => c is (>= 'a' and <= 'z') or (>= '0' and <= '9')).ToArray());

    /// <summary>Accepts the full style name, its id, or any unambiguous fragment ("deco", "whiplash").</summary>
    public static BorderStyle ResolveStyle(string style)
    {
        string key = Norm(style);
        var exact = Styles.FirstOrDefault(s => Norm(s.Name) == key || Norm(s.Id) == key);
        if (exact is not null) return exact;
        var partial = Styles.Where(s => Norm(s.Name).Contains(key) || Norm(s.Id).Contains(key)).ToList();
        if (key.Length != 0 && partial.Count == 1) return partial[0];
        throw new BorderException($"Unknown style \"{style}\". Available: {string.Join(", ", Styles.Select(s => s.Name))}");
    }

    public static void CheckSize(int width, int height)
    {
        foreach (var (label, v) in new[] { ("width", width), ("height", height) })
        {
            if (v < MinSize || v > MaxSize) throw SizeError(label, v.ToString(CultureInfo.InvariantCulture));
        }
    }

    /// <summary>Parse a width or height given as text (command line, query string); rejects anything but whole pixels in range.</summary>
    public static int ParseSize(string label, string text)
    {
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int v) || v < MinSize || v > MaxSize)
            throw SizeError(label, text);
        return v;
    }

    private static BorderException SizeError(string label, string got) =>
        new($"{label} must be a whole number of pixels between {MinSize} and {MaxSize} (got {got})");

    /// <summary>Parse the optional thickness fraction; null or empty means the default.</summary>
    public static double? ParseThickness(string? text, string label = "thickness")
    {
        if (string.IsNullOrEmpty(text)) return null;
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) || !(v >= 0.05 && v <= 0.3))
            throw new BorderException($"{label} must be between 0.05 and 0.3");
        return v;
    }

    /// <summary>Parse the optional fill grey for white-grey mode; null or empty means the default.</summary>
    public static int? ParseGrey(string? text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        if (!int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int v) || v < 0 || v > 255)
            throw new BorderException($"grey must be a whole number from 0 to 255 (got {text})");
        return v;
    }

    /// <summary>Inner SVG markup for a frame (no &lt;svg&gt; wrapper), so frames can be composed into sheets.</summary>
    public static string GenerateMarkup(BorderOptions opts)
    {
        CheckSize(opts.Width, opts.Height);
        var st = ResolveStyle(opts.Style);
        var frame = new Frame(opts.Width, opts.Height, $"{st.Id}:{opts.Seed}", opts.Thickness, st.Band);
        return st.Draw(frame);
    }

    public static string GenerateSvg(BorderOptions opts)
    {
        string inner = GenerateMarkup(opts);
        return $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{opts.Width}\" height=\"{opts.Height}\" viewBox=\"0 0 {opts.Width} {opts.Height}\">{inner}</svg>";
    }
}
