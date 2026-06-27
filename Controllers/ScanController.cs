using cerberus_securitygate.DTOs;
using cerberus_securitygate.Services;
using Microsoft.AspNetCore.Mvc;

namespace cerberus_securitygate.Controllers;

[ApiController]
[Route("api/scan")]
public class ScanController : ControllerBase
{
    private readonly ScanRequestService _service;

    public ScanController(ScanRequestService service)
    {
        _service = service;
    }

    [HttpPost("request")]
    public async Task<IActionResult> CreateScanRequest([FromBody] CreateScanRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var response = await _service.CreateAsync(dto);
        return StatusCode(201, response);
    }
}