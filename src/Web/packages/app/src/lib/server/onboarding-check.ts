import type { Cookies } from "@sveltejs/kit";

const COOKIE_NAME = "nocturne-setup-complete";

export interface OnboardingResult {
  isComplete: boolean;
}

/**
 * Check if onboarding has been seen.
 * Cookie-only check — no API calls.
 */
export function checkOnboarding(cookies: Cookies): OnboardingResult {
  return { isComplete: cookies.get(COOKIE_NAME) === "true" };
}

/**
 * Clear the onboarding cookie so the next navigation re-evaluates.
 */
export function invalidateOnboardingCache(cookies: Cookies): void {
  cookies.delete(COOKIE_NAME, { path: "/" });
}
