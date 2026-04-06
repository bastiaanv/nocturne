<script lang="ts">
  import WizardShell from "$lib/components/setup/WizardShell.svelte";
  import { RadioGroup, RadioGroupItem } from "$lib/components/ui/radio-group";
  import { Switch } from "$lib/components/ui/switch";
  import { Checkbox } from "$lib/components/ui/checkbox";
  import { Label } from "$lib/components/ui/label";
  import {
    Collapsible,
    CollapsibleContent,
    CollapsibleTrigger,
  } from "$lib/components/ui/collapsible";
  import {
    Globe,
    Lock,
    ChevronDown,
    Info,
  } from "lucide-svelte";
  import {
    getPublicAccessConfig,
    getTenantRoles,
    savePublicAccess,
  } from "./permissions.remote";

  const configQuery = $derived(getPublicAccessConfig());
  const rolesQuery = $derived(getTenantRoles());

  // Form state
  let mode = $state<"denied" | "readable">("denied");
  let limitTo24Hours = $state(true);
  let advancedOpen = $state(false);
  let saving = $state(false);
  let initialized = $state(false);

  // Sync form state from loaded config (once)
  $effect(() => {
    const config = configQuery.current;
    if (config && !initialized) {
      mode = config.mode;
      limitTo24Hours = config.limitTo24Hours;
      initialized = true;
    }
  });

  const resources = [
    { key: "entries.read", label: "Glucose entries" },
    { key: "treatments.read", label: "Treatments" },
    { key: "devicestatus.read", label: "Device status" },
    { key: "profile.read", label: "Profiles" },
    { key: "health.read", label: "Health data" },
  ];

  // Per-resource checkbox state for the advanced section
  let resourceChecks = $state<Record<string, boolean>>(
    Object.fromEntries(resources.map((r) => [r.key, true])),
  );

  const readableRoleId = $derived(
    (rolesQuery.current ?? []).find((r) => r.slug === "readable")?.id,
  );
  const deniedRoleId = $derived(
    (rolesQuery.current ?? []).find((r) => r.slug === "denied")?.id,
  );

  const summaryText = $derived.by(() => {
    if (mode === "denied") {
      return "Your data is private. Only people you explicitly invite will have access.";
    }
    if (limitTo24Hours) {
      return "Anyone can view the last 24 hours of your data without signing in.";
    }
    return "Anyone can view all of your data without signing in.";
  });

  async function handleSave(): Promise<boolean> {
    const config = configQuery.current;
    if (!config?.memberId) {
      // No Public member exists yet — skip silently during setup.
      // The backend creates it on first tenant provisioning; if it's
      // missing the user can configure later in Settings > Members.
      return true;
    }

    saving = true;
    try {
      await savePublicAccess({
        memberId: config.memberId,
        mode,
        limitTo24Hours,
        readableRoleId,
        deniedRoleId,
      });
      return true;
    } catch (err) {
      console.error("Failed to save public access settings:", err);
      return false;
    } finally {
      saving = false;
    }
  }
</script>

<svelte:head>
  <title>Sharing & Privacy - Setup - Nocturne</title>
</svelte:head>

<WizardShell
  title="Sharing & Privacy"
  description="Choose who can see your data. This controls the built-in Public access subject."
  currentStep={2}
  totalSteps={8}
  prevHref="/settings/setup/passkey"
  nextHref="/settings/setup/patient"
  showSkip={true}
  {saving}
  onSave={handleSave}
>
  <div class="space-y-6">
    <!-- Access mode selection -->
    <RadioGroup bind:value={mode} class="gap-0">
      <label
        class="flex items-start gap-4 rounded-t-lg border p-4 cursor-pointer has-[[data-state=checked]]:border-primary has-[[data-state=checked]]:bg-primary/5"
      >
        <RadioGroupItem value="denied" class="mt-0.5" />
        <div class="flex-1 space-y-1">
          <div class="flex items-center gap-2">
            <Lock class="h-4 w-4 text-muted-foreground" />
            <span class="text-sm font-medium">Invite only</span>
          </div>
          <p class="text-xs text-muted-foreground">
            Your data is completely private. Only people you explicitly invite
            can access your instance.
          </p>
        </div>
      </label>

      <label
        class="flex items-start gap-4 rounded-b-lg border border-t-0 p-4 cursor-pointer has-[[data-state=checked]]:border-primary has-[[data-state=checked]]:bg-primary/5"
      >
        <RadioGroupItem value="readable" class="mt-0.5" />
        <div class="flex-1 space-y-1">
          <div class="flex items-center gap-2">
            <Globe class="h-4 w-4 text-muted-foreground" />
            <span class="text-sm font-medium">Publicly readable</span>
          </div>
          <p class="text-xs text-muted-foreground">
            Anyone with the link can view your data without signing in.
            Useful for sharing with caregivers or medical staff.
          </p>
        </div>
      </label>
    </RadioGroup>

    <!-- Options shown when publicly readable -->
    {#if mode === "readable"}
      <div class="space-y-4 rounded-lg border p-4">
        <!-- 24-hour toggle -->
        <div class="flex items-center justify-between">
          <div class="space-y-1">
            <Label class="text-sm font-medium">Limit to last 24 hours</Label>
            <p class="text-xs text-muted-foreground">
              When enabled, public viewers only see the most recent 24 hours of
              data instead of the full history.
            </p>
          </div>
          <Switch bind:checked={limitTo24Hours} />
        </div>

        <!-- Advanced: per-resource toggles -->
        <Collapsible bind:open={advancedOpen}>
          <CollapsibleTrigger
            class="flex w-full items-center justify-between rounded-md px-2 py-1.5 text-sm font-medium text-muted-foreground hover:bg-muted/50 transition-colors"
          >
            Advanced
            <ChevronDown
              class="h-4 w-4 transition-transform {advancedOpen
                ? 'rotate-180'
                : ''}"
            />
          </CollapsibleTrigger>
          <CollapsibleContent>
            <div class="mt-3 space-y-3 pl-2">
              <p class="text-xs text-muted-foreground">
                Choose which data types are visible to public viewers.
                By default, all are included when readable access is granted.
              </p>
              {#each resources as resource (resource.key)}
                <label class="flex items-center gap-3 cursor-pointer">
                  <Checkbox bind:checked={resourceChecks[resource.key]} />
                  <span class="text-sm">{resource.label}</span>
                </label>
              {/each}
            </div>
          </CollapsibleContent>
        </Collapsible>
      </div>
    {/if}

    <!-- Summary -->
    <div
      class="flex items-start gap-3 rounded-md border bg-muted/30 p-3"
    >
      <Info class="mt-0.5 h-4 w-4 shrink-0 text-muted-foreground" />
      <p class="text-sm text-muted-foreground">{summaryText}</p>
    </div>

    <p class="text-xs text-muted-foreground">
      You can change these settings at any time in Settings &gt; Members.
    </p>
  </div>
</WizardShell>
