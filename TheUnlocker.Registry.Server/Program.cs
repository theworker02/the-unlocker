using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.RateLimiting;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Azure.Storage.Blobs;
using MongoDB.Driver;
using TheUnlocker.CrashReporting;
using TheUnlocker.Modding;
using TheUnlocker.Registry;
using TheUnlocker.Registry.Server.Jobs;
using TheUnlocker.Registry.Server.Mongo;
using TheUnlocker.Registry.Server.Repositories;
using TheUnlocker.Review;
using TheUnlocker.Storage;
using TheUnlocker.Sync;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Request.Headers["X-Api-Key"].FirstOrDefault() ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 20
            }));
});
var app = builder.Build();
var store = new RegistryStore(Path.Combine(app.Environment.ContentRootPath, "App_Data"));
IRegistryRepository repository = new JsonRegistryRepository();
IRegistryJobQueue jobQueue = string.IsNullOrWhiteSpace(builder.Configuration["Redis:ConnectionString"])
    ? new InMemoryJobQueue()
    : new RedisJobQueue(builder.Configuration["Redis:ConnectionString"]!);
IPackageStorage packageStorage = CreatePackageStorage(builder.Configuration, app.Environment.ContentRootPath);

app.UseSwagger();
app.UseSwaggerUI();
app.UseRateLimiter();
app.Use(async (context, next) =>
{
    if (!RequiresAuth(context.Request))
    {
        await next();
        return;
    }

    var identity = RegistryIdentity.FromRequest(context.Request, store);
    if (identity is null)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new { error = "Missing or invalid API key/JWT session." });
        return;
    }

    if (!identity.Allows(context.Request.Method, context.Request.Path))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new { error = "The credential does not include the required scope." });
        return;
    }

    context.Items["registry.identity"] = identity;
    await next();
});

if (builder.Configuration.GetValue("Mongo:RunMigrations", false))
{
    var options = new MongoRegistryOptions
    {
        ConnectionString = builder.Configuration["Mongo:ConnectionString"] ?? "mongodb://localhost:27017",
        DatabaseName = builder.Configuration["Mongo:DatabaseName"] ?? "theunlocker_registry"
    };
    var database = new MongoClient(options.ConnectionString).GetDatabase(options.DatabaseName);
    await new MongoMigrationRunner(database).ApplyAsync();
}

if (builder.Configuration["Registry:StorageProvider"]?.Equals("Mongo", StringComparison.OrdinalIgnoreCase) == true)
{
    var options = new MongoRegistryOptions
    {
        ConnectionString = builder.Configuration["Mongo:ConnectionString"] ?? "mongodb://localhost:27017",
        DatabaseName = builder.Configuration["Mongo:DatabaseName"] ?? "theunlocker_registry"
    };
    repository = new MongoRegistryRepository(new MongoClient(options.ConnectionString).GetDatabase(options.DatabaseName));
}

app.MapGet("/", () => Results.Ok(new
{
    name = "TheUnlocker Registry",
    storageProvider = repository.ProviderName,
    routes = new[] { "/users", "/auth/api-keys", "/mods", "/mods/{id}", "/crash-reports", "/sync/{userId}" }
}));

app.MapGet("/health", async () => Results.Ok(new
{
    status = "Healthy",
    storageProvider = repository.ProviderName,
    mongo = builder.Configuration["Registry:StorageProvider"]?.Equals("Mongo", StringComparison.OrdinalIgnoreCase) == true ? "Configured" : "Disabled",
    redis = string.IsNullOrWhiteSpace(builder.Configuration["Redis:ConnectionString"]) ? "InMemory" : "Configured",
    objectStorage = packageStorage.GetType().Name,
    workerHeartbeat = store.LoadWorkerHeartbeats().OrderByDescending(x => x.SeenAt).FirstOrDefault(),
    queues = new
    {
        packageScan = (await jobQueue.PeekAsync("package-scan")).Count,
        compatibilityTest = (await jobQueue.PeekAsync("compatibility-test")).Count,
        webhook = (await jobQueue.PeekAsync("webhook")).Count,
        deadLetter = (await jobQueue.PeekAsync("dead-letter")).Count
    },
    latestScan = store.LoadPackageScans().OrderByDescending(x => x.CreatedAt).FirstOrDefault()
}));

app.MapPost("/health/worker-heartbeat", (WorkerHeartbeat heartbeat) =>
{
    var saved = heartbeat with { SeenAt = DateTimeOffset.UtcNow };
    store.AddWorkerHeartbeat(saved);
    return Results.Ok(saved);
});

app.MapPost("/users", (CreateUserRequest request) =>
{
    var user = store.AddUser(request);
    return Results.Created($"/users/{user.Id}", user);
});

app.MapPost("/auth/api-keys", (CreateApiKeyRequest request) =>
{
    var key = store.CreateApiKey(request.UserId, request.Name);
    return key is null ? Results.NotFound() : Results.Ok(key);
});

app.MapPost("/auth/api-keys/scoped", (CreateScopedApiKeyRequest request) =>
{
    var key = store.CreateScopedApiKey(request);
    return key is null ? Results.NotFound() : Results.Ok(key);
});

