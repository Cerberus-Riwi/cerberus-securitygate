using cerberus_securitygate.Data;
using cerberus_securitygate.DTOs;
using Microsoft.EntityFrameworkCore;

namespace cerberus_securitygate.Services;

public class ScanStatusService
{
    private readonly CerberusDbContext _db;

    public ScanStatusService(CerberusDbContext db)
    {
        _db = db;
    }

    public async Task<ScanStatusResponseDto?> GetStatusAsync(Guid scanId)
    {
        var scanRequest = await _db.ScanRequests
            .FirstOrDefaultAsync(s => s.ScanId == scanId);

        if (scanRequest is null)
            return null;

        var results = await _db.ScanResults
            .Where(r => r.ScanId == scanId)
            .ToListAsync();

        return new ScanStatusResponseDto
        {
            ScanId    = scanId,
            Status    = ComputeStatus(results),
            ReceivedAt = scanRequest.ReceivedAt,
            Services  = results.Select(r => r.ServiceId).ToList()
        };
    }

    public async Task<List<ScanListItemDto>> GetListAsync()
    {
        var requests = await _db.ScanRequests
            .OrderByDescending(s => s.RequestedAt)
            .ToListAsync();

        var scanIds = requests.Select(r => r.ScanId).ToList();

        var results = await _db.ScanResults
            .Where(r => scanIds.Contains(r.ScanId))
            .Include(r => r.Findings)
            .ToListAsync();

        var resultsByScan = results
            .GroupBy(r => r.ScanId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return requests.Select(req =>
        {
            var scanResults = resultsByScan.GetValueOrDefault(req.ScanId, []);
            var allFindings = scanResults.SelectMany(r => r.Findings).ToList();

            ScanSummaryDto? summary = null;
            if (allFindings.Count > 0)
            {
                summary = new ScanSummaryDto
                {
                    Critical = allFindings.Count(f => f.Severity == "critical"),
                    High     = allFindings.Count(f => f.Severity == "high"),
                    Medium   = allFindings.Count(f => f.Severity == "medium"),
                    Low      = allFindings.Count(f => f.Severity == "low"),
                    Info     = allFindings.Count(f => f.Severity == "info"),
                };
            }

            return new ScanListItemDto
            {
                ScanId        = req.ScanId,
                RepositoryUrl = req.RepositoryUrl,
                Branch        = req.Branch,
                Status        = ComputeStatus(scanResults),
                TriggeredBy   = req.TriggeredBy,
                RequestedAt   = req.RequestedAt,
                Summary       = summary,
            };
        }).ToList();
    }

    public async Task<ScanDetailsDto?> GetDetailsAsync(Guid scanId)
    {
        var request = await _db.ScanRequests
            .FirstOrDefaultAsync(s => s.ScanId == scanId);

        if (request is null)
            return null;

        var results = await _db.ScanResults
            .Where(r => r.ScanId == scanId)
            .Include(r => r.Findings)
            .ToListAsync();

        var allFindings = results.SelectMany(r => r.Findings).ToList();

        var summary = new ScanSummaryDto
        {
            Critical = allFindings.Count(f => f.Severity == "critical"),
            High     = allFindings.Count(f => f.Severity == "high"),
            Medium   = allFindings.Count(f => f.Severity == "medium"),
            Low      = allFindings.Count(f => f.Severity == "low"),
            Info     = allFindings.Count(f => f.Severity == "info"),
        };

        var resultDtos = results.Select(r => new ScanResultDetailDto
        {
            ServiceId    = r.ServiceId,
            Status       = r.Status,
            CompletedAt  = r.CompletedAt,
            ErrorMessage = r.ErrorMessage,
            Findings     = r.Findings.Select(f => new FindingDto
            {
                Id             = f.Id,
                Severity       = f.Severity,
                Title          = f.Title,
                Description    = f.Description,
                RuleId         = f.RuleId,
                FilePath       = f.FilePath,
                LocationUrl    = f.LocationUrl,
                LineStart      = f.LineStart,
                LineEnd        = f.LineEnd,
                Recommendation = f.Recommendation,
            }).ToList(),
        }).ToList();

        return new ScanDetailsDto
        {
            ScanId        = request.ScanId,
            RepositoryUrl = request.RepositoryUrl,
            Branch        = request.Branch,
            Status        = ComputeStatus(results),
            TriggeredBy   = request.TriggeredBy,
            RequestedAt   = request.RequestedAt,
            Summary       = summary,
            Results       = resultDtos,
        };
    }

    private static string ComputeStatus(List<Models.ScanResult> results)
    {
        if (results.Count == 0) return "pending";
        if (results.All(r => r.Status is "success" or "failed" or "timeout")) return "completed";
        return "running";
    }
}
