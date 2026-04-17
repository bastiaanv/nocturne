<script lang="ts">
  import * as Card from "$lib/components/ui/card";
  import { Button } from "$lib/components/ui/button";
  import { Input } from "$lib/components/ui/input";
  import { Label } from "$lib/components/ui/label";
  import {
    Fingerprint,
    Loader2,
    Check,
    Copy,
    AlertTriangle,
    ShieldCheck,
    ExternalLink,
    UserPlus,
  } from "lucide-svelte";
  import { startRegistration } from "@simplewebauthn/browser";
  import { page } from "$app/state";
  import { goto } from "$app/navigation";
  import {
    getInviteInfo,
    acceptInvite,
  } from "$lib/api/generated/memberInvites.generated.remote";
  import {
    inviteOptions,
    inviteComplete,
  } from "$lib/api/generated/passkeys.generated.remote";
  import {
    getAuthState,
    getOidcProviders,
    setAuthCookies,
  } from "$routes/(fullscreen)/auth/auth.remote";

  // ── URL params ────────────────────────────────────────────────────
  const token = $derived(page.url.searchParams.get("token") ?? "");

  // ── Remote data ───────────────────────────────────────────────────
  const authStateQuery = getAuthState();
  const inviteInfoQuery = $derived(token ? getInviteInfo(token) : undefined);
  const oidcQuery = getOidcProviders();

  const isAuthenticated = $derived(authStateQuery.current?.isAuthenticated ?? false);
  const inviteInfo = $derived(inviteInfoQuery?.current);
  const oidc = $derived(oidcQuery.current);
  const hasOidc = $derived(oidc?.enabled && (oidc?.providers?.length ?? 0) > 0);

  // ── Invite validity ───────────────────────────────────────────────
  const inviteError = $derived.by(() => {
    if (!token) return "No invite token provided. Please check the link you were given.";
    if (inviteInfoQuery?.error) return "This invite link is invalid or has expired.";
    if (inviteInfo && !inviteInfo.isValid) {
      if (inviteInfo.isExpired) return "This invite has expired.";
      if (inviteInfo.isRevoked) return "This invite has been revoked.";
      return "This invite is no longer valid.";
    }
    return null;
  });

  // ── Accept invite (authenticated) ────────────────────────────────
  let isAccepting = $state(false);
  let acceptError = $state<string | null>(null);

  async function handleAcceptInvite() {
    isAccepting = true;
    acceptError = null;
    try {
      const result = await acceptInvite(token);
      if (result.success) {
        await goto("/", { replaceState: true });
      } else {
        acceptError = result.errorDescription ?? "Failed to accept invite.";
      }
    } catch (err) {
      acceptError = err instanceof Error ? err.message : "Failed to accept invite.";
    } finally {
      isAccepting = false;
    }
  }

  // ── OIDC login ───────────────────────────────────────────────────
  let isRedirecting = $state(false);
  let selectedProvider = $state<string | null>(null);

  function loginWithProvider(providerId: string) {
    isRedirecting = true;
    selectedProvider = providerId;
    const params = new URLSearchParams();
    params.set("provider", providerId);
    params.set("returnUrl", `/join?token=${encodeURIComponent(token)}`);
    window.location.href = `/api/v4/oidc/login?${params.toString()}`;
  }

  // ── Passkey registration ─────────────────────────────────────────
  let displayName = $state("");
  let username = $state("");
  let isRegistering = $state(false);
  let registrationComplete = $state(false);
  let recoveryCodes = $state<string[]>([]);
  let passkeyError = $state<string | null>(null);
  let codesCopied = $state(false);

  const canRegister = $derived(
    displayName.trim().length > 0 && username.trim().length > 0
  );

  async function handlePasskeyRegister() {
    isRegistering = true;
    passkeyError = null;

    try {
      // Get registration options via invite flow
      const response = await inviteOptions({
        token,
        username: username.trim(),
        displayName: displayName.trim(),
      });
      const options = JSON.parse(response.options ?? "");
      const challengeToken = response.challengeToken ?? "";

      // WebAuthn ceremony
      const attestation = await startRegistration({ optionsJSON: options });

      // Complete registration (also accepts the invite server-side)
      const result = await inviteComplete({
        token,
        attestationResponseJson: JSON.stringify(attestation),
        challengeToken,
      });

      // Set auth cookies
      if (result.accessToken) {
        await setAuthCookies({
          accessToken: result.accessToken,
          refreshToken: result.refreshToken ?? undefined,
          expiresIn: result.expiresIn ?? undefined,
        });
      }

      registrationComplete = true;
      recoveryCodes = result.recoveryCodes ?? [];
    } catch (err) {
      passkeyError =
        err instanceof Error ? err.message : "Failed to register passkey.";
    } finally {
      isRegistering = false;
    }
  }

  async function copyRecoveryCodes() {
    const text = recoveryCodes.join("\n");
    try {
      await navigator.clipboard.writeText(text);
      codesCopied = true;
    } catch {
      codesCopied = true;
    }
  }

  function goHome() {
    goto("/", { replaceState: true });
  }
