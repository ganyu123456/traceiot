using Microsoft.Extensions.Logging;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.Identity;

namespace TraceIot;

/// <summary>数据库初始种子：创建默认 admin 用户和角色</summary>
public class TraceIotDataSeeder : IDataSeedContributor, ITransientDependency
{
    private readonly IdentityUserManager  _userManager;
    private readonly IdentityRoleManager  _roleManager;
    private readonly IGuidGenerator       _guidGenerator;
    private readonly ILogger<TraceIotDataSeeder> _logger;

    public TraceIotDataSeeder(
        IdentityUserManager userManager,
        IdentityRoleManager roleManager,
        IGuidGenerator guidGenerator,
        ILogger<TraceIotDataSeeder> logger)
    {
        _userManager   = userManager;
        _roleManager   = roleManager;
        _guidGenerator = guidGenerator;
        _logger        = logger;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        await EnsureRoleAsync("admin",    "系统管理员");
        await EnsureRoleAsync("operator", "操作员");

        await EnsureUserAsync("admin", "Admin@123456", "admin@traceiot.com", "系统管理员", "admin");
    }

    private async Task EnsureRoleAsync(string roleName, string description)
    {
        if (await _roleManager.RoleExistsAsync(roleName)) return;

        var role = new IdentityRole(_guidGenerator.Create(), roleName)
        {
            IsPublic = true,
            IsStatic = true
        };
        var result = await _roleManager.CreateAsync(role);
        if (result.Succeeded)
            _logger.LogInformation("创建角色: {Role}", roleName);
    }

    private async Task EnsureUserAsync(string userName, string password, string email, string name, string role)
    {
        if (await _userManager.FindByNameAsync(userName) != null) return;

        var user = new IdentityUser(_guidGenerator.Create(), userName, email)
        {
            Name = name
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            _logger.LogError("创建用户失败: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        await _userManager.AddToRoleAsync(user, role);
        _logger.LogInformation("创建用户: {UserName}，密码: {Password}", userName, password);
    }
}
