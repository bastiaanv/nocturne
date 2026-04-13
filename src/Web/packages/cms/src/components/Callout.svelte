<script lang="ts" module>
  export type CalloutType = 'info' | 'warning' | 'danger' | 'tip';
</script>

<script lang="ts">
  import type { Snippet } from 'svelte';

  let {
    type = 'info',
    title,
    children,
  }: {
    type?: CalloutType;
    title?: string;
    children: Snippet;
  } = $props();

  const styles: Record<CalloutType, string> = {
    info: 'border-blue-500/40 bg-blue-50 dark:bg-blue-950/20 text-blue-900 dark:text-blue-100',
    warning: 'border-yellow-500/40 bg-yellow-50 dark:bg-yellow-950/20 text-yellow-900 dark:text-yellow-100',
    danger: 'border-red-500/40 bg-red-50 dark:bg-red-950/20 text-red-900 dark:text-red-100',
    tip: 'border-green-500/40 bg-green-50 dark:bg-green-950/20 text-green-900 dark:text-green-100',
  };
</script>

<div class="rounded-lg border-l-4 p-4 my-4 {styles[type]}" role="note">
  {#if title}
    <p class="font-semibold mb-1">{title}</p>
  {/if}
  <div class="text-sm">
    {@render children()}
  </div>
</div>
