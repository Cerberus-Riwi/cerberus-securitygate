using cerberus_securitygate.Data;
using cerberus_securitygate.DTOs;
using cerberus_securitygate.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace cerberus_securitygate.Services;

public class WebhookService
{
    private readonly CerberusDbContext _db;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(CerberusDbContext db, ILogger<WebhookService> logger)
    {
        _db = db;
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

        var alreadyProcessed = await _db.ScanResults
            .AnyAsync(r => r.ScanId == dto.ScanId && r.ServiceId == dto.ServiceId);

        if (alreadyProcessed)
        {
            _logger.LogInformation(
                "Scan-result from {ServiceId} for scan {ScanId} already processed — skipping",
                dto.ServiceId, dto.ScanId);
            return true;
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
            return true;
        }

        _logger.LogInformation(
            "Processed scan-result from {ServiceId} for scan {ScanId} — status: {Status}, findings: {Count}",
            dto.ServiceId, dto.ScanId, dto.Status, dto.Findings.Count);

        return true;
    }
}