using cerberus_securitygate.DTOs;
using cerberus_securitygate.Services;
using Microsoft.AspNetCore.Mvc;

namespace cerberus_securitygate.Controllers;

[ApiController]
[Route("api/scan")]
public class ScanController : ControllerBase
{
    private readonly ScanRequestService _scanRequestService;
    private readonly ScanStatusService _scanStatusService;

    public ScanController(ScanRequestService scanRequestService, ScanStatusService scanStatusService)
    {
        _scanRequestService = scanRequestService;
        _scanStatusService = scanStatusService;
    }

    [HttpPost("request")]
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
}