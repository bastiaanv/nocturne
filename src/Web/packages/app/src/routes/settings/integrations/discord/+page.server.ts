import type { PageServerLoad, Actions } from "./$types";
import { fail, redirect } from "@sveltejs/kit";
import { signOAuthLinkState } from "$lib/server/bot/oauth-state";

/**
 * Settings → Integrations → Discord page.
 *
 * Shows the current tenant's chat identity links (one per Nocturne account
 * the signed-in user has linked their Discord to) with full CRUD actions
 * (edit label/display name, set default, revoke). Also exposes an
 * OAuth2 "Link my Discord account" button and a "Add Nocturne bot to a
 * Discord server" link.
 */
export const load: PageServerLoad = async ({ locals, url }) => {
	if (!locals.isAuthenticated || !locals.user) {
		throw redirect(303, `/auth/login?returnUrl=${encodeURIComponent(url.pathname)}`);
	}

	const links = await locals.apiClient.chatIdentity.getLinks();

	return {
		links,
		discordApplicationId: process.env.DISCORD_APPLICATION_ID ?? null,
		isOauthConfigured:
			!!process.env.DISCORD_APPLICATION_ID && !!process.env.DISCORD_CLIENT_SECRET,
		baseDomain: process.env.PUBLIC_BASE_DOMAIN ?? null,
		currentHost: url.host,
	};
};

function getTenantSlug(host: string, baseDomain: string | null): string | null {
	if (!baseDomain) return null;
	const baseHost = baseDomain.split(":")[0] ?? baseDomain;
	const currentHost = host.split(":")[0] ?? host;
	if (!currentHost.endsWith(`.${baseHost}`)) return null;
	const slug = currentHost.slice(0, currentHost.length - baseHost.length - 1);
	return slug || null;
}

export const actions: Actions = {
	/** Begin the Discord OAuth2 link flow. Redirects to Discord. */
	linkDiscord: async ({ url }) => {
		const clientId = process.env.DISCORD_APPLICATION_ID;
		const baseDomain = process.env.PUBLIC_BASE_DOMAIN;
		if (!clientId || !baseDomain) {
			return fail(500, { error: "Discord OAuth2 is not configured on this server." });
		}

		const slug = getTenantSlug(url.host, baseDomain);
		if (!slug) {
			return fail(400, {
				error: "Could not determine tenant slug from current host.",
			});
		}

		const state = signOAuthLinkState(slug);
		const redirectUri = `https://${baseDomain}/auth/bot/discord/callback`;
		const authorizeUrl = new URL("https://discord.com/api/oauth2/authorize");
		authorizeUrl.searchParams.set("client_id", clientId);
		authorizeUrl.searchParams.set("redirect_uri", redirectUri);
		authorizeUrl.searchParams.set("response_type", "code");
		authorizeUrl.searchParams.set("scope", "identify");
		authorizeUrl.searchParams.set("state", state);
		authorizeUrl.searchParams.set("prompt", "none");

		throw redirect(303, authorizeUrl.toString());
	},

	setDefault: async ({ request, locals }) => {
		const data = await request.formData();
		const id = data.get("id") as string | null;
		if (!id) return fail(400, { error: "Missing link id." });
		try {
			await locals.apiClient.chatIdentity.setDefault(id);
			return { ok: true };
		} catch (err) {
			console.error("setDefault failed:", err);
			return fail(500, { error: "Failed to set default link." });
		}
	},

	updateLink: async ({ request, locals }) => {
		const data = await request.formData();
		const id = data.get("id") as string | null;
		const label = (data.get("label") as string | null)?.trim();
		const displayName = (data.get("displayName") as string | null)?.trim();
		if (!id) return fail(400, { error: "Missing link id." });

		if (label && !/^[a-z0-9][a-z0-9-]{0,62}[a-z0-9]?$/.test(label)) {
			return fail(400, {
				error: "Label must be lowercase letters, digits, or hyphens.",
			});
		}

		try {
			await locals.apiClient.chatIdentity.updateLink(id, {
				label: label || undefined,
				displayName: displayName || undefined,
			});
			return { ok: true };
		} catch (err: unknown) {
			const msg =
				err && typeof err === "object" && "message" in err
					? String((err as { message: unknown }).message)
					: "Failed to update link.";
			console.error("updateLink failed:", err);
			return fail(500, { error: msg });
		}
	},

	revokeLink: async ({ request, locals }) => {
		const data = await request.formData();
		const id = data.get("id") as string | null;
		if (!id) return fail(400, { error: "Missing link id." });
		try {
			await locals.apiClient.chatIdentity.revokeLink(id);
			return { ok: true };
		} catch (err) {
			console.error("revokeLink failed:", err);
			return fail(500, { error: "Failed to revoke link." });
		}
	},
};
