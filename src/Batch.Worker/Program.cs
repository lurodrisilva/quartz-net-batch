using Batch.Infrastructure.DependencyInjection;
using Batch.Observability.DependencyInjection;
using Batch.Worker.HealthChecks;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// ── Structured Logging via Serilog ──────────────────────────────────────────
// CompactJsonFormatter produces one JSON object per line — ideal for Dynatrace
// log ingestion and any structured-log backend. Includes all scope properties
// (job.name, dt.trace_id, etc.) automatically.
builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .WriteTo.Console(new CompactJsonFormatter());
});

// ── Observability (Dynatrace SDK, Metrics) ──────────────────────────────────
builder.Services.AddBatchObservability();

// ── Quartz.NET + Job Registration ───────────────────────────────────────────
builder.Services.AddBatchInfrastructure(builder.Configuration);

// ── Health Checks ───────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddCheck<QuartzHealthCheck>("quartz-scheduler", tags: ["ready", "live"]);

var app = builder.Build();

// ── Health Check Endpoints (used by Kubernetes probes) ──────────────────────
app.MapHealthChecks("/health/live", new()
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new()
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/startup", new()
{
    Predicate = _ => true
});

Log.Information("BatchScheduler starting");

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "BatchScheduler terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
