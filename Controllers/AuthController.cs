using cerberus_securitygate.Data;
using cerberus_securitygate.DTOs;
using cerberus_securitygate.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BC = BCrypt.Net.BCrypt;

namespace cerberus_securitygate.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly CerberusDbContext _db;
    private readonly JwtService _jwt;
    private readonly ILogger<AuthController> _logger;

    public AuthController(CerberusDbContext db, JwtService jwt, ILogger<AuthController> logger)
    {
        _db     = db;
        _jwt    = jwt;
        _logger = logger;
    }

    /// <summary>Login con email y contraseña. Devuelve JWT.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower());

        if (user is null || !BC.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { error = "Credenciales incorrectas." });

        if (!user.IsActive)
            return Unauthorized(new { error = "Cuenta deshabilitada. Contacta al administrador." });

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        var (token, expiresAt) = _jwt.Generate(user);

        _logger.LogInformation("Login exitoso: {Email} ({Role})", user.Email, user.Role);

        return Ok(new LoginResponseDto
        {
            Token     = token,
            ExpiresAt = expiresAt,
            User      = new UserProfileDto { Id = user.Id, Email = user.Email, Role = user.Role },
        });
    }

    /// <summary>Devuelve el perfil del usuario autenticado (valida el JWT).</summary>
    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Me()
    {
        var id    = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        var email = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)?.Value;
        var role  = User.FindFirst("role")?.Value;

        if (id is null || email is null || role is null)
            return Unauthorized();

        // Traefik ForwardAuth lee estos headers y los propaga a los servicios internos
        Response.Headers["X-User-Id"]    = id;
        Response.Headers["X-User-Email"] = email;
        Response.Headers["X-User-Role"]  = role;

        return Ok(new UserProfileDto { Id = Guid.Parse(id), Email = email, Role = role });
    }
}
