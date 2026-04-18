<script lang="ts">
  import { onMount } from "svelte";
  import { goto } from "$app/navigation";
  import {
    getUploaderApps,
    getUploaderSetup,
    getActiveDataSources,
    getServicesOverview,
  } from "$api/generated/services.generated.remote";
  import type {
    UploaderApp,
    UploaderSetupResponse,
    DataSourceInfo,
    AvailableConnector,
    SyncResult,
  } from "$lib/api/generated/nocturne-api-client";
  import { markSetupComplete } from "../setup.remote";
  import ConnectorSetup from "$lib/components/connectors/ConnectorSetup.svelte";
  import DataSourceSelectionView from "$lib/components/connectors/DataSourceSelectionView.svelte";
  import UploaderSetupView from "$lib/components/connectors/UploaderSetupView.svelte";

  // ── View state ──────────────────────────────────────────────────────

  type ViewState = "selection" | "connector" | "uploader";
  let viewState = $state<ViewState>("selection");

  // ── Connector state ───────────────────────────────────────────────
  let selectedConnectorId = $state<string | null>(null);

  // ── Uploader state ────────────────────────────────────────────────
  let selectedApp = $state<UploaderApp | null>(null);
  let setupResponse = $state<UploaderSetupResponse | null>(null);

  // ── Data state ────────────────────────────────────────────────────

  let uploaderApps = $state<UploaderApp[]>([]);
  let connectors = $state<AvailableConnector[]>([]);
  let dataSources = $state<DataSourceInfo[]>([]);
  let isLoading = $state(true);
  let loadError = $state<string | null>(null);

  // ── Load data ─────────────────────────────────────────────────────

  onMount(async () => {
    await loadData();
  });

  async function loadData() {
    isLoading = true;
    loadError = null;

    try {
      const [apps, sources, overview] = await Promise.all([
        getUploaderApps(),
        getActiveDataSources().catch(() => [] as DataSourceInfo[]),
        getServicesOverview(),
      ]);

      uploaderApps = apps ?? [];
      dataSources = sources ?? [];

      // Filter out "nightscout" connector (handled on the triage page)
      connectors = (overview?.availableConnectors ?? []).filter(
        (c) => c.id?.toLowerCase() !== "nightscout",
      );
    } catch (e) {
      loadError = e instanceof Error ? e.message : "Failed to load data sources";
    } finally {
      isLoading = false;
    }
  }

  // ── Select a connector ────────────────────────────────────────────

  function selectConnector(connector: AvailableConnector) {
    selectedConnectorId = connector.id ?? null;
    viewState = "connector";
  }

  async function handleConnectorComplete(_result: SyncResult) {
    await markSetupComplete();
    await goto("/", { invalidateAll: true });
  }

  function handleConnectorCancel() {
    selectedConnectorId = null;
    viewState = "selection";
  }

  // ── Select an uploader app ────────────────────────────────────────

  async function selectApp(app: UploaderApp) {
    selectedApp = app;
    viewState = "uploader";

    try {
      const result = await getUploaderSetup(app.id!);
      setupResponse = result;
    } catch {
      setupResponse = null;
    }
  }

  function handleBackToSelection() {
    selectedApp = null;
    setupResponse = null;
    selectedConnectorId = null;
    viewState = "selection";
  }

  // ── Skip ──────────────────────────────────────────────────────────

  async function handleSkip() {
    await markSetupComplete();
    await goto("/", { invalidateAll: true });
  }

  async function handleUploaderConnected() {
    await handleSkip();
  }
</script>

<svelte:head>
  <title>Connect a Data Source - Setup - Nocturne</title>
</svelte:head>

<div class="container mx-auto max-w-2xl p-6 space-y-6">
  {#if viewState === "selection"}
    <DataSourceSelectionView
      {connectors}
      {uploaderApps}
      {dataSources}
      {isLoading}
      {loadError}
      onSelectConnector={(id) => {
        const connector = connectors.find((c) => c.id === id);
        if (connector) selectConnector(connector);
      }}
      onSelectUploader={selectApp}
      onSkip={handleSkip}
    />

  {:else if viewState === "connector"}
    <div class="space-y-4">
      <ConnectorSetup
        connectorId={selectedConnectorId ?? undefined}
        onComplete={handleConnectorComplete}
        onCancel={handleConnectorCancel}
        showToggle={false}
        showDangerZone={false}
        showCapabilities={false}
        primaryAction="save-and-sync"
      />
    </div>

  {:else if viewState === "uploader"}
    <UploaderSetupView
      app={selectedApp}
      {setupResponse}
      onBack={handleBackToSelection}
      onConnected={handleUploaderConnected}
    />
  {/if}
</div>
