namespace cerberus_securitygate.Configuration;

public class ZapScanOptions
{
    public const string SectionName = "ZapScan";

    public List<TargetUrlMapping> TargetUrls { get; set; } = new();
}

public class TargetUrlMapping
{
    public string RepositoryUrl { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
}
