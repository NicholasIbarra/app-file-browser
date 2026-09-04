using FileBrowser.Api.ErrorHandling;
using FileBrowser.Api.Extensions;
using FileBrowser.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSwagger();
builder.Services.AddEndpointControllers();
builder.Services.AddProblemDetails();
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddDefaultHealthChecks();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddInfrastructure(
    builder.Configuration,
    builder.Environment.ContentRootPath);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // Azure Container Apps / App Service terminate TLS at a load balancer
    // whose address isn't fixed, so the usual known-network allowlist
    // can't be used here - trust the platform's ingress instead.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler(o => { });
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseOpenApi();
app.MapControllers();
app.UseDefaultCorsPolicy();

app.Run();