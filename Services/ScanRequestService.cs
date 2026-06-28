using cerberus_securitygate.Data;
using cerberus_securitygate.DTOs;
using cerberus_securitygate.Models;

namespace cerberus_securitygate.Services;

public class ScanRequestService
{
    private readonly CerberusDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<ScanRequestService> _logger;

    public ScanRequestService(CerberusDbContext db, IConfiguration config, ILogger<ScanRequestService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    public async Task<ScanRequestResponseDto> CreateAsync(CreateScanRequestDto dto)
    {
        var scanRequest = new ScanRequest
        {
            ScanId = Guid.NewGuid(),
            RepositoryUrl = dto.RepositoryUrl,
            Branch = dto.Branch,
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
            await using var publisher = await ScanRequestPublisher.CreateAsync(host, port, user, password);
            await publisher.PublishAsync(scanRequest);
            _logger.LogInformation("Published scan-request {ScanId} to RabbitMQ", scanRequest.ScanId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish scan-request {ScanId} to RabbitMQ", scanRequest.ScanId);
        }
    }
}