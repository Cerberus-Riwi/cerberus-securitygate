namespace cerberus_securitygate.DTOs;

public class ScanStatusResponseDto
{
    public Guid ScanId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; set; }
    public List<string> Services { get; set; } = new();
}