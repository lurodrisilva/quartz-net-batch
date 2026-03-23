using Batch.Application.Jobs;
using Batch.Domain.Interfaces;
using Dynatrace.OneAgent.Sdk.Api;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Quartz;
using Xunit;

namespace Batch.UnitTests.Jobs;

public class SampleRecurringJobTests
{
    private readonly IJobTracer _tracer;
    private readonly IJobMetrics _metrics;
    private readonly IOneAgentSdk _sdk;
    private readonly ILogger<SampleRecurringJob> _logger;
    private readonly SampleRecurringJob _sut;

    public SampleRecurringJobTests()
    {
        _tracer = Substitute.For<IJobTracer>();
        _metrics = Substitute.For<IJobMetrics>();
        _sdk = Substitute.For<IOneAgentSdk>();
        _logger = Substitute.For<ILogger<SampleRecurringJob>>();

        var traceScope = Substitute.For<IJobTraceScope>();
        _tracer.StartJobTrace(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(traceScope);

        _sut = new SampleRecurringJob(_tracer, _metrics, _sdk, _logger);
    }

    [Fact]
    public async Task Execute_WithValidContext_RecordsStartAndSuccessMetrics()
    {
        var context = CreateJobContext(batchSize: 5);

        await _sut.Execute(context);

        _metrics.Received(1).RecordJobStarted("SampleRecurringJob", "RecurringJobs");
        _metrics.Received(1).RecordJobSucceeded("SampleRecurringJob", "RecurringJobs");
        _metrics.Received(1).RecordJobDuration("SampleRecurringJob", "RecurringJobs", Arg.Any<double>());
        _metrics.DidNotReceive().RecordJobFailed(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Execute_WithDefaultBatchSize_ProcessesDefaultItems()
    {
        var context = CreateJobContext(batchSize: 0);

        await _sut.Execute(context);

        _metrics.Received(1).RecordJobSucceeded("SampleRecurringJob", "RecurringJobs");
    }

    [Fact]
    public async Task Execute_StartsTrace_WithCorrectJobIdentity()
    {
        var context = CreateJobContext(batchSize: 1);

        await _sut.Execute(context);

        _tracer.Received(1).StartJobTrace("SampleRecurringJob", "RecurringJobs", Arg.Any<string>());
    }

    [Fact]
    public async Task Execute_WhenCancelled_ThrowsJobExecutionException()
    {
        var cts = new CancellationTokenSource();
        var context = CreateJobContext(batchSize: 1000, cancellationToken: cts.Token);

        cts.Cancel();

        var act = () => _sut.Execute(context);
        await act.Should().ThrowAsync<JobExecutionException>();
    }

    private static IJobExecutionContext CreateJobContext(
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        var context = Substitute.For<IJobExecutionContext>();
        var jobDetail = Substitute.For<IJobDetail>();
        var trigger = Substitute.For<ITrigger>();

        var jobDataMap = new JobDataMap { { "BatchSize", batchSize } };
        var jobDetailDataMap = new JobDataMap();

        jobDetail.Key.Returns(new JobKey("SampleRecurringJob", "RecurringJobs"));
        jobDetail.JobDataMap.Returns(jobDetailDataMap);
        trigger.Key.Returns(new TriggerKey("SampleRecurringTrigger", "RecurringTriggers"));

        context.JobDetail.Returns(jobDetail);
        context.Trigger.Returns(trigger);
        context.MergedJobDataMap.Returns(jobDataMap);
        context.FireInstanceId.Returns("test-fire-id");
        context.ScheduledFireTimeUtc.Returns(DateTimeOffset.UtcNow);
        context.CancellationToken.Returns(cancellationToken);

        return context;
    }
}
