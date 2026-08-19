import type { LayoutServerLoad } from "./$types";
import { getOriginalHost } from "$lib/server/request-host";
import {
  classifyHost,
  isTenantlessHost,
  parseDashboardSlugs,
} from "$lib/server/tenantless-host";
import { getRequestStatus } from "$lib/server/request-status";
import {
  LANGUAGE_COOKIE_NAME,
  PREFS_COOKIE_NAME,
  hasStoredPreferences,
  parsePrefsCookie,
  resolveLanguage,
  type ColorScheme,
} from "$lib/stores/appearance-store.svelte";
import type { UserDisplayPreferences } from "$lib/api";

/**
 * Root layout server load function.
 * Provides session data to all routes.
 * Auth gating is handled by route group layouts.
 * Setup/recovery mode detection is in hooks.server.ts.
 */
export const load: LayoutServerLoad = async ({ locals, request, cookies }) => {
  // Tenant identity is resolved here, from the request host against BASE_DOMAIN,
  // so the browser never has to guess it by counting hostname labels. A share
  // host carries a token rather than a slug, so it has no tenant to name, and a
  // reserved dashboard slug names no single tenant either.
  const host = getOriginalHost(request);
  const baseDomain = process.env.BASE_DOMAIN ?? null;
  const dashboardSlugs = parseDashboardSlugs(process.env.DASHBOARD_SLUGS);
  const { kind, slug } = classifyHost(host, baseDomain, dashboardSlugs);
  const tenantSlug = slug;

  // Resolved once here, for every route: the apex needs the API's answer (does a sole tenant
  // resolve behind it?) and the share host needs the tenant's pinned public appearance. Asking
  // per-page would repeat both the question and the round-trip.
  const status = await getRequestStatus(locals);

  // The appearance an admin pinned for the public share view. Only meaningful on the share host;
  // injected as the highest-precedence preference layer so anonymous viewers render the pinned
  // units/time/theme instead of their (absent) defaults.
  const tenantless = isTenantlessHost(kind, kind === "apex" ? Boolean(status?.tenantSlug) : false);

  const statusAppearance = locals.isShareHost ? status?.shareAppearance : null;
  const shareAppearance = statusAppearance ? toDisplayPreferences(statusAppearance) : null;

  // Display preferences for SSR, in the same precedence the browser applies them
  // (backend blob over the mirrored cookie) so the markup matches hydration.
  const serverPrefs = locals.isAuthenticated ? locals.user?.preferences : null;
  const cookiePrefs = parsePrefsCookie(cookies.get(PREFS_COOKIE_NAME));
  const displayPreferences = [
    shareAppearance,
    hasStoredPreferences(serverPrefs) ? serverPrefs : null,
    cookiePrefs,
  ].filter((prefs) => prefs !== null && prefs !== undefined);
  const displayLanguage = resolveLanguage(
    locals.isAuthenticated ? locals.user?.preferredLanguage : null,
    cookies.get(LANGUAGE_COOKIE_NAME)
  );

  return {
    displayPreferences,
    displayLanguage,
    user: locals.user,
    isAuthenticated: locals.isAuthenticated,
    effectivePermissions: locals.effectivePermissions ?? [],
    isPlatformAdmin: locals.isPlatformAdmin,
    isPlatformAccessGrant: locals.isPlatformAccessGrant ?? false,
    tenantSlug,
    tenantless,
    baseDomain,
    dashboardSlugs,
    isShareHost: locals.isShareHost,
    shareAppearance,
    shareColorScheme: (locals.isShareHost
      ? (statusAppearance?.colorScheme as ColorScheme | undefined) ?? null
      : null) as ColorScheme | null,
  };
};

/**
 * The share appearance's fields that line up with the display-preference shape by name; the
 * color scheme is deliberately left out — mode-watcher drives it separately, not via the
 * preference layers.
 */
function toDisplayPreferences(a: {
  glucoseUnits?: string | undefined;
  timeFormat?: string | undefined;
  colorTheme?: string | undefined;
  regionFormat?: string | undefined;
}): UserDisplayPreferences {
  return {
    glucoseUnits: a.glucoseUnits,
    timeFormat: a.timeFormat,
    colorTheme: a.colorTheme,
    regionFormat: a.regionFormat,
  };
}
