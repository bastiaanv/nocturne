<script lang="ts">
  import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
  } from "$lib/components/ui/card";
  import { Button } from "$lib/components/ui/button";
  import { Badge } from "$lib/components/ui/badge";
  import * as Tabs from "$lib/components/ui/tabs";
  import * as Dialog from "$lib/components/ui/dialog";
  import { Input } from "$lib/components/ui/input";
  import { Label } from "$lib/components/ui/label";
  import { Textarea } from "$lib/components/ui/textarea";
  import { Checkbox } from "$lib/components/ui/checkbox";
  import {
    Shield,
    ShieldCheck,
    Users,
    KeyRound,
    Plus,
    Pencil,
    Trash2,
    Loader2,
    AlertTriangle,
    Copy,
    Check,
    User,
    Globe,
    TriangleAlert,
    Smartphone,
    Monitor,
    ToggleLeft,
    ToggleRight,
  } from "lucide-svelte";
  import * as Alert from "$lib/components/ui/alert";
  import * as rolesRemote from "$lib/api/generated/roles.generated.remote";
  import * as grantsRemote from "$lib/data/oauth.remote";
  import * as oidcRemote from "./oidc-providers.remote";
  import * as adminSubjectsRemote from "./admin-subjects.remote";
  import type { PageProps } from "./$types";
  import ProviderIcon from "$lib/components/auth/ProviderIcon.svelte";
  import type {
    TenantMemberDto,
    TenantRoleDto,
    OAuthGrantDto,
    OidcProviderResponse,
    OidcProviderTestResult,
  } from "$api";

  let { data }: PageProps = $props();
  const currentUserSubjectId = $derived(data?.user?.subjectId);

  // Platform admin toggle state
  let platformAdminError = $state<string | null>(null);
  let platformAdminSavingId = $state<string | null>(null);

  async function togglePlatformAdmin(subject: TenantMemberDto) {
    if (!subject.id) return;
    platformAdminError = null;
    platformAdminSavingId = subject.id;
    const next = !(subject as TenantMemberDto & { isPlatformAdmin?: boolean }).isPlatformAdmin;
    try {
      await adminSubjectsRemote.setPlatformAdmin({
        subjectId: subject.id,
        isPlatformAdmin: next,
      });
      subjects = subjects.map((s) =>
        s.id === subject.id ? { ...s, isPlatformAdmin: next } as TenantMemberDto : s
      );
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : String(err);
      if (message.includes("last_platform_admin")) {
        platformAdminError =
          "Cannot demote the last platform admin. Promote another user first.";
      } else {
        platformAdminError = "Failed to update platform admin status.";
      }
      console.error("Failed to set platform admin:", err);
    } finally {
      platformAdminSavingId = null;
    }
  }

  // State
  let activeTab = $state("users");
  let loading = $state(true);
  let error = $state<string | null>(null);

  let subjects = $state<TenantMemberDto[]>([]);
  let roles = $state<TenantRoleDto[]>([]);
  let grants = $state<OAuthGrantDto[]>([]);

  // Subject dialog state
  let isSubjectDialogOpen = $state(false);
  let isNewSubject = $state(false);
  let subjectFormName = $state("");
  let subjectFormNotes = $state("");
  let subjectFormRoles = $state<string[]>([]);
  let subjectSaving = $state(false);

  // Role dialog state
  let isRoleDialogOpen = $state(false);
  let editingRole = $state<TenantRoleDto | null>(null);
  let isNewRole = $state(false);
  let roleFormName = $state("");
  let roleFormNotes = $state("");
  let roleFormPermissions = $state<string[]>([]);
  let customPermission = $state("");
  let roleSaving = $state(false);
  let roleCreatedFromSubjectDialog = $state(false); // Track if we opened role dialog from subject dialog

  // Derived: check if admin role is selected (shows warning)
  const hasAdminRoleSelected = $derived(
    subjectFormRoles.includes("admin") ||
      subjectFormRoles.some((r) => {
        const role = roles.find((rl) => rl.name === r);
        return (
          role?.permissions?.includes("*") ||
          role?.permissions?.includes("admin")
        );
      })
  );

  // Token dialog state
  let isTokenDialogOpen = $state(false);
  let generatedToken = $state<string | null>(null);
  let tokenCopied = $state(false);

  // Derived counts
  const subjectCount = $derived(subjects.length);

  // ============================================================================
  // Identity Providers (OIDC) state
  // ============================================================================
  let oidcProviders = $state<OidcProviderResponse[]>([]);
  let oidcConfigManaged = $state(false);
  let oidcLoading = $state(false);
  let oidcError = $state<string | null>(null);

  // Provider dialog
  let isProviderDialogOpen = $state(false);
  let editingProvider = $state<OidcProviderResponse | null>(null);
  let providerSaving = $state(false);
  let providerDialogError = $state<string | null>(null);

  // Form fields
  let providerName = $state("");
  let providerIssuerUrl = $state("");
  let providerClientId = $state("");
  let providerClientSecret = $state("");
  let providerScopes = $state("openid profile email");
  let providerDefaultRoles = $state("readable");
  let providerIcon = $state("");
  let providerButtonColor = $state("");
  let providerDisplayOrder = $state(0);
  let providerIsEnabled = $state(true);

  // Test connection state
  let testingProvider = $state(false);
  let testResult = $state<OidcProviderTestResult | null>(null);

  function resetProviderForm() {
    editingProvider = null;
    providerName = "";
    providerIssuerUrl = "";
    providerClientId = "";
    providerClientSecret = "";
    providerScopes = "openid profile email";
    providerDefaultRoles = "readable";
    providerIcon = "";
    providerButtonColor = "";
    providerDisplayOrder = 0;
    providerIsEnabled = true;
    providerDialogError = null;
    testResult = null;
  }

  function openCreateProviderDialog() {
    resetProviderForm();
    isProviderDialogOpen = true;
  }

  function openEditProviderDialog(p: OidcProviderResponse) {
    resetProviderForm();
    editingProvider = p;
    providerName = p.name ?? "";
    providerIssuerUrl = p.issuerUrl ?? "";
    providerClientId = p.clientId ?? "";
    providerClientSecret = "";
    providerScopes = (p.scopes ?? ["openid", "profile", "email"]).join(", ");
    providerDefaultRoles = (p.defaultRoles ?? ["readable"]).join(", ");
    providerIcon = p.icon ?? "";
    providerButtonColor = p.buttonColor ?? "";
    providerDisplayOrder = p.displayOrder ?? 0;
    providerIsEnabled = p.isEnabled ?? true;
    isProviderDialogOpen = true;
  }

  function parseList(value: string): string[] {
    return value
      .split(/[,\s]+/)
      .map((s) => s.trim())
      .filter((s) => s.length > 0);
  }

  async function loadOidcData() {
    oidcLoading = true;
    oidcError = null;
    try {
      const [managed, providers] = await Promise.all([
        oidcRemote.getConfigManaged(),
        oidcRemote.getOidcProviders(),
      ]);
      oidcConfigManaged = managed;
      oidcProviders = providers ?? [];
    } catch (err) {
      console.error("Failed to load OIDC providers:", err);
      oidcError = "Failed to load identity providers";
    } finally {
      oidcLoading = false;
    }
  }

  async function saveProvider() {
    providerSaving = true;
    providerDialogError = null;
    try {
      const scopes = parseList(providerScopes);
      const defaultRoles = parseList(providerDefaultRoles);
      const base = {
        name: providerName,
        issuerUrl: providerIssuerUrl,
        clientId: providerClientId,
        clientSecret: providerClientSecret || undefined,
        scopes: scopes.length > 0 ? scopes : undefined,
        defaultRoles: defaultRoles.length > 0 ? defaultRoles : undefined,
        icon: providerIcon || undefined,
        buttonColor: providerButtonColor || undefined,
        displayOrder: providerDisplayOrder,
        isEnabled: providerIsEnabled,
      };

      if (editingProvider?.id) {
        await oidcRemote.updateOidcProvider({ id: editingProvider.id, ...base });
      } else {
        await oidcRemote.createOidcProvider(base);
      }
      isProviderDialogOpen = false;
      await loadOidcData();
    } catch (err: unknown) {
      console.error("Failed to save provider:", err);
      const message =
        err instanceof Error ? err.message : "Failed to save provider";
      providerDialogError = message.includes("would_lock_out_users")
        ? "This change would lock out all users. Ensure at least one authentication method remains available."
        : message;
    } finally {
      providerSaving = false;
    }
  }

  async function deleteProvider(p: OidcProviderResponse) {
    if (!p.id) return;
    if (!confirm(`Delete provider "${p.name}"?`)) return;
    try {
      await oidcRemote.deleteOidcProvider(p.id);
      await loadOidcData();
    } catch (err: unknown) {
      const message =
        err instanceof Error ? err.message : "Failed to delete provider";
      oidcError = message.includes("would_lock_out_users")
        ? "Deleting this provider would lock out all users."
        : message;
    }
  }

  async function toggleProvider(p: OidcProviderResponse) {
    if (!p.id) return;
    try {
      if (p.isEnabled) {
        await oidcRemote.disableOidcProvider(p.id);
      } else {
        await oidcRemote.enableOidcProvider(p.id);
      }
      await loadOidcData();
    } catch (err: unknown) {
      const message =
        err instanceof Error ? err.message : "Failed to toggle provider";
      oidcError = message.includes("would_lock_out_users")
        ? "Disabling this provider would lock out all users."
        : message;
    }
  }

  async function testProviderConnection() {
    testingProvider = true;
    testResult = null;
    try {
      testResult = await oidcRemote.testOidcProviderConfig({
        issuerUrl: providerIssuerUrl,
        clientId: providerClientId,
        clientSecret: providerClientSecret || undefined,
      });
    } catch (err: unknown) {
      testResult = {
        success: false,
        error: err instanceof Error ? err.message : "Test failed",
      };
    } finally {
      testingProvider = false;
    }
  }

  // Load data
  async function loadData() {
    loading = true;
    error = null;
    try {
      const [rols, grantsList] = await Promise.all([
        rolesRemote.getRoles(),
        loadAllGrants(),
      ]);
      await loadOidcData();
      roles = rols || [];
      grants = grantsList;
    } catch (err) {
      console.error("Failed to load admin data:", err);
      error = "Failed to load admin data";
    } finally {
      loading = false;
    }
  }

  // Load grants across all users (admin view)
  async function loadAllGrants(): Promise<OAuthGrantDto[]> {
    try {
      // For now, we can only get grants for the current user
      // In a full implementation, we'd need an admin endpoint to get all grants
      return [];
    } catch (err) {
      console.error("Failed to load grants:", err);
      return [];
    }
  }

  // Initial load
  $effect(() => {
    loadData();
  });

  // Format date
  function formatDate(dateStr: Date | undefined): string {
    if (!dateStr) return "Never";
    return new Date(dateStr).toLocaleDateString(undefined, {
      month: "short",
      day: "numeric",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  }

  // Helper to check if subject is a system subject (property may not exist in API)
  function isSystemSubjectCheck(subject: TenantMemberDto): boolean {
    return (
      "isSystemSubject" in subject &&
      (subject as TenantMemberDto & { isSystemSubject?: boolean }).isSystemSubject === true
    );
  }

  // Get subject type icon
  function getSubjectIcon(subject: TenantMemberDto) {
    // Public system subject gets a globe icon
    if (isSystemSubjectCheck(subject) && subject.name === "Public") {
      return Globe;
    }
    return User; // Regular user
  }

  // ============================================================================
  // Subject handlers
  // ============================================================================

  function openNewSubject() {
    isNewSubject = true;
    subjectFormName = "";
    subjectFormNotes = "";
    subjectFormRoles = [];
    isSubjectDialogOpen = true;
  }

  function openEditSubject(subject: TenantMemberDto) {
    isNewSubject = false;
    subjectFormName = subject.name || "";
    subjectFormNotes = subject.label || "";
    subjectFormRoles = subject.roles?.map((r) => r.name ?? "").filter(Boolean) ?? [];
    isSubjectDialogOpen = true;
  }

  async function saveSubject() {
    subjectSaving = true;
    try {
      // Subject management is handled via member invites and tenant membership.
      // Direct subject creation/update is not available in this API version.
      isSubjectDialogOpen = false;
      await loadData();
    } catch (err) {
      console.error("Failed to save subject:", err);
    } finally {
      subjectSaving = false;
    }
  }

  async function deleteSubjectHandler(_id: string) {
    if (!confirm("Delete this subject? This action cannot be undone.")) return;
    try {
      // Subject deletion is handled via tenant membership removal.
      await loadData();
    } catch (err) {
      console.error("Failed to delete subject:", err);
    }
  }

  function toggleSubjectRole(roleName: string) {
    if (subjectFormRoles.includes(roleName)) {
      subjectFormRoles = subjectFormRoles.filter((r) => r !== roleName);
    } else {
      subjectFormRoles = [...subjectFormRoles, roleName];
    }
  }

  // ============================================================================
  // Role handlers
  // ============================================================================

  async function saveRole() {
    roleSaving = true;
    const wasFromSubjectDialog = roleCreatedFromSubjectDialog;
    const newRoleName = roleFormName;
    try {
      if (isNewRole) {
        await rolesRemote.createRole({
          name: roleFormName,
          permissions: roleFormPermissions,
          description: roleFormNotes || undefined,
        });
      } else if (editingRole?.id) {
        await rolesRemote.updateRole({
          id: editingRole.id,
          request: {
            name: roleFormName,
            permissions: roleFormPermissions,
            description: roleFormNotes || undefined,
          },
        });
      }
      isRoleDialogOpen = false;
      roleCreatedFromSubjectDialog = false;
      await loadData();

      // If role was created from subject dialog, reopen it and select the new role
      if (wasFromSubjectDialog && isNewRole) {
        // Wait for roles to update, then add the new role to subject selection
        subjectFormRoles = [...subjectFormRoles, newRoleName];
        isSubjectDialogOpen = true;
      }
    } catch (err) {
      console.error("Failed to save role:", err);
    } finally {
      roleSaving = false;
    }
  }

  function togglePermission(permission: string) {
    if (roleFormPermissions.includes(permission)) {
      roleFormPermissions = roleFormPermissions.filter((p) => p !== permission);
    } else {
      roleFormPermissions = [...roleFormPermissions, permission];
    }
  }

  function addCustomPermission() {
    if (
      customPermission.trim() &&
      !roleFormPermissions.includes(customPermission.trim())
    ) {
      roleFormPermissions = [...roleFormPermissions, customPermission.trim()];
      customPermission = "";
    }
  }

  // ============================================================================
  // Grant handlers
  // ============================================================================

  async function revokeGrant(grantId: string) {
    if (!confirm("Revoke device access? This will log out the device and require re-authorization.")) return;
    try {
      await grantsRemote.revokeGrant({ grantId });
      await loadData();
    } catch (err) {
      console.error("Failed to revoke grant:", err);
    }
  }

  // ============================================================================
  // Token handlers
  // ============================================================================

  async function copyToken() {
    if (generatedToken) {
      await navigator.clipboard.writeText(generatedToken);
      tokenCopied = true;
      setTimeout(() => {
        tokenCopied = false;
      }, 2000);
    }
  }

  // Known permission categories for the picker
  const permissionCategories = [
    {
      name: "API - Entries",
      permissions: [
        "api:entries:read",
        "api:entries:create",
        "api:entries:update",
        "api:entries:delete",
        "api:entries:*",
      ],
    },
    {
      name: "API - Treatments",
      permissions: [
        "api:treatments:read",
        "api:treatments:create",
        "api:treatments:update",
        "api:treatments:delete",
        "api:treatments:*",
      ],
    },
    {
      name: "API - Device Status",
      permissions: [
        "api:devicestatus:read",
        "api:devicestatus:create",
        "api:devicestatus:update",
        "api:devicestatus:delete",
        "api:devicestatus:*",
      ],
    },
    {
      name: "API - Profile",
      permissions: ["api:profile:read", "api:profile:create", "api:profile:*"],
    },
    {
      name: "API - Food",
      permissions: [
        "api:food:read",
        "api:food:create",
        "api:food:update",
        "api:food:delete",
        "api:food:*",
      ],
    },
    {
      name: "Care Portal",
      permissions: [
        "careportal:read",
        "careportal:create",
        "careportal:update",
        "careportal:*",
      ],
    },
    {
      name: "Admin",
      permissions: ["admin", "*"],
    },
  ];
</script>

<svelte:head>
  <title>Administration - Settings - Nocturne</title>
</svelte:head>

<div class="container mx-auto p-6 max-w-5xl">
  <!-- Header -->
  <div class="mb-8">
    <div class="flex items-center gap-3 mb-2">
      <div
        class="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10"
      >
        <Shield class="h-5 w-5 text-primary" />
      </div>
      <div>
        <h1 class="text-3xl font-bold tracking-tight">Administration</h1>
        <p class="text-muted-foreground">
          Manage users, connected devices, and access control
        </p>
      </div>
    </div>
  </div>

  {#if loading}
    <div class="flex items-center justify-center py-12">
      <Loader2 class="h-8 w-8 animate-spin text-muted-foreground" />
    </div>
  {:else if error}
    <Card class="border-destructive">
      <CardContent class="py-6 text-center">
        <AlertTriangle class="h-8 w-8 text-destructive mx-auto mb-2" />
        <p class="text-destructive">{error}</p>
        <Button variant="outline" class="mt-4" onclick={loadData}>Retry</Button>
      </CardContent>
    </Card>
  {:else}
    <Tabs.Root bind:value={activeTab} class="space-y-6">
      <Tabs.List class={oidcConfigManaged ? "grid w-full grid-cols-3" : "grid w-full grid-cols-4"}>
        <Tabs.Trigger value="users" class="gap-2">
          <Users class="h-4 w-4" />
          Users
          {#if subjectCount > 0}
            <Badge variant="secondary" class="ml-1">{subjectCount}</Badge>
          {/if}
        </Tabs.Trigger>
        <Tabs.Trigger value="devices" class="gap-2">
          <Smartphone class="h-4 w-4" />
          Connected Devices
          {#if grants.length > 0}
            <Badge variant="secondary" class="ml-1">{grants.length}</Badge>
          {/if}
        </Tabs.Trigger>
        {#if !oidcConfigManaged}
          <Tabs.Trigger value="identity-providers" class="gap-2">
            <Shield class="h-4 w-4" />
            Identity Providers
            {#if oidcProviders.length > 0}
              <Badge variant="secondary" class="ml-1">{oidcProviders.length}</Badge>
            {/if}
          </Tabs.Trigger>
        {/if}
      </Tabs.List>

      <!-- Users Tab -->
      <Tabs.Content value="users">
        <Card>
          <CardHeader class="flex flex-row items-center justify-between">
            <div>
              <CardTitle>Users</CardTitle>
              <CardDescription>
                User accounts and their roles. For device access, use OAuth device authorization flow.
              </CardDescription>
            </div>
            <Button onclick={openNewSubject}>
              <Plus class="h-4 w-4 mr-2" />
              New User
            </Button>
          </CardHeader>
          <CardContent>
            {#if platformAdminError}
              <Alert.Root variant="destructive" class="mb-4">
                <TriangleAlert class="h-4 w-4" />
                <Alert.Description>{platformAdminError}</Alert.Description>
              </Alert.Root>
            {/if}
            {#if subjects.length === 0}
              <div class="text-center py-8 text-muted-foreground">
                <Users class="h-12 w-12 mx-auto mb-3 opacity-50" />
                <p>No users found</p>
                <p class="text-sm">Create your first user account to get started</p>
              </div>
            {:else}
              <div class="space-y-3">
                {#each subjects as subject}
                  {@const Icon = getSubjectIcon(subject)}
                  {@const isPublicSubject =
                    isSystemSubjectCheck(subject) && subject.name === "Public"}
                  {@const isPlatformAdmin = (subject as TenantMemberDto & { isPlatformAdmin?: boolean }).isPlatformAdmin}
                  <div
                    class="flex items-center justify-between p-4 rounded-lg border {isPublicSubject
                      ? 'bg-primary/5 border-primary/20'
                      : ''}"
                  >
                    <div class="flex items-center gap-3">
                      <div
                        class="p-2 rounded-lg {isPublicSubject
                          ? 'bg-primary/10'
                          : 'bg-muted'}"
                      >
                        <Icon
                          class="h-5 w-5 {isPublicSubject
                            ? 'text-primary'
                            : ''}"
                        />
                      </div>
                      <div>
                        <div class="font-medium flex items-center gap-2">
                          {subject.name}
                          {#if isPublicSubject}
                            <Badge variant="secondary" class="text-xs">
                              <Globe class="h-3 w-3 mr-1" />
                              Unauthenticated Access
                            </Badge>
                          {/if}
                          {#if subject.roles && subject.roles.some((r) => r.name === "admin")}
                            <Badge variant="default" class="text-xs">
                              Admin
                            </Badge>
                          {/if}
                          {#if isPlatformAdmin}
                            <Badge variant="default" class="text-xs">
                              <ShieldCheck class="h-3 w-3 mr-1" />
                              Platform Admin
                            </Badge>
                          {/if}
                        </div>
                        {#if isPublicSubject}
                          <div class="text-sm text-muted-foreground">
                            Defines what unauthenticated users can access
                          </div>
                        {/if}
                        <div class="text-sm text-muted-foreground">
                          {#if subject.roles && subject.roles.length > 0}
                            Roles: {subject.roles.map((r) => r.name ?? "").filter((n) => n !== "admin").join(", ") || "Admin"}
                          {:else}
                            No roles assigned
                          {/if}
                        </div>
                        <div class="text-xs text-muted-foreground mt-1">
                          Created: {formatDate(subject.sysCreatedAt)}
                        </div>
                      </div>
                    </div>
                    <div class="flex items-center gap-2">
                      {#if !isSystemSubjectCheck(subject) && subject.id !== currentUserSubjectId}
                        <Button
                          variant="ghost"
                          size="icon"
                          disabled={platformAdminSavingId === subject.id}
                          onclick={() => togglePlatformAdmin(subject)}
                          title={isPlatformAdmin
                            ? "Revoke platform admin"
                            : "Grant platform admin"}
                          aria-label={isPlatformAdmin
                            ? `Revoke platform admin from ${subject.name}`
                            : `Grant platform admin to ${subject.name}`}
                        >
                          {#if isPlatformAdmin}
                            <ShieldCheck class="h-4 w-4 text-primary" />
                          {:else}
                            <Shield class="h-4 w-4 text-muted-foreground" />
                          {/if}
                        </Button>
                      {/if}
                      <Button
                        variant="ghost"
                        size="icon"
                        onclick={() => openEditSubject(subject)}
                      >
                        <Pencil class="h-4 w-4" />
                      </Button>
                      {#if !isSystemSubjectCheck(subject)}
                        <Button
                          variant="ghost"
                          size="icon"
                          onclick={() => deleteSubjectHandler(subject.id!)}
                        >
                          <Trash2 class="h-4 w-4" />
                        </Button>
                      {/if}
                    </div>
                  </div>
                {/each}
              </div>
            {/if}
          </CardContent>
        </Card>
      </Tabs.Content>

      <!-- Connected Devices Tab -->
      <Tabs.Content value="devices">
        <Card>
          <CardHeader>
            <CardTitle>Connected Devices</CardTitle>
            <CardDescription>
              Devices and applications connected via OAuth. Users authorize devices using the device flow at /oauth/device.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div class="space-y-4">
              <!-- Info box about OAuth device flow -->
              <Alert.Root>
                <Smartphone class="h-4 w-4" />
                <Alert.Title>Modern OAuth Device Authorization</Alert.Title>
                <Alert.Description>
                  <div class="space-y-2">
                    <p>
                      Devices connect using the OAuth Device Authorization Flow. Users visit <strong>/oauth/device</strong> to authorize devices with a simple code.
                    </p>
                    <p class="text-xs">
                      This replaces manual API token generation. Devices get scoped access tokens that can be refreshed and revoked.
                    </p>
                  </div>
                </Alert.Description>
              </Alert.Root>

              {#if grants.length === 0}
                <div class="text-center py-8 text-muted-foreground">
                  <Smartphone class="h-12 w-12 mx-auto mb-3 opacity-50" />
                  <p>No connected devices</p>
                  <p class="text-sm">Devices will appear here after users authorize them</p>
                </div>
              {:else}
                <div class="space-y-3">
                  {#each grants as grant}
                    <div
                      class="flex items-center justify-between p-4 rounded-lg border"
                    >
                      <div class="flex items-center gap-3">
                        <div class="p-2 rounded-lg bg-muted">
                          {#if grant.isKnownClient}
                            <Monitor class="h-5 w-5" />
                          {:else}
                            <Smartphone class="h-5 w-5" />
                          {/if}
                        </div>
                        <div>
                          <div class="font-medium flex items-center gap-2">
                            {grant.clientDisplayName || grant.clientId || "Unknown Device"}
                            {#if grant.isKnownClient}
                              <Badge variant="secondary" class="text-xs">
                                Verified
                              </Badge>
                            {/if}
                          </div>
                          <div class="text-sm text-muted-foreground">
                            Client: {grant.clientId}
                          </div>
                          <div class="text-sm text-muted-foreground">
                            Scopes: {(grant.scopes ?? []).join(", ")}
                          </div>
                          <div class="text-xs text-muted-foreground mt-1">
                            Created: {formatDate(grant.createdAt)}
                            {#if grant.lastUsedAt}
                              • Last used: {formatDate(grant.lastUsedAt)}
                            {/if}
                          </div>
                        </div>
                      </div>
                      <div class="flex items-center gap-2">
                        <Button
                          variant="ghost"
                          size="icon"
                          onclick={() => revokeGrant(grant.id ?? "")}
                        >
                          <Trash2 class="h-4 w-4" />
                        </Button>
                      </div>
                    </div>
                  {/each}
                </div>
              {/if}

              <!-- Legacy token section removed: access tokens are no longer exposed via TenantMemberDto -->
            </div>
          </CardContent>
        </Card>
      </Tabs.Content>

      {#if !oidcConfigManaged}
        <Tabs.Content value="identity-providers">
          <Card>
            <CardHeader class="flex flex-row items-center justify-between">
              <div>
                <CardTitle>Identity Providers</CardTitle>
                <CardDescription>
                  Configure OpenID Connect providers for single sign-on.
                </CardDescription>
              </div>
              <Button onclick={openCreateProviderDialog} class="gap-2">
                <Plus class="h-4 w-4" />
                Add Provider
              </Button>
            </CardHeader>
            <CardContent>
              {#if oidcLoading}
                <div class="flex items-center justify-center py-8">
                  <Loader2 class="h-6 w-6 animate-spin text-muted-foreground" />
                </div>
              {:else if oidcError}
                <Alert.Root variant="destructive">
                  <AlertTriangle class="h-4 w-4" />
                  <Alert.Description>{oidcError}</Alert.Description>
                </Alert.Root>
              {:else if oidcProviders.length === 0}
                <div class="text-center py-8 text-muted-foreground">
                  <Shield class="h-12 w-12 mx-auto mb-2 opacity-50" />
                  <p>No identity providers configured.</p>
                </div>
              {:else}
                <div class="space-y-2">
                  {#each oidcProviders as provider (provider.id)}
                    <div class="flex items-center justify-between gap-3 rounded-md border p-3">
                      <div class="flex items-center gap-3 min-w-0">
                        <ProviderIcon slug={provider.icon} />
                        <div class="min-w-0">
                          <div class="flex items-center gap-2">
                            <span class="font-medium truncate">{provider.name}</span>
                            {#if provider.isEnabled}
                              <Badge variant="secondary">Enabled</Badge>
                            {:else}
                              <Badge variant="outline">Disabled</Badge>
                            {/if}
                          </div>
                          <div class="text-xs text-muted-foreground truncate">
                            {provider.issuerUrl}
                          </div>
                        </div>
                      </div>
                      <div class="flex items-center gap-1 shrink-0">
                        <Button
                          variant="ghost"
                          size="sm"
                          onclick={() => toggleProvider(provider)}
                          title={provider.isEnabled ? "Disable" : "Enable"}
                        >
                          {#if provider.isEnabled}
                            <ToggleRight class="h-4 w-4" />
                          {:else}
                            <ToggleLeft class="h-4 w-4" />
                          {/if}
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onclick={() => openEditProviderDialog(provider)}
                          title="Edit"
                        >
                          <Pencil class="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onclick={() => deleteProvider(provider)}
                          title="Delete"
                        >
                          <Trash2 class="h-4 w-4" />
                        </Button>
                      </div>
                    </div>
                  {/each}
                </div>
              {/if}
            </CardContent>
          </Card>
        </Tabs.Content>
      {/if}
    </Tabs.Root>
  {/if}

  <!-- OIDC Provider Create/Edit Dialog -->
  <Dialog.Root bind:open={isProviderDialogOpen}>
    <Dialog.Content class="max-w-2xl max-h-[90vh] overflow-y-auto">
      <Dialog.Header>
        <Dialog.Title>
          {editingProvider ? "Edit Identity Provider" : "Add Identity Provider"}
        </Dialog.Title>
        <Dialog.Description>
          Configure an OpenID Connect provider for single sign-on.
        </Dialog.Description>
      </Dialog.Header>

      <div class="space-y-4 py-2">
        {#if providerDialogError}
          <Alert.Root variant="destructive">
            <AlertTriangle class="h-4 w-4" />
            <Alert.Description>{providerDialogError}</Alert.Description>
          </Alert.Root>
        {/if}

        <div class="space-y-2">
          <Label for="provider-name">Name</Label>
          <Input id="provider-name" bind:value={providerName} placeholder="My Provider" />
        </div>

        <div class="space-y-2">
          <Label for="provider-issuer">Issuer URL</Label>
          <Input
            id="provider-issuer"
            type="url"
            bind:value={providerIssuerUrl}
            placeholder="https://accounts.example.com"
          />
        </div>

        <div class="space-y-2">
          <Label for="provider-client-id">Client ID</Label>
          <Input id="provider-client-id" bind:value={providerClientId} />
        </div>

        <div class="space-y-2">
          <Label for="provider-client-secret">Client Secret</Label>
          <Input
            id="provider-client-secret"
            type="password"
            bind:value={providerClientSecret}
            placeholder={editingProvider ? "Leave blank to keep existing" : ""}
          />
        </div>

        <div class="space-y-2">
          <Label for="provider-scopes">Scopes</Label>
          <Input
            id="provider-scopes"
            bind:value={providerScopes}
            placeholder="openid profile email"
          />
          <p class="text-xs text-muted-foreground">Comma or space separated</p>
        </div>

        <div class="space-y-2">
          <Label for="provider-roles">Default Roles</Label>
          <Input
            id="provider-roles"
            bind:value={providerDefaultRoles}
            placeholder="readable"
          />
          <p class="text-xs text-muted-foreground">
            Comma-separated roles assigned to new users from this provider
          </p>
        </div>

        <div class="space-y-2">
          <Label for="provider-icon">Icon</Label>
          <Input
            id="provider-icon"
            bind:value={providerIcon}
            placeholder="google, apple, microsoft, github, or a URL"
          />
        </div>

        <div class="space-y-2">
          <Label for="provider-color">Button Color</Label>
          <Input
            id="provider-color"
            bind:value={providerButtonColor}
            placeholder="#1a73e8"
          />
        </div>

        <div class="space-y-2">
          <Label for="provider-order">Display Order</Label>
          <Input
            id="provider-order"
            type="number"
            bind:value={providerDisplayOrder}
          />
        </div>

        <div class="flex items-center gap-2">
          <Checkbox id="provider-enabled" bind:checked={providerIsEnabled} />
          <Label for="provider-enabled">Enabled</Label>
        </div>

        <div class="border-t pt-4 space-y-2">
          <Button
            variant="outline"
            onclick={testProviderConnection}
            disabled={testingProvider || !providerIssuerUrl || !providerClientId}
            class="gap-2"
          >
            {#if testingProvider}
              <Loader2 class="h-4 w-4 animate-spin" />
            {:else}
              <Globe class="h-4 w-4" />
            {/if}
            Test Connection
          </Button>
          {#if testResult}
            {#if testResult.success}
              <Alert.Root>
                <Check class="h-4 w-4" />
                <Alert.Description>
                  Connection successful{testResult.responseTime
                    ? ` (${testResult.responseTime})`
                    : ""}.
                  {#if testResult.warnings && testResult.warnings.length > 0}
                    <ul class="list-disc list-inside mt-1 text-xs">
                      {#each testResult.warnings as warn}
                        <li>{warn}</li>
                      {/each}
                    </ul>
                  {/if}
                </Alert.Description>
              </Alert.Root>
            {:else}
              <Alert.Root variant="destructive">
                <AlertTriangle class="h-4 w-4" />
                <Alert.Description>
                  {testResult.error || "Connection test failed"}
                </Alert.Description>
              </Alert.Root>
            {/if}
          {/if}
        </div>
      </div>

      <Dialog.Footer>
        <Button variant="outline" onclick={() => (isProviderDialogOpen = false)}>
          Cancel
        </Button>
        <Button onclick={saveProvider} disabled={providerSaving} class="gap-2">
          {#if providerSaving}
            <Loader2 class="h-4 w-4 animate-spin" />
          {/if}
          {editingProvider ? "Save Changes" : "Create Provider"}
        </Button>
      </Dialog.Footer>
    </Dialog.Content>
  </Dialog.Root>
</div>

<!-- User Dialog -->
<Dialog.Root bind:open={isSubjectDialogOpen}>
  <Dialog.Content class="max-w-lg">
    <Dialog.Header>
      <Dialog.Title>
        {isNewSubject ? "New User" : "Edit User"}
      </Dialog.Title>
      <Dialog.Description>
        {isNewSubject
          ? "Create a new user account."
          : "Update user details and role assignments."}
      </Dialog.Description>
    </Dialog.Header>

    <div class="space-y-4 py-4">
      <div class="space-y-2">
        <Label for="subject-name">Name</Label>
        <Input
          id="subject-name"
          bind:value={subjectFormName}
          placeholder="e.g., John Doe"
        />
      </div>

      <div class="space-y-2">
        <Label for="subject-notes">Notes (optional)</Label>
        <Textarea
          id="subject-notes"
          bind:value={subjectFormNotes}
          placeholder="Additional information about this user"
          rows={2}
        />
      </div>

      <div class="space-y-2">
        <Label>Roles</Label>
        <div
          class="border rounded-lg p-3 space-y-2 bg-muted/50"
        >
          {#if roles.length === 0}
            <p class="text-sm text-muted-foreground">No roles available</p>
          {:else}
            <!-- Show predefined roles first -->
            {#each roles.filter(r => r.isSystem) as role}
              <label class="flex items-center gap-2 cursor-pointer">
                <Checkbox
                  checked={subjectFormRoles.includes(role.name ?? "")}
                  onCheckedChange={() => toggleSubjectRole(role.name ?? "")}
                />
                <div class="flex-1">
                  <span class="text-sm font-medium">{role.name}</span>
                  <Badge variant="secondary" class="text-xs ml-2">Predefined</Badge>
                </div>
              </label>
            {/each}

            <!-- Show custom roles if any -->
            {#if roles.filter(r => !r.isSystem).length > 0}
              <div class="pt-2 border-t">
                <p class="text-xs text-muted-foreground mb-2">Custom Roles</p>
                {#each roles.filter(r => !r.isSystem) as role}
                  <label class="flex items-center gap-2 cursor-pointer">
                    <Checkbox
                      checked={subjectFormRoles.includes(role.name ?? "")}
                      onCheckedChange={() => toggleSubjectRole(role.name ?? "")}
                    />
                    <span class="text-sm">{role.name}</span>
                  </label>
                {/each}
              </div>
            {/if}
          {/if}
        </div>
        <p class="text-xs text-muted-foreground">
          Use predefined roles for standard access levels. OAuth scopes provide fine-grained device permissions.
        </p>
      </div>

      {#if hasAdminRoleSelected}
        <Alert.Root variant="destructive">
          <TriangleAlert class="h-4 w-4" />
          <Alert.Title>Full Admin Access</Alert.Title>
          <Alert.Description>
            This user will have complete control of this Nocturne instance,
            including the ability to manage other users, modify all data,
            and change system settings. Only assign admin access to trusted users.
          </Alert.Description>
        </Alert.Root>
      {/if}
    </div>

    <Dialog.Footer>
      <Button
        variant="outline"
        onclick={() => (isSubjectDialogOpen = false)}
        disabled={subjectSaving}
      >
        Cancel
      </Button>
      <Button
        onclick={saveSubject}
        disabled={!subjectFormName || subjectSaving}
      >
        {#if subjectSaving}
          <Loader2 class="h-4 w-4 mr-2 animate-spin" />
        {/if}
        {isNewSubject ? "Create" : "Save"}
      </Button>
    </Dialog.Footer>
  </Dialog.Content>
</Dialog.Root>

<!-- Role Dialog -->
<Dialog.Root bind:open={isRoleDialogOpen}>
  <Dialog.Content class="max-w-2xl max-h-[85vh] overflow-y-auto">
    <Dialog.Header>
      <Dialog.Title>
        {isNewRole ? "New Role" : "Edit Role"}
      </Dialog.Title>
      <Dialog.Description>
        {#if roleCreatedFromSubjectDialog}
          Create a role with fine-grained permissions for your subject. After
          saving, you'll return to the subject dialog with this role selected.
        {:else if isNewRole}
          Create a new role with specific permissions.
        {:else}
          Update role details and permissions.
        {/if}
      </Dialog.Description>
    </Dialog.Header>

    <div class="space-y-4 py-4">
      <div class="space-y-2">
        <Label for="role-name">Name</Label>
        <Input
          id="role-name"
          bind:value={roleFormName}
          placeholder="e.g., api-readonly"
          disabled={editingRole?.isSystem}
        />
      </div>

      <div class="space-y-2">
        <Label for="role-notes">Notes (optional)</Label>
        <Textarea
          id="role-notes"
          bind:value={roleFormNotes}
          placeholder="Description of this role's purpose"
          rows={2}
          disabled={editingRole?.isSystem}
        />
      </div>

      <div class="space-y-2">
        <Label>Permissions</Label>

        <div class="space-y-4">
          {#each permissionCategories as category}
            <div class="border rounded-lg p-3 bg-muted/50">
              <h4 class="text-sm font-medium mb-2">{category.name}</h4>
              <div class="grid grid-cols-2 gap-2">
                {#each category.permissions as perm}
                  <label class="flex items-center gap-2 cursor-pointer">
                    <Checkbox
                      checked={roleFormPermissions.includes(perm)}
                      onCheckedChange={() => togglePermission(perm)}
                      disabled={editingRole?.isSystem}
                    />
                    <span class="text-sm font-mono">{perm}</span>
                  </label>
                {/each}
              </div>
            </div>
          {/each}

          <!-- Custom permission input -->
          <div class="border rounded-lg p-3">
            <h4 class="text-sm font-medium mb-2">Custom Permission</h4>
            <div class="flex gap-2">
              <Input
                bind:value={customPermission}
                placeholder="e.g., api:custom:read"
                class="font-mono"
                disabled={editingRole?.isSystem}
              />
              <Button
                variant="outline"
                size="sm"
                onclick={addCustomPermission}
                disabled={!customPermission.trim() ||
                  editingRole?.isSystem}
              >
                Add
              </Button>
            </div>
          </div>

          <!-- Selected permissions summary -->
          {#if roleFormPermissions.length > 0}
            <div class="border rounded-lg p-3">
              <h4 class="text-sm font-medium mb-2">
                Selected Permissions ({roleFormPermissions.length})
              </h4>
              <div class="flex flex-wrap gap-1">
                {#each roleFormPermissions as perm}
                  <Badge variant="secondary" class="font-mono text-xs">
                    {perm}
                    {#if !editingRole?.isSystem}
                      <button
                        class="ml-1 hover:text-destructive"
                        onclick={() => togglePermission(perm)}
                      >
                        ×
                      </button>
                    {/if}
                  </Badge>
                {/each}
              </div>
            </div>
          {/if}
        </div>
      </div>
    </div>

    <Dialog.Footer>
      <Button
        variant="outline"
        onclick={() => (isRoleDialogOpen = false)}
        disabled={roleSaving}
      >
        Cancel
      </Button>
      <Button
        onclick={saveRole}
        disabled={!roleFormName || roleSaving || editingRole?.isSystem}
      >
        {#if roleSaving}
          <Loader2 class="h-4 w-4 mr-2 animate-spin" />
        {/if}
        {isNewRole ? "Create" : "Save"}
      </Button>
    </Dialog.Footer>
  </Dialog.Content>
</Dialog.Root>

<!-- Legacy Token Dialog -->
<Dialog.Root bind:open={isTokenDialogOpen}>
  <Dialog.Content class="max-w-lg">
    <Dialog.Header>
      <Dialog.Title>Legacy API Token</Dialog.Title>
      <Dialog.Description>
        This is a legacy static token. New integrations should use OAuth device flow instead.
      </Dialog.Description>
    </Dialog.Header>

    <div class="space-y-4 py-4">
      {#if generatedToken}
        <Alert.Root variant="destructive">
          <AlertTriangle class="h-4 w-4" />
          <Alert.Title>Legacy Authentication Method</Alert.Title>
          <Alert.Description>
            Static tokens cannot be refreshed or scoped. Consider migrating to OAuth device authorization for better security.
          </Alert.Description>
        </Alert.Root>

        <div class="p-4 rounded-lg bg-muted font-mono text-sm break-all">
          {generatedToken}
        </div>
        <div class="flex gap-2">
          <Button class="flex-1" onclick={copyToken}>
            {#if tokenCopied}
              <Check class="h-4 w-4 mr-2" />
              Copied!
            {:else}
              <Copy class="h-4 w-4 mr-2" />
              Copy to Clipboard
            {/if}
          </Button>
        </div>
        <p class="text-sm text-muted-foreground">
          Use in the <code class="px-1 py-0.5 rounded bg-muted">Authorization</code>
          header or as an <code class="px-1 py-0.5 rounded bg-muted">api-secret</code> query parameter.
        </p>
      {:else}
        <div class="text-center py-8 text-muted-foreground">
          <KeyRound class="h-8 w-8 mx-auto mb-2" />
          <p>No access token available for this user.</p>
        </div>
      {/if}
    </div>

    <Dialog.Footer>
      <Button variant="outline" onclick={() => (isTokenDialogOpen = false)}>
        Close
      </Button>
    </Dialog.Footer>
  </Dialog.Content>
</Dialog.Root>

