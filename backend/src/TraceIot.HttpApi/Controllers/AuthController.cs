using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TraceIot.Auth;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Identity;

namespace TraceIot.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : AbpControllerBase
{
    private readonly IdentityUserManager _userManager;
    private readonly IConfiguration _config;

    public AuthController(IdentityUserManager userManager, IConfiguration config)
    {
        _userManager = userManager;
        _config      = config;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto input)
    {
        var user = await _userManager.FindByNameAsync(input.UserName);
        if (user == null)
            return Unauthorized(new { message = "用户名或密码错误" });

        var passwordValid = await _userManager.CheckPasswordAsync(user, input.Password);
        if (!passwordValid)
            return Unauthorized(new { message = "用户名或密码错误" });

        var roles = await _userManager.GetRolesAsync(user);

        var jwtKey    = _config["Jwt:Key"]!;
        var issuer    = _config["Jwt:Issuer"]!;
        var audience  = _config["Jwt:Audience"]!;
        var expHours  = int.Parse(_config["Jwt:ExpirationHours"] ?? "24");
        var expiresAt = DateTime.UtcNow.AddHours(expHours);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name,           user.UserName ?? ""),
            new(ClaimTypes.Email,          user.Email    ?? ""),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token       = new JwtSecurityToken(issuer, audience, claims,
                              expires: expiresAt, signingCredentials: credentials);

        return Ok(new LoginResultDto
        {
            Token     = new JwtSecurityTokenHandler().WriteToken(token),
            UserName  = user.UserName ?? "",
            Email     = user.Email,
            Roles     = roles.ToList(),
            ExpiresAt = expiresAt
        });
    }

    [HttpGet("profile")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user  = await _userManager.GetByIdAsync(Guid.Parse(userId));
        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            id       = user.Id,
            userName = user.UserName,
            email    = user.Email,
            name     = user.Name,
            roles
        });
    }
}