app.MapGet("/auth/oauth/{provider}/start", (string provider) =>
{
    var configured = new[] { "github", "discord", "microsoft" };
    if (!configured.Contains(provider, StringComparer.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { error = "Unsupported OAuth provider." });
    }

    return Results.Ok(new
    {
        provider,
        authorizationUrl = $"/auth/oauth/{provider}/callback?code=dev-code",
        note = "Wire this route to the provider OAuth app credentials in production."
    });
});

app.MapPost("/auth/oauth/{provider}/callback", (string provider, OAuthCallbackRequest request) =>
{
    var user = store.AddUser(new CreateUserRequest(request.DisplayName, $"{provider}:{request.Subject}"));
    return Results.Ok(new { user, session = store.CreateJwtSession(user.Id), provider, request.Subject });
});

app.MapPost("/auth/sessions", (CreateSessionRequest request) =>
{
    var user = store.FindUser(request.UserId);
    return user is null ? Results.NotFound() : Results.Ok(store.CreateJwtSession(user.Id));
});

app.MapGet("/mods", (
    string? q,
    string? game,
    string? permission,
    string? trust,
    string? dependency,
    string? tag,
    int? minRating,
    DateTimeOffset? updatedSince) =>
{
    var query = new MarketplaceSearchQuery
    {
        Text = q,
        GameId = game,
        Permission = permission,
        TrustLevel = trust,
        Dependency = dependency,
        Tag = tag,
        MinimumRating = minRating,
        UpdatedSince = updatedSince
    };
    return Results.Ok(store.Search(query));
});

