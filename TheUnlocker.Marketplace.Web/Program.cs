using System.Text;
using System.Text.Json;
using TheUnlocker.Modding;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient("registry", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["RegistryBaseUrl"] ?? "http://localhost:5077");
});

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", async (IHttpClientFactory clients) =>
{
    var mods = await LoadMods(clients);
    var filters = """
<form class="filters" method="get" action="/search">
  <input name="q" placeholder="Search mods" />
  <input name="game" placeholder="Game" />
  <input name="tag" placeholder="Tag" />
  <input name="permission" placeholder="Permission" />
  <select name="trust"><option value="">Any trust</option><option>Official</option><option>Trusted Publisher</option><option>Unknown</option></select>
  <button type="submit">Search</button>
</form>
""";
    var cards = string.Join("", mods.Select(mod => $"""
<article class="card">
  <h2><a href="/mods/{Html(mod.Id)}">{Html(mod.Name)}</a></h2>
  <p>{Html(mod.Description)}</p>
  <span class="badge">{Html(mod.TrustLevel)}</span>
  <span class="badge">{Html(mod.GameId)}</span>
  <p><a href="/publishers/{Html(mod.Author)}">{Html(mod.Author)}</a></p>
  <p><a href="theunlocker://install/{Uri.EscapeDataString(mod.Id)}">Install with TheUnlocker</a></p>
</article>
"""));

    return Results.Content(Page("TheUnlocker Marketplace", $"""
<section class="hero">
  <h1>TheUnlocker Marketplace</h1>
  <p>Browse approved mods, changelogs, ratings, comments, compatibility notes, and one-click install links.</p>
</section>
{filters}
<main class="grid">{cards}</main>
<section class="detail">
  <h2>Collections</h2>
  <p><a href="/collections/vanilla-plus">Vanilla+ Starter Pack</a></p>
</section>
"""), "text/html");
});

app.MapGet("/search", async (string? q, string? game, string? tag, string? permission, string? trust, IHttpClientFactory clients) =>
{
    var query = new StringBuilder("/mods?");
    void Add(string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            query.Append(Uri.EscapeDataString(name)).Append('=').Append(Uri.EscapeDataString(value)).Append('&');
        }
    }

    Add("q", q);
    Add("game", game);
    Add("tag", tag);
    Add("permission", permission);
    Add("trust", trust);
    var mods = await LoadMods(clients, query.ToString());
    var cards = string.Join("", mods.Select(mod => $"""
<article class="card">
  <h2><a href="/mods/{Html(mod.Id)}">{Html(mod.Name)}</a></h2>
  <p>{Html(mod.Description)}</p>
  <span class="badge">{Html(mod.TrustLevel)}</span>
  <span class="badge">{Html(mod.GameId)}</span>
</article>
"""));
    return Results.Content(Page("Search TheUnlocker Marketplace", $"""
<main class="detail">
  <a href="/">Back</a>
  <h1>Search</h1>
  <form class="filters" method="get" action="/search">
    <input name="q" value="{Html(q ?? "")}" placeholder="Search mods" />
    <input name="game" value="{Html(game ?? "")}" placeholder="Game" />
    <input name="tag" value="{Html(tag ?? "")}" placeholder="Tag" />
    <input name="permission" value="{Html(permission ?? "")}" placeholder="Permission" />
    <input name="trust" value="{Html(trust ?? "")}" placeholder="Trust" />
    <button type="submit">Filter</button>
  </form>
</main>
<main class="grid">{cards}</main>
"""), "text/html");
});

