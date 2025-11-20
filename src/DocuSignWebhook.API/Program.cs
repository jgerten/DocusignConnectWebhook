using DocuSignWebhook.Application.Interfaces;
using DocuSignWebhook.Application.Interfaces.Services;
using DocuSignWebhook.Application.Services;
using DocuSignWebhook.Infrastructure.Data;
using DocuSignWebhook.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/docusign-webhook-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "DocuSign Webhook API",
        Version = "v1",
        Description = "API for receiving DocuSign Connect webhooks and storing documents in MinIO"
    });
});

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IApplicationDbContext>(provider =>
    provider.GetRequiredService<ApplicationDbContext>());

// DocuSign configuration
var docusignAccountId = builder.Configuration["DocuSign:AccountId"] ?? "";
var docusignAccessToken = builder.Configuration["DocuSign:AccessToken"] ?? "";
var docusignBasePath = builder.Configuration["DocuSign:BasePath"] ?? "https://demo.docusign.net/restapi";

builder.Services.AddScoped<IDocuSignService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<DocuSignService>>();
    return new DocuSignService(logger, docusignAccountId, docusignAccessToken, docusignBasePath);
});

// MinIO configuration
var minioEndpoint = builder.Configuration["MinIO:Endpoint"] ?? "localhost:9000";
var minioAccessKey = builder.Configuration["MinIO:AccessKey"] ?? "minioadmin";
var minioSecretKey = builder.Configuration["MinIO:SecretKey"] ?? "minioadmin";
var minioUseSSL = builder.Configuration.GetValue<bool>("MinIO:UseSSL", false);

builder.Services.AddSingleton<IMinioStorageService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<MinioStorageService>>();
    return new MinioStorageService(logger, minioEndpoint, minioAccessKey, minioSecretKey, minioUseSSL);
});

// Webhook processor
var hmacSecret = builder.Configuration["DocuSign:HmacSecret"] ?? "";
var defaultBucket = builder.Configuration["MinIO:DefaultBucket"] ?? "docusign-documents";

builder.Services.AddScoped<IWebhookProcessor>(provider =>
{
    var context = provider.GetRequiredService<IApplicationDbContext>();
    var docusign = provider.GetRequiredService<IDocuSignService>();
    var minio = provider.GetRequiredService<IMinioStorageService>();
    var logger = provider.GetRequiredService<ILogger<WebhookProcessor>>();
    return new WebhookProcessor(context, docusign, minio, logger, hmacSecret, defaultBucket);
});

// CORS (configure as needed)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

//app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();
    Log.Information("Database migration completed");
}

Log.Information("DocuSign Webhook API starting...");

app.Run();
