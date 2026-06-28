using System.ComponentModel.DataAnnotations.Schema;

namespace cerberus_securitygate.Models;

[Table("scan_results", Schema = "cerberus")]
public class ScanResult
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("scan_id")]
    public Guid ScanId { get; set; }

    [Column("service_id")]
    public string ServiceId { get; set; } = string.Empty;

    [Column("status")]
    public string Status { get; set; } = string.Empty;

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("completed_at")]
    public DateTimeOffset CompletedAt { get; set; }

    [Column("received_at")]
    public DateTimeOffset ReceivedAt { get; set; }

    public List<Finding> Findings { get; set; } = new();
}