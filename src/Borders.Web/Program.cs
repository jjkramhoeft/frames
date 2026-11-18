// Local preview app: serves wwwroot/index.html and renders frames on request.
using System.Text.RegularExpressions;
using Borders;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.SetMinimumLevel(LogLevel.Warning);
var app = builder.Build();

// Set the PORT environment variable to listen somewhere else.
int port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out int p) && p > 0 ? p : 5174;
app.Urls.Add($"http://localhost:{port}");

string indexHtml = Path.Combine(AppContext.BaseDirectory, "wwwroot", "index.html");

static string? Q(HttpRequest req, string key) => req.Query.TryGetValue(key, out var v) ? v.ToString() : null;

static BorderOptions ReadOpts(HttpRequest req)
{
    string? Text(string key) => Q(req, key) is { Length: > 0 } s ? s : null;
    var style = BorderGenerator.ResolveStyle(Text("style") ?? BorderGenerator.Styles[0].Id);
    return new BorderOptions
    {
        Style = style.Id,
        Width = BorderGenerator.ParseSize("width", Text("width") ?? "512"),
        Height = BorderGenerator.ParseSize("height", Text("height") ?? "512"),
        Seed = Q(req, "seed") ?? "1",
        Mode = Text("mode") ?? "bw",
        Grey = BorderGenerator.ParseGrey(Text("grey")),
        Thickness = BorderGenerator.ParseThickness(Text("thickness")),
    };
}

IResult Frame(HttpContext ctx, bool svg)
{
    ctx.Response.Headers.CacheControl = "no-store";
    try
    {
        var opts = ReadOpts(ctx.Request);
        string suffix = svg || opts.Mode == "bw" ? "" : $"_{opts.Mode}";
        string safeSeed = Regex.Replace(opts.Seed, @"[^\w.-]+", "_", RegexOptions.ECMAScript);
        string name = $"{opts.Style}_{opts.Width}x{opts.Height}_seed{safeSeed}{suffix}.{(svg ? "svg" : "png")}";
        if (ctx.Request.Query.ContainsKey("download"))
            ctx.Response.Headers.ContentDisposition = $"attachment; filename=\"{name}\"";
        return svg
            ? Results.Text(BorderGenerator.GenerateSvg(opts), "image/svg+xml")
            : Results.Bytes(PngRenderer.RenderPng(opts), "image/png");
    }
    catch (Exception err)
    {
        return Results.Text(err.Message, "text/plain; charset=utf-8", statusCode: 400);
    }
}

IResult Index(HttpContext ctx)
{
    ctx.Response.Headers.CacheControl = "no-store";
    return Results.File(indexHtml, "text/html; charset=utf-8");
}

app.MapGet("/", Index);
app.MapGet("/index.html", Index);
app.MapGet("/api/styles", (HttpContext ctx) =>
{
    ctx.Response.Headers.CacheControl = "no-store";
    return Results.Json(new
    {
        styles = BorderGenerator.Styles.Select(s => new { id = s.Id, name = s.Name }),
        minSize = BorderGenerator.MinSize,
        maxSize = BorderGenerator.MaxSize,
    });
});
app.MapGet("/api/frame.png", (HttpContext ctx) => Frame(ctx, svg: false));
app.MapGet("/api/frame.svg", (HttpContext ctx) => Frame(ctx, svg: true));

Console.WriteLine($"Border generator running at http://localhost:{port}");
app.Run();
