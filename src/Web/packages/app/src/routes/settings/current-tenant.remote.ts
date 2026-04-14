/**
 * Remote function to get the current tenant ID.
 * Shared across settings pages that need the current tenant context.
 */
import { getRequestEvent, query } from "$app/server";
import { error, redirect } from "@sveltejs/kit";

/**
 * Get the current (default) tenant ID for the authenticated user.
 */
export const getCurrentTenantId = query(async () => {
  const { locals, url } = getRequestEvent();

  if (!locals.isAuthenticated) {
    throw redirect(302, `/auth/login?returnUrl=${encodeURIComponent(url.pathname + url.search)}`);
  }

  const apiClient = locals.apiClient;
  try {
    const tenants = await apiClient.myTenants.getMyTenants();
    const defaultTenant = tenants.find((t) => t.isDefault);
    return defaultTenant?.id ?? null;
  } catch (err) {
    const status = (err as any)?.status;
    if (status === 401) {
      throw redirect(302, `/auth/login?returnUrl=${encodeURIComponent(url.pathname + url.search)}`);
    }
    if (status === 403) throw error(403, "Forbidden");
    console.error("Error in getCurrentTenantId:", err);
    throw error(500, "Failed to get current tenant");
  }
});
