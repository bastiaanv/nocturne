<script lang="ts">
  import { onMount } from "svelte";
  import { browser } from "$app/environment";
  import QRCode from "qrcode";
  import { Button } from "$lib/components/ui/button";
  import { Smartphone } from "lucide-svelte";
  import { buildXdripDeepLink, buildConnectPageUrl } from "$lib/utils/xdrip-links";

  /** Origin URL of the Nocturne instance (trailing slash tolerated). */
  let { instanceUrl }: { instanceUrl: string } = $props();

  let qrDataUrl = $state<string | null>(null);
  let isAndroid = $state(false);

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
    } catch {
      // QR generation failed — users can still copy the connect URL below.
    }
  });
</script>

<div class="space-y-4">
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
