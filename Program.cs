var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

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
