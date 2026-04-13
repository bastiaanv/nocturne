import { readdir, writeFile, mkdir } from 'node:fs/promises';
import { join, basename } from 'node:path';

export interface EmailBuildOptions {
	/** Directory containing .svx email templates */
	inputDir: string;
	/** Directory to write rendered HTML files */
	outputDir: string;
	/** Locales to render */
	locales: string[];
	/** Render function: takes template path + locale, returns HTML string */
	render: (templatePath: string, locale: string) => Promise<string>;
}

export async function buildEmails(options: EmailBuildOptions): Promise<void> {
	const { inputDir, outputDir, locales, render } = options;

	await mkdir(outputDir, { recursive: true });

	const files = await readdir(inputDir);
	const templates = files.filter((f) => f.endsWith('.svx'));

	console.log(`Building ${templates.length} email templates for ${locales.length} locales...`);

	for (const template of templates) {
		const name = basename(template, '.svx');
		const templatePath = join(inputDir, template);

		for (const locale of locales) {
			const html = await render(templatePath, locale);
			const outFile = join(outputDir, `${name}.${locale}.html`);
			await writeFile(outFile, html, 'utf-8');
			console.log(`  Built: ${outFile}`);
		}
	}

	console.log('Email build complete.');
}
