using Microsoft.EntityFrameworkCore;
using TraceIot.Alarms;
using TraceIot.Devices;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.PermissionManagement;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement;
using Volo.Abp.SettingManagement.EntityFrameworkCore;

namespace TraceIot.EntityFrameworkCore;

/// <summary>
/// 统一 DbContext：合并业务表 + ABP Identity + Permission + Setting，
/// 只需一套 Migration 即可建全所有表。
/// </summary>
[ConnectionStringName("Default")]
public class TraceIotDbContext
    : AbpDbContext<TraceIotDbContext>,
      IIdentityDbContext,
      IPermissionManagementDbContext,
      ISettingManagementDbContext
{
    // ── 业务表 ────────────────────────────────────────────
    public DbSet<Device> Devices { get; set; } = null!;
    public DbSet<DeviceGroup> DeviceGroups { get; set; } = null!;
    public DbSet<AlarmRecord> AlarmRecords { get; set; } = null!;

    // ── ABP Identity 表 ───────────────────────────────────
    public DbSet<IdentityUser> Users { get; set; } = null!;
    public DbSet<IdentityRole> Roles { get; set; } = null!;
    public DbSet<IdentityClaimType> ClaimTypes { get; set; } = null!;
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; } = null!;
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; } = null!;
    public DbSet<IdentityLinkUser> LinkUsers { get; set; } = null!;
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; } = null!;
    public DbSet<IdentitySession> Sessions { get; set; } = null!;

    // ── ABP PermissionManagement 表 ───────────────────────
    public DbSet<PermissionGroupDefinitionRecord> PermissionGroups { get; set; } = null!;
    public DbSet<PermissionDefinitionRecord> Permissions { get; set; } = null!;
    public DbSet<PermissionGrant> PermissionGrants { get; set; } = null!;

    // ── ABP SettingManagement 表 ──────────────────────────
    public DbSet<Setting> Settings { get; set; } = null!;
    public DbSet<SettingDefinitionRecord> SettingDefinitionRecords { get; set; } = null!;

    public TraceIotDbContext(DbContextOptions<TraceIotDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ConfigureIdentity();
        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();

        // 设备分组
        builder.Entity<DeviceGroup>(b =>
        {
            b.ToTable("device_groups");
            b.Property(x => x.Name).IsRequired().HasMaxLength(64);
            b.Property(x => x.Description).HasMaxLength(256);
        });

        // 设备
        builder.Entity<Device>(b =>
        {
            b.ToTable("devices");
            b.Property(x => x.DeviceCode).IsRequired().HasMaxLength(64);
            b.HasIndex(x => x.DeviceCode).IsUnique();
            b.Property(x => x.DeviceName).IsRequired().HasMaxLength(128);
            b.Property(x => x.Remark).HasMaxLength(512);
            b.Property(x => x.Status).HasConversion<short>();
            b.Property(x => x.GeofenceConfig).HasColumnType("jsonb");
            b.Property(x => x.LastLat).HasColumnType("decimal(10,7)");
            b.Property(x => x.LastLng).HasColumnType("decimal(10,7)");
            b.Property(x => x.LastSpeed).HasColumnType("decimal(8,2)");
            b.Property(x => x.LastDirection).HasColumnType("decimal(6,2)");
        });

        // 告警记录
        builder.Entity<AlarmRecord>(b =>
        {
            b.ToTable("alarm_records");
            b.Property(x => x.DeviceCode).IsRequired().HasMaxLength(64);
            b.Property(x => x.AlarmType).HasConversion<short>();
            b.Property(x => x.AlarmValue).HasColumnType("decimal(10,4)");
            b.Property(x => x.Lat).HasColumnType("decimal(10,7)");
            b.Property(x => x.Lng).HasColumnType("decimal(10,7)");
        });
    }
}
