using cerberus_securitygate.Data;
using cerberus_securitygate.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<CerberusDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<ScanRequestService>();
builder.Services.AddScoped<ScanStatusService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "healthy",
    service = "SecurityGate",
    timestamp = DateTime.UtcNow
}));

app.MapGet("/api/ready", () => Results.Ok(new
{
    status = "ready",
    service = "SecurityGate",
    timestamp = DateTime.UtcNow
}));

app.Run();