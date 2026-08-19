<script lang="ts">
  import { page } from "$app/state";
  import { Button } from "$lib/components/ui/button";
  import * as Card from "$lib/components/ui/card";
  import { Copy, Palette } from "lucide-svelte";
  import {
    getShareLink,
    setShareLinkAppearance,
  } from "$api/generated/shareLinks.generated.remote";
  import {
    glucoseUnits,
    timeFormat,
    colorTheme,
    getColorScheme,
  } from "$lib/stores/appearance-store.svelte";
  import type { ShareAppearance } from "$lib/api";

  const effectivePermissions: string[] = $derived(
    (page.data as any).effectivePermissions ?? [],
  );
  const canManageSharing = $derived(
    effectivePermissions.includes("*") ||
      effectivePermissions.includes("sharing.manage"),
  );

  const shareQuery = $derived(canManageSharing ? getShareLink() : null);
  const share = $derived(shareQuery?.current ?? null);

  // Optimistic override held only while a mutation is in flight; null = use server truth.
  let pendingAppearance = $state<ShareAppearance | null>(null);
  const appearance = $derived(pendingAppearance ?? share?.appearance ?? {});

  let busy = $state(false);
  let errorMessage = $state<string | null>(null);
  let successMessage = $state<string | null>(null);

  async function setAppearance(update: Partial<ShareAppearance>) {
    const next = { ...appearance, ...update };
    pendingAppearance = next;
    errorMessage = null;
    busy = true;
    try {
      await setShareLinkAppearance({ appearance: next });
    } catch {
      errorMessage = "Couldn't update the link's appearance. Please try again.";
    } finally {
      pendingAppearance = null;
      busy = false;
    }
  }

  // The share link has no identity of its own, so an admin starting from their own look uses
  // their current appearance settings as the starting point.
  async function copyMyAppearance() {
    errorMessage = null;
    busy = true;
    try {
      await setShareLinkAppearance({
        appearance: {
          glucoseUnits: glucoseUnits.current,
          timeFormat: timeFormat.current,
          colorTheme: colorTheme.current,
          colorScheme: getColorScheme(),
        },
      });
      successMessage = "Your appearance was copied to the public link.";
      setTimeout(() => (successMessage = null), 3000);
    } catch {
      errorMessage = "Couldn't copy your appearance to the link. Please try again.";
    } finally {
      busy = false;
    }
  }
</script>

{#if canManageSharing}
  <Card.Root>
    <div class="flex items-start gap-4 border-b border-border p-5 @md:p-6">
      <div class="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-primary">
        <Palette class="h-5 w-5" />
      </div>
      <div class="min-w-0 flex-1">
        <h2 class="text-lg font-semibold">Public link appearance</h2>
        <p class="mt-0.5 max-w-prose text-sm text-muted-foreground">
          How the read-only view looks to anyone with your link. Applies even
          before the link is turned on.
        </p>
      </div>
      <Button
        variant="outline"
        size="sm"
        class="shrink-0"
        disabled={busy}
        onclick={copyMyAppearance}
      >
        <Copy class="mr-1.5 h-4 w-4" />
        Copy my appearance
      </Button>
    </div>

    <div class="space-y-4 px-5 py-5 @md:px-6">
      {#if errorMessage}
        <div class="rounded-md border border-destructive/20 bg-destructive/5 p-3">
          <p class="text-sm text-destructive">{errorMessage}</p>
        </div>
      {/if}
      {#if successMessage}
        <div class="rounded-md border border-green-200 bg-green-50 p-3 dark:border-green-900/50 dark:bg-green-900/20">
          <p class="text-sm text-green-800 dark:text-green-200">{successMessage}</p>
        </div>
      {/if}

      <div class="grid gap-4 @md:grid-cols-2">
        <div class="space-y-1.5">
          <div class="text-xs font-medium text-muted-foreground">Glucose units</div>
          <div class="inline-flex rounded-lg bg-muted p-1 @sm:flex">
            {#each ["mg/dl", "mmol"] as unit (unit)}
              <button
                type="button"
                onclick={() => setAppearance({ glucoseUnits: unit })}
                class="flex-1 rounded-md px-3 py-1.5 text-xs font-medium transition-colors {appearance.glucoseUnits ===
                unit
                  ? 'bg-background text-foreground shadow-sm'
                  : 'text-muted-foreground hover:text-foreground'}"
              >
                {unit === "mg/dl" ? "mg/dL" : "mmol/L"}
              </button>
            {/each}
          </div>
        </div>

        <div class="space-y-1.5">
          <div class="text-xs font-medium text-muted-foreground">Time format</div>
          <div class="inline-flex rounded-lg bg-muted p-1 @sm:flex">
            {#each ["12", "24"] as fmt (fmt)}
              <button
                type="button"
                onclick={() => setAppearance({ timeFormat: fmt })}
                class="flex-1 rounded-md px-3 py-1.5 text-xs font-medium transition-colors {appearance.timeFormat ===
                fmt
                  ? 'bg-background text-foreground shadow-sm'
                  : 'text-muted-foreground hover:text-foreground'}"
              >
                {fmt}h
              </button>
            {/each}
          </div>
        </div>

        <div class="space-y-1.5">
          <div class="text-xs font-medium text-muted-foreground">Color scheme</div>
          <div class="inline-flex rounded-lg bg-muted p-1 @sm:flex">
            {#each [
              { value: "system", label: "System" },
              { value: "light", label: "Light" },
              { value: "dark", label: "Dark" },
            ] as scheme (scheme.value)}
              <button
                type="button"
                onclick={() => setAppearance({ colorScheme: scheme.value })}
                class="flex-1 rounded-md px-3 py-1.5 text-xs font-medium transition-colors {appearance.colorScheme ===
                scheme.value
                  ? 'bg-background text-foreground shadow-sm'
                  : 'text-muted-foreground hover:text-foreground'}"
              >
                {scheme.label}
              </button>
            {/each}
          </div>
        </div>

        <div class="space-y-1.5">
          <div class="text-xs font-medium text-muted-foreground">Color theme</div>
          <div class="inline-flex rounded-lg bg-muted p-1 @sm:flex">
            {#each [
              { value: "nocturne", label: "Nocturne" },
              { value: "trio", label: "Trio" },
              { value: "aaps", label: "AAPS" },
              { value: "classic", label: "Classic" },
            ] as theme (theme.value)}
              <button
                type="button"
                onclick={() => setAppearance({ colorTheme: theme.value })}
                class="flex-1 rounded-md px-2 py-1.5 text-xs font-medium transition-colors {appearance.colorTheme ===
                theme.value
                  ? 'bg-background text-foreground shadow-sm'
                  : 'text-muted-foreground hover:text-foreground'}"
              >
                {theme.label}
              </button>
            {/each}
          </div>
        </div>
      </div>
    </div>
  </Card.Root>
{/if}
