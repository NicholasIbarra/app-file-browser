using FileBrowser.Api.ErrorHandling;
using FileBrowser.Api.Extensions;
using FileBrowser.Api.Hubs;
using FileBrowser.Application.Files.Events;
using FileBrowser.Infrastructure;
using Hangfire;
using MediatR;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSwagger();
builder.Services.AddEndpointControllers();
builder.Services.AddSignalR();
builder.Services.AddTransient<INotificationHandler<FileCreatedEvent>, FileUploadCompletedEventHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddCorsPolicy();
builder.Services.AddDefaultHealthChecks();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddInfrastructure(
    builder.Configuration, 
    builder.Environment.ContentRootPath,
    Assembly.GetExecutingAssembly());

var app = builder.Build();

app.UseExceptionHandler(o => { });
app.MapDefaultEndpoints();
app.UseHttpsRedirection();
app.UseOpenApi();
app.MapControllers();
app.UseDefaultCorsPolicy();
app.MapHub<FileHub>("/hubs/files");
app.UseHangfireServer();
app.UseHangfireDashboard();

app.Run();
