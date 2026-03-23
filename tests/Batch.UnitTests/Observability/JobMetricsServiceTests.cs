using System.Diagnostics.Metrics;
using Batch.Domain.Constants;
using Batch.Observability.Metrics;
using FluentAssertions;
using Xunit;

namespace Batch.UnitTests.Observability;

public class JobMetricsServiceTests : IDisposable
{
    private readonly MeterListener _listener;
    private readonly JobMetricsService _sut;
    private readonly List<(string Name, object? Value, KeyValuePair<string, object?>[] Tags)> _recorded = new();

    public JobMetricsServiceTests()
    {
        var meterFactory = new TestMeterFactory();
        _sut = new JobMetricsService(meterFactory);

        _listener = new MeterListener();
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == JobConstants.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        _listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            _recorded.Add((instrument.Name, measurement, tags.ToArray()));
        });

        _listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
        {
            _recorded.Add((instrument.Name, measurement, tags.ToArray()));
        });

        _listener.Start();
    }

    [Fact]
    public void RecordJobStarted_EmitsStartedCounter()
    {
        _sut.RecordJobStarted("TestJob", "TestGroup");
        _listener.RecordObservableInstruments();

        _recorded.Should().ContainSingle(r => r.Name == "batch.job.started");
    }

    [Fact]
    public void RecordJobSucceeded_EmitsSucceededCounter()
    {
        _sut.RecordJobSucceeded("TestJob", "TestGroup");
        _listener.RecordObservableInstruments();

        _recorded.Should().ContainSingle(r => r.Name == "batch.job.succeeded");
    }

    [Fact]
    public void RecordJobFailed_EmitsFailedCounterWithExceptionType()
    {
        _sut.RecordJobFailed("TestJob", "TestGroup", "InvalidOperationException");
        _listener.RecordObservableInstruments();

        var failed = _recorded.SingleOrDefault(r => r.Name == "batch.job.failed");
        failed.Should().NotBeNull();
        failed.Tags.Should().Contain(t => t.Key == "exception.type" && (string?)t.Value == "InvalidOperationException");
    }

    [Fact]
    public void RecordJobDuration_EmitsDurationHistogram()
    {
        _sut.RecordJobDuration("TestJob", "TestGroup", 150.5);
        _listener.RecordObservableInstruments();

        var duration = _recorded.SingleOrDefault(r => r.Name == "batch.job.duration");
        duration.Should().NotBeNull();
        duration.Value.Should().Be(150.5);
    }

    [Fact]
    public void RecordMisfire_EmitsMisfireCounter()
    {
        _sut.RecordMisfire("TestJob", "TestGroup");
        _listener.RecordObservableInstruments();

        _recorded.Should().ContainSingle(r => r.Name == "batch.job.misfires");
    }

    public void Dispose()
    {
        _listener.Dispose();
        GC.SuppressFinalize(this);
    }
}

internal sealed class TestMeterFactory : IMeterFactory
{
    private readonly List<Meter> _meters = new();

    public Meter Create(MeterOptions options)
    {
        var meter = new Meter(options.Name, options.Version);
        _meters.Add(meter);
        return meter;
    }

    public void Dispose()
    {
        foreach (var meter in _meters)
        {
            meter.Dispose();
        }
    }
}
