using cerberus_securitygate.Configuration;

namespace cerberus_securitygate.Services;

public class TargetUrlResolver
{
    private readonly Dictionary<string, string> _targetsByRepository;

    public TargetUrlResolver(IConfiguration config)
    {
        var options = config.GetSection(ZapScanOptions.SectionName).Get<ZapScanOptions>()
            ?? new ZapScanOptions();

        _targetsByRepository = new Dictionary<string, string>();
        foreach (var mapping in options.TargetUrls)
            _targetsByRepository[Normalize(mapping.RepositoryUrl)] = mapping.TargetUrl;
    }

    public string? Resolve(string repositoryUrl)
        => _targetsByRepository.GetValueOrDefault(Normalize(repositoryUrl));

    private static string Normalize(string repositoryUrl)
        => repositoryUrl.TrimEnd('/');
}
