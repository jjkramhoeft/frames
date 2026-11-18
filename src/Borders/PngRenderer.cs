// Rasterise the generated SVG to a PNG with SkiaSharp (via Svg.Skia).
using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using SkiaSharp;
using Svg.Skia;

namespace Borders;

public static class PngRenderer
{
    /// <summary>
    ///   bw          (default) black ink + white paper fills on a transparent background
    ///   ink         black ink only: white areas become transparent (alpha carries the artwork)
    ///   white       the same drawing as ink but in pure white, ready for a multiply/colour tint
    ///   white-grey  white line art with the ornament fills kept opaque in grey (Grey, 0-255),
    ///               so a multiply tint gives the tint colour for lines and a darker shade for fills
    /// </summary>
    public static readonly IReadOnlyList<string> Modes = ["bw", "ink", "white", "white-grey"];
    public const int DefaultGrey = 102;

    private static readonly uint[] CrcTable = BuildCrcTable();

    private static uint[] BuildCrcTable()
    {
        var t = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            uint c = n;
            for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xedb88320u ^ (c >> 1) : c >> 1;
            t[n] = c;
        }
        return t;
    }

    private static uint Crc32(ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        uint c = 0xffffffffu;
        foreach (byte b in type) c = CrcTable[(c ^ b) & 0xff] ^ (c >> 8);
        foreach (byte b in data) c = CrcTable[(c ^ b) & 0xff] ^ (c >> 8);
        return c ^ 0xffffffffu;
    }

    private static void WriteChunk(Stream s, string type, ReadOnlySpan<byte> data)
    {
        Span<byte> four = stackalloc byte[4];
        Span<byte> name = stackalloc byte[4];
        Encoding.ASCII.GetBytes(type, name);
        BinaryPrimitives.WriteUInt32BigEndian(four, (uint)data.Length);
        s.Write(four);
        s.Write(name);
        s.Write(data);
        BinaryPrimitives.WriteUInt32BigEndian(four, Crc32(name, data));
        s.Write(four);
    }

    /// <summary>Minimal RGBA8 PNG encoder (straight alpha in, no colour management).</summary>
    public static byte[] EncodePng(int width, int height, ReadOnlySpan<byte> rgba)
    {
        Span<byte> ihdr = stackalloc byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(ihdr, (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(ihdr[4..], (uint)height);
        ihdr[8] = 8; // bit depth
        ihdr[9] = 6; // RGBA
        int stride = width * 4;
        using var packed = new MemoryStream();
        using (var z = new ZLibStream(packed, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            for (int y = 0; y < height; y++)
            {
                z.WriteByte(0); // filter: none
                z.Write(rgba.Slice(y * stride, stride));
            }
        }
        using var png = new MemoryStream();
        png.Write([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]);
        WriteChunk(png, "IHDR", ihdr);
        WriteChunk(png, "IDAT", packed.GetBuffer().AsSpan(0, (int)packed.Length));
        WriteChunk(png, "IEND", []);
        return png.ToArray();
    }

    /// <summary>Rasterise an SVG string to straight-alpha RGBA pixels at the given size (4 bytes per pixel).</summary>
    public static byte[] Rasterise(string svg, int width, int height)
    {
        using var skSvg = new SKSvg();
        var picture = skSvg.FromSvg(svg) ?? throw new InvalidOperationException("The SVG could not be parsed.");
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var bitmap = new SKBitmap(info);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.DrawPicture(picture);
        }
        var px = bitmap.GetPixelSpan().ToArray(); // premultiplied RGBA
        for (int i = 0; i < px.Length; i += 4)
        {
            int a = px[i + 3];
            if (a == 0)
            {
                px[i] = px[i + 1] = px[i + 2] = 0;
            }
            else if (a < 255)
            {
                // un-premultiply, and force a neutral grey so the output is strictly black/white/grey
                byte v = (byte)Math.Min(255, (px[i] * 255 + a / 2) / a);
                px[i] = px[i + 1] = px[i + 2] = v;
            }
            else
            {
                px[i + 1] = px[i + 2] = px[i];
            }
        }
        return px;
    }

    /// <summary>Render a frame to PNG bytes.</summary>
    public static byte[] RenderPng(BorderOptions opts)
    {
        string mode = (opts.Mode ?? "bw").Replace("gray", "grey");
        if (!Modes.Contains(mode))
            throw new BorderException($"Unknown mode \"{mode}\" (use {string.Join(", ", Modes.Select(m => $"\"{m}\""))})");

        int width = opts.Width, height = opts.Height;
        byte[] pixels = Rasterise(BorderGenerator.GenerateSvg(opts), width, height);

        if (mode is "ink" or "white")
        {
            // Ink coverage goes into alpha; the colour is one flat value, so edges stay clean when tinted.
            byte v = mode == "white" ? (byte)255 : (byte)0;
            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i + 3] = (byte)((pixels[i + 3] * (255 - pixels[i]) + 127) / 255);
                pixels[i] = pixels[i + 1] = pixels[i + 2] = v;
            }
        }
        if (mode == "white-grey")
        {
            // Invert the tones but keep the alpha: ink becomes white, paper fills become the chosen grey.
            int grey = opts.Grey ?? DefaultGrey;
            if (grey < 0 || grey > 255) throw new BorderException($"grey must be a whole number from 0 to 255 (got {grey})");
            for (int i = 0; i < pixels.Length; i += 4)
            {
                if (pixels[i + 3] == 0) continue;
                pixels[i] = pixels[i + 1] = pixels[i + 2] = (byte)Math.Floor(255 - ((255 - grey) * pixels[i]) / 255.0 + 0.5);
            }
        }
        return EncodePng(width, height, pixels);
    }
}
