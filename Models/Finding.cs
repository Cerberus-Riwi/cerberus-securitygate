using System.ComponentModel.DataAnnotations.Schema;

namespace cerberus_securitygate.Models;

[Table("findings", Schema = "cerberus")]
public class Finding
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("scan_result_id")]
    public Guid ScanResultId { get; set; }

    [Column("severity")]
    public string Severity { get; set; } = string.Empty;

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("rule_id")]
    public string RuleId { get; set; } = string.Empty;

    [Column("file_path")]
    public string? FilePath { get; set; }

    [Column("location_url")]
    public string? LocationUrl { get; set; }

    [Column("line_start")]
    public int? LineStart { get; set; }

    [Column("line_end")]
    public int? LineEnd { get; set; }

    [Column("recommendation")]
    public string? Recommendation { get; set; }
}