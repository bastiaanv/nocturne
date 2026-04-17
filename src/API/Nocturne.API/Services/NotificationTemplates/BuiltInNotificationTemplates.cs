using Nocturne.Core.Models;

namespace Nocturne.API.Services.NotificationTemplates;

public static class BuiltInNotificationTemplates
{
    public static NotificationTemplateRegistry AddBuiltInTemplates(
        this NotificationTemplateRegistry registry)
    {
        registry.Register(new NotificationTemplate
        {
            Type = "tracker.unconfigured",
            Category = NotificationCategory.ActionRequired,
            DefaultUrgency = NotificationUrgency.Info,
            Icon = "settings-2",
            Source = "tracker-service"
        });

        registry.Register(new NotificationTemplate
        {
            Type = "tracker.alert",
            Category = NotificationCategory.Alert,
            DefaultUrgency = NotificationUrgency.Warn,
            Icon = "timer",
            Source = "tracker-service"
        });

        registry.Register(new NotificationTemplate
        {
            Type = "system.statistics_summary",
            Category = NotificationCategory.Informational,
            DefaultUrgency = NotificationUrgency.Info,
            Icon = "info",
            Source = "system"
        });

        registry.Register(new NotificationTemplate
        {
            Type = "system.help_response",
            Category = NotificationCategory.Informational,
            DefaultUrgency = NotificationUrgency.Info,
            Icon = "help-circle",
            Source = "system"
        });

        registry.Register(new NotificationTemplate
        {
            Type = "passkey.anonymous_login_request",
            Category = NotificationCategory.ActionRequired,
            DefaultUrgency = NotificationUrgency.Info,
            Icon = "user",
            Source = "auth"
        });

        registry.Register(new NotificationTemplate
        {
            Type = "glucose.predicted_low",
            Category = NotificationCategory.Alert,
            DefaultUrgency = NotificationUrgency.Urgent,
            Icon = "trending-down",
            Source = "glucose-engine"
        });

        registry.Register(new NotificationTemplate
        {
            Type = "meal_matching.suggested_match",
            Category = NotificationCategory.ActionRequired,
            DefaultUrgency = NotificationUrgency.Info,
            Icon = "utensils",
            Source = "meal-matching"
        });

        registry.Register(new NotificationTemplate
        {
            Type = "tracker.suggested_match",
            Category = NotificationCategory.ActionRequired,
            DefaultUrgency = NotificationUrgency.Info,
            Icon = "refresh-cw",
            Source = "tracker-service"
        });

        registry.Register(new NotificationTemplate
        {
            Type = "glucose.compression_low_review",
            Category = NotificationCategory.ActionRequired,
            DefaultUrgency = NotificationUrgency.Info,
            Icon = "info",
            Source = "compression-low-engine"
        });

        return registry;
    }
}
