<script lang="ts">
  import { onMount, onDestroy } from "svelte";
  import { goto } from "$app/navigation";
  import {
    getUploaderApps,
    getUploaderSetup,
    getActiveDataSources,
    getServicesOverview,
  } from "$api/generated/services.generated.remote";
  import {
    UploaderPlatform,
    UploaderCategory,
  } from "$lib/api/generated/nocturne-api-client";
  import type {
    UploaderApp,
    UploaderSetupResponse,
    DataSourceInfo,
    AvailableConnector,
    SyncResult,
  } from "$lib/api/generated/nocturne-api-client";
  import { markSetupComplete } from "../setup.remote";
  import ConnectorSetup from "$lib/components/connectors/ConnectorSetup.svelte";
  import * as Card from "$lib/components/ui/card";
  import { Button } from "$lib/components/ui/button";
  import { Badge } from "$lib/components/ui/badge";
  import {
    AlertCircle,
    CheckCircle,
    ChevronLeft,
    ChevronRight,
    Check,
    Loader2,
    Upload,
    Activity,
    Smartphone,
    Clock,
    Cloud,
    Plug,
    Shield,
    ShieldAlert,
    AlertTriangle,
    X,
  } from "lucide-svelte";
  import Apple from "lucide-svelte/icons/apple";
  import { Input } from "$lib/components/ui/input";
  import { Separator } from "$lib/components/ui/separator";
  import { getDeviceInfo, approveDevice, denyDevice } from "../../../oauth/oauth.remote";
  import { getOAuthScopeDescription } from "$lib/constants/oauth-scopes";
  import QRCode from "qrcode";

  // ── View state ──────────────────────────────────────────────────────

  type ViewState = "selection" | "connector" | "uploader";
  let viewState = $state<ViewState>("selection");

  // ── Connector state ───────────────────────────────────────────────
  let selectedConnectorId = $state<string | null>(null);

  // ── Uploader state ────────────────────────────────────────────────
  let selectedApp = $state<UploaderApp | null>(null);
  let setupResponse = $state<UploaderSetupResponse | null>(null);
  let setupLoading = $state(false);

  // ── Data state ────────────────────────────────────────────────────

  let uploaderApps = $state<UploaderApp[]>([]);
  let connectors = $state<AvailableConnector[]>([]);
  let dataSources = $state<DataSourceInfo[]>([]);
  let isLoading = $state(true);
  let loadError = $state<string | null>(null);


  // ── Device authorization (OAuth) state ────────────────────────────

  let qrCodeDataUrl = $state<string | null>(null);
  let deviceCodeInput = $state("");
  let deviceLookupLoading = $state(false);
  let deviceLookupError = $state<string | null>(null);
  let deviceInfo = $state<{
    userCode: string;
    clientId: string;
    displayName: string | null;
    isKnown: boolean;
    scopes: string[];
  } | null>(null);
  let deviceApproveLoading = $state(false);
  let deviceApproved = $state(false);
  let deviceDenied = $state(false);

  // ── Connection polling ────────────────────────────────────────────

  let pollInterval = $state<ReturnType<typeof setInterval> | null>(null);

  // ── Platform filter ──────────────────────────────────────────────

  type PlatformFilter = "all" | UploaderPlatform;
  let platformFilter = $state<PlatformFilter>("all");

  // ── Categories ────────────────────────────────────────────────────

  const categoryLabels: Record<string, string> = {
    [UploaderCategory.Cgm]: "CGM Apps",
    [UploaderCategory.AidSystem]: "AID Systems",
    [UploaderCategory.Uploader]: "General Uploaders",
  };

  const categoryOrder = [UploaderCategory.Cgm, UploaderCategory.AidSystem, UploaderCategory.Uploader];

  const filteredApps = $derived(
    platformFilter === "all"
      ? uploaderApps
      : uploaderApps.filter((app) => app.platform === platformFilter),
  );

  const groupedApps = $derived.by(() => {
    const groups: Record<string, UploaderApp[]> = {};
    for (const app of filteredApps) {
      const cat = app.category ?? UploaderCategory.Uploader as string;
      if (!groups[cat]) groups[cat] = [];
      groups[cat].push(app);
    }
    return categoryOrder
      .filter((cat) => groups[cat]?.length)
      .map((cat) => ({ category: cat, label: categoryLabels[cat] ?? cat, apps: groups[cat] }));
  });

  // ── Detect which uploaders have sent data ─────────────────────────

  function isDetected(appId: string | undefined): boolean {
    if (!appId) return false;
    return dataSources.some(
      (ds) => ds.sourceType?.toLowerCase() === appId.toLowerCase(),
    );
  }

  function getDataSource(appId: string | undefined): DataSourceInfo | undefined {
    if (!appId) return undefined;
    return dataSources.find(
      (ds) => ds.sourceType?.toLowerCase() === appId.toLowerCase(),
    );
  }

  // ── Load data ─────────────────────────────────────────────────────

  onMount(async () => {
    await loadData();
  });

  onDestroy(() => {
    stopPolling();
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
    setupLoading = true;
    viewState = "uploader";
    resetDeviceState();

    try {
      const result = await getUploaderSetup(app.id!);
      setupResponse = result;

      // Generate QR code for apps with OAuth deep-link support
      if (result?.connectUrl) {
        await generateQrCode(result.connectUrl);
      }
    } catch {
      setupResponse = null;
    } finally {
      setupLoading = false;
    }

    // Start polling for connection if not already detected (legacy uploaders only)
    if (!isDetected(app.id) && !setupResponse?.connectUrl) {
      startPolling();
    }
  }

  function handleBackToSelection() {
    stopPolling();
    selectedApp = null;
    setupResponse = null;
    selectedConnectorId = null;
    resetDeviceState();
    viewState = "selection";
  }

  function resetDeviceState() {
    qrCodeDataUrl = null;
    deviceCodeInput = "";
    deviceLookupLoading = false;
    deviceLookupError = null;
    deviceInfo = null;
    deviceApproveLoading = false;
    deviceApproved = false;
    deviceDenied = false;
  }

  async function generateQrCode(url: string) {
    try {
      qrCodeDataUrl = await QRCode.toDataURL(url, {
        width: 200,
        margin: 2,
        color: { dark: "#000000", light: "#ffffff" },
      });
    } catch {
      qrCodeDataUrl = null;
    }
  }

  async function lookupDeviceCode() {
    const code = deviceCodeInput.trim();
    if (!code) return;

    deviceLookupLoading = true;
    deviceLookupError = null;

    try {
      const info = await getDeviceInfo({ userCode: code });
      if (!info) {
        deviceLookupError = "Invalid or expired device code.";
        return;
      }
      deviceInfo = {
        userCode: info.userCode ?? code,
        clientId: info.clientId ?? "",
        displayName: info.clientDisplayName ?? null,
        isKnown: info.isKnownClient ?? false,
        scopes: (info.scopes ?? []).filter(Boolean) as string[],
      };
    } catch {
      deviceLookupError = "Invalid or expired device code. Please check and try again.";
    } finally {
      deviceLookupLoading = false;
    }
  }

  async function handleApproveDevice() {
    if (!deviceInfo) return;
    deviceApproveLoading = true;
    try {
      await approveDevice({ userCode: deviceInfo.userCode });
      deviceApproved = true;
      startPolling();
    } catch {
      deviceLookupError = "Failed to approve. The code may have expired.";
    } finally {
      deviceApproveLoading = false;
    }
  }

  async function handleDenyDevice() {
    if (!deviceInfo) return;
    deviceApproveLoading = true;
    try {
      await denyDevice({ userCode: deviceInfo.userCode });
      deviceDenied = true;
    } catch {
      deviceLookupError = "Failed to deny the request.";
    } finally {
      deviceApproveLoading = false;
    }
  }

  // ── Polling ───────────────────────────────────────────────────────

  function startPolling() {
    stopPolling();
    pollInterval = setInterval(async () => {
      try {
        const sources = await getActiveDataSources();
        dataSources = sources ?? [];

        // Stop polling if we detect data from the selected app
        if (selectedApp && isDetected(selectedApp.id)) {
          stopPolling();
        }
      } catch {
        // Silently continue polling
      }
    }, 15_000);
  }

  function stopPolling() {
    if (pollInterval) {
      clearInterval(pollInterval);
      pollInterval = null;
    }
  }

  // ── Skip ──────────────────────────────────────────────────────────

  async function handleSkip() {
    await markSetupComplete();
    await goto("/", { invalidateAll: true });
  }

  // ── Platform icon helper ──────────────────────────────────────────

  function getPlatformLabel(platform: UploaderPlatform | undefined): string {
    switch (platform) {
      case UploaderPlatform.IOS:
        return "iOS";
      case UploaderPlatform.Android:
        return "Android";
      case UploaderPlatform.Desktop:
        return "Desktop";
      case UploaderPlatform.Web:
        return "Web";
      default:
        return "Unknown";
    }
  }

  function getCategoryIcon(category: UploaderCategory | undefined) {
    switch (category) {
      case UploaderCategory.Cgm:
      case UploaderCategory.AidSystem:
        return Activity;
      case UploaderCategory.Uploader:
        return Upload;
      default:
        return Upload;
    }
  }
</script>

<svelte:head>
  <title>Connect a Data Source - Setup - Nocturne</title>
</svelte:head>

<div class="container mx-auto max-w-2xl p-6 space-y-6">
  {#if viewState === "selection"}
    <!-- ── Selection View ──────────────────────────────────────── -->
    <div>
      <h1 class="text-2xl font-bold tracking-tight">Connect a Data Source</h1>
      <p class="text-muted-foreground">
        Choose a cloud service or phone app to start sending glucose and treatment data to Nocturne.
      </p>
    </div>

    {#if isLoading}
      <div class="flex items-center justify-center py-12">
        <Loader2 class="h-6 w-6 animate-spin text-muted-foreground" />
      </div>
    {:else if loadError}
      <Card.Root class="border-destructive">
        <Card.Content class="flex items-center gap-3 pt-6">
          <AlertCircle class="h-5 w-5 text-destructive" />
          <div>
            <p class="font-medium">Failed to load data sources</p>
            <p class="text-sm text-muted-foreground">{loadError}</p>
          </div>
        </Card.Content>
      </Card.Root>
    {:else}
      <div class="space-y-8">
        <!-- Cloud Services (Connectors) -->
        {#if connectors.length > 0}
          <div class="space-y-3">
            <h2 class="text-sm font-medium text-muted-foreground flex items-center gap-2">
              <Cloud class="h-4 w-4" />
              Cloud Services
            </h2>
            <div class="grid gap-3 sm:grid-cols-2">
              {#each connectors as connector (connector.id)}
                {@const configured = connector.isConfigured ?? false}
                <button
                  type="button"
                  class="flex items-center gap-4 p-4 rounded-lg border transition-colors text-left group {configured
                    ? 'border-green-500/30 bg-green-500/5 hover:bg-green-500/10'
                    : 'bg-muted/30 hover:border-primary/50 hover:bg-accent/50'}"
                  onclick={() => selectConnector(connector)}
                >
                  <div
                    class="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg {configured
                      ? 'bg-green-500/10 text-green-600'
                      : 'bg-primary/10 text-primary'}"
                  >
                    {#if configured}
                      <CheckCircle class="h-5 w-5" />
                    {:else}
                      <Plug class="h-5 w-5" />
                    {/if}
                  </div>
                  <div class="flex-1 min-w-0">
                    <div class="flex items-center gap-2 flex-wrap">
                      <span class="font-medium">{connector.name}</span>
                      {#if configured}
                        <Badge variant="secondary" class="text-xs text-green-600">
                          Connected
                        </Badge>
                      {/if}
                    </div>
                    {#if connector.description}
                      <p class="text-sm text-muted-foreground line-clamp-1">
                        {connector.description}
                      </p>
                    {/if}
                  </div>
                  <ChevronRight
                    class="h-4 w-4 text-muted-foreground group-hover:text-foreground transition-colors shrink-0"
                  />
                </button>
              {/each}
            </div>
          </div>
        {/if}

        <!-- Phone Apps (Uploaders) -->
        {#if uploaderApps.length > 0}
          <div class="space-y-3">
            <div class="flex items-center justify-between">
              <h2 class="text-sm font-medium text-muted-foreground flex items-center gap-2">
                <Smartphone class="h-4 w-4" />
                Phone Apps
              </h2>
              <div class="flex items-center gap-1">
                <Button
                  variant={platformFilter === "all" ? "default" : "outline"}
                  size="sm"
                  onclick={() => (platformFilter = "all")}
                >
                  All
                </Button>
                <Button
                  variant={platformFilter === UploaderPlatform.IOS ? "default" : "outline"}
                  size="sm"
                  class="gap-1.5"
                  onclick={() => (platformFilter = UploaderPlatform.IOS)}
                >
                  <Apple class="h-3.5 w-3.5" />
                  iOS
                </Button>
                <Button
                  variant={platformFilter === UploaderPlatform.Android ? "default" : "outline"}
                  size="sm"
                  class="gap-1.5"
                  onclick={() => (platformFilter = UploaderPlatform.Android)}
                >
                  <Smartphone class="h-3.5 w-3.5" />
                  Android
                </Button>
              </div>
            </div>

            <div class="space-y-6">
              {#each groupedApps as group (group.category)}
                <div class="space-y-3">
                  <h3 class="text-sm font-medium text-muted-foreground">{group.label}</h3>
                  <div class="grid gap-3 sm:grid-cols-2">
                    {#each group.apps as app (app.id)}
                      {@const detected = isDetected(app.id)}
                      {@const Icon = getCategoryIcon(app.category)}
                      <button
                        type="button"
                        class="flex items-center gap-4 p-4 rounded-lg border transition-colors text-left group {detected
                          ? 'border-green-500/30 bg-green-500/5 hover:bg-green-500/10'
                          : 'bg-muted/30 hover:border-primary/50 hover:bg-accent/50'}"
                        onclick={() => selectApp(app)}
                      >
                        <div
                          class="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg {detected
                            ? 'bg-green-500/10 text-green-600'
                            : 'bg-primary/10 text-primary'}"
                        >
                          {#if detected}
                            <CheckCircle class="h-5 w-5" />
                          {:else}
                            <Icon class="h-5 w-5" />
                          {/if}
                        </div>
                        <div class="flex-1 min-w-0">
                          <div class="flex items-center gap-2 flex-wrap">
                            <span class="font-medium">{app.name}</span>
                            <Badge variant="outline" class="text-xs gap-1">
                              {getPlatformLabel(app.platform)}
                            </Badge>
                            {#if detected}
                              <Badge variant="secondary" class="text-xs text-green-600">
                                Connected
                              </Badge>
                            {/if}
                          </div>
                          {#if app.description}
                            <p class="text-sm text-muted-foreground line-clamp-1">
                              {app.description}
                            </p>
                          {/if}
                        </div>
                        <ChevronRight
                          class="h-4 w-4 text-muted-foreground group-hover:text-foreground transition-colors shrink-0"
                        />
                      </button>
                    {/each}
                  </div>
                </div>
              {/each}
            </div>
          </div>
        {/if}

        <!-- Empty state when both are empty -->
        {#if connectors.length === 0 && uploaderApps.length === 0}
          <Card.Root>
            <Card.Content class="py-8 text-center">
              <Plug class="h-12 w-12 mx-auto mb-4 text-muted-foreground" />
              <p class="font-medium">No data sources available</p>
              <p class="text-sm text-muted-foreground mt-1">
                There are no data sources available at this time.
              </p>
            </Card.Content>
          </Card.Root>
        {/if}
      </div>

      <!-- Skip link -->
      <div class="pt-4 text-center">
        <button
          type="button"
          class="text-sm text-muted-foreground hover:text-foreground underline-offset-4 hover:underline transition-colors"
          onclick={handleSkip}
        >
          Skip for now
        </button>
      </div>
    {/if}

  {:else if viewState === "connector"}
    <!-- ── Connector View ──────────────────────────────────────── -->
    <div class="space-y-4">
      <Button
        variant="ghost"
        size="sm"
        class="gap-1 -ml-2"
        onclick={handleConnectorCancel}
      >
        <ChevronLeft class="h-4 w-4" />
        Back to data sources
      </Button>

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
    <!-- ── Uploader View ───────────────────────────────────────── -->
    <div class="space-y-4">
      <Button
        variant="ghost"
        size="sm"
        class="gap-1 -ml-2"
        onclick={handleBackToSelection}
      >
        <ChevronLeft class="h-4 w-4" />
        Back to data sources
      </Button>

      {#if setupLoading}
        <div class="flex items-center justify-center py-8">
          <Loader2 class="h-6 w-6 animate-spin text-muted-foreground" />
        </div>
      {:else if setupResponse}
        <div>
          <h2 class="text-lg font-semibold">{selectedApp?.name}</h2>
          {#if selectedApp?.description}
            <p class="text-sm text-muted-foreground">
              {selectedApp.description}
            </p>
          {/if}
        </div>

        {#if setupResponse.connectUrl}
          <!-- ── OAuth Device Flow (QR code + inline authorization) ── -->
          {#if deviceApproved}
            <!-- Approved — waiting for data -->
            <Card.Root class="border-green-500/30">
              <Card.Content class="space-y-2 pt-6">
                <div class="flex items-center gap-3">
                  <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-green-100 dark:bg-green-900/30">
                    <Check class="h-5 w-5 text-green-600 dark:text-green-400" />
                  </div>
                  <div>
                    <p class="font-medium text-green-600">Device Authorized</p>
                    <p class="text-sm text-muted-foreground">
                      {selectedApp?.name} is now connected. You can return to your device.
                    </p>
                  </div>
                </div>
              </Card.Content>
            </Card.Root>

            <!-- Connection status (polling for data) -->
            {@const detected = isDetected(selectedApp?.id)}
            {@const ds = getDataSource(selectedApp?.id)}
            {#if detected && ds}
              <Card.Root class="border-green-500/30">
                <Card.Content class="flex items-center gap-3 pt-6">
                  <CheckCircle class="h-5 w-5 text-green-500" />
                  <div>
                    <p class="font-medium text-green-600">Receiving Data</p>
                    <p class="text-sm text-muted-foreground">
                      {selectedApp?.name} is sending data. {ds.entriesLast24h ?? 0} entries in the last 24 hours.
                    </p>
                  </div>
                </Card.Content>
              </Card.Root>

              <div class="flex justify-end">
                <Button onclick={handleSkip}>Continue to Dashboard</Button>
              </div>
            {:else}
              <Card.Root class="border-muted">
                <Card.Content class="flex items-center gap-3 pt-6">
                  <Clock class="h-5 w-5 text-muted-foreground" />
                  <div>
                    <p class="font-medium">Waiting for data</p>
                    <p class="text-sm text-muted-foreground">
                      It can take a few minutes for the first glucose reading to arrive. This page will update automatically.
                    </p>
                  </div>
                </Card.Content>
              </Card.Root>
            {/if}

          {:else if deviceDenied}
            <!-- Denied -->
            <Card.Root>
              <Card.Content class="flex items-center gap-3 pt-6">
                <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-muted">
                  <X class="h-5 w-5 text-muted-foreground" />
                </div>
                <div>
                  <p class="font-medium">Authorization Denied</p>
                  <p class="text-sm text-muted-foreground">The device will not be granted access.</p>
                </div>
              </Card.Content>
            </Card.Root>

          {:else if deviceInfo}
            <!-- Step 2: Approve / deny the device -->
            <Card.Root>
              <Card.Header class="space-y-1 text-center">
                <div class="mx-auto mb-2 flex h-12 w-12 items-center justify-center rounded-full bg-primary/10">
                  <Shield class="h-6 w-6 text-primary" />
                </div>
                <Card.Title class="text-lg">Authorize Application</Card.Title>
                <Card.Description>
                  <span class="font-semibold text-foreground">{deviceInfo.displayName ?? deviceInfo.clientId}</span>
                  wants to access your Nocturne data.
                </Card.Description>
              </Card.Header>
              <Card.Content class="space-y-4">
                {#if !deviceInfo.isKnown}
                  <div class="flex items-start gap-3 rounded-md border border-yellow-200 bg-yellow-50 p-3 dark:border-yellow-900/50 dark:bg-yellow-900/20">
                    <AlertTriangle class="mt-0.5 h-4 w-4 shrink-0 text-yellow-600 dark:text-yellow-400" />
                    <p class="text-sm text-yellow-800 dark:text-yellow-200">
                      This application is not in the Nocturne known app directory. Only approve if you trust it.
                    </p>
                  </div>
                {/if}

                <Separator />

                <div>
                  <p class="mb-3 text-sm font-medium">This application is requesting permission to:</p>
                  <ul class="space-y-2">
                    {#each deviceInfo.scopes as scope}
                      <li class="flex items-start gap-3 text-sm">
                        <Check class="mt-0.5 h-4 w-4 shrink-0 text-primary" />
                        <span class="text-muted-foreground">{getOAuthScopeDescription(scope)}</span>
                      </li>
                    {/each}
                  </ul>
                </div>

                {#if deviceInfo.scopes.includes("*")}
                  <div class="flex items-start gap-3 rounded-md border border-destructive/20 bg-destructive/5 p-3">
                    <ShieldAlert class="mt-0.5 h-4 w-4 shrink-0 text-destructive" />
                    <p class="text-sm text-destructive">This app is requesting full access, including the ability to delete data.</p>
                  </div>
                {:else}
                  <div class="flex items-start gap-3 rounded-md bg-muted/50 p-3">
                    <Shield class="mt-0.5 h-4 w-4 shrink-0 text-muted-foreground" />
                    <p class="text-sm text-muted-foreground">This app cannot delete your data.</p>
                  </div>
                {/if}

                {#if deviceLookupError}
                  <div class="flex items-start gap-3 rounded-md border border-destructive/20 bg-destructive/5 p-3">
                    <AlertTriangle class="mt-0.5 h-4 w-4 shrink-0 text-destructive" />
                    <p class="text-sm text-destructive">{deviceLookupError}</p>
                  </div>
                {/if}

                <Separator />

                <div class="flex gap-3">
                  <Button
                    variant="outline"
                    class="flex-1"
                    disabled={deviceApproveLoading}
                    onclick={handleDenyDevice}
                  >
                    {#if deviceApproveLoading}
                      <Loader2 class="mr-2 h-4 w-4 animate-spin" />
                    {/if}
                    Deny
                  </Button>
                  <Button
                    class="flex-1"
                    disabled={deviceApproveLoading}
                    onclick={handleApproveDevice}
                  >
                    {#if deviceApproveLoading}
                      <Loader2 class="mr-2 h-4 w-4 animate-spin" />
                    {/if}
                    Approve
                  </Button>
                </div>
              </Card.Content>
            </Card.Root>

          {:else}
            <!-- Step 1: QR code + code entry -->
            <Card.Root>
              <Card.Header class="text-center pb-3">
                <Card.Title class="text-sm">Scan to Connect</Card.Title>
                <Card.Description>
                  Scan this QR code with your phone's camera to open {selectedApp?.name} and start the connection.
                </Card.Description>
              </Card.Header>
              <Card.Content class="flex flex-col items-center gap-4">
                {#if qrCodeDataUrl}
                  <div class="rounded-lg border bg-white p-2">
                    <img src={qrCodeDataUrl} alt="QR code to connect {selectedApp?.name}" class="h-48 w-48" />
                  </div>
                {:else}
                  <div class="flex h-48 w-48 items-center justify-center rounded-lg border bg-muted">
                    <Loader2 class="h-6 w-6 animate-spin text-muted-foreground" />
                  </div>
                {/if}
              </Card.Content>
            </Card.Root>

            <Card.Root>
              <Card.Header class="pb-3">
                <Card.Title class="text-sm">Enter Authorization Code</Card.Title>
                <Card.Description>
                  After scanning, {selectedApp?.name} will show an 8-character code. Enter it here to approve the connection.
                </Card.Description>
              </Card.Header>
              <Card.Content class="space-y-4">
                {#if deviceLookupError}
                  <div class="flex items-start gap-3 rounded-md border border-destructive/20 bg-destructive/5 p-3">
                    <AlertTriangle class="mt-0.5 h-4 w-4 shrink-0 text-destructive" />
                    <p class="text-sm text-destructive">{deviceLookupError}</p>
                  </div>
                {/if}

                <form
                  class="flex gap-2"
                  onsubmit={(e) => {
                    e.preventDefault();
                    lookupDeviceCode();
                  }}
                >
                  <Input
                    type="text"
                    placeholder="XXXX-YYYY"
                    maxlength={9}
                    autocomplete="off"
                    class="text-center text-lg tracking-widest uppercase"
                    bind:value={deviceCodeInput}
                    disabled={deviceLookupLoading}
                  />
                  <Button type="submit" disabled={deviceLookupLoading || !deviceCodeInput.trim()}>
                    {#if deviceLookupLoading}
                      <Loader2 class="h-4 w-4 animate-spin" />
                    {:else}
                      Continue
                    {/if}
                  </Button>
                </form>
              </Card.Content>
            </Card.Root>
          {/if}

        {/if}
      {:else}
        <Card.Root class="border-destructive">
          <Card.Content class="flex items-center gap-3 pt-6">
            <AlertCircle class="h-5 w-5 text-destructive" />
            <div>
              <p class="font-medium">Failed to load setup instructions</p>
              <p class="text-sm text-muted-foreground">
                Could not load setup details for {selectedApp?.name}. Please try again.
              </p>
            </div>
          </Card.Content>
        </Card.Root>
      {/if}
    </div>
  {/if}
</div>
