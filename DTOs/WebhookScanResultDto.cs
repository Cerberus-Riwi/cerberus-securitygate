using System.ComponentModel.DataAnnotations;

namespace cerberus_securitygate.DTOs;

public class WebhookScanResultDto
{
    [Required]
    public Guid ScanId { get; set; }

    [Required]
    public string ServiceId { get; set; } = string.Empty;

    [Required]
    public string Status { get; set; } = string.Empty;

    public List<FindingDto> Findings { get; set; } = new();

    [Required]
    public DateTimeOffset CompletedAt { get; set; }

    public string? ErrorMessage { get; set; }
}

public class FindingDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public string Severity { get; set; } = string.Empty;

    [Required]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public string RuleId { get; set; } = string.Empty;

    [Required]
    public string FilePath { get; set; } = string.Empty;

    public int? LineStart { get; set; }
    public int? LineEnd { get; set; }
    public string? Recommendation { get; set; }
}