app.MapGet("/mods/{id}", async (string id, IHttpClientFactory clients) =>
{
    var mods = await LoadMods(clients);
    var mod = mods.FirstOrDefault(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    if (mod is null)
    {
        return Results.NotFound();
    }

    var versions = string.Join("", mod.Versions.Select(version => $"""
<li><strong>{Html(version.Version)}</strong> - <a href="{Html(version.DownloadUrl)}">download</a> - {Html(version.Changelog)}</li>
"""));
    var screenshots = string.Join("", mod.Screenshots.Select(url => $"""<img class="shot" src="{Html(url)}" alt="{Html(mod.Name)} screenshot" />"""));
    var badges = string.Join("", mod.Badges.Select(badge => $"""<span class="badge">{Html(badge)}</span>"""));

    return Results.Content(Page(mod.Name, $"""
<main class="detail">
  <a href="/">Back</a>
  <h1>{Html(mod.Name)}</h1>
  <p>{badges}</p>
  <p>{Html(mod.Description)}</p>
  <p><strong>Author:</strong> <a href="/publishers/{Html(mod.Author)}">{Html(mod.Author)}</a></p>
  <p><a class="button" href="theunlocker://install/{Uri.EscapeDataString(mod.Id)}">Install with TheUnlocker</a></p>
  <h2>Media</h2>
  <div class="media">{screenshots}</div>
  <h2>Description</h2>
  <article class="markdown">{RenderMarkdown(mod.MarkdownDescription)}</article>
  <h2>Versions</h2>
  <ul>{versions}</ul>
  <h2>Ratings and Reviews</h2>
  <p>{mod.Rating:0.0} stars from {mod.ReviewCount} reviews</p>
  <h2>Compatibility</h2>
  <p>{Html(mod.CompatibilitySummary)}</p>
</main>
"""), "text/html");
});

app.MapGet("/publishers/{publisherId}", async (string publisherId, IHttpClientFactory clients) =>
{
    var mods = (await LoadMods(clients)).Where(mod => mod.Author.Equals(publisherId, StringComparison.OrdinalIgnoreCase)).ToList();
    var cards = string.Join("", mods.Select(mod => $"""<li><a href="/mods/{Html(mod.Id)}">{Html(mod.Name)}</a> {Html(mod.Versions.FirstOrDefault()?.Version ?? "")}</li>"""));
    return Results.Content(Page($"{publisherId} Publisher", $"""
<main class="detail">
  <a href="/">Back</a>
  <h1>{Html(publisherId)}</h1>
  <p>Publisher profile, mod catalog, ratings, and public release history.</p>
  <ul>{cards}</ul>
</main>
"""), "text/html");
});

app.MapGet("/publishers/{publisherId}/upload", (string publisherId) =>
{
    return Results.Content(Page("Publisher Upload", $"""
<main class="detail">
  <a href="/publishers/{Html(publisherId)}">Back</a>
  <h1>Upload Package</h1>
  <form class="stack" method="post" enctype="multipart/form-data" action="/publisher-upload">
    <input name="publisherId" value="{Html(publisherId)}" />
    <input name="modId" placeholder="Mod ID" />
    <input name="version" placeholder="Version" />
    <textarea name="changelog" placeholder="Changelog"></textarea>
    <textarea name="releaseNotes" placeholder="Release notes"></textarea>
    <input name="permissions" placeholder="Permissions, comma separated" />
    <input name="updateRing" placeholder="stable, beta, nightly" />
    <input name="commitSha" placeholder="Commit SHA" />
    <input name="ciRunUrl" placeholder="CI run URL" />
    <input type="file" name="package" />
    <input type="file" name="screenshots" multiple />
    <button type="submit">Upload</button>
  </form>
</main>
"""), "text/html");
});

app.MapPost("/publisher-upload", async (HttpRequest request, IHttpClientFactory clients) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest();
    }

    var form = await request.ReadFormAsync();
    var modId = form["modId"].ToString();
    var version = form["version"].ToString();
    var package = form.Files.GetFile("package");
    if (string.IsNullOrWhiteSpace(modId) || string.IsNullOrWhiteSpace(version) || package is null)
    {
        return Results.BadRequest("modId, version, and package are required.");
    }

    using var content = new MultipartFormDataContent();
    content.Add(new StreamContent(package.OpenReadStream()), "package", package.FileName);
    content.Add(new StringContent(form["changelog"].ToString()), "changelog");
    content.Add(new StringContent(form["commitSha"].ToString()), "commitSha");
    content.Add(new StringContent(form["ciRunUrl"].ToString()), "ciRunUrl");
    var response = await clients.CreateClient("registry").PostAsync($"/mods/{Uri.EscapeDataString(modId)}/packages?version={Uri.EscapeDataString(version)}", content);
    return Results.Content(Page("Upload Submitted", $"""
<main class="detail">
  <h1>Upload Submitted</h1>
  <p>Registry response: {(int)response.StatusCode} {Html(response.ReasonPhrase ?? "")}</p>
  <p><a href="/mods/{Html(modId)}">View mod</a></p>
</main>
"""), "text/html");
});

app.MapGet("/admin", async (IHttpClientFactory clients) =>
{
    var queue = await SafeGet(clients, "/admin/review-queue");
    var audit = await SafeGet(clients, "/admin/audit-log");
    return Results.Content(Page("Registry Admin", $"""
<main class="detail">
  <h1>Registry Admin</h1>
  <h2>Moderation Queue</h2>
  <pre>{Html(queue)}</pre>
  <h2>Audit Log</h2>
  <pre>{Html(audit)}</pre>
</main>
"""), "text/html");
});

app.MapGet("/collections/{id}", (string id) =>
{
    return Results.Content(Page("Mod Collection", $"""
<main class="detail">
  <a href="/">Back</a>
  <h1>{Html(id)}</h1>
  <p>Shareable lockfile-backed mod collection.</p>
  <p><a class="button" href="theunlocker://install-pack/{Uri.EscapeDataString(id)}">Install Modpack with TheUnlocker</a></p>
  <h2>Included Mods</h2>
  <ul><li>hello-world</li></ul>
</main>
"""), "text/html");
});

app.Run();

