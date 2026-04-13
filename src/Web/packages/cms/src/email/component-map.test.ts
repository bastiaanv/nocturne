import { describe, it, expect } from 'vitest';
import { validateComponentUsage } from './component-map.ts';

describe('validateComponentUsage', () => {
	it('returns empty array when all components are in the allowlist', () => {
		const result = validateComponentUsage(['Callout', 'Var'], {
			Callout: {} as any,
			Var: {} as any,
		});
		expect(result).toEqual([]);
	});

	it('returns missing components', () => {
		const result = validateComponentUsage(['Callout', 'Button', 'Chart'], {
			Callout: {} as any,
		});
		expect(result).toEqual(['Button', 'Chart']);
	});

	it('handles empty usage list', () => {
		const result = validateComponentUsage([], { Callout: {} as any });
		expect(result).toEqual([]);
	});

	it('handles empty allowlist', () => {
		const result = validateComponentUsage(['Callout'], {});
		expect(result).toEqual(['Callout']);
	});
});
