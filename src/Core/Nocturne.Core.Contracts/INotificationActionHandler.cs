namespace Nocturne.Core.Contracts;

/// <summary>
/// Handles notification-specific actions. Registered per notification type string.
/// </summary>
public interface INotificationActionHandler
{
    /// <summary>
    /// The notification type string this handler is responsible for (e.g., "SuggestedMealMatch")
    /// </summary>
    string NotificationType { get; }

    /// <summary>
    /// Handle an action on a notification of this type.
    /// </summary>
    Task<bool> HandleAsync(
        Guid notificationId,
        string actionId,
        string userId,
        string? sourceId,
        Dictionary<string, object>? metadata,
        CancellationToken cancellationToken = default);
}
