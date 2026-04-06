import { command, query, getRequestEvent } from "$app/server";
import { z } from "zod";

/**
 * Get the current public access configuration.
 * Returns the Public subject's membership info from the members list.
 */
export const getPublicAccessConfig = query(async () => {
  const { apiClient } = getRequestEvent().locals;
  const members = await apiClient.memberInvites.getMembers();
  const publicMember = members.find((m: any) => m.name === "Public");

  if (!publicMember) {
    return {
      configured: false,
      memberId: undefined as string | undefined,
      mode: "denied" as const,
      limitTo24Hours: true,
      roles: [] as Array<{ id?: string; slug?: string; name?: string }>,
      directPermissions: [] as string[],
    };
  }

  const hasRoles = (publicMember.roles ?? []).length > 0;
  const hasDirectPerms = (publicMember.directPermissions ?? []).length > 0;
  const isReadable = (publicMember.roles ?? []).some(
    (r: any) => r.slug === "readable",
  );

  return {
    configured: hasRoles || hasDirectPerms,
    memberId: publicMember.id,
    mode: isReadable ? ("readable" as const) : ("denied" as const),
    limitTo24Hours: (publicMember as any).limitTo24Hours ?? true,
    roles: publicMember.roles ?? [],
    directPermissions: publicMember.directPermissions ?? [],
  };
});

/**
 * Get available tenant roles (to find readable/denied role IDs).
 */
export const getTenantRoles = query(async () => {
  const { apiClient } = getRequestEvent().locals;
  // TODO: Use apiClient.roles.getRoles() once NSwag client is regenerated.
  // For now, extract unique roles from member data.
  const members = await apiClient.memberInvites.getMembers();
  const roleMap = new Map<string, { id: string; slug: string; name: string }>();
  for (const m of members) {
    for (const r of (m as any).roles ?? []) {
      if (r.id && r.slug) {
        roleMap.set(r.id, {
          id: r.id,
          slug: r.slug ?? "",
          name: r.name ?? "",
        });
      }
    }
  }
  return [...roleMap.values()];
});

/**
 * Save public access configuration.
 */
export const savePublicAccess = command(
  z.object({
    memberId: z.string(),
    mode: z.enum(["readable", "denied"]),
    limitTo24Hours: z.boolean(),
    readableRoleId: z.string().optional(),
    deniedRoleId: z.string().optional(),
  }),
  async ({ memberId, mode, limitTo24Hours, readableRoleId, deniedRoleId }) => {
    const { apiClient } = getRequestEvent().locals;

    if (mode === "denied") {
      if (deniedRoleId) {
        await (apiClient.memberInvites as any).setMemberRoles(memberId, {
          roleIds: [deniedRoleId],
        });
      } else {
        // Clear all roles and permissions — the backend "denied" role
        // may not exist yet; fall back to removing readable.
        await (apiClient.memberInvites as any).setMemberRoles(memberId, {
          roleIds: [],
        });
        await (apiClient.memberInvites as any).setMemberPermissions(memberId, {
          directPermissions: [],
        });
      }
    } else if (mode === "readable" && readableRoleId) {
      await (apiClient.memberInvites as any).setMemberRoles(memberId, {
        roleIds: [readableRoleId],
      });
      // Clear any custom direct permissions
      await (apiClient.memberInvites as any).setMemberPermissions(memberId, {
        directPermissions: null,
      });
    }

    // Update LimitTo24Hours
    // TODO: Remove `any` cast once NSwag client is regenerated with this endpoint
    try {
      await (apiClient.memberInvites as any).setMemberLimitTo24Hours(memberId, {
        limitTo24Hours,
      });
    } catch {
      console.warn(
        "setMemberLimitTo24Hours not available yet — will work after NSwag regen",
      );
    }

    return { success: true };
  },
);
