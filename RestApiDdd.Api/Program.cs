using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using RestApiDdd.Api.Configuration;
using RestApiDdd.Api.Middleware;
using RestApiDdd.Api.Security;
using RestApiDdd.Infrastructure;
using RestApiDdd.Service;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting RestApiDdd.Api host.");

    var builder = WebApplication.CreateBuilder(args);
    await builder.Configuration.LoadAwsParameterStoreSecretsAsync();

    builder.Host.UseConfiguredSerilog();

    var jwtOptions = builder.Configuration
        .GetSection(JwtOptions.SectionName)
        .Get<JwtOptions>() ?? new JwtOptions();

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();
    builder.Services.AddHealthChecks();
    builder.Services.AddRequestResponseLoggingConfiguration(builder.Configuration);
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(
            builder.Environment.ContentRootPath,
            "App_Data",
            "DataProtectionKeys")));
    builder.Services.AddServiceLayer();
    builder.Services.AddInfrastructure(builder.Configuration);

    if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
    {
        throw new InvalidOperationException("JWT signing key is required. Configure Jwt:SigningKey.");
    }

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2)
            };
        });

    builder.Services.AddAuthorization();

    var app = builder.Build();

    app.UseRequestResponseLogging();

    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();

    app.UseMiddleware<PostSearchRequestMethodOverrideMiddleware>();

    app.UseRouting();
    app.UseMiddleware<ApiVersionedDtoMiddleware>();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHealthChecks("/health").AllowAnonymous();
    app.MapControllers();

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "RestApiDdd.Api host terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
