import { OAuthScope } from "$lib/api/generated/nocturne-api-client";

export { OAuthScope } from "$lib/api/generated/nocturne-api-client";

export const OAUTH_SCOPE_DESCRIPTIONS: Readonly<Record<OAuthScope, string>> = {
  [OAuthScope.EntriesRead]: "View glucose readings",
  [OAuthScope.EntriesReadWrite]: "View and record glucose readings",
  [OAuthScope.TreatmentsRead]: "View treatments",
  [OAuthScope.TreatmentsReadWrite]: "View and record treatments",
  [OAuthScope.DeviceStatusRead]: "View device status",
  [OAuthScope.DeviceStatusReadWrite]: "View and update device status",
  [OAuthScope.ProfileRead]: "View profile settings",
  [OAuthScope.ProfileReadWrite]: "View and update profile settings",
  [OAuthScope.NotificationsRead]: "View notifications",
  [OAuthScope.NotificationsReadWrite]: "Manage notifications",
  [OAuthScope.ReportsRead]: "View reports and analytics",
  [OAuthScope.IdentityRead]: "View basic account info",
  [OAuthScope.SharingReadWrite]: "Manage sharing settings",
  [OAuthScope.HealthRead]: "View all health data (read-only)",
  [OAuthScope.FullAccess]: "Full access including delete",
} as const;

export const OAUTH_AVAILABLE_SCOPES = Object.values(OAuthScope) as ReadonlyArray<OAuthScope>;

export function getOAuthScopeDescription(scope: OAuthScope): string;
export function getOAuthScopeDescription(scope: string): string;
export function getOAuthScopeDescription(scope: string): string {
  return (OAUTH_SCOPE_DESCRIPTIONS as Record<string, string>)[scope] ?? scope;
}
