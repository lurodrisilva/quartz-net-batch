using Batch.Domain.Interfaces;
using Batch.Observability.DependencyInjection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Quartz;
using Xunit;

namespace Batch.IntegrationTests;

public sealed class QuartzHostFixture : IAsyncLifetime
{
    public IHost Host { get; private set; } = null!;
    public ISchedulerFactory SchedulerFactory { get; private set; } = null!;
    public IJobMetrics Metrics { get; } = Substitute.For<IJobMetrics>();

    public async Task InitializeAsync()
    {
        Host = new HostBuilder()
            .ConfigureLogging(logging => logging.AddConsole())
            .ConfigureServices(services =>
            {
                services.AddBatchObservability();
                services.AddSingleton(Metrics);

                services.AddQuartz(q =>
                {
                    q.SchedulerName = "TestScheduler";
                    q.UseInMemoryStore();
                    q.UseDefaultThreadPool(tp => tp.MaxConcurrency = 2);
                });

                services.AddQuartzHostedService(options =>
                {
                    options.WaitForJobsToComplete = true;
                    options.StartDelay = TimeSpan.Zero;
                });
            })
            .Build();

        await Host.StartAsync();
        SchedulerFactory = Host.Services.GetRequiredService<ISchedulerFactory>();
    }

    public async Task DisposeAsync()
    {
        await Host.StopAsync(TimeSpan.FromSeconds(10));
        Host.Dispose();
    }
}

public class QuartzInMemoryIntegrationTests : IClassFixture<QuartzHostFixture>
{
    private readonly QuartzHostFixture _fixture;

    public QuartzInMemoryIntegrationTests(QuartzHostFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Scheduler_StartsSuccessfully_AndIsRunning()
    {
        var scheduler = await _fixture.SchedulerFactory.GetScheduler();

        scheduler.IsStarted.Should().BeTrue();
        scheduler.IsShutdown.Should().BeFalse();
    }

    [Fact]
    public async Task Scheduler_ReportsMetadata()
    {
        var scheduler = await _fixture.SchedulerFactory.GetScheduler();
        var metadata = await scheduler.GetMetaData();

        metadata.SchedulerName.Should().Be("TestScheduler");
        metadata.ThreadPoolSize.Should().Be(2);
        metadata.InStandbyMode.Should().BeFalse();
    }
}
