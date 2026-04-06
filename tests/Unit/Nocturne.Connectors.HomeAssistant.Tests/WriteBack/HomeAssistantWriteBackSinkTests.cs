using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nocturne.Connectors.HomeAssistant.Configurations;
using Nocturne.Connectors.HomeAssistant.Services;
using Nocturne.Connectors.HomeAssistant.WriteBack;
using Nocturne.Core.Models;
using Xunit;

namespace Nocturne.Connectors.HomeAssistant.Tests.WriteBack;

public class HomeAssistantWriteBackSinkTests
{
    private readonly Mock<IHomeAssistantApiClient> _apiClientMock = new();
    private readonly Mock<ILogger<HomeAssistantWriteBackSink>> _loggerMock = new();

    private HomeAssistantWriteBackSink CreateSink(
        bool writeBackEnabled = true,
        HashSet<WriteBackDataType>? writeBackTypes = null)
    {
        var config = new HomeAssistantConnectorConfiguration
        {
            WriteBackEnabled = writeBackEnabled,
            WriteBackTypes = writeBackTypes ?? [WriteBackDataType.Glucose]
        };

        return new HomeAssistantWriteBackSink(_apiClientMock.Object, config, _loggerMock.Object);
    }

    private static Entry CreateRecentEntry(double sgv = 120, string direction = "Flat")
    {
        return new Entry
        {
            Mills = DateTimeOffset.UtcNow.AddSeconds(-30).ToUnixTimeMilliseconds(),
            Sgv = sgv,
            Direction = direction
        };
    }

    private static Entry CreateStaleEntry()
    {
        return new Entry
        {
            Mills = DateTimeOffset.UtcNow.AddMinutes(-15).ToUnixTimeMilliseconds(),
            Sgv = 100,
            Direction = "Flat"
        };
    }

    [Fact]
    public async Task OnCreatedAsync_WhenWriteBackDisabled_DoesNothing()
    {
        var sink = CreateSink(writeBackEnabled: false);
        var entry = CreateRecentEntry();

        await sink.OnCreatedAsync(entry);

        _apiClientMock.Verify(
            x => x.SetStateAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnCreatedAsync_WhenEntryIsStale_DoesNothing()
    {
        var sink = CreateSink();
        var entry = CreateStaleEntry();

        await sink.OnCreatedAsync(entry);

        _apiClientMock.Verify(
            x => x.SetStateAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnCreatedAsync_WhenGlucoseEnabled_PushesGlucoseState()
    {
        _apiClientMock
            .Setup(x => x.SetStateAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sink = CreateSink(writeBackTypes: [WriteBackDataType.Glucose]);
        var entry = CreateRecentEntry(sgv: 145, direction: "FortyFiveUp");

        await sink.OnCreatedAsync(entry);

        _apiClientMock.Verify(
            x => x.SetStateAsync(
                "sensor.nocturne_glucose",
                It.Is<string>(s => s.StartsWith("145")),
                It.Is<Dictionary<string, object>>(d =>
                    d["unit_of_measurement"].Equals("mg/dL") &&
                    d["trend"].Equals("FortyFiveUp")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnCreatedAsync_WhenGlucoseNotInWriteBackTypes_SkipsGlucose()
    {
        var sink = CreateSink(writeBackTypes: [WriteBackDataType.Iob]);
        var entry = CreateRecentEntry();

        await sink.OnCreatedAsync(entry);

        _apiClientMock.Verify(
            x => x.SetStateAsync(
                "sensor.nocturne_glucose",
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnCreatedAsync_IndividualFailureDoesNotBlockOthers()
    {
        _apiClientMock
            .Setup(x => x.SetStateAsync(
                "sensor.nocturne_glucose",
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var sink = CreateSink(writeBackTypes:
        [
            WriteBackDataType.Glucose,
            WriteBackDataType.Iob
        ]);
        var entry = CreateRecentEntry();

        // Should not throw even though glucose push fails
        var act = () => sink.OnCreatedAsync(entry);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task OnCreatedAsync_BatchUsesLatestEntry()
    {
        _apiClientMock
            .Setup(x => x.SetStateAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sink = CreateSink(writeBackTypes: [WriteBackDataType.Glucose]);

        var older = new Entry
        {
            Mills = DateTimeOffset.UtcNow.AddSeconds(-60).ToUnixTimeMilliseconds(),
            Sgv = 100,
            Direction = "Flat"
        };
        var latest = new Entry
        {
            Mills = DateTimeOffset.UtcNow.AddSeconds(-10).ToUnixTimeMilliseconds(),
            Sgv = 180,
            Direction = "SingleUp"
        };

        await sink.OnCreatedAsync(new List<Entry> { older, latest });

        _apiClientMock.Verify(
            x => x.SetStateAsync(
                "sensor.nocturne_glucose",
                "180",
                It.Is<Dictionary<string, object>>(d => d["trend"].Equals("SingleUp")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
