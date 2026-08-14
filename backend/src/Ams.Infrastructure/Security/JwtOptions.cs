using System.ComponentModel.DataAnnotations;

namespace Ams.Infrastructure.Security;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// HMAC signing key, supplied via configuration/environment — never committed.
    /// Must be at least 32 bytes for HS256.
    /// </summary>
    [Required, MinLength(32)]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int AccessTokenMinutes { get; set; } = 60;
}
