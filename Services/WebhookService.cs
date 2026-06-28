using cerberus_securitygate.Data;
using cerberus_securitygate.DTOs;
using cerberus_securitygate.Models;
using Microsoft.EntityFrameworkCore;

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
                LineStart = f.LineStart,
                LineEnd = f.LineEnd,
                Recommendation = f.Recommendation
            });
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Processed scan-result from {ServiceId} for scan {ScanId} — status: {Status}, findings: {Count}",
            dto.ServiceId, dto.ScanId, dto.Status, dto.Findings.Count);

        return true;
    }
}