</script>

<svelte:head>
  <title>Join - Nocturne</title>
</svelte:head>

<div class="flex flex-1 items-center justify-center p-4">
  <Card.Root class="w-full max-w-md">
    {#if inviteError}
      <!-- Error state -->
      <Card.Header class="space-y-1 text-center">
        <div
          class="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-destructive/10"
        >
          <AlertTriangle class="h-6 w-6 text-destructive" />
        </div>
        <Card.Title class="text-2xl font-bold">Invalid Invite</Card.Title>
        <Card.Description>{inviteError}</Card.Description>
      </Card.Header>
    {:else if !inviteInfo}
      <!-- Loading state -->
      <Card.Header class="space-y-1 text-center">
        <div
          class="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-primary/10"
        >
          <Loader2 class="h-6 w-6 text-primary animate-spin" />
        </div>
        <Card.Title class="text-2xl font-bold">Loading invite...</Card.Title>
      </Card.Header>
    {:else if registrationComplete}
      <!-- Passkey registration complete — show recovery codes -->
      <Card.Header class="space-y-1 text-center">
        <div
          class="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-green-500/10"
        >
          <Check class="h-6 w-6 text-green-600" />
        </div>
        <Card.Title class="text-2xl font-bold">You're In</Card.Title>
        <Card.Description>
          Your account has been created and you've joined
          {inviteInfo.tenantName ?? "the site"}.
        </Card.Description>
      </Card.Header>

      <Card.Content>
        <div class="space-y-4">
          <div class="space-y-3">
            <div class="flex items-center gap-2">
              <ShieldCheck class="h-5 w-5 text-primary" />
              <h3 class="font-medium">Recovery Codes</h3>
            </div>
            <p class="text-sm text-muted-foreground">
              Save these recovery codes in a safe place. If you lose access to
              your passkey, you can use one of these codes to sign in. Each code
              can only be used once.
            </p>

            {#if recoveryCodes.length > 0}
              <div
                class="grid grid-cols-2 gap-2 rounded-lg border bg-muted/50 p-4"
              >
                {#each recoveryCodes as code}
                  <code
                    class="rounded bg-background px-2 py-1 text-center text-sm font-mono"
                  >
                    {code}
                  </code>
                {/each}
              </div>

              <Button
                variant={codesCopied ? "outline" : "default"}
                class="w-full"
                onclick={copyRecoveryCodes}
              >
                {#if codesCopied}
                  <Check class="mr-2 h-4 w-4" />
                  Codes copied
                {:else}
                  <Copy class="mr-2 h-4 w-4" />
                  Copy recovery codes
                {/if}
              </Button>

              {#if !codesCopied}
                <p class="text-center text-xs text-muted-foreground">
                  You must copy your recovery codes before continuing.
                </p>
              {/if}
            {:else}
              <p class="text-sm text-muted-foreground">
                No recovery codes were returned. You can generate new ones later
                from your account settings.
              </p>
            {/if}
          </div>

          <Button
            class="w-full"
            size="lg"
            disabled={recoveryCodes.length > 0 && !codesCopied}
            onclick={goHome}
          >
            Continue to Nocturne
          </Button>
        </div>
      </Card.Content>
    {:else if isAuthenticated}
      <!-- Authenticated — just accept the invite -->
      <Card.Header class="space-y-1 text-center">
        <div
          class="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-primary/10"
        >
          <UserPlus class="h-6 w-6 text-primary" />
        </div>
        <Card.Title class="text-2xl font-bold">Accept Invite</Card.Title>
        <Card.Description>
          {#if inviteInfo.createdByName}
            <strong>{inviteInfo.createdByName}</strong> has invited you to join
          {:else}
            You've been invited to join
          {/if}
          <strong>{inviteInfo.tenantName ?? "a site"}</strong>.
        </Card.Description>
      </Card.Header>

      <Card.Content>
        <div class="space-y-4">
          {#if acceptError}
            <div
              class="flex items-start gap-3 rounded-md border border-destructive/20 bg-destructive/5 p-3"
            >
              <AlertTriangle
                class="mt-0.5 h-4 w-4 shrink-0 text-destructive"
              />
              <p class="text-sm text-destructive">{acceptError}</p>
            </div>
          {/if}

          <Button
            class="w-full"
            size="lg"
            disabled={isAccepting}
            onclick={handleAcceptInvite}
          >
            {#if isAccepting}
              <Loader2 class="mr-2 h-5 w-5 animate-spin" />
              Joining...
            {:else}
              <UserPlus class="mr-2 h-5 w-5" />
              Accept Invite
            {/if}
          </Button>
        </div>
      </Card.Content>
    {:else}
      <!-- Not authenticated — show OIDC + passkey options -->
      <Card.Header class="space-y-1 text-center">
        <div
          class="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-primary/10"
        >
          <UserPlus class="h-6 w-6 text-primary" />
        </div>
        <Card.Title class="text-2xl font-bold">Join Nocturne</Card.Title>
        <Card.Description>
          {#if inviteInfo.createdByName}
            <strong>{inviteInfo.createdByName}</strong> has invited you to join
          {:else}
            You've been invited to join
          {/if}
          <strong>{inviteInfo.tenantName ?? "a site"}</strong>.
          Create an account or sign in to accept.
        </Card.Description>
      </Card.Header>

      <Card.Content>
        <div class="space-y-4">
          {#if passkeyError}
            <div
              class="flex items-start gap-3 rounded-md border border-destructive/20 bg-destructive/5 p-3"
            >
              <AlertTriangle
                class="mt-0.5 h-4 w-4 shrink-0 text-destructive"
              />
              <p class="text-sm text-destructive">{passkeyError}</p>
            </div>
          {/if}

          {#if hasOidc && oidc}
            <!-- OIDC provider buttons -->
            <div class="space-y-3">
              {#each oidc.providers as provider}
                <Button
                  variant="outline"
                  class="w-full h-11 relative"
                  disabled={isRedirecting || isRegistering}
                  onclick={() => provider.id && loginWithProvider(provider.id)}
                >
                  {#if isRedirecting && selectedProvider === provider.id}
                    <Loader2 class="mr-2 h-4 w-4 animate-spin" />
                    Redirecting...
                  {:else}
                    <ExternalLink class="mr-2 h-4 w-4" />
                    Continue with {provider.name}
                  {/if}
                </Button>
              {/each}
            </div>

            <div class="relative">
              <div class="absolute inset-0 flex items-center">
                <span class="w-full border-t"></span>
              </div>
              <div class="relative flex justify-center text-xs uppercase">
                <span class="bg-background px-2 text-muted-foreground">
                  Or create an account with a passkey
                </span>
              </div>
            </div>
          {/if}

          <!-- Passkey registration form -->
          <div class="space-y-2">
            <Label for="display-name">Display name</Label>
            <Input
              id="display-name"
              type="text"
              placeholder="Your name"
              bind:value={displayName}
              disabled={isRegistering || isRedirecting}
            />
            <p class="text-xs text-muted-foreground">
              This is how you will appear to others.
            </p>
          </div>

          <div class="space-y-2">
            <Label for="pk-username">Username</Label>
            <Input
              id="pk-username"
              type="text"
              placeholder="your-username"
              bind:value={username}
              disabled={isRegistering || isRedirecting}
            />
            <p class="text-xs text-muted-foreground">
              A unique identifier for your account.
            </p>
          </div>

          <Button
            class="w-full"
            size="lg"
            disabled={!canRegister || isRegistering || isRedirecting}
            onclick={handlePasskeyRegister}
          >
            {#if isRegistering}
              <Loader2 class="mr-2 h-5 w-5 animate-spin" />
              Waiting for passkey...
            {:else}
              <Fingerprint class="mr-2 h-5 w-5" />
              Create account with passkey
            {/if}
          </Button>
        </div>
      </Card.Content>
    {/if}
  </Card.Root>
</div>
