using InfluxDB.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Text;
using TraceIot.EntityFrameworkCore;
using TraceIot.MqttWorker;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;
using Volo.Abp.Swashbuckle;

namespace TraceIot;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreMvcModule),
    typeof(AbpSwashbuckleModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(TraceIotHttpApiModule),
    typeof(TraceIotApplicationModule),
    typeof(TraceIotEntityFrameworkCoreModule),
    typeof(TraceIotMqttWorkerModule)
)]
public class TraceIotHttpApiHostModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        ConfigureJwt(context, configuration);
        ConfigureCors(context, configuration);
        ConfigureSwagger(context);
        ConfigureRedis(context, configuration);
        ConfigureInfluxDb(context, configuration);
    }

    private void ConfigureJwt(ServiceConfigurationContext context, IConfiguration config)
    {
        var jwtKey = config["Jwt:Key"] ?? throw new Exception("Jwt:Key 未配置");
        context.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer              = config["Jwt:Issuer"],
                    ValidAudience            = config["Jwt:Audience"],
                    IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };
            });
        context.Services.AddAuthorization();
    }

    private void ConfigureCors(ServiceConfigurationContext context, IConfiguration config)
    {
        var corsOrigins = config["App:CorsOrigins"]?.Split(",", StringSplitOptions.RemoveEmptyEntries) ?? [];
        context.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
                builder.WithOrigins(corsOrigins)
                       .AllowAnyHeader()
                       .AllowAnyMethod()
                       .AllowCredentials());
        });
    }

    private void ConfigureSwagger(ServiceConfigurationContext context)
    {
        context.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "TraceIoT API", Version = "v1" });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme.",
                Name        = "Authorization",
                In          = ParameterLocation.Header,
                Type        = SecuritySchemeType.ApiKey,
                Scheme      = "Bearer"
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });
    }

    private void ConfigureRedis(ServiceConfigurationContext context, IConfiguration config)
    {
        var redisConfig = config["Redis:Configuration"] ?? "localhost:6379";
        context.Services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisConfig));
    }

    private void ConfigureInfluxDb(ServiceConfigurationContext context, IConfiguration config)
    {
        var url   = config["InfluxDB:Url"]   ?? "http://localhost:8086";
        var token = config["InfluxDB:Token"] ?? "";
        context.Services.AddSingleton<IInfluxDBClient>(InfluxDBClientFactory.Create(url, token));
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();

        app.UseRouting();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAbpSerilogEnrichers();

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "TraceIoT API V1");
        });

        app.UseConfiguredEndpoints();
    }
}
