<script lang="ts">
  import WizardShell from "$lib/components/setup/WizardShell.svelte";
  import { PatientClinicalForm, createWeightState } from "$lib/components/patient";
  import { Input } from "$lib/components/ui/input";
  import { Label } from "$lib/components/ui/label";

  let saving = $state(false);
  let saveDisabled = $state(true);
  let clinicalSaveFn = $state<(() => Promise<boolean>) | undefined>(undefined);

  const weight = createWeightState();

  function handleState(api: { save: () => Promise<boolean>; saving: boolean; isValid: boolean }) {
    saving = api.saving || weight.saving;
    saveDisabled = !api.isValid;
    clinicalSaveFn = api.save;
  }

  async function handleSave(): Promise<boolean> {
    const [clinicalOk, weightOk] = await Promise.all([
      clinicalSaveFn ? clinicalSaveFn() : Promise.resolve(true),
      weight.save(),
    ]);
    return clinicalOk && weightOk;
  }
</script>

<svelte:head>
  <title>Patient Record - Setup - Nocturne</title>
</svelte:head>

<WizardShell
  title="Patient Record"
  description="Tell us a bit about yourself and your diabetes. Only diabetes type is required."
  currentStep={3}
  totalSteps={8}
  prevHref="/setup/permissions"
  nextHref="/setup/devices"
  {saveDisabled}
  {saving}
  onSave={handleSave}
>
  <PatientClinicalForm onstate={handleState} />

  <div class="grid gap-4 sm:grid-cols-2">
    <div class="space-y-2">
      <Label for="weight-kg">Weight (kg)</Label>
      <Input
        id="weight-kg"
        type="number"
        step="0.1"
        min="0"
        bind:value={weight.weightKg}
        placeholder="e.g. 70.5"
      />
    </div>
  </div>

  {#if weight.saveError}
    <p class="text-sm text-destructive">{weight.saveError}</p>
  {/if}
</WizardShell>
