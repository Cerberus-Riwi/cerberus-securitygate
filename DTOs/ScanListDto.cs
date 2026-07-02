namespace cerberus_securitygate.DTOs;

public class ScanSummaryDto
{
    public int Critical { get; set; }
    public int High     { get; set; }
    public int Medium   { get; set; }
    public int Low      { get; set; }
    public int Info     { get; set; }
}

public class ScanListItemDto
{
    public Guid            ScanId        { get; set; }
    public string          RepositoryUrl { get; set; } = string.Empty;
    public string          Branch        { get; set; } = string.Empty;
    public string          Status        { get; set; } = string.Empty;
    public string?         TriggeredBy   { get; set; }
    public DateTimeOffset  RequestedAt   { get; set; }
    public ScanSummaryDto? Summary       { get; set; }
}

public class FindingDto
{
    public Guid    Id             { get; set; }
    public string  Severity       { get; set; } = string.Empty;
    public string  Title          { get; set; } = string.Empty;
    public string? Description    { get; set; }
    public string  RuleId         { get; set; } = string.Empty;
    public string? FilePath       { get; set; }
    public string? LocationUrl    { get; set; }
    public int?    LineStart      { get; set; }
    public int?    LineEnd        { get; set; }
    public string? Recommendation { get; set; }
}

public class ScanResultDetailDto
{
    public string         ServiceId    { get; set; } = string.Empty;
    public string         Status       { get; set; } = string.Empty;
    public DateTimeOffset CompletedAt  { get; set; }
    public string?        ErrorMessage { get; set; }
    public List<FindingDto> Findings   { get; set; } = new();
}

public class ScanDetailsDto
{
    public Guid            ScanId        { get; set; }
    public string          RepositoryUrl { get; set; } = string.Empty;
    public string          Branch        { get; set; } = string.Empty;
    public string          Status        { get; set; } = string.Empty;
    public string?         TriggeredBy   { get; set; }
    public DateTimeOffset  RequestedAt   { get; set; }
    public ScanSummaryDto  Summary       { get; set; } = new();
    public List<ScanResultDetailDto> Results { get; set; } = new();
}
