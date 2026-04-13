import { describe, it, expect } from 'vitest';
import { unified } from 'unified';
import remarkParse from 'remark-parse';
import remarkHtml from 'remark-html';
import { remarkVars } from './vars.ts';

async function process(markdown: string): Promise<string> {
  const result = await unified()
    .use(remarkParse)
    .use(remarkVars)
    .use(remarkHtml, { sanitize: false })
    .process(markdown);
  return String(result);
}

describe('remarkVars', () => {
  it('replaces {var:name} with {{name}} in paragraph text', async () => {
    const result = await process('Hello {var:userName}!');
    expect(result).toContain('{{userName}}');
  });

  it('replaces multiple vars in the same paragraph', async () => {
    const result = await process('Hello {var:firstName} {var:lastName}!');
    expect(result).toContain('{{firstName}}');
    expect(result).toContain('{{lastName}}');
  });

  it('replaces vars in headings', async () => {
    const result = await process('# Welcome {var:userName}');
    expect(result).toContain('{{userName}}');
  });

  it('replaces vars in list items', async () => {
    const result = await process('- Name: {var:userName}\n- Email: {var:userEmail}');
    expect(result).toContain('{{userName}}');
    expect(result).toContain('{{userEmail}}');
  });

  it('leaves text without vars unchanged', async () => {
    const result = await process('Hello world!');
    expect(result).toContain('Hello world!');
    expect(result).not.toContain('{{');
  });

  it('handles vars with underscores and camelCase', async () => {
    const result = await process('{var:user_name} and {var:weekStart}');
    expect(result).toContain('{{user_name}}');
    expect(result).toContain('{{weekStart}}');
  });

  it('does not replace malformed var syntax', async () => {
    const result = await process('{var:} and {var} and {variable:name}');
    expect(result).not.toContain('{{}}');
  });
});
