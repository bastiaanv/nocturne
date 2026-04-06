using Microsoft.Extensions.Logging;
using Nocturne.Connectors.HomeAssistant.Configurations;
using Nocturne.Connectors.HomeAssistant.Services;
using Nocturne.Core.Contracts.Events;
using Nocturne.Core.Models;

namespace Nocturne.Connectors.HomeAssistant.WriteBack;

/// <summary>
/// Pushes Nocturne data to Home Assistant entities when new glucose entries arrive.
/// Implements IDataEventSink&lt;Entry&gt; to piggyback on glucose domain events.
/// </summary>
public class HomeAssistantWriteBackSink(
    IHomeAssistantApiClient apiClient,
    HomeAssistantConnectorConfiguration config,
    ILogger<HomeAssistantWriteBackSink> logger) : IDataEventSink<Entry>
{
    private static readonly TimeSpan StalenessThreshold = TimeSpan.FromMinutes(10);

    public async Task OnCreatedAsync(Entry item, CancellationToken ct = default)
    {
        if (!config.WriteBackEnabled)
            return;

        if (IsStale(item))
            return;

        if (config.WriteBackTypes.Contains(WriteBackDataType.Glucose))
            await PushGlucoseAsync(item, ct);

        if (config.WriteBackTypes.Contains(WriteBackDataType.Iob))
            await PushIobAsync(ct);

        if (config.WriteBackTypes.Contains(WriteBackDataType.Cob))
            await PushCobAsync(ct);

        if (config.WriteBackTypes.Contains(WriteBackDataType.PredictedBg))
            await PushPredictedBgAsync(ct);

        if (config.WriteBackTypes.Contains(WriteBackDataType.LoopStatus))
            await PushLoopStatusAsync(ct);
    }

    public async Task OnCreatedAsync(IReadOnlyList<Entry> items, CancellationToken ct = default)
    {
        if (!config.WriteBackEnabled || items.Count == 0)
            return;

        var latest = items.MaxBy(e => e.Mills);
        if (latest != null)
            await OnCreatedAsync(latest, ct);
    }

    private static bool IsStale(Entry entry)
    {
        var entryTime = DateTimeOffset.FromUnixTimeMilliseconds(entry.Mills);
        return DateTimeOffset.UtcNow - entryTime > StalenessThreshold;
    }

    private async Task PushGlucoseAsync(Entry entry, CancellationToken ct)
    {
        try
        {
            var attributes = new Dictionary<string, object>
            {
                ["unit_of_measurement"] = "mg/dL",
                ["device_class"] = "blood_glucose",
                ["friendly_name"] = "Nocturne Glucose",
                ["icon"] = "mdi:diabetes",
                ["trend"] = entry.Direction ?? "Unknown",
                ["last_updated"] = DateTimeOffset.UtcNow.ToString("o")
            };

            await apiClient.SetStateAsync("sensor.nocturne_glucose",
                entry.Sgv?.ToString() ?? "0", attributes, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to push glucose to HA");
        }
    }

    // Computed value pushes — stubbed for Task 10
    private async Task PushIobAsync(CancellationToken ct)
    {
        try
        {
            // TODO: Task 10 will wire up ChartDataService to get latest IOB
            logger.LogDebug("IOB write-back pending ChartDataService integration");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to push IOB to HA");
        }
    }

    private async Task PushCobAsync(CancellationToken ct)
    {
        try
        {
            logger.LogDebug("COB write-back pending ChartDataService integration");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to push COB to HA");
        }
    }

    private async Task PushPredictedBgAsync(CancellationToken ct)
    {
        try
        {
            logger.LogDebug("Predicted BG write-back pending ChartDataService integration");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to push predicted BG to HA");
        }
    }

    private async Task PushLoopStatusAsync(CancellationToken ct)
    {
        try
        {
            logger.LogDebug("Loop status write-back pending ChartDataService integration");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to push loop status to HA");
        }
    }
}
