using FinanceOS.Backend.Services;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Dependency Injection
builder.Services.AddSingleton<IAppState, AppState>();
builder.Services.AddSingleton<IFileProcessingService, FileProcessingService>();
builder.Services.AddSingleton<IFinanceEngineService, FinanceEngineService>();
builder.Services.AddSingleton<IResourceEngineService, ResourceEngineService>();
builder.Services.AddSingleton<IExportService, ExportService>();

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200") // Angular default port
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAngular");

// Serve uploads folder as static files (optional, mirroring Node behavior)
var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadDir),
    RequestPath = "/uploads"
});

app.UseAuthorization();

app.MapGet("/api/ping", () => Results.Ok(new { status = "pong", version = "v1.0.0-dotnet" }));

app.MapControllers();

app.Run();
