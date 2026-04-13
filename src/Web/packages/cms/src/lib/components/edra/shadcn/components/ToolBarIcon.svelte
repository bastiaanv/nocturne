<script lang="ts">
	import { Button } from '@nocturne/ui/ui/button';
	import { cn } from '@nocturne/ui/utils';
	import type { Editor } from '@tiptap/core';
	import type { EdraToolBarCommands } from '../../commands/types.ts';
	import EdraToolTip from './EdraToolTip.svelte';

	interface Props {
		editor: Editor;
		command: EdraToolBarCommands;
	}

	const { editor, command }: Props = $props();
</script>

<EdraToolTip tooltip={command.tooltip ?? ''} shortCut={command.shortCut ?? ''}>
	<Button
		variant="ghost"
		size="icon"
		class={cn(command.isActive?.(editor) && 'bg-muted')}
		onclick={() => command.onClick?.(editor)}
		disabled={command.clickable ? !command.clickable(editor) : false}
	>
		{@const Icon = command.icon}
		<Icon />
	</Button>
</EdraToolTip>
