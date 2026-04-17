using Nocturne.Core.Models;

namespace Nocturne.API.Services;

public interface INotificationTemplateRegistry
{
    NotificationTemplate? GetTemplate(string type);
}

public class NotificationTemplateRegistry : INotificationTemplateRegistry
{
    private readonly Dictionary<string, NotificationTemplate> _templates = new();

    public void Register(NotificationTemplate template)
    {
        _templates[template.Type] = template;
    }

    public NotificationTemplate? GetTemplate(string type)
    {
        return _templates.GetValueOrDefault(type);
    }
}
