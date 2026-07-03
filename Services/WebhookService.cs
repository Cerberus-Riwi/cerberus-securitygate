using cerberus_securitygate.Data;
using cerberus_securitygate.DTOs;
using cerberus_securitygate.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace cerberus_securitygate.Services;

public class WebhookService
{
    private readonly CerberusDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<WebhookService> _logger;
    private const string Exchange = "cerberus.findings.ready";

    public WebhookService(CerberusDbContext db, IConfiguration config, ILogger<WebhookService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    public async Task<bool> ProcessScanResultAsync(WebhookScanResultDto dto)
    {
        var scanExists = await _db.ScanRequests
            .AnyAsync(s => s.ScanId == dto.ScanId);

        if (!scanExists)
        {
            _logger.LogWarning("Webhook received for unknown scanId {ScanId}", dto.ScanId);
            return false;
        }

        await PersistScanResultAsync(dto);
        await TryPublishFindingsReadyAsync(dto);

        return true;
    }

    private async Task PersistScanResultAsync(WebhookScanResultDto dto)
    {
        var alreadyProcessed = await _db.ScanResults
            .AnyAsync(r => r.ScanId == dto.ScanId && r.ServiceId == dto.ServiceId);

        if (alreadyProcessed)
        {
            _logger.LogInformation(
                "Scan-result from {ServiceId} for scan {ScanId} already processed — skipping",
                dto.ServiceId, dto.ScanId);
            return;
        }

        var scanResult = new ScanResult
        {
            Id = Guid.NewGuid(),
            ScanId = dto.ScanId,
            ServiceId = dto.ServiceId,
            Status = dto.Status,
            ErrorMessage = dto.ErrorMessage,
            CompletedAt = dto.CompletedAt,
            ReceivedAt = DateTimeOffset.UtcNow
        };

        _db.ScanResults.Add(scanResult);

        foreach (var f in dto.Findings)
        {
            _db.Findings.Add(new Finding
            {
                Id = f.Id,
                ScanResultId = scanResult.Id,
                Severity = f.Severity,
                Title = f.Title,
                Description = f.Description,
                RuleId = f.RuleId,
                FilePath = f.FilePath,
                LocationUrl = f.LocationUrl,
                LineStart = f.LineStart,
                LineEnd = f.LineEnd,
                Recommendation = f.Recommendation
            });
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            _logger.LogInformation(
                "Scan-result from {ServiceId} for scan {ScanId} already existed (concurrent) — treated as processed",
                dto.ServiceId, dto.ScanId);
            return;
        }

        _logger.LogInformation(
            "Processed scan-result from {ServiceId} for scan {ScanId} — status: {Status}, findings: {Count}",
            dto.ServiceId, dto.ScanId, dto.Status, dto.Findings.Count);
    }

    private async Task TryPublishFindingsReadyAsync(WebhookScanResultDto dto)
    {
        var host = _config["RabbitMQ:Host"];
        var port = int.TryParse(_config["RabbitMQ:Port"], out var p) ? p : 5672;
        var user = _config["RabbitMQ:User"] ?? "guest";
        var password = _config["RabbitMQ:Password"] ?? "guest";

        if (string.IsNullOrEmpty(host))
        {
            _logger.LogWarning("RabbitMQ:Host not configured — skipping findings.ready publish for scan {ScanId}", dto.ScanId);
            return;
        }

        try
        {
            await using var publisher = await RabbitMqPublisher.CreateAsync(host, port, user, password, Exchange);
            await publisher.PublishAsync(BuildMessage(dto), dto.ScanId.ToString());
            _logger.LogInformation("Published scan-result {ScanId}/{ServiceId} to {Exchange}", dto.ScanId, dto.ServiceId, Exchange);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish findings.ready for scan {ScanId} — result persisted, broker fan-out skipped", dto.ScanId);
        }
    }

    private static object BuildMessage(WebhookScanResultDto dto) => new
    {
        scanId = dto.ScanId,
        serviceId = dto.ServiceId,
        status = dto.Status,
        findings = dto.Findings.Select(f => new
        {
            id = f.Id,
            severity = f.Severity,
            title = f.Title,
            description = f.Description,
            ruleId = f.RuleId,
            filePath = f.FilePath,
            locationUrl = f.LocationUrl,
            lineStart = f.LineStart,
            lineEnd = f.LineEnd,
            recommendation = f.Recommendation
        }),
        completedAt = dto.CompletedAt,
        errorMessage = dto.ErrorMessage
    };
}