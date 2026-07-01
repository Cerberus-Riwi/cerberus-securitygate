using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cerberus_securitygate.Models;

[Table("users", Schema = "cerberus")]
public class User
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("email")]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Column("password_hash")]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("role")]
    [MaxLength(20)]
    public string Role { get; set; } = "user";

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("last_login_at")]
    public DateTimeOffset? LastLoginAt { get; set; }
}
