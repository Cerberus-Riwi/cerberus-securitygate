using cerberus_securitygate.Data;
using cerberus_securitygate.DTOs;
using cerberus_securitygate.Models;

namespace cerberus_securitygate.Services;

public class ScanRequestService
{
    private readonly CerberusDbContext _db;

    public ScanRequestService(CerberusDbContext db)
    {
        _db = db;
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

        return new ScanRequestResponseDto
        {
            ScanId = scanRequest.ScanId,
            Status = "pending",
            ReceivedAt = scanRequest.ReceivedAt
        };
    }
}