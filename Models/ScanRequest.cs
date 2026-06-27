using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cerberus_securitygate.Models;

[Table("scan_requests", Schema = "cerberus")]
public class ScanRequest
{
    [Column("scan_id")]
    public Guid ScanId { get; set; }

    [Column("repository_url")]
    public string RepositoryUrl { get; set; } = string.Empty;

    [Column("branch")]
    public string Branch { get; set; } = string.Empty;

    [Column("commit_hash")]
    public string CommitHash { get; set; } = string.Empty;

    [Column("requested_at")]
    public DateTimeOffset RequestedAt { get; set; }

    [Column("pr_number")]
    public int? PrNumber { get; set; }

    [Column("triggered_by")]
    public string? TriggeredBy { get; set; }

    [Column("received_at")]
    public DateTimeOffset ReceivedAt { get; set; }
}