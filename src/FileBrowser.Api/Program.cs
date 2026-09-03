using FileBrowser.Api.ErrorHandling;
using FileBrowser.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSwagger();
builder.Services.AddEndpointControllers();
builder.Services.AddProblemDetails();
builder.Services.AddCorsPolicy();
builder.Services.AddDefaultHealthChecks();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler(o => { });
app.MapDefaultEndpoints();
app.UseHttpsRedirection();
app.UseOpenApi();
app.MapControllers();
app.UseDefaultCorsPolicy();

app.Run();