static async Task<IReadOnlyCollection<RegistryModDto>> LoadMods(IHttpClientFactory clients, string route = "/mods")
{
    try
    {
        var json = await clients.CreateClient("registry").GetStringAsync(route);
        return JsonSerializer.Deserialize<List<RegistryModDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    }
    catch
    {
        return [
            new RegistryModDto("hello-world", "Hello World", "Sample Author", "Local demo entry while the registry is offline.", "Approved", [], [
                new RegistryVersionDto("1.0.0", "#", "", "Initial package", DateTimeOffset.UtcNow)
            ], "unity", "Trusted Publisher", [], ["Verified Safe", "Works on Unity"], [], "## Hello World\nA sample mod.", 4.8, 12, "Compatible with current sample runtime.")
        ];
    }
}

static async Task<string> SafeGet(IHttpClientFactory clients, string route)
{
    try
    {
        return await clients.CreateClient("registry").GetStringAsync(route);
    }
    catch (Exception ex)
    {
        return ex.Message;
    }
}

static string Page(string title, string body)
{
    return $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>{{Html(title)}}</title>
  <style>
    body { margin: 0; font-family: Segoe UI, Arial, sans-serif; background: #f6f7f9; color: #17191c; }
    .hero { padding: 56px 8vw 32px; background: #111827; color: white; }
    .hero h1, .detail h1 { font-size: 44px; margin: 0 0 12px; }
    .hero p { max-width: 760px; font-size: 18px; line-height: 1.5; }
    .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(260px, 1fr)); gap: 16px; padding: 28px 8vw; }
    .card { background: white; border: 1px solid #d7dce3; border-radius: 8px; padding: 18px; }
    .card h2 { margin: 0 0 8px; font-size: 22px; }
    .card a, .detail a { color: #1254d1; }
    .detail { max-width: 840px; margin: 0 auto; padding: 44px 24px; }
    .button { display: inline-block; background: #111827; color: white !important; padding: 10px 14px; border-radius: 6px; text-decoration: none; }
    .badge { display: inline-block; border: 1px solid #bcc6d3; border-radius: 999px; padding: 3px 8px; margin: 2px 4px 2px 0; font-size: 12px; }
    .media { display: flex; gap: 10px; flex-wrap: wrap; }
    .shot { width: 220px; aspect-ratio: 16 / 9; object-fit: cover; border: 1px solid #d7dce3; border-radius: 6px; }
    .markdown { white-space: pre-wrap; background: #fff; border: 1px solid #d7dce3; border-radius: 8px; padding: 14px; }
    .filters { display: flex; flex-wrap: wrap; gap: 8px; padding: 18px 8vw; background: white; border-bottom: 1px solid #d7dce3; }
    .filters input, .filters select, .filters button, .stack input, .stack textarea, .stack button { min-height: 34px; border: 1px solid #bcc6d3; border-radius: 6px; padding: 6px 8px; }
    .stack { display: grid; gap: 10px; }
    pre { overflow: auto; background: #111827; color: white; padding: 12px; border-radius: 8px; }
  </style>
</head>
<body>{{body}}</body>
</html>
""";
}

static string Html(string value) => System.Net.WebUtility.HtmlEncode(value);

static string RenderMarkdown(string markdown)
{
    var encoded = Html(markdown);
    var lines = encoded.Replace("\r\n", "\n").Split('\n');
    var rendered = new StringBuilder();
    foreach (var line in lines)
    {
        if (line.StartsWith("### ", StringComparison.Ordinal))
        {
            rendered.Append("<h3>").Append(line[4..]).Append("</h3>");
        }
        else if (line.StartsWith("## ", StringComparison.Ordinal))
        {
            rendered.Append("<h2>").Append(line[3..]).Append("</h2>");
        }
        else if (line.StartsWith("# ", StringComparison.Ordinal))
        {
            rendered.Append("<h1>").Append(line[2..]).Append("</h1>");
        }
        else if (line.StartsWith("- ", StringComparison.Ordinal))
        {
            rendered.Append("<p>").Append(line).Append("</p>");
        }
        else
        {
            rendered.Append("<p>").Append(line).Append("</p>");
        }
    }

    return rendered.ToString();
}

public sealed record RegistryModDto(
    string Id,
    string Name,
    string Author,
    string Description,
    string Status,
    string[] Tags,
    List<RegistryVersionDto> Versions,
    string GameId = "",
    string TrustLevel = "Unknown",
    string[]? Permissions = null,
    string[]? Badges = null,
    string[]? Screenshots = null,
    string MarkdownDescription = "",
    double Rating = 0,
    int ReviewCount = 0,
    string CompatibilitySummary = "")
{
    public string[] Badges { get; init; } = Badges ?? [];
    public string[] Screenshots { get; init; } = Screenshots ?? [];
}
public sealed record RegistryVersionDto(string Version, string DownloadUrl, string Sha256, string Changelog, DateTimeOffset CreatedAt);
