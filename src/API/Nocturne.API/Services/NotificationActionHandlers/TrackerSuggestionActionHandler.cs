using Nocturne.Core.Contracts;
using Nocturne.Core.Models;

namespace Nocturne.API.Services.NotificationActionHandlers;

/// <summary>
/// Handles actions on suggested tracker match notifications.
/// </summary>
public class TrackerSuggestionActionHandler(
    ITrackerSuggestionService trackerSuggestionService,
    ILogger<TrackerSuggestionActionHandler> logger
) : INotificationActionHandler
{
    public string NotificationType => "tracker.suggested_match";

    public async Task<bool> HandleAsync(
        Guid notificationId,
        string actionId,
        string userId,
        string? sourceId,
        Dictionary<string, object>? metadata,
        CancellationToken cancellationToken = default)
    {
        switch (actionId.ToLowerInvariant())
        {
            case "accept":
                // Accept resets the tracker (completes current instance, starts new one)
                return await trackerSuggestionService.AcceptSuggestionAsync(
                    notificationId,
                    userId,
                    cancellationToken);

            case "dismiss":
                // Dismiss just archives the notification
                return await trackerSuggestionService.DismissSuggestionAsync(
                    notificationId,
                    userId,
                    cancellationToken);

            default:
                logger.LogWarning(
                    "Unknown action {ActionId} for tracker suggestion notification {NotificationId}",
                    actionId, notificationId);
                return false;
        }
    }
}
