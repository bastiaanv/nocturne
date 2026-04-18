<script lang="ts">
  import type { BolusType } from "$lib/api";
  import * as Select from "$lib/components/ui/select";
  import { Input } from "$lib/components/ui/input";
  import { Label } from "$lib/components/ui/label";
  import { Checkbox } from "$lib/components/ui/checkbox";
  import { Syringe } from "lucide-svelte";

  interface Props {
    form: {
      insulin: number | null;
      bolusType: BolusType | undefined;
      programmed: number | undefined;
      delivered: number | undefined;
      duration: number | undefined;
      automatic: boolean;
      insulinType: string;
      isBasalInsulin: boolean;
    };
  }

  let { form = $bindable() }: Props = $props();

  const bolusTypeOptions: BolusType[] = ["Normal", "Square", "Dual"] as BolusType[];
</script>

<div class="grid grid-cols-2 gap-4">
  <div class="space-y-2">
    <Label for="insulin" class="flex items-center gap-1.5">
      <Syringe class="h-3.5 w-3.5 text-blue-500" />
      Insulin (U)
    </Label>
    <Input
      id="insulin"
      type="number"
      step="0.05"
      min="0"
      bind:value={form.insulin}
    />
  </div>
  <div class="space-y-2">
    <Label>Bolus Type</Label>
    <Select.Root
      type="single"
      value={form.bolusType ?? ""}
      onValueChange={(v) => {
        form.bolusType = (v as BolusType) || undefined;
      }}
    >
      <Select.Trigger>
        {form.bolusType || "Select..."}
      </Select.Trigger>
      <Select.Content>
        {#each bolusTypeOptions as opt}
          <Select.Item value={opt}>{opt}</Select.Item>
        {/each}
      </Select.Content>
    </Select.Root>
  </div>
</div>

<div class="grid grid-cols-3 gap-4">
  <div class="space-y-2">
    <Label for="programmed">Programmed</Label>
    <Input
      id="programmed"
      type="number"
      step="0.05"
      min="0"
      bind:value={form.programmed}
      placeholder={"\u2014"}
    />
  </div>
  <div class="space-y-2">
    <Label for="delivered">Delivered</Label>
    <Input
      id="delivered"
      type="number"
      step="0.05"
      min="0"
      bind:value={form.delivered}
      placeholder={"\u2014"}
    />
  </div>
  <div class="space-y-2">
    <Label for="duration">Duration (min)</Label>
    <Input
      id="duration"
      type="number"
      step="1"
      min="0"
      bind:value={form.duration}
      placeholder={"\u2014"}
    />
  </div>
</div>

<div class="space-y-2">
  <Label for="insulinType">Insulin Type</Label>
  <Input
    id="insulinType"
    bind:value={form.insulinType}
    placeholder="e.g. Rapid, Long-acting"
  />
</div>

<div class="flex gap-6">
  <div class="flex items-center gap-2">
    <Checkbox id="automatic" bind:checked={form.automatic} />
    <Label for="automatic" class="text-sm font-normal cursor-pointer">
      Automatic
    </Label>
  </div>
  <div class="flex items-center gap-2">
    <Checkbox
      id="isBasalInsulin"
      bind:checked={form.isBasalInsulin}
    />
    <Label
      for="isBasalInsulin"
      class="text-sm font-normal cursor-pointer"
    >
      Basal Insulin
    </Label>
  </div>
</div>
