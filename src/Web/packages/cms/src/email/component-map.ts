import type { Component } from 'svelte';

export interface EmailComponentMap {
	[componentName: string]: Component;
}

/**
 * Validates that all components used in a template are in the allowlist.
 * Returns an array of component names that are NOT in the map (violations).
 */
export function validateComponentUsage(
	templateComponents: string[],
	allowlist: EmailComponentMap,
): string[] {
	return templateComponents.filter((name) => !(name in allowlist));
}
