<script lang="ts">
  import { onMount, onDestroy } from "svelte";
  import { browser } from "$app/environment";
  import QRCode from "qrcode";
  import { getActiveDataSources } from "$api/generated/services.generated.remote";
  import type { DataSourceInfo } from "$lib/api/generated/nocturne-api-client";
  import { Button } from "$lib/components/ui/button";
  import { Smartphone, CheckCircle, Loader2 } from "lucide-svelte";
  import { buildXdripDeepLink, buildConnectPageUrl } from "$lib/utils/xdrip-links";

  /** Origin URL of the Nocturne instance (trailing slash tolerated). */
  let { instanceUrl }: { instanceUrl: string } = $props();

  let qrDataUrl = $state<string | null>(null);
  let isAndroid = $state(false);

  type ConnectionState = "waiting" | "connected" | "timeout";
  let connectionState = $state<ConnectionState>("waiting");
  let pollInterval: ReturnType<typeof setInterval> | null = null;
  let pollStartTime = 0;
  const POLL_INTERVAL_MS = 10_000;
  const POLL_TIMEOUT_MS = 60_000;

  const connectPageUrl = $derived(buildConnectPageUrl(instanceUrl));
  const deepLink = $derived(buildXdripDeepLink(instanceUrl));

  onMount(async () => {
    if (browser) {
      isAndroid = /android/i.test(navigator.userAgent);
    }
    try {
      qrDataUrl = await QRCode.toDataURL(connectPageUrl, {
        width: 200,
        margin: 2,
        color: { dark: "#000000", light: "#ffffff" },
      });
    } catch (err) {
      console.warn("[XdripQuickConnect] QR code generation failed:", err);
    }
    startPolling();
  });

  onDestroy(() => {
    stopPolling();
  });

  function isXdripDetected(sources: DataSourceInfo[]): boolean {
    return sources.some((ds) => ds.sourceType?.toLowerCase() === "xdrip");
  }

  async function checkStatus() {
    try {
      const sources = (await getActiveDataSources()) ?? [];
      if (isXdripDetected(sources)) {
        connectionState = "connected";
        stopPolling();
        return;
      }
      if (Date.now() - pollStartTime > POLL_TIMEOUT_MS) {
        connectionState = "timeout";
        stopPolling();
      }
    } catch (err) {
      console.warn("[XdripQuickConnect] Data source poll failed:", err);
    }
  }

  function startPolling() {
    stopPolling();
    connectionState = "waiting";
    pollStartTime = Date.now();
    checkStatus();
    pollInterval = setInterval(checkStatus, POLL_INTERVAL_MS);
  }

  function stopPolling() {
    if (pollInterval !== null) {
      clearInterval(pollInterval);
      pollInterval = null;
    }
  }

  function retryCheck() {
    startPolling();
  }
</script>

<div class="space-y-4">
  {#if connectionState === "waiting"}
    <div
      role="status"
      aria-live="polite"
      aria-atomic="true"
      class="bg-muted text-muted-foreground flex items-center gap-2 rounded p-3 text-sm"
    >
      <Loader2 class="h-4 w-4 animate-spin" />
      Waiting for data from xDrip+...
    </div>
  {:else if connectionState === "connected"}
    <div
      role="status"
      aria-live="polite"
      aria-atomic="true"
      class="flex items-center gap-2 rounded bg-green-50 p-3 text-sm text-green-900 dark:bg-green-950 dark:text-green-100"
    >
      <CheckCircle class="h-4 w-4" />
      Connected! Data is flowing from xDrip+.
    </div>
  {:else if connectionState === "timeout"}
    <div
      role="status"
      aria-live="polite"
      aria-atomic="true"
      class="bg-muted space-y-2 rounded p-3 text-sm"
    >
      <p>No data from xDrip+ yet. This is normal if xDrip+ hasn't had a new reading.</p>
      <Button variant="outline" size="sm" onclick={retryCheck}>
        Check now
      </Button>
    </div>
  {/if}

  <div>
    <p class="text-sm font-medium">Quick Connect</p>
    <p class="text-muted-foreground text-sm">
      Automatically configure xDrip+ with your Nocturne instance.
    </p>
  </div>

  {#if isAndroid}
    <Button href={deepLink} class="w-full">
      <Smartphone class="mr-2 h-4 w-4" />
      Open in xDrip+
    </Button>
    {#if qrDataUrl}
      <details class="text-sm">
        <summary class="text-muted-foreground cursor-pointer">
          Or scan from another device
        </summary>
        <div class="flex justify-center pt-3">
          <img
            src={qrDataUrl}
            alt="QR code to connect xDrip+"
            width="200"
            height="200"
            class="rounded"
          />
        </div>
      </details>
    {/if}
  {:else}
    {#if qrDataUrl}
      <div class="flex justify-center">
        <img
          src={qrDataUrl}
          alt="QR code to connect xDrip+"
          width="200"
          height="200"
          class="rounded"
        />
      </div>
      <p class="text-muted-foreground text-center text-sm">
        Scan this QR code with your phone's camera
      </p>
    {/if}
    <details class="text-sm">
      <summary class="text-muted-foreground cursor-pointer">
        Or open this link on your phone
      </summary>
      <code class="bg-muted mt-2 block rounded px-3 py-2 text-xs break-all">
        {connectPageUrl}
      </code>
    </details>
  {/if}
</div>
