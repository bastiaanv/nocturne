<script lang="ts">
  import { Info } from "lucide-svelte";
  import { Badge } from "$lib/components/ui/badge";

  // Local type definition for statistic reliability
  interface StatisticReliability {
    meetsReliabilityCriteria?: boolean;
    daysOfData?: number;
    recommendedMinimumDays?: number;
  }

  let { reliability } = $props<{ reliability?: StatisticReliability | null }>();
</script>

{#if reliability && reliability.meetsReliabilityCriteria === false}
  <Badge
    variant="outline"
    class="text-amber-600 bg-amber-50 border-amber-200 dark:text-amber-400 dark:bg-amber-950/30 dark:border-amber-800 gap-1.5"
  >
    <Info class="size-3 shrink-0" />
    <span>
      Based on {reliability.daysOfData ?? 0} days of data ({reliability.recommendedMinimumDays ?? 14} recommended)
    </span>
  </Badge>
{/if}
