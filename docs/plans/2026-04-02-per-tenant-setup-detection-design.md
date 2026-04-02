# Per-Tenant Setup Detection Design

**Date:** 2026-04-02
**Status:** Approved

## Problem

After Stripe checkout completes, users are redirected to `https://{slug}.{domain}?setup=true`. This should trigger the passkey setup flow, but it doesn't — the API never signals `setupRequired` for multi-tenant deployments.

Root cause: `RecoveryModeCheckService` explicitly skips `IsSetupRequired` when `BaseDomain` is configured (multi-tenant mode), because an empty database is the expected initial state. `RecoveryModeState` is a global singleton, so it can't represent per-tenant setup state. Additionally, `RecoveryModeMiddleware` runs before `TenantResolutionMiddleware`, so it structurally cannot do per-tenant checks.

## Design

### New `TenantSetupMiddleware`

A new middleware inserted immediately after `TenantResolutionMiddleware` in the pipeline:

```
RecoveryModeMiddleware       (global orphaned-subjects recovery, unchanged)
TenantResolutionMiddleware   (resolves tenant from subdomain)
TenantSetupMiddleware        (NEW — per-tenant setup check)
AuthenticationMiddleware
...
```

On each request where a tenant is resolved, the middleware checks whether the current tenant has any `TenantMember` rows using EF Core's existing tenant-scoped query (global filter applies `WHERE tenant_id = {current}` automatically). If no members exist, all `/api/*` paths return 503:

```json
{
  "error": "setup_required",
  "message": "Initial setup required. Please register a passkey to secure your account.",
  "setupRequired": true,
  "recoveryMode": false
}
```

The same allow-list as `RecoveryModeMiddleware` is used:
- `/api/auth/passkey/*`
- `/api/auth/totp/*`
- `/api/metadata*`
- `/api/admin/tenants/validate-slug`
- `/api/v4/me/tenants/validate-slug`

The existing `hooks.server.ts` 503 handler in nocturne-web already reads `setupRequired: true` and redirects to `/settings/setup/passkey` — no changes needed there.

**Single-tenant (self-hosted) mode:** Unaffected. `RecoveryModeMiddleware` handles setup via the global `IsSetupRequired` flag (only set when `BaseDomain` is not configured). `TenantSetupMiddleware` is still registered but `RecoveryModeMiddleware` fires first and the tenant resolves to the default anyway.

**Performance:** The `AnyAsync()` check against an indexed FK column is cheap. Once the first member exists, the condition is false and the middleware exits immediately on every subsequent request with no DB round-trip on the hot path (EF Core translates to `SELECT 1 WHERE EXISTS (...)`).

### Update `PasskeyController` Setup Endpoints

Both `SetupOptions` (`POST /api/auth/passkey/setup/options`) and `SetupComplete` (`POST /api/auth/passkey/setup/complete`) currently hardcode:

```csharp
var defaultTenant = await db.Tenants.FirstOrDefaultAsync(t => t.IsDefault);
```

Updated to use the resolved tenant from `ITenantAccessor`:

```csharp
var tenant = await db.Tenants.FindAsync(tenantAccessor.TenantId);
```

All downstream logic (subject creation, passkey registration, role seeding, token issuance) is already parameterised on `TenantId` — this is the only change. In self-hosted mode `tenantAccessor.TenantId` resolves to the default tenant, so behaviour is unchanged.

### The `?setup=true` Query Parameter

Not handled — it was always redundant. The 503 from `TenantSetupMiddleware` is what drives the redirect. The parameter is left in the Stripe success URL in nocturne-cloud as a harmless hint.

## Files Changed (nocturne repo)

| File | Change |
|------|--------|
| `src/API/Nocturne.API/Middleware/TenantSetupMiddleware.cs` | New |
| `src/API/Nocturne.API/Controllers/PasskeyController.cs` | Edit — `SetupOptions` and `SetupComplete` |
| `src/API/Nocturne.API/Program.cs` | Edit — register middleware after `TenantResolutionMiddleware` |
