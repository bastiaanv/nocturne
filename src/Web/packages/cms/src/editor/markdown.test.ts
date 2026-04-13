import { describe, it, expect } from 'vitest';
import { serializeCalloutToMarkdown, generateFrontmatter, toSvx } from './markdown.ts';

describe('serializeCalloutToMarkdown', () => {
	it('serializes callout node to svx component syntax', () => {
		const result = serializeCalloutToMarkdown('tip', 'Stay tuned for more content.');
		expect(result).toBe('<Callout type="tip">\nStay tuned for more content.\n</Callout>');
	});

	it('defaults to info type', () => {
		const result = serializeCalloutToMarkdown(undefined, 'A note.');
		expect(result).toBe('<Callout type="info">\nA note.\n</Callout>');
	});
});

describe('generateFrontmatter', () => {
	it('generates YAML frontmatter from metadata', () => {
		const meta = {
			title: 'Test Post',
			slug: 'test-post',
			date: '2026-04-13',
			tags: ['news', 'update'],
			category: 'announcements',
			author: 'Rhys',
			summary: 'A test post'
		};
		const result = generateFrontmatter(meta);
		expect(result).toContain('---');
		expect(result).toContain('title: Test Post');
		expect(result).toContain('slug: test-post');
		expect(result).toContain('tags: [news, update]');
	});

	it('excludes undefined values', () => {
		const meta = { title: 'Post', slug: 'post', image: undefined };
		const result = generateFrontmatter(meta);
		expect(result).not.toContain('image');
	});

	it('handles boolean values', () => {
		const meta = { title: 'Draft', draft: true };
		const result = generateFrontmatter(meta);
		expect(result).toContain('draft: true');
	});
});

describe('toSvx', () => {
	it('combines frontmatter and body into a complete .svx file', () => {
		const result = toSvx({ title: 'Post' }, '# Hello\n\nContent here');
		expect(result).toContain('---');
		expect(result).toContain('title: Post');
		expect(result).toContain('# Hello');
	});

	it('includes import block when imports provided', () => {
		const result = toSvx({ title: 'Post' }, '# Hello', [
			"Callout from '@nocturne/cms/components/Callout.svelte'"
		]);
		expect(result).toContain('<script>');
		expect(result).toContain("import Callout from '@nocturne/cms/components/Callout.svelte';");
		expect(result).toContain('</script>');
	});
});
