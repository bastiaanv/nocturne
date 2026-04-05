# Per-Tenant Recovery Mode Design

**Date:** 2026-04-05
**Status:** Approved

## Problem

`RecoveryModeCheckService` runs at startup and sets a global `RecoveryModeState.IsEnabled` singleton when any active non-system subject across any tenant has no passkey and no OIDC binding. `RecoveryModeMiddleware` runs before tenant resolution and blocks all API traffic with 503 — including admin provisioning calls from the billing service. One tenant's orphaned subject locks down the entire instance, preventing new tenant creation.

## Design

### Make `RecoveryModeCheckService` multi-tenant aware

In multi-tenant mode (`BaseDomain` configured), skip the orphaned-subject scan entirely. The `NOCTURNE_RECOVERY_MODE=true` env var override continues to work as a single-tenant escape hatch.

### Make `RecoveryModeMiddleware` a no-op in multi-tenant mode

When `BaseDomain` is configured and the env var override is not set, pass everything through. Single-tenant behaviour is unchanged.

### Expand `TenantSetupMiddleware` with per-tenant recovery detection

Two sequential checks, ordered to short-circuit on the common (healthy) case:

1. `PasskeyCredentials.AnyAsync()` — if false, return 503 `setupRequired` (existing behaviour)
2. Only if passkeys exist: check for orphaned subjects (active, non-system, no passkey, no OIDC binding) — if found, return 503 `recoveryMode`

No caching. Both are indexed `EXISTS` queries scoped by tenant filter. The common case (healthy tenant with credentials and no orphaned subjects) pays for two sub-millisecond queries. Recovery clears immediately when the user registers a passkey — no restart required.

### Make `PasskeyController` status endpoints tenant-aware

`GetRecoveryModeStatus` and `GetAuthStatus` currently read from the global `RecoveryModeState` singleton. In multi-tenant mode, they query the database for the resolved tenant instead.

### Update recovery page copy

Remove "will be deactivated on next restart" and "restart the application after registering a passkey" from the recovery page — recovery now clears immediately when a passkey is registered.

### Cherry-pick slug changes (nocturne-cloud repo)

From branch `claude/fix-tenant-recovery-mode-UP7tn`: add `Slug` to `SubscriptionEntity` with migration and unique index. Fixes a bug where `DeactivateTenantAsync` was passing the user's email as the tenant display name. Discard the `RecoveryMode` flag on `UpdateTenantRequest` — not needed with the detection-based approach.

## Middleware pipeline (unchanged order)

```
RecoveryModeMiddleware       → no-op in multi-tenant (unless env var override)
TenantResolutionMiddleware   → resolves tenant from subdomain
TenantSetupMiddleware        → per-tenant setup + recovery check (expanded)
AuthenticationMiddleware     → ...
```

## Files changed

### Nocturne repo

| File | Change |
|------|--------|
| `src/API/Nocturne.API/Services/Auth/RecoveryModeCheckService.cs` | Skip orphaned-subject scan when `BaseDomain` is set |
| `src/API/Nocturne.API/Middleware/RecoveryModeMiddleware.cs` | Pass through when multi-tenant and no env var override |
| `src/API/Nocturne.API/Middleware/TenantSetupMiddleware.cs` | Add orphaned-subject recovery check |
| `src/API/Nocturne.API/Controllers/PasskeyController.cs` | Make `GetRecoveryModeStatus` + `GetAuthStatus` tenant-aware |
| `src/Web/packages/app/src/routes/auth/recovery/+page.svelte` | Remove restart-required copy |
| `src/Web/locales/en.po` | Update recovery mode string |
| Tests for all of the above | |

### Nocturne-cloud repo

| File | Change |
|------|--------|
| `services/billing/Data/Entities/SubscriptionEntity.cs` | Add `Slug` property |
| `services/billing/Data/BillingDbContext.cs` | Add unique index on `Slug` |
| `services/billing/Migrations/...` | Cherry-pick migration |
| `services/billing/Services/StripeWebhookHandler.cs` | Store slug on creation, fix deactivation |
| `services/provisioner/NocturneAdminClient.cs` | No `RecoveryMode` flag (discard) |
| Tests | Update to include `Slug` |

## Decisions

- **No caching** of tenant health status — indexed `EXISTS` queries are cheap; caching introduces staleness window during recovery
- **`NOCTURNE_RECOVERY_MODE` env var** kept as single-tenant escape hatch only
- **No changes to `/api/admin/` allowlist** in single-tenant `RecoveryModeMiddleware` — single-tenant has no external provisioning
- **`state.IsSetupRequired = false` in `SetupComplete`** left as-is — harmless no-op in multi-tenant
