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

        string status;
        if (results.Count == 0)
            status = "pending";
        else if (results.All(r => r.Status is "success" or "failed" or "timeout"))
            status = "completed";
        else
            status = "running";

        return new ScanStatusResponseDto
        {
            ScanId = scanId,
            Status = status,
            ReceivedAt = scanRequest.ReceivedAt,
            Services = results.Select(r => r.ServiceId).ToList()
        };
    }
}