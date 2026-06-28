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

    public ScanController(
        ScanRequestService scanRequestService,
        ScanStatusService scanStatusService,
        WebhookService webhookService,
        IConfiguration config)
    {
        _scanRequestService = scanRequestService;
        _scanStatusService = scanStatusService;
        _webhookService = webhookService;
        _config = config;
    }

    [HttpPost("request")]

    [EnableRateLimiting("per-ip")]
    public async Task<IActionResult> CreateScanRequest([FromBody] CreateScanRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var response = await _scanRequestService.CreateAsync(dto);
        return StatusCode(201, response);
    }

    [HttpGet("{id}/status")]
    public async Task<IActionResult> GetScanStatus(Guid id)
    {
        var result = await _scanStatusService.GetStatusAsync(id);

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