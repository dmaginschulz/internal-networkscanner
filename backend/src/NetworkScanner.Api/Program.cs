using FastEndpoints;
using FastEndpoints.Swagger;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/networkscanner-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add FastEndpoints
builder.Services.AddFastEndpoints();

// Add Swagger
builder.Services.SwaggerDocument(o =>
{
    o.DocumentSettings = s =>
    {
        s.Title = "Network Scanner API";
        s.Version = "v1";
        s.Description = "REST API for scanning and discovering network devices";
    };
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? new[] { "http://localhost:5173" };

        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add Memory Cache
builder.Services.AddMemoryCache();

// Configure Scanner Settings
builder.Services.Configure<NetworkScanner.Api.Configuration.ScannerConfiguration>(
    builder.Configuration.GetSection("ScannerConfiguration"));

// Register custom services
builder.Services.AddScoped<NetworkScanner.Api.Services.INetworkScannerService, NetworkScanner.Api.Services.NetworkScannerService>();
builder.Services.AddScoped<NetworkScanner.Api.Services.IPortScannerService, NetworkScanner.Api.Services.PortScannerService>();
builder.Services.AddScoped<NetworkScanner.Api.Services.IDeviceDiscoveryService, NetworkScanner.Api.Services.DeviceDiscoveryService>();
builder.Services.AddScoped<NetworkScanner.Api.Services.ITopologyDiscoveryService, NetworkScanner.Api.Services.TopologyDiscoveryService>();
builder.Services.AddScoped<NetworkScanner.Api.Services.IInferenceTopologyService, NetworkScanner.Api.Services.InferenceTopologyService>();
builder.Services.AddSingleton<NetworkScanner.Api.Services.IDeviceRepository, NetworkScanner.Api.Services.DeviceRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseSerilogRequestLogging();

app.UseCors("AllowFrontend");

// Use FastEndpoints
app.UseFastEndpoints(c =>
{
    c.Endpoints.RoutePrefix = "api";
});

// Use Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerGen();
}

app.Run();

Log.Information("Network Scanner API started successfully");
