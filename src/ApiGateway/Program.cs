using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using Shared.Consul;
using System.Text;
using Ocelot.Provider.Polly;
using Shared.Telemetry;

var builder = WebApplication.CreateBuilder(args);

// ── Set Port ──────────────────────────────────────────────
builder.WebHost.UseUrls("http://localhost:5000");

// ── Load Ocelot Config ────────────────────────────────────
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// ── JWT Authentication ────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();

// ── Consul ────────────────────────────────────────────────
builder.Services.AddConsulRegistration(builder.Configuration);

// ── Health Checks ─────────────────────────────────────────
builder.Services.AddHealthChecks();

// ── Ocelot ────────────────────────────────────────────────
builder.Services.AddOcelot(builder.Configuration)
    .AddConsul()
    .AddPolly();

builder.Services.AddDistributedTracing(
    builder.Configuration, "api-gateway");

var app = builder.Build();

// ── Global Exception Handler ──────────────────────────────
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var logger = context.RequestServices
            .GetRequiredService<ILogger<Program>>();

        var exceptionFeature = context.Features
            .Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();

        if (exceptionFeature != null)
        {
            logger.LogError(exceptionFeature.Error,
                "[ERROR] Unhandled exception on {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
        }

        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            message = "An unexpected error occurred. Please try again later."
        });
    });
});

// ── Health Check — handled via MapWhen BEFORE Ocelot ──────
// MapWhen creates a separate pipeline branch for /health
// Ocelot never sees this request
app.MapWhen(
    context => context.Request.Path.Equals("/health"),
    healthApp =>
    {
        healthApp.UseHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description
                    })
                };
                await context.Response.WriteAsJsonAsync(result);
            }
        });
    });

app.UseAuthentication();
app.UseAuthorization();


// ── Ocelot — handles all other routes ────────────────────
await app.UseOcelot();

app.Run();