# Decorative border generator (prototype)

Generates decorative & vintage vector borders as PNG files. Written in C# (.NET 10).

**Input:** width and height in pixels, a style, a seed.
**Output:** a PNG of exactly that size. The middle and background are transparent; the frame is
drawn in black and white so it can be tinted later.

## Styles

| id | name |
| --- | --- |
| `art-deco` | Art Deco Line Borders |
| `botanical` | Botanical Floral Frames |
| `whiplash` | Art Nouveau Whiplash Borders |
| `art-nouveau` | Art Nouveau Borders |
| `victorian` | Victorian Flourishes |
| `geometric` | Geometric Line Borders |
| `engraved` | Engraved Floral Frames |
| `organic` | Organic Borders |
| `calligraphic` | Calligraphic Flourishes |
| `modern` | Modern Borders |
| `classic` | Classic Borders |

Modern and Classic are the simple ones: a few clean rules with one quiet accent, drawn in a band
half as wide as the ornate styles. Modern picks between offset rectangles, corner brackets,
crossing lines, an opened frame and rounded corners; Classic between thick-and-thin rules with
plain, scooped, notched or crossed (Oxford) corners.

The same style + size + seed always produces the same image. A different seed changes the design
within the style (corner type, motifs, spacing, proportions).

## Setup

Requires the .NET 10 SDK. Packages restore on the first build; `nuget.config` restores from
nuget.org only. The one dependency is `Svg.Skia` (brings SkiaSharp), which rasterises the SVG.
On Linux also add `SkiaSharp.NativeAssets.Linux`.

```bash
dotnet build
```

## Web app

```bash
dotnet run --project src/Borders.Web
```

Open http://localhost:5174 (set the `PORT` environment variable to change the port). Pick a style,
size and seed; preview a tint; download the PNG or SVG.

## Command line

```bash
dotnet run --project src/Borders.Cli -- --style "Art Deco Line Borders" --size 512x512 --seed 7
```

```bash
dotnet run --project src/Borders.Cli -- --style whiplash --width 540 --height 960 --seed spring --out frame.png
```

Options: `--mode bw|ink|white|white-grey`, `--grey 0–255`, `--thickness 0.05–0.3`, `--out file.png|file.svg`, `--list`, `--help`.
Without `--out` the file is written to `out/<style>_<w>x<h>_seed<seed>.png` under the current folder.

### Output modes

- `bw` (default): black ink plus white "paper" fills inside ornaments (petals, sunbursts, pebbles),
  on a transparent background. Tint by mapping black → ink colour and white → paper colour.
- `ink`: black only. White areas become transparent, so the alpha channel is the whole drawing
  and tinting is a plain colour replace.
- `white`: the same drawing as `ink`, but in pure white. Use this when the PNG will be tinted
  afterwards: a multiply or colour-overlay tint turns white into the tint colour, whereas black
  stays black. Every pixel is `255,255,255`; anti-aliasing lives in the alpha channel only.
- `white-grey`: white line art with the ornament fills kept opaque in grey, instead of turning
  transparent. Under a multiply tint the lines take the tint colour and the fills a darker shade
  of it. `--grey` sets the fill tone (0 = black, 255 = white, default 102).

```bash
dotnet run --project src/Borders.Cli -- --style victorian --size 800x600 --seed 42 --mode white
```

```bash
dotnet run --project src/Borders.Cli -- --style botanical --size 800x600 --seed 42 --mode white-grey --grey 120
```

## From code

Reference `src/Borders`:

```csharp
using Borders;

var opts = new BorderOptions { Style = "victorian", Width = 800, Height = 600, Seed = "42" };
byte[] png = PngRenderer.RenderPng(opts);        // PNG file bytes
string svg = BorderGenerator.GenerateSvg(opts);  // the same frame as SVG
```

`BorderOptions` also takes `Mode`, `Grey` and `Thickness`.

## Samples and tests

```bash
dotnet run --project src/Borders.Samples
```

Renders every style at the three test sizes (512×512, 800×600, 540×960) into `out/samples`,
review sheets into `out/sheets`, and an overview into `out/gallery.png`, under the current folder
(`--out <dir>` picks another one). Pass a style filter and seeds to explore:
`dotnet run --project src/Borders.Samples -- organic --seeds 1,2,3`.

```bash
dotnet test
```

Checks every style at every test size: exact output size, transparent middle, strictly
black/white/grey pixels, artwork on all four sides, deterministic seeds, and the output modes.
A few tests pin reference values (random numbers, number formatting, one known frame) so a change
that would alter what existing seeds draw is caught.

## How it works

- `src/Borders` – the library.
  - `BorderGenerator.cs` – style registry; turns `(width, height, style, seed)` into an SVG string.
  - `Styles/*.cs` – one file per style. Each receives a `Frame` (size `W`, `H`, band thickness
    `T`, line unit `U`, seeded `Rnd`) and returns SVG markup.
  - `Frame.cs` – symmetry helpers (corners, edges, quadrant mirroring) and a perimeter "track"
    coordinate system used by the flowing styles.
  - `Geom.cs`, `Ornament.cs` – seeded RNG, splines, variable-width ribbons, leaves, flowers, scrolls.
  - `PngRenderer.cs` – rasterises the SVG with SkiaSharp, applies the output mode and writes the PNG.
- `src/Borders.Cli` – the command line.
- `src/Borders.Web` – the preview server; the page is `wwwroot/index.html`.
- `src/Borders.Samples` – the samples and review sheets.
- `tests/Borders.Tests` – the tests.

Everything scales from the band thickness (13% of the shorter side by default; half of that for
Modern and Classic, set by `Band` in the style registry), so motifs are fitted to each edge length
rather than stretched.
