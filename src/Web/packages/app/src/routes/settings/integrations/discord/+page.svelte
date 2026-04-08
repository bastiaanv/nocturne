<script lang="ts">
	import type { PageData, ActionData } from "./$types";
	import * as Card from "$lib/components/ui/card";
	import { Button } from "$lib/components/ui/button";
	import { Input } from "$lib/components/ui/input";
	import { Label } from "$lib/components/ui/label";
	import { Badge } from "$lib/components/ui/badge";
	import { Link2, Link2Off, Plus, Star, Pencil, Save, X } from "lucide-svelte";

	let { data, form }: { data: PageData; form: ActionData } = $props();

	let editingId: string | null = $state(null);
	let editLabel = $state("");
	let editDisplayName = $state("");

	function startEdit(id: string, label: string, displayName: string) {
		editingId = id;
		editLabel = label;
		editDisplayName = displayName;
	}

	function cancelEdit() {
		editingId = null;
		editLabel = "";
		editDisplayName = "";
	}

	const botInviteUrl = $derived(
		data.discordApplicationId
			? `https://discord.com/api/oauth2/authorize?client_id=${data.discordApplicationId}&scope=bot+applications.commands&permissions=2147484672`
			: null,
	);
</script>

<div class="container mx-auto max-w-2xl p-6 space-y-6">
	<div>
		<h1 class="text-2xl font-bold">Discord Integration</h1>
		<p class="text-muted-foreground">
			Link your Discord account to query glucose and receive alerts through the Nocturne bot.
		</p>
	</div>

	{#if form?.error}
		<Card.Root class="border-destructive">
			<Card.Content class="pt-6">
				<p class="text-sm text-destructive">{form.error}</p>
			</Card.Content>
		</Card.Root>
	{/if}

	<Card.Root>
		<Card.Header>
			<Card.Title>Linked accounts</Card.Title>
			<Card.Description>
				Discord accounts linked to <strong>this Nocturne instance</strong>. Each one can be
				queried from Discord with <code>/bg &lt;label&gt;</code>.
			</Card.Description>
		</Card.Header>
		<Card.Content class="space-y-3">
			{#if data.links.length === 0}
				<p class="text-sm text-muted-foreground">
					No Discord accounts linked yet. Use the button below to connect one.
				</p>
			{:else}
				{#each data.links as link (link.id)}
					<div class="flex flex-col gap-2 p-3 border rounded-md">
						{#if editingId === link.id}
							<form method="POST" action="?/updateLink" class="space-y-2">
								<input type="hidden" name="id" value={link.id} />
								<div class="space-y-1">
									<Label for="label-{link.id}">Label (used in <code>/bg &lt;label&gt;</code>)</Label>
									<Input
										id="label-{link.id}"
										name="label"
										bind:value={editLabel}
										pattern="[a-z0-9][a-z0-9\-]{'{0,62}'}[a-z0-9]?"
										placeholder="e.g. lily"
										required
									/>
								</div>
								<div class="space-y-1">
									<Label for="displayName-{link.id}">Display name</Label>
									<Input
										id="displayName-{link.id}"
										name="displayName"
										bind:value={editDisplayName}
										placeholder="e.g. Lily"
										required
									/>
								</div>
								<div class="flex gap-2">
									<Button type="submit" size="sm">
										<Save class="size-4 mr-1" />
										Save
									</Button>
									<Button type="button" variant="ghost" size="sm" onclick={cancelEdit}>
										<X class="size-4 mr-1" />
										Cancel
									</Button>
								</div>
							</form>
						{:else}
							<div class="flex items-center justify-between gap-2">
								<div class="flex items-center gap-2 min-w-0 flex-1">
									<Link2 class="size-4 text-muted-foreground shrink-0" />
									<div class="min-w-0">
										<div class="font-medium flex items-center gap-2">
											<span class="truncate">{link.displayName}</span>
											{#if link.isDefault}
												<Badge variant="secondary" class="shrink-0">Default</Badge>
											{/if}
										</div>
										<div class="text-xs text-muted-foreground truncate">
											<code>{link.label}</code>
											{#if link.platformUserId}
												· Discord <code>{link.platformUserId}</code>
											{/if}
										</div>
									</div>
								</div>
								<div class="flex gap-1 shrink-0">
									{#if !link.isDefault}
										<form method="POST" action="?/setDefault">
											<input type="hidden" name="id" value={link.id} />
											<Button type="submit" size="icon" variant="ghost" title="Set as default">
												<Star class="size-4" />
											</Button>
										</form>
									{/if}
									<Button
										type="button"
										size="icon"
										variant="ghost"
										title="Edit"
										onclick={() => startEdit(link.id ?? "", link.label ?? "", link.displayName ?? "")}
									>
										<Pencil class="size-4" />
									</Button>
									<form method="POST" action="?/revokeLink">
										<input type="hidden" name="id" value={link.id} />
										<Button type="submit" size="icon" variant="ghost" title="Revoke">
											<Link2Off class="size-4" />
										</Button>
									</form>
								</div>
							</div>
						{/if}
					</div>
				{/each}
			{/if}
		</Card.Content>
		<Card.Footer class="flex flex-col gap-2 items-stretch">
			{#if data.isOauthConfigured}
				<form method="POST" action="?/linkDiscord">
					<Button type="submit" class="w-full">
						<Plus class="size-4 mr-2" />
						Link my Discord account
					</Button>
				</form>
			{:else}
				<p class="text-xs text-muted-foreground">
					Discord OAuth2 is not configured on this instance. Ask the administrator to set
					<code>DISCORD_APPLICATION_ID</code> and <code>DISCORD_CLIENT_SECRET</code>, or run
					<code>/connect</code> directly from Discord.
				</p>
			{/if}
		</Card.Footer>
	</Card.Root>

	{#if botInviteUrl}
		<Card.Root>
			<Card.Header>
				<Card.Title>Add the bot to a Discord server</Card.Title>
				<Card.Description>
					Invite the Nocturne bot to a Discord server you manage so you can use
					<code>/bg</code> there.
				</Card.Description>
			</Card.Header>
			<Card.Content>
				<Button href={botInviteUrl} target="_blank" rel="noopener noreferrer" variant="outline">
					Open Discord invite
				</Button>
			</Card.Content>
		</Card.Root>
	{/if}
</div>
