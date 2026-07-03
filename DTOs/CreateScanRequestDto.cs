using System.ComponentModel.DataAnnotations;

namespace cerberus_securitygate.DTOs;

public class CreateScanRequestDto
{
    [Required]
    [RegularExpression(@"^https://github\.com/.+/.+$",
        ErrorMessage = "repositoryUrl must be a valid GitHub HTTPS URL")]
    public string RepositoryUrl { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [MaxLength(255)]
    public string Branch { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^[0-9a-f]{40}$",
        ErrorMessage = "commitHash must be a 40-character lowercase hex SHA-1")]
    public string CommitHash { get; set; } = string.Empty;

    // Optional: URL that ZAP should scan, chosen by the caller (e.g. the frontend).
    // When null/empty, ScanRequestService falls back to the ZapScan:TargetUrls
    // ConfigMap mapping. Only format is validated here; SSRF safety is enforced
    // in the controller via UrlSafetyValidator.
    [RegularExpression(@"^https?://.+$",
        ErrorMessage = "targetUrl must be an absolute http/https URL")]
    public string? TargetUrl { get; set; }

    [Required]
    public DateTimeOffset RequestedAt { get; set; }

    [Range(1, int.MaxValue)]
    public int? PrNumber { get; set; }

    [MaxLength(100)]
    public string? TriggeredBy { get; set; }
}