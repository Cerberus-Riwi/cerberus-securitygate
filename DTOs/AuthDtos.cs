using System.ComponentModel.DataAnnotations;

namespace cerberus_securitygate.DTOs;

public class LoginRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public UserProfileDto User { get; set; } = default!;
}

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
