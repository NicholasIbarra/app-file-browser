using FileBrowser.Api.ErrorHandling;
using FileBrowser.Api.Extensions;
using FileBrowser.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSwagger();
builder.Services.AddEndpointControllers();
builder.Services.AddProblemDetails();
builder.Services.AddCorsPolicy();
builder.Services.AddDefaultHealthChecks();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddInfrastructure(
    builder.Configuration, 
    builder.Environment.ContentRootPath);

var app = builder.Build();

app.UseExceptionHandler(o => { });
app.MapDefaultEndpoints();
app.UseHttpsRedirection();
app.UseOpenApi();
app.MapControllers();
app.UseDefaultCorsPolicy();

app.Run();