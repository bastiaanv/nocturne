<script lang="ts">
  let {
    content = '',
    mode = 'markdown',
  }: {
    content: string;
    mode: 'markdown' | 'email';
  } = $props();
</script>

<div class="flex h-full w-[400px] shrink-0 flex-col border-l border-border/40 overflow-hidden">
  <div class="flex items-center border-b border-border/40 px-4 py-2">
    <span class="text-sm font-medium text-muted-foreground">Preview</span>
  </div>

  <div class="flex-1 overflow-y-auto p-6">
    {#if mode === 'markdown'}
      <article class="prose prose-neutral dark:prose-invert max-w-none">
        {@html content}
      </article>
    {:else}
      <div class="rounded-lg border border-border/40 bg-muted/20 p-4">
        <p class="text-sm text-muted-foreground">Email preview will use better-svelte-email renderer.</p>
        <article class="mt-4 prose prose-neutral dark:prose-invert max-w-none">
          {@html content}
        </article>
      </div>
    {/if}
  </div>
</div>
