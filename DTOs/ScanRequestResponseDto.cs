namespace cerberus_securitygate.DTOs;

public class ScanRequestResponseDto
{
    public Guid ScanId { get; set; }
    public string Status { get; set; } = "pending";
    public DateTimeOffset ReceivedAt { get; set; }
}