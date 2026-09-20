using modular_mlm.Infrastructure.Data;
using modular_mlm.Web.Infrastructure.Observability;
using modular_mlm.Web.Infrastructure.Security;
using modular_mlm.Web.Infrastructure.Tenancy;
using modular_mlm.Web.Infrastructure.Versioning;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

builder.AddKeyVaultIfConfigured();
builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

var app = builder.Build();

// Forwarded host values are accepted only from the known proxies/networks configured by ASP.NET.
app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    await app.InitialiseDatabaseAsync();
}
else
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// AppHost and the Next.js development proxy communicate over the internal HTTP
// endpoint. Avoid redirecting that server-side proxy request to a browser-visible
// HTTPS origin during local development.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseRateLimiter();
app.UseRequestTimeouts();
app.UseMarketplaceSecurityHeaders();
app.UseMarketplaceApiVersioning();
app.UseMiddleware<ApiTelemetryMiddleware>();

app.UseFileServer();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseMiddleware<AuthenticationSessionMiddleware>();
app.UseAuthorization();
app.UseAntiforgery();
app.UseOrganizationResolution();

app.Map(
    "/",
    () =>
        Results.Ok(
            new { service = "Modular Marketplace Binary MLM API", documentation = "/scalar" }
        )
);

app.MapDefaultEndpoints();
app.MapEndpoints(typeof(Program).Assembly);

app.Run();
