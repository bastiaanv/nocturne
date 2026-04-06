using Nocturne.Connectors.Core.Extensions;
using Nocturne.Connectors.Core.Models;
using Nocturne.Core.Constants;

namespace Nocturne.Connectors.HomeAssistant;

[ConnectorRegistration(
    "HomeAssistant",
    ServiceNames.HomeAssistantConnector,
    "HOME_ASSISTANT",
    "ConnectSource.HomeAssistant",
    "home-assistant-connector",
    "home",
    ConnectorCategory.Sync,
    "Bidirectional sync with Home Assistant",
    "Home Assistant",
    SupportsHistoricalSync = false,
    SupportsManualSync = true,
    SupportedDataTypes = [
        SyncDataType.Glucose, SyncDataType.Boluses,
        SyncDataType.CarbIntake, SyncDataType.Activity, SyncDataType.ManualBG
    ]
)]
public class HomeAssistantConnectorConfiguration : BaseConnectorConfiguration
{
    public HomeAssistantConnectorConfiguration()
    {
        ConnectSource = ConnectSource.HomeAssistant;
    }

    [ConnectorProperty(ConnectorPropertyKey.Url, Required = true)]
    public string Url { get; init; } = string.Empty;

    public Dictionary<SyncDataType, string> EntityMappings { get; init; } = new();

    [ConnectorProperty(ConnectorPropertyKey.WriteBackEnabled)]
    public bool WriteBackEnabled { get; set; } = false;

    public HashSet<WriteBackDataType> WriteBackTypes { get; init; } = [];

    [ConnectorProperty(ConnectorPropertyKey.WebhookEnabled)]
    public bool WebhookEnabled { get; set; } = false;
}
