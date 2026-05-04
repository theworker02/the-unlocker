using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TheUnlocker.Adapter.Minecraft;
using TheUnlocker.Adapter.Unity;
using TheUnlocker.Adapter.Unreal;
using TheUnlocker.Adapters;
using TheUnlocker.Registry;
using TheUnlocker.Registry.Server.Jobs;
using TheUnlocker.Scanning;

namespace TheUnlocker.Registry.Worker;

public sealed class Worker : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IRegistryJobQueue _jobs;
    private readonly HttpClient _http = new();
    private readonly IReadOnlyCollection<IGameAdapter> _adapters =
    [
        new UnityGameAdapter(),
        new UnrealGameAdapter(),
        new MinecraftGameAdapter()
    ];

    public Worker(ILogger<Worker> logger, IConfiguration configuration, IRegistryJobQueue jobs)
    {
        _logger = logger;
        _configuration = configuration;
        _jobs = jobs;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TheUnlocker registry worker started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            await SendHeartbeatAsync(stoppingToken);
            foreach (var queue in new[] { "package-scan", "reproducible-build", "webhook", "compatibility-test" })
            {
                var job = await _jobs.DequeueAsync(queue, stoppingToken);
                if (job is null)
                {
                    continue;
                }

                try
                {
                    await ProcessAsync(job, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Job {JobId} failed", job.Id);
                    await _jobs.DeadLetterAsync(job, ex.Message, stoppingToken);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private Task ProcessAsync(RegistryJob job, CancellationToken ct)
    {
        _logger.LogInformation("Processing {Type} job {JobId}", job.Type, job.Id);
        return job.Type switch
        {
            "package-scan" => ProcessPackageScanAsync(job, ct),
            "reproducible-build" => ProcessReproducibleBuildAsync(job, ct),
            "webhook" => ProcessWebhookAsync(job, ct),
            "compatibility-test" => ProcessCompatibilityTestAsync(job, ct),
            _ => Task.CompletedTask
        };
    }

    private async Task ProcessPackageScanAsync(RegistryJob job, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var payload = JsonSerializer.Serialize(job.Payload, JsonOptions);
        var doc = JsonDocument.Parse(payload);
        var packagePath = doc.RootElement.TryGetProperty("packagePath", out var packagePathElement)
            ? packagePathElement.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(packagePath) || !File.Exists(packagePath))
        {
            _logger.LogInformation("Package scan job {JobId} recorded metadata-only payload: {Payload}", job.Id, payload);
            return;
        }

        var scanners = new List<IMalwareScanner> { new NoOpMalwareScanner() };
        if (!string.IsNullOrWhiteSpace(_configuration["Scanning:ClamAvPath"]))
        {
            scanners.Add(new ClamAvScanner(_configuration["Scanning:ClamAvPath"]!));
        }

        if (!string.IsNullOrWhiteSpace(_configuration["Scanning:YaraRulesPath"]))
        {
            scanners.Add(new YaraScanner(_configuration["Scanning:YaraRulesPath"]!, _configuration["Scanning:YaraPath"] ?? "yara"));
        }

        var report = await new PackageScanningPipeline(scanners).ScanAsync(packagePath, ct);
        stopwatch.Stop();
        var output = Path.Combine(_configuration["Worker:ReportsDirectory"] ?? "worker-reports", $"{job.Id}-scan.json");
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(report, JsonOptions), ct);
        await PostRegistryAsync("/admin/package-scans/results", new
        {
            packageId = report.Reputation.ModId,
            version = "",
            status = report.ManifestValid ? "Scanned" : "Invalid",
            riskScore = report.Reputation.Score,
            duration = stopwatch.Elapsed,
            createdAt = DateTimeOffset.UtcNow
        }, ct);
    }

    private async Task ProcessReproducibleBuildAsync(RegistryJob job, CancellationToken ct)
    {
        var output = Path.Combine(_configuration["Worker:ReportsDirectory"] ?? "worker-reports", $"{job.Id}-reproducible-build.json");
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
        {
            job.Id,
            Status = "QueuedForExternalBuilder",
            job.Payload,
            CheckedAt = DateTimeOffset.UtcNow
        }, JsonOptions), ct);
    }

    private async Task ProcessWebhookAsync(RegistryJob job, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(job.Payload, JsonOptions);
        var url = _configuration["Worker:WebhookTarget"];
        if (string.IsNullOrWhiteSpace(url))
        {
            _logger.LogInformation("Webhook job {JobId} has no configured target.", job.Id);
            return;
        }

        var secret = _configuration["Worker:WebhookSecret"] ?? "";
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-TheUnlocker-Signature", Sign(payload, secret));
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("TheUnlocker.Registry.Worker", "1.0"));
        var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    private async Task ProcessCompatibilityTestAsync(RegistryJob job, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(job.Payload, JsonOptions);
        var gameRoots = (_configuration["Worker:GameRoots"] ?? "")
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var detected = _adapters.SelectMany(adapter => adapter.DetectGames(gameRoots)).ToArray();
        var output = Path.Combine(_configuration["Worker:ReportsDirectory"] ?? "worker-reports", $"{job.Id}-compatibility.json");
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
        {
            job.Id,
            Payload = payload,
            DetectedGames = detected,
            Status = "Completed",
            CompletedAt = DateTimeOffset.UtcNow
        }, JsonOptions), ct);
    }

    private static string Sign(string payload, string secret)
    {
        if (string.IsNullOrEmpty(secret))
        {
            return "";
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }

    private Task SendHeartbeatAsync(CancellationToken ct)
    {
        return PostRegistryAsync("/health/worker-heartbeat", new
        {
            workerId = Environment.MachineName,
            queues = new[] { "package-scan", "reproducible-build", "webhook", "compatibility-test" },
            seenAt = DateTimeOffset.UtcNow
        }, ct);
    }

    private async Task PostRegistryAsync(string path, object payload, CancellationToken ct)
    {
        var registry = _configuration["Worker:RegistryBaseUrl"];
        if (string.IsNullOrWhiteSpace(registry))
        {
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{registry.TrimEnd('/')}{path}")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json")
        };
        var apiKey = _configuration["Worker:RegistryApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.Add("X-Api-Key", apiKey);
        }

        try
        {
            await _http.SendAsync(request, ct);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Registry callback {Path} failed", path);
        }
    }
}
