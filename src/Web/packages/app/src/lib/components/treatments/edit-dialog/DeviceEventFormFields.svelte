<script lang="ts">
  import type { DeviceEventType } from "$lib/api";
  import * as Select from "$lib/components/ui/select";
  import { Label } from "$lib/components/ui/label";
  import { Textarea } from "$lib/components/ui/textarea";
  import { Smartphone } from "lucide-svelte";

  interface Props {
    form: {
      eventType: DeviceEventType | undefined;
      notes: string;
    };
  }

  let { form = $bindable() }: Props = $props();

  const deviceEventTypeOptions: DeviceEventType[] = [
    "SensorStart",
    "SensorChange",
    "SensorStop",
    "SiteChange",
    "InsulinChange",
    "PumpBatteryChange",
    "PodChange",
    "ReservoirChange",
    "CannulaChange",
    "TransmitterSensorInsert",
    "PodActivated",
    "PodDeactivated",
    "PumpSuspend",
    "PumpResume",
    "Priming",
    "TubePriming",
    "NeedlePriming",
    "Rewind",
    "DateChanged",
    "TimeChanged",
    "BolusMaxChanged",
    "BasalMaxChanged",
    "ProfileSwitch",
  ] as DeviceEventType[];
</script>

<div class="space-y-2">
  <Label class="flex items-center gap-1.5">
    <Smartphone class="h-3.5 w-3.5 text-orange-500" />
    Event Type
  </Label>
  <Select.Root
    type="single"
    value={form.eventType ?? ""}
    onValueChange={(v) => {
      form.eventType = (v as DeviceEventType) || undefined;
    }}
  >
    <Select.Trigger>
      {form.eventType || "Select..."}
    </Select.Trigger>
    <Select.Content>
      {#each deviceEventTypeOptions as opt}
        <Select.Item value={opt}>{opt}</Select.Item>
      {/each}
    </Select.Content>
  </Select.Root>
</div>

<div class="space-y-2">
  <Label for="deviceNotes">Notes</Label>
  <Textarea
    id="deviceNotes"
    bind:value={form.notes}
    placeholder="Additional notes..."
    rows={3}
  />
</div>
