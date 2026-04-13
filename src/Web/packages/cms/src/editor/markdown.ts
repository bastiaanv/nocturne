export function serializeCalloutToMarkdown(type: string | undefined, content: string): string {
	const calloutType = type ?? 'info';
	return `<Callout type="${calloutType}">\n${content}\n</Callout>`;
}

export function generateFrontmatter(metadata: Record<string, unknown>): string {
	const lines: string[] = ['---'];

	for (const [key, value] of Object.entries(metadata)) {
		if (value === undefined || value === null) continue;

		if (Array.isArray(value)) {
			lines.push(`${key}: [${value.join(', ')}]`);
		} else if (typeof value === 'boolean') {
			lines.push(`${key}: ${value}`);
		} else {
			lines.push(`${key}: ${value}`);
		}
	}

	lines.push('---');
	return lines.join('\n');
}

export function toSvx(
	metadata: Record<string, unknown>,
	body: string,
	imports?: string[]
): string {
	const frontmatter = generateFrontmatter(metadata);
	const importBlock = imports?.length
		? `\n<script>\n${imports.map((i) => `  import ${i};`).join('\n')}\n</script>\n`
		: '';

	return `${frontmatter}${importBlock}\n${body}\n`;
}
