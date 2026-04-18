<script lang="ts">
  import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
  } from "$lib/components/ui/card";
  import { Button } from "$lib/components/ui/button";
  import { Switch } from "$lib/components/ui/switch";
  import { Label } from "$lib/components/ui/label";
  import { Input } from "$lib/components/ui/input";
  import { Moon, Save, Loader2 } from "lucide-svelte";

  interface Props {
    enabled: boolean;
    start: string;
    end: string;
    overrideCritical: boolean;
    saving: boolean;
    onSave: () => void;
  }

  let { enabled = $bindable(), start = $bindable(), end = $bindable(), overrideCritical = $bindable(), saving, onSave }: Props = $props();
</script>

<Card>
  <CardHeader>
    <CardTitle class="flex items-center gap-2">
      <Moon class="h-5 w-5" />
      Quiet Hours
    </CardTitle>
    <CardDescription>
      Suppress non-critical alerts during specific hours
    </CardDescription>
  </CardHeader>
  <CardContent class="space-y-4">
    <div class="flex items-center justify-between">
      <Label for="qh-enabled">Enable quiet hours</Label>
      <Switch id="qh-enabled" bind:checked={enabled} />
    </div>

    {#if enabled}
      <div class="grid grid-cols-2 gap-4">
        <div class="space-y-2">
          <Label for="qh-start">Start Time</Label>
          <Input id="qh-start" type="time" bind:value={start} />
        </div>
        <div class="space-y-2">
          <Label for="qh-end">End Time</Label>
          <Input id="qh-end" type="time" bind:value={end} />
        </div>
      </div>

      <div class="flex items-center justify-between">
        <div>
          <Label for="qh-override">Allow critical alerts during quiet hours</Label>
          <p class="text-xs text-muted-foreground">
            Critical alerts bypass quiet hours
          </p>
        </div>
        <Switch id="qh-override" bind:checked={overrideCritical} />
      </div>
    {/if}

    <div class="flex justify-end">
      <Button
        size="sm"
        onclick={onSave}
        disabled={saving}
      >
        {#if saving}
          <Loader2 class="h-4 w-4 mr-2 animate-spin" />
        {:else}
          <Save class="h-4 w-4 mr-2" />
        {/if}
        Save
      </Button>
    </div>
  </CardContent>
</Card>
