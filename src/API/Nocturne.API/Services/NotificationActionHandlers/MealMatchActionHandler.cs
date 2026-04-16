using Nocturne.Core.Contracts;
using Nocturne.Core.Models;

namespace Nocturne.API.Services.NotificationActionHandlers;

/// <summary>
/// Handles actions on suggested meal match notifications.
/// </summary>
public class MealMatchActionHandler(
    IInAppNotificationService notificationService,
    IConnectorFoodEntryRepository foodEntryRepository,
    ILogger<MealMatchActionHandler> logger
) : INotificationActionHandler
{
    public string NotificationType => "meal_matching.suggested_match";

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
                // Accept action is handled via MealMatchingController
                // Just archive the notification here
                return await notificationService.ArchiveNotificationAsync(
                    notificationId,
                    NotificationArchiveReason.Completed,
                    cancellationToken);

            case "dismiss":
                if (sourceId != null && Guid.TryParse(sourceId, out var foodEntryId))
                {
                    // Mark the food entry as standalone
                    await foodEntryRepository.UpdateStatusAsync(
                        foodEntryId,
                        ConnectorFoodEntryStatus.Standalone,
                        null,
                        cancellationToken);
                }
                return await notificationService.ArchiveNotificationAsync(
                    notificationId,
                    NotificationArchiveReason.Dismissed,
                    cancellationToken);

            case "review":
                // Review opens a dialog client-side, just return true
                return true;

            default:
                logger.LogWarning(
                    "Unknown action {ActionId} for meal match notification {NotificationId}",
                    actionId, notificationId);
                return false;
        }
    }
}
