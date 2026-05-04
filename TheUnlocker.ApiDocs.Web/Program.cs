using System.Reflection;
using TheUnlocker.Modding;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () =>
{
    var sdkTypes = typeof(IMod).Assembly.GetExportedTypes()
        .OrderBy(type => type.Name)
        .Select(type => $"<li><a href=\"/types/{type.FullName}\">{type.Name}</a> <code>{type.Namespace}</code></li>");

    return Results.Content($$"""
<!doctype html>
<html>
<head><title>TheUnlocker SDK Docs</title><style>body{font-family:Segoe UI,Arial;margin:40px;line-height:1.5} code{background:#f2f4f7;padding:2px 5px}</style></head>
<body>
<h1>TheUnlocker SDK Docs</h1>
<p>Generated from <code>TheUnlocker.Modding.Abstractions</code>.</p>
<ul>{{string.Join("", sdkTypes)}}</ul>
</body>
</html>
""", "text/html");
});

app.MapGet("/types/{**name}", (string name) =>
{
    var type = typeof(IMod).Assembly.GetType(name);
    if (type is null)
    {
        return Results.NotFound();
    }

    var members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .Select(member => $"<li><code>{member.MemberType}</code> {member.Name}</li>");

    return Results.Content($$"""
<!doctype html>
<html>
<head><title>{{type.Name}}</title><style>body{font-family:Segoe UI,Arial;margin:40px;line-height:1.5} code{background:#f2f4f7;padding:2px 5px}</style></head>
<body>
<a href="/">Back</a>
<h1>{{type.FullName}}</h1>
<ul>{{string.Join("", members)}}</ul>
</body>
</html>
""", "text/html");
});

app.Run();
