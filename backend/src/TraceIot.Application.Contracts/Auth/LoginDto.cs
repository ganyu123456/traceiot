using System.ComponentModel.DataAnnotations;

namespace TraceIot.Auth;

public class LoginDto
{
    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResultDto
{
    public string Token { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime ExpiresAt { get; set; }
}
