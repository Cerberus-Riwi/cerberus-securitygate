using cerberus_securitygate.DTOs;
using cerberus_securitygate.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace cerberus_securitygate.Controllers;

[ApiController]
[Route("api/scan")]
public class ScanController : ControllerBase
{
    private readonly ScanRequestService _scanRequestService;
    private readonly ScanStatusService _scanStatusService;
    private readonly WebhookService _webhookService;
    private readonly IConfiguration _config;
    private readonly UrlSafetyValidator _urlValidator;

    public ScanController(
            ScanRequestService scanRequestService,
            ScanStatusService scanStatusService,
            WebhookService webhookService,
            IConfiguration config,
            UrlSafetyValidator urlValidator)
    {
        _scanRequestService = scanRequestService;
        _scanStatusService = scanStatusService;
        _webhookService = webhookService;
        _config = config;
        _urlValidator = urlValidator;
    }

    [HttpPost("request")]
    [EnableRateLimiting("per-ip")]
    public async Task<IActionResult> CreateScanRequest([FromBody] CreateScanRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!_urlValidator.IsSafe(dto.RepositoryUrl))
            return BadRequest(new { error = "repositoryUrl points to a forbidden or unreachable destination" });

        var response = await _scanRequestService.CreateAsync(dto);
        return StatusCode(201, response);
    }

    [HttpGet("list")]
    public async Task<IActionResult> ListScans()
    {
        var items = await _scanStatusService.GetListAsync();
        return Ok(items);
    }

    [HttpGet("{id}/status")]
    public async Task<IActionResult> GetScanStatus(Guid id)
    {
        var result = await _scanStatusService.GetStatusAsync(id);

        if (result is null)
            return NotFound(new { error = $"Scan {id} not found" });

        return Ok(result);
    }

    [HttpGet("{id}/details")]
    public async Task<IActionResult> GetScanDetails(Guid id)
    {
        var result = await _scanStatusService.GetDetailsAsync(id);

        if (result is null)
            return NotFound(new { error = $"Scan {id} not found" });

        return Ok(result);
    }

    [HttpPost("webhook/result")]
    public async Task<IActionResult> ReceiveScanResult([FromBody] WebhookScanResultDto dto)
    {
        var token = _config["Webhook:InternalToken"];
        var provided = Request.Headers["X-Internal-Token"].FirstOrDefault();

        if (string.IsNullOrEmpty(token) || provided != token)
            return Unauthorized(new { error = "Invalid or missing X-Internal-Token" });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var found = await _webhookService.ProcessScanResultAsync(dto);

        if (!found)
            return NotFound(new { error = $"Scan {dto.ScanId} not found" });

        return Ok(new { message = "scan-result processed", scanId = dto.ScanId });
    }
}