app.MapGet("/mods/{id}", (string id) =>
{
    var mod = store.Mods.FirstOrDefault(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    return mod is null ? Results.NotFound() : Results.Ok(mod);
});

app.MapPost("/mods", (RegistryMod request) =>
{
    var mod = store.UpsertMod(request with { Status = request.Status == "" ? ModerationStatus.Submitted.ToString() : request.Status });
    store.AddAudit("system", "publisher", "mods.upsert", mod.Id, "");
    return Results.Created($"/mods/{mod.Id}", mod);
});

app.MapPost("/mods/{id}/versions", (string id, RegistryVersion request) =>
{
    var mod = store.AddVersion(id, request);
    return mod is null ? Results.NotFound() : Results.Ok(mod);
});

app.MapPost("/mods/{id}/compatibility", (string id, SdkCompatibilityEntry entry) =>
{
    var saved = entry with { ModId = id, RecordedAt = DateTimeOffset.UtcNow };
    store.AddSdkCompatibility(saved);
    return Results.Ok(saved);
});

app.MapGet("/mods/{id}/compatibility", (string id) => Results.Ok(store.LoadSdkCompatibility().Where(x => x.ModId.Equals(id, StringComparison.OrdinalIgnoreCase))));

app.MapPost("/mods/{id}/publisher-verification", (string id, PublisherVerification verification) =>
{
    var saved = verification with { ModId = id, RequestedAt = DateTimeOffset.UtcNow };
    store.AddPublisherVerification(saved);
    store.AddAudit(verification.PublisherId, "publisher", "publisher.verify.request", id, "");
    return Results.Ok(saved);
});

app.MapPost("/admin/publisher-verifications/{publisherId}/approve", (string publisherId, PublisherVerificationDecision decision) =>
{
    var saved = decision with { PublisherId = publisherId, DecidedAt = DateTimeOffset.UtcNow, Status = "Verified" };
    store.AddPublisherVerificationDecision(saved);
    store.AddAudit(decision.Reviewer, "admin", "publisher.verify.approve", publisherId, "");
    return Results.Ok(saved);
});

app.MapPost("/mods/{id}/packages", async (string id, HttpRequest request) =>
{
    var version = request.Query["version"].ToString();
    if (!request.HasFormContentType || string.IsNullOrWhiteSpace(version))
    {
        return Results.BadRequest(new { error = "Upload multipart form data with ?version=." });
    }

    var form = await request.ReadFormAsync();
    var file = form.Files.FirstOrDefault();
    if (file is null)
    {
        return Results.BadRequest(new { error = "Missing package file." });
    }

    await using var stream = file.OpenReadStream();
    var stored = await packageStorage.PutAsync($"{id}/{version}/{file.FileName}", stream);
    store.AddProvenance(new PackageProvenance
    {
        PackageId = $"{id}@{version}",
        UploadedBy = request.Headers.Authorization.ToString(),
        SignedBy = form["signedBy"].ToString(),
        CommitSha = form["commitSha"].ToString(),
        CiRunUrl = form["ciRunUrl"].ToString(),
        Sha256 = stored.Sha256,
        SourceRepository = form["sourceRepository"].ToString()
    });
    await jobQueue.EnqueueAsync(new RegistryJob { Type = "package-scan", Payload = new { id, version, stored.Key, stored.Sha256 } });

    var mod = store.AddVersion(id, new RegistryVersion(version, stored.Url, stored.Sha256, form["changelog"].ToString(), DateTimeOffset.UtcNow));
    store.AddWebhookEvent(new RegistryWebhookEvent { EventType = "mod.version.created", Payload = new { id, version } });
    store.AddAudit("system", "publisher", "packages.upload", $"{id}@{version}", request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "");
    return mod is null ? Results.NotFound() : Results.Ok(new { stored, mod });
});

app.MapGet("/packages/{**key}", async (string key) =>
{
    try
    {
        var stream = await packageStorage.GetAsync(key);
        return Results.File(stream, "application/zip", Path.GetFileName(key));
    }
    catch
    {
        return Results.NotFound();
    }
});

app.MapPost("/mods/{id}/review", (string id, ModerationRecord request) =>
{
    var record = new ModerationRecord
    {
        ModId = id,
        Version = request.Version,
        Status = request.Status,
        Reviewer = request.Reviewer,
        Notes = request.Notes,
        Flags = request.Flags,
        UpdatedAt = DateTimeOffset.UtcNow
    };
    store.AddModeration(record);
    store.AddWebhookEvent(new RegistryWebhookEvent { EventType = "moderation.changed", Payload = record });
    store.AddAudit(record.Reviewer, "moderator", "mods.review", $"{id}@{record.Version}", "");
    return Results.Ok(record);
});

app.MapPost("/mods/{id}/flags", (string id, ModerationFlag request) =>
{
    var flag = request with { ModId = id, CreatedAt = DateTimeOffset.UtcNow };
    store.AddFlag(flag);
    return Results.Ok(flag);
});

app.MapPost("/mods/{id}/certifications", (string id, CertificationBadge badge) =>
{
    store.AddCertification(id, badge);
    return Results.Ok(badge);
});

app.MapPost("/mods/{id}/provenance", (string id, PackageProvenance provenance) =>
{
    var saved = new PackageProvenance
    {
        PackageId = string.IsNullOrWhiteSpace(provenance.PackageId) ? id : provenance.PackageId,
        UploadedBy = provenance.UploadedBy,
        SignedBy = provenance.SignedBy,
        CommitSha = provenance.CommitSha,
        CiRunUrl = provenance.CiRunUrl,
        Timestamp = provenance.Timestamp,
        Sha256 = provenance.Sha256,
        SourceRepository = provenance.SourceRepository
    };
    store.AddProvenance(saved);
    return Results.Ok(saved);
});

app.MapPost("/mods/{id}/reproducible-builds", (string id, ReproducibleBuildRequest request) =>
{
    var saved = new ReproducibleBuildRequest
    {
        ModId = id,
        Version = request.Version,
        RepositoryUrl = request.RepositoryUrl,
        CommitSha = request.CommitSha,
        BuildCommand = request.BuildCommand,
        ExpectedSha256 = request.ExpectedSha256
    };
    store.AddBuildVerification(saved);
    return Results.Accepted($"/mods/{id}/reproducible-builds", new { status = "queued", saved.ModId, saved.Version });
});

app.MapPost("/compatibility-tests", async (CompatibilityTestRequest request) =>
{
    var result = store.AddCompatibilityJob(request);
    await jobQueue.EnqueueAsync(new RegistryJob { Type = "compatibility-test", Payload = request });
    return Results.Accepted($"/compatibility-tests/{result.JobId}", result);
});

app.MapPost("/mods/{id}/ratings", (string id, RatingRequest request) =>
{
    var rating = request with { ModId = id, CreatedAt = DateTimeOffset.UtcNow };
    store.AddRating(rating);
    return Results.Ok(rating);
});

app.MapPost("/mods/{id}/reviews/{reviewId}/moderate", (string id, string reviewId, ReviewModerationRequest request) =>
{
    var record = new ReviewModerationRecord(id, reviewId, request.Action, request.Moderator, request.Notes, DateTimeOffset.UtcNow);
    store.AddReviewModeration(record);
    store.AddAudit(request.Moderator, "moderator", "reviews.moderate", $"{id}/{reviewId}", "");
    return Results.Ok(record);
});

app.MapPost("/mods/{id}/comments", (string id, CommentRequest request) =>
{
    var comment = request with { ModId = id, CreatedAt = DateTimeOffset.UtcNow };
    store.AddComment(comment);
    return Results.Ok(comment);
});

app.MapPost("/crash-reports", (CrashReport report) =>
{
    store.AddCrashReport(report);
    if (store.LoadCrashReports().Count(x => x.SuspectedModIds.Intersect(report.SuspectedModIds).Any()) >= 3)
    {
        store.AddWebhookEvent(new RegistryWebhookEvent { EventType = "crash.spike", Payload = report });
    }
    return Results.Created($"/crash-reports/{report.Id}", report);
});

app.MapGet("/admin/review-queue", () => Results.Ok(new
{
    uploads = store.Mods.Where(x => x.Status is "Submitted" or "Scanned"),
    flags = store.LoadFlags(),
    quarantined = store.LoadModeration().Where(x => x.Status == ModerationStatus.Quarantined),
    reviewModeration = store.LoadReviewModeration()
}));

app.MapPost("/admin/package-scans", async (PackageScanQueueRequest request) =>
{
    await jobQueue.EnqueueAsync(new RegistryJob { Type = "package-scan", Payload = request });
    store.AddAudit("system", "admin", "jobs.package-scan.queue", request.PackagePath, "");
    return Results.Accepted("/admin/jobs/package-scan", request);
});

app.MapPost("/admin/package-scans/results", (PackageScanRecord record) =>
{
    var saved = record with { CreatedAt = DateTimeOffset.UtcNow };
    store.AddPackageScan(saved);
    return Results.Ok(saved);
});

app.MapGet("/admin/scanner-rules", () => Results.Ok(store.LoadScannerRules().DefaultIfEmpty(new ModerationScannerRuleSetRecord("default", new ModerationScannerRuleSet().Rules.ToArray(), DateTimeOffset.UtcNow))));

app.MapPost("/admin/scanner-rules", (ModerationScannerRuleSetRecord rules) =>
{
    var saved = rules with { UpdatedAt = DateTimeOffset.UtcNow };
    store.AddScannerRules(saved);
    return Results.Ok(saved);
});

app.MapPost("/admin/jobs/{type}/retry", async (string type) =>
{
    var dead = await jobQueue.PeekAsync("dead-letter");
    var matching = dead.FirstOrDefault(x => x.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
    if (matching is null)
    {
        return Results.NotFound(new { error = "No dead-letter job of that type was found." });
    }

    await jobQueue.EnqueueAsync(matching);
    return Results.Accepted($"/admin/jobs/{type}", matching);
});

app.MapPost("/admin/webhooks/deliver", async (RegistryWebhookEvent webhookEvent) =>
{
    await jobQueue.EnqueueAsync(new RegistryJob { Type = "webhook", Payload = webhookEvent });
    store.AddWebhookEvent(webhookEvent);
    return Results.Accepted("/admin/jobs/webhook", webhookEvent);
});

app.MapGet("/admin/publisher-keys", () => Results.Ok(store.LoadPublisherKeys()));

app.MapGet("/admin/audit-log", () => Results.Ok(store.LoadAudit()));

app.MapGet("/admin/jobs/{type}", async (string type) => Results.Ok(await jobQueue.PeekAsync(type)));

app.MapGet("/publishers/{publisherId}/dashboard", (string publisherId) => Results.Ok(new
{
    publisherId,
    mods = store.Mods.Where(x => x.Author.Equals(publisherId, StringComparison.OrdinalIgnoreCase)),
    crashReports = store.LoadCrashReports().Where(x => x.SuspectedModIds.Any(id => id.Contains(publisherId, StringComparison.OrdinalIgnoreCase))),
    installStats = store.LoadInstallStats().Where(x => x.PublisherId.Equals(publisherId, StringComparison.OrdinalIgnoreCase)),
    analytics = new PublisherAnalyticsService(store).Build(publisherId)
}));

app.MapGet("/publishers/{publisherId}/verification", (string publisherId) => Results.Ok(new
{
    requests = store.LoadPublisherVerifications().Where(x => x.PublisherId.Equals(publisherId, StringComparison.OrdinalIgnoreCase)),
    decisions = store.LoadPublisherVerificationDecisions().Where(x => x.PublisherId.Equals(publisherId, StringComparison.OrdinalIgnoreCase))
}));

app.MapPost("/publishers/{publisherId}/keys", (string publisherId, PublisherKeyRecord key) =>
{
    var saved = key with { PublisherId = publisherId, CreatedAt = DateTimeOffset.UtcNow };
    store.AddPublisherKey(saved);
    return Results.Ok(saved);
});

app.MapPost("/webhooks", (RegistryWebhook webhook) =>
{
    store.AddWebhook(webhook);
    return Results.Ok(webhook);
});

app.MapGet("/webhooks/events", () => Results.Ok(store.LoadWebhookEvents()));

app.MapPost("/advisories", (VulnerabilityAdvisory advisory) =>
{
    store.AddAdvisory(advisory);
    return Results.Ok(advisory);
});

app.MapGet("/advisories", () => Results.Ok(store.LoadAdvisories()));

app.MapPost("/collections", (ModCollection collection) =>
{
    var saved = collection with { UpdatedAt = DateTimeOffset.UtcNow };
    store.AddCollection(saved);
    return Results.Ok(saved);
});

app.MapGet("/collections", () => Results.Ok(store.LoadCollections()));

app.MapPost("/compatibility/patch-recommendations", (CompatibilityRecommendationRequest request) =>
{
    var manifests = request.Mods.Select(mod => new ModManifest { Id = mod.Id, Targets = mod.Targets }).ToArray();
    return Results.Ok(new ConflictPatchRecommendationEngine().Recommend(manifests));
});

app.MapPost("/crash-reports/cluster", (CrashReportClusterRequest request) =>
{
    var reports = store.LoadCrashReports();
    return Results.Ok(reports
        .GroupBy(report => string.Join("|", new[] { report.Summary }.Concat(report.SuspectedModIds.OrderBy(x => x))))
        .Select(group => new CrashCluster(group.Key, group.Count(), group.SelectMany(x => x.SuspectedModIds).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()))
        .OrderByDescending(cluster => cluster.Count));
});

app.MapPost("/organizations", (OrganizationAccount organization) =>
{
    store.AddOrganization(organization);
    return Results.Ok(organization);
});

app.MapPost("/organizations/{organizationId}/teams", (string organizationId, PublisherTeam team) =>
{
    var saved = new PublisherTeam
    {
        Id = string.IsNullOrWhiteSpace(team.Id) ? Guid.NewGuid().ToString("N") : team.Id,
        OrganizationId = organizationId,
        Name = team.Name,
        MemberUserIds = team.MemberUserIds
    };
    store.AddPublisherTeam(saved);
    return Results.Ok(saved);
});

app.MapPost("/private-registries", (PrivateRegistry registry) =>
{
    store.AddPrivateRegistry(registry);
    return Results.Ok(registry);
});

app.MapPost("/update-rings", (ModUpdateRing ring) =>
{
    store.AddUpdateRing(ring);
    return Results.Ok(ring);
});

app.MapPost("/index/signed", (SignedIndexRequest request) =>
{
    var index = new ModRepositoryIndex
    {
        Mods = store.Mods.Select(mod => new ModRepositoryEntry
        {
            Id = mod.Id,
            Name = mod.Name,
            Description = mod.Description,
            Version = mod.Versions.OrderByDescending(x => x.CreatedAt).FirstOrDefault()?.Version ?? "0.0.0",
            DownloadUrl = mod.Versions.OrderByDescending(x => x.CreatedAt).FirstOrDefault()?.DownloadUrl ?? "",
            Sha256 = mod.Versions.OrderByDescending(x => x.CreatedAt).FirstOrDefault()?.Sha256 ?? "",
            Permissions = mod.Permissions ?? []
        }).ToList()
    };
    return Results.Ok(new SignedIndexService().Sign(index, request.PrivateKeyPem, request.PublicKeyPem));
});

app.MapPost("/sync/{userId}", (string userId, UserSyncState state) =>
{
    var synced = new UserSyncState
    {
        UserId = userId,
        InstalledMods = state.InstalledMods,
        Favorites = state.Favorites,
        Profiles = state.Profiles,
        Ratings = state.Ratings
    };
    store.SaveSync(synced);
    return Results.Ok(synced);
});

app.MapGet("/sync/{userId}", (string userId) => Results.Ok(store.LoadSync(userId)));

app.Run();

static IPackageStorage CreatePackageStorage(IConfiguration configuration, string contentRoot)
{
    var provider = configuration["Storage:Provider"] ?? "Local";
    if (provider.Equals("S3", StringComparison.OrdinalIgnoreCase) || provider.Equals("MinIO", StringComparison.OrdinalIgnoreCase))
    {
        var serviceUrl = configuration["S3:ServiceUrl"] ?? "http://localhost:9000";
        var accessKey = configuration["S3:AccessKey"] ?? "minioadmin";
        var secretKey = configuration["S3:SecretKey"] ?? "minioadmin";
        var bucket = configuration["S3:Bucket"] ?? "theunlocker-packages";
        var publicBaseUrl = configuration["S3:PublicBaseUrl"] ?? $"{serviceUrl.TrimEnd('/')}/{bucket}";
        var client = new AmazonS3Client(
            new BasicAWSCredentials(accessKey, secretKey),
            new AmazonS3Config
            {
                ServiceURL = serviceUrl,
                ForcePathStyle = true,
                AuthenticationRegion = RegionEndpoint.USEast1.SystemName
            });
        return new S3PackageStorage(client, bucket, publicBaseUrl);
    }

    if (provider.Equals("Azure", StringComparison.OrdinalIgnoreCase))
    {
        var connectionString = configuration["AzureBlob:ConnectionString"] ?? "UseDevelopmentStorage=true";
        var container = configuration["AzureBlob:Container"] ?? "theunlocker-packages";
        var publicBaseUrl = configuration["AzureBlob:PublicBaseUrl"] ?? "";
        return new AzureBlobPackageStorage(new BlobContainerClient(connectionString, container), publicBaseUrl);
    }

    return new LocalPackageStorage(
        Path.Combine(contentRoot, "App_Data", "packages"),
        "/packages");
}

static bool RequiresAuth(HttpRequest request)
{
    if (HttpMethods.IsGet(request.Method) || request.Path.StartsWithSegments("/auth") || request.Path.StartsWithSegments("/users"))
    {
        return false;
    }

    if (request.Path.StartsWithSegments("/health/worker-heartbeat"))
    {
        return false;
    }

    if (request.Path.StartsWithSegments("/crash-reports") && HttpMethods.IsPost(request.Method))
    {
        return false;
    }

    return true;
}

public sealed record CreateUserRequest(string DisplayName, string Email);
public sealed record CreateApiKeyRequest(string UserId, string Name);
public sealed record CreateScopedApiKeyRequest(string UserId, string Name, string[] Scopes, string Role);
public sealed record OAuthCallbackRequest(string Code, string Subject, string DisplayName);
public sealed record CreateSessionRequest(string UserId);
public sealed record JwtSessionResponse(string UserId, string Token, DateTimeOffset ExpiresAt);
public sealed record RegistryUser(string Id, string DisplayName, string Email, DateTimeOffset CreatedAt);
public sealed record ApiKeyResponse(string UserId, string Name, string Key, DateTimeOffset CreatedAt);
public sealed record RegistryVersion(string Version, string DownloadUrl, string Sha256, string Changelog, DateTimeOffset CreatedAt);
public sealed record RegistryMod(string Id, string Name, string Author, string Description, string Status, string[] Tags, List<RegistryVersion> Versions, string GameId = "", string TrustLevel = "Unknown", string[]? Permissions = null);
public sealed record ModerationFlag(string ModId, string Reason, string Reporter, DateTimeOffset CreatedAt);
public sealed record RatingRequest(string ModId, string UserId, int Stars, DateTimeOffset CreatedAt);
public sealed record CommentRequest(string ModId, string UserId, string Body, DateTimeOffset CreatedAt);
public sealed record PublisherKeyRecord(string PublisherId, string KeyId, string PublicKeyPem, DateTimeOffset CreatedAt);
public sealed record InstallStat(string ModId, string PublisherId, int Installs, DateTimeOffset UpdatedAt);
public sealed record CompatibilityTestRequest(string GameId, string GameVersion, string ModpackUrl, string AdapterId);
public sealed record CompatibilityTestResult(string JobId, string Status, CompatibilityTestRequest Request, DateTimeOffset CreatedAt);
public sealed record ScopedApiKeyResponse(string UserId, string Name, string Key, string[] Scopes, string Role, DateTimeOffset CreatedAt);
public sealed record SignedIndexRequest(string PrivateKeyPem, string PublicKeyPem);
public sealed record ReviewModerationRequest(string Action, string Moderator, string Notes);
public sealed record ReviewModerationRecord(string ModId, string ReviewId, string Action, string Moderator, string Notes, DateTimeOffset CreatedAt);
public sealed record PackageScanQueueRequest(string PackagePath, string ModId, string Version);
public sealed record WorkerHeartbeat(string WorkerId, string[] Queues, DateTimeOffset SeenAt);
public sealed record PackageScanRecord(string PackageId, string Version, string Status, int RiskScore, TimeSpan Duration, DateTimeOffset CreatedAt);
public sealed record SdkCompatibilityEntry(string ModId, string ModVersion, string SdkVersion, string RuntimeVersion, string AppVersion, string Status, DateTimeOffset RecordedAt);
public sealed record PublisherVerification(string ModId, string PublisherId, string Method, string Evidence, DateTimeOffset RequestedAt);
public sealed record PublisherVerificationDecision(string PublisherId, string Reviewer, string Status, string Notes, DateTimeOffset DecidedAt);
public sealed record ModerationScannerRuleSetRecord(string Id, ModerationScannerRule[] Rules, DateTimeOffset UpdatedAt);
public sealed record ModCollection(string Id, string Name, string[] Maintainers, string[] ModIds, string UpdateRing, string Changelog, DateTimeOffset UpdatedAt);
public sealed record CompatibilityRecommendationRequest(CompatibilityRecommendationMod[] Mods);
public sealed record CompatibilityRecommendationMod(string Id, string[] Targets);
public sealed record CrashReportClusterRequest(string? GameId);
public sealed record CrashCluster(string Signature, int Count, string[] SuspectedModIds);

file sealed record RegistryIdentity(string UserId, string Role, string[] Scopes)
{
    public static RegistryIdentity? FromRequest(HttpRequest request, RegistryStore store)
    {
        var apiKey = request.Headers["X-Api-Key"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            var key = store.FindScopedApiKey(apiKey);
            return key is null ? null : new RegistryIdentity(key.UserId, key.Role, key.Scopes);
        }

        var authorization = request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var session = store.FindJwtSession(authorization["Bearer ".Length..].Trim());
            return session is null ? null : new RegistryIdentity(session.UserId, "user", ["mods:read", "mods:write", "reviews:write"]);
        }

        return null;
    }

    public bool Allows(string method, PathString path)
    {
        if (Role is "admin")
        {
            return true;
        }

        var required = path.StartsWithSegments("/admin") ? "admin:write"
            : path.StartsWithSegments("/publishers") ? "publisher:write"
            : path.StartsWithSegments("/mods") && method is "POST" or "PUT" or "DELETE" ? "mods:write"
            : "registry:write";
        return Scopes.Contains(required, StringComparer.OrdinalIgnoreCase);
    }
}

file sealed class PublisherAnalyticsService
{
    private readonly RegistryStore _store;

    public PublisherAnalyticsService(RegistryStore store)
    {
        _store = store;
    }

    public object Build(string publisherId)
    {
        var mods = _store.Mods.Where(x => x.Author.Equals(publisherId, StringComparison.OrdinalIgnoreCase)).ToList();
        var modIds = mods.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var crashes = _store.LoadCrashReports().Count(x => x.SuspectedModIds.Any(modIds.Contains));
        var installs = _store.LoadInstallStats().Where(x => modIds.Contains(x.ModId)).Sum(x => x.Installs);
        return new
        {
            publisherId,
            modCount = mods.Count,
            installs,
            crashes,
            crashRate = installs <= 0 ? 0 : (double)crashes / installs,
            ratings = _store.LoadRatings().Where(x => modIds.Contains(x.ModId)).ToList(),
            marketplaceConversion = installs <= 0 ? "No install data yet" : "Install conversion tracking ready"
        };
    }
}

file sealed class RegistryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    private readonly string _root;
    private readonly UserSyncStore _syncStore;

    public RegistryStore(string root)
    {
        _root = root;
        _syncStore = new UserSyncStore(Path.Combine(root, "sync"));
        Directory.CreateDirectory(root);
    }

    public IReadOnlyCollection<RegistryMod> Mods => Load<RegistryMod>("mods.json");

    public IReadOnlyCollection<RegistryMod> Search(MarketplaceSearchQuery query)
    {
        IEnumerable<RegistryMod> mods = Mods;
        if (!string.IsNullOrWhiteSpace(query.Text))
        {
            mods = mods.Where(x => x.Name.Contains(query.Text, StringComparison.OrdinalIgnoreCase)
                || x.Description.Contains(query.Text, StringComparison.OrdinalIgnoreCase)
                || x.Id.Contains(query.Text, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.GameId))
        {
            mods = mods.Where(x => x.GameId.Equals(query.GameId, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Permission))
        {
            mods = mods.Where(x => (x.Permissions ?? []).Contains(query.Permission, StringComparer.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.TrustLevel))
        {
            mods = mods.Where(x => x.TrustLevel.Equals(query.TrustLevel, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Tag))
        {
            mods = mods.Where(x => x.Tags.Contains(query.Tag, StringComparer.OrdinalIgnoreCase));
        }

        return mods.ToList();
    }

    public RegistryUser AddUser(CreateUserRequest request)
    {
        var users = Load<RegistryUser>("users.json").ToList();
        var user = new RegistryUser(Guid.NewGuid().ToString("N"), request.DisplayName, request.Email, DateTimeOffset.UtcNow);
        users.Add(user);
        Save("users.json", users);
        return user;
    }

    public ApiKeyResponse? CreateApiKey(string userId, string name)
    {
        if (!Load<RegistryUser>("users.json").Any(x => x.Id == userId))
        {
            return null;
        }

        var keys = Load<ApiKeyResponse>("api-keys.json").ToList();
        var key = new ApiKeyResponse(userId, name, Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), DateTimeOffset.UtcNow);
        keys.Add(key);
        Save("api-keys.json", keys);
        return key;
    }

    public ScopedApiKeyResponse? CreateScopedApiKey(CreateScopedApiKeyRequest request)
    {
        if (!Load<RegistryUser>("users.json").Any(x => x.Id == request.UserId))
        {
            return null;
        }

        var keys = Load<ScopedApiKeyResponse>("scoped-api-keys.json").ToList();
        var key = new ScopedApiKeyResponse(request.UserId, request.Name, Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), request.Scopes, request.Role, DateTimeOffset.UtcNow);
        keys.Add(key);
        Save("scoped-api-keys.json", keys);
        return key;
    }

    public RegistryUser? FindUser(string userId) => Load<RegistryUser>("users.json").FirstOrDefault(x => x.Id == userId);

    public ScopedApiKeyResponse? FindScopedApiKey(string apiKey) => Load<ScopedApiKeyResponse>("scoped-api-keys.json").FirstOrDefault(x => x.Key == apiKey);

    public JwtSessionResponse CreateJwtSession(string userId)
    {
        var sessions = Load<JwtSessionResponse>("jwt-sessions.json").Where(x => x.ExpiresAt > DateTimeOffset.UtcNow).ToList();
        var session = new JwtSessionResponse(userId, Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)), DateTimeOffset.UtcNow.AddHours(12));
        sessions.Add(session);
        Save("jwt-sessions.json", sessions);
        return session;
    }

    public JwtSessionResponse? FindJwtSession(string token) => Load<JwtSessionResponse>("jwt-sessions.json")
        .FirstOrDefault(x => x.Token == token && x.ExpiresAt > DateTimeOffset.UtcNow);

    public RegistryMod UpsertMod(RegistryMod mod)
    {
        var mods = Mods.Where(x => !x.Id.Equals(mod.Id, StringComparison.OrdinalIgnoreCase)).ToList();
        mods.Add(mod);
        Save("mods.json", mods);
        return mod;
    }

    public RegistryMod? AddVersion(string id, RegistryVersion version)
    {
        var mods = Mods.ToList();
        var index = mods.FindIndex(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return null;
        }

        var existing = mods[index];
        var versions = existing.Versions.Where(x => x.Version != version.Version).ToList();
        versions.Add(version with { CreatedAt = version.CreatedAt == default ? DateTimeOffset.UtcNow : version.CreatedAt });
        var updated = existing with { Versions = versions.OrderByDescending(x => x.CreatedAt).ToList() };
        mods[index] = updated;
        Save("mods.json", mods);
        return updated;
    }

    public void AddModeration(ModerationRecord record) => Append("moderation.json", record);
    public void AddFlag(ModerationFlag flag) => Append("flags.json", flag);
    public void AddRating(RatingRequest rating) => Append("ratings.json", rating);
    public void AddComment(CommentRequest comment) => Append("comments.json", comment);
    public void AddReviewModeration(ReviewModerationRecord record) => Append("review-moderation.json", record);
    public void AddCrashReport(CrashReport report) => Append("crash-reports.json", report);
    public void AddWorkerHeartbeat(WorkerHeartbeat heartbeat) => Append("worker-heartbeats.json", heartbeat);
    public void AddPackageScan(PackageScanRecord record) => Append("package-scans.json", record);
    public void AddSdkCompatibility(SdkCompatibilityEntry entry) => Append("sdk-compatibility.json", entry);
    public void AddPublisherVerification(PublisherVerification verification) => Append("publisher-verifications.json", verification);
    public void AddPublisherVerificationDecision(PublisherVerificationDecision decision) => Append("publisher-verification-decisions.json", decision);
    public void AddScannerRules(ModerationScannerRuleSetRecord rules) => Append("scanner-rules.json", rules);
    public void AddCollection(ModCollection collection) => Append("collections.json", collection);
    public void AddCertification(string modId, CertificationBadge badge) => Append("certifications.json", new { ModId = modId, Badge = badge });
    public void AddProvenance(PackageProvenance provenance) => Append("provenance.json", provenance);
    public void AddBuildVerification(ReproducibleBuildRequest request) => Append("reproducible-builds.json", request);
    public CompatibilityTestResult AddCompatibilityJob(CompatibilityTestRequest request)
    {
        var result = new CompatibilityTestResult(Guid.NewGuid().ToString("N"), "Queued", request, DateTimeOffset.UtcNow);
        Append("compatibility-tests.json", result);
        return result;
    }

    public void AddPublisherKey(PublisherKeyRecord key) => Append("publisher-keys.json", key);
    public void AddWebhook(RegistryWebhook webhook) => Append("webhooks.json", webhook);
    public void AddWebhookEvent(RegistryWebhookEvent webhookEvent) => Append("webhook-events.json", webhookEvent);
    public void AddAdvisory(VulnerabilityAdvisory advisory) => Append("advisories.json", advisory);
    public void AddOrganization(OrganizationAccount organization) => Append("organizations.json", organization);
    public void AddPublisherTeam(PublisherTeam team) => Append("publisher-teams.json", team);
    public void AddPrivateRegistry(PrivateRegistry registry) => Append("private-registries.json", registry);
    public void AddUpdateRing(ModUpdateRing ring) => Append("update-rings.json", ring);
    public void AddAudit(string actorId, string role, string action, string target, string ipAddress)
    {
        Append("audit-log.json", new AuditLogEntry
        {
            ActorId = actorId,
            Role = role,
            Action = action,
            Target = target,
            IpAddress = ipAddress
        });
    }

    public IReadOnlyCollection<ModerationRecord> LoadModeration() => Load<ModerationRecord>("moderation.json");
    public IReadOnlyCollection<ModerationFlag> LoadFlags() => Load<ModerationFlag>("flags.json");
    public IReadOnlyCollection<CrashReport> LoadCrashReports() => Load<CrashReport>("crash-reports.json");
    public IReadOnlyCollection<PublisherKeyRecord> LoadPublisherKeys() => Load<PublisherKeyRecord>("publisher-keys.json");
    public IReadOnlyCollection<InstallStat> LoadInstallStats() => Load<InstallStat>("install-stats.json");
    public IReadOnlyCollection<RatingRequest> LoadRatings() => Load<RatingRequest>("ratings.json");
    public IReadOnlyCollection<ReviewModerationRecord> LoadReviewModeration() => Load<ReviewModerationRecord>("review-moderation.json");
    public IReadOnlyCollection<WorkerHeartbeat> LoadWorkerHeartbeats() => Load<WorkerHeartbeat>("worker-heartbeats.json");
    public IReadOnlyCollection<PackageScanRecord> LoadPackageScans() => Load<PackageScanRecord>("package-scans.json");
    public IReadOnlyCollection<SdkCompatibilityEntry> LoadSdkCompatibility() => Load<SdkCompatibilityEntry>("sdk-compatibility.json");
    public IReadOnlyCollection<PublisherVerification> LoadPublisherVerifications() => Load<PublisherVerification>("publisher-verifications.json");
    public IReadOnlyCollection<PublisherVerificationDecision> LoadPublisherVerificationDecisions() => Load<PublisherVerificationDecision>("publisher-verification-decisions.json");
    public IReadOnlyCollection<ModerationScannerRuleSetRecord> LoadScannerRules() => Load<ModerationScannerRuleSetRecord>("scanner-rules.json");
    public IReadOnlyCollection<ModCollection> LoadCollections() => Load<ModCollection>("collections.json");
    public IReadOnlyCollection<AuditLogEntry> LoadAudit() => Load<AuditLogEntry>("audit-log.json");
    public IReadOnlyCollection<RegistryWebhookEvent> LoadWebhookEvents() => Load<RegistryWebhookEvent>("webhook-events.json");
    public IReadOnlyCollection<VulnerabilityAdvisory> LoadAdvisories() => Load<VulnerabilityAdvisory>("advisories.json");
    public UserSyncState LoadSync(string userId) => _syncStore.Load(userId);
    public void SaveSync(UserSyncState state) => _syncStore.Save(state);

    private void Append<T>(string fileName, T item)
    {
        var items = Load<T>(fileName).ToList();
        items.Add(item);
        Save(fileName, items);
    }

    private IReadOnlyCollection<T> Load<T>(string fileName)
    {
        var path = Path.Combine(_root, fileName);
        if (!File.Exists(path))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<T>>(File.ReadAllText(path), JsonOptions) ?? [];
    }

    private void Save<T>(string fileName, IEnumerable<T> items)
    {
        File.WriteAllText(Path.Combine(_root, fileName), JsonSerializer.Serialize(items, JsonOptions));
    }
}
