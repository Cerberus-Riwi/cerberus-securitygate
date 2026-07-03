using cerberus_securitygate.Data;
using cerberus_securitygate.DTOs;
using cerberus_securitygate.Models;

namespace cerberus_securitygate.Services;

public class ScanRequestService
{
    private readonly CerberusDbContext _db;
    private readonly IConfiguration _config;
    private readonly TargetUrlResolver _targetUrlResolver;
    private readonly ILogger<ScanRequestService> _logger;
    private const string Exchange = "cerberus.scan.requests";

    public ScanRequestService(
        CerberusDbContext db,
        IConfiguration config,
        TargetUrlResolver targetUrlResolver,
        ILogger<ScanRequestService> logger)
    {
        _db = db;
        _config = config;
        _targetUrlResolver = targetUrlResolver;
        _logger = logger;
    }

    public async Task<ScanRequestResponseDto> CreateAsync(CreateScanRequestDto dto)
    {
        var scanRequest = new ScanRequest
        {
            ScanId = Guid.NewGuid(),
            RepositoryUrl = dto.RepositoryUrl,
            Branch = dto.Branch,
            // Prefer the caller-supplied target URL; fall back to the static
            // ZapScan:TargetUrls ConfigMap mapping when the DTO omits it. This
            // keeps the existing deployment working until the frontend always
            // sends targetUrl, at which point the resolver fallback can be dropped.
            TargetUrl = string.IsNullOrWhiteSpace(dto.TargetUrl)
                ? _targetUrlResolver.Resolve(dto.RepositoryUrl)
                : dto.TargetUrl,
            CommitHash = dto.CommitHash,
            RequestedAt = dto.RequestedAt,
            PrNumber = dto.PrNumber,
            TriggeredBy = dto.TriggeredBy,
            ReceivedAt = DateTimeOffset.UtcNow
        };

        _db.ScanRequests.Add(scanRequest);
        await _db.SaveChangesAsync();

        await TryPublishAsync(scanRequest);

        return new ScanRequestResponseDto
        {
            ScanId = scanRequest.ScanId,
            Status = "pending",
            ReceivedAt = scanRequest.ReceivedAt
        };
    }

    private async Task TryPublishAsync(ScanRequest scanRequest)
    {
        var host = _config["RabbitMQ:Host"];
        var port = int.TryParse(_config["RabbitMQ:Port"], out var p) ? p : 5672;
        var user = _config["RabbitMQ:User"] ?? "guest";
        var password = _config["RabbitMQ:Password"] ?? "guest";

        if (string.IsNullOrEmpty(host))
        {
            _logger.LogWarning("RabbitMQ:Host not configured — skipping publish for scan {ScanId}", scanRequest.ScanId);
            return;
        }

        try
        {
            await using var publisher = await RabbitMqPublisher.CreateAsync(host, port, user, password, Exchange);
            await publisher.PublishAsync(BuildMessage(scanRequest), scanRequest.ScanId.ToString());
            _logger.LogInformation("Published scan-request {ScanId} to RabbitMQ", scanRequest.ScanId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish scan-request {ScanId} to RabbitMQ", scanRequest.ScanId);
        }
    }

    private static object BuildMessage(ScanRequest scanRequest)
    {
        object? metadata = scanRequest.PrNumber is null && scanRequest.TriggeredBy is null
            ? null
            : new
            {
                prNumber = scanRequest.PrNumber,
                triggeredBy = scanRequest.TriggeredBy
            };

        return new
        {
            scanId = scanRequest.ScanId,
            repositoryUrl = scanRequest.RepositoryUrl,
            branch = scanRequest.Branch,
            commitHash = scanRequest.CommitHash,
            targetUrl = scanRequest.TargetUrl,
            requestedAt = scanRequest.RequestedAt,
            metadata
        };
    }
}