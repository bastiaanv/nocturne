import type { BlogManifest } from './types.ts';

export interface RssOptions {
  siteUrl: string;
  title: string;
  description?: string;
  language?: string;
}

function escapeXml(str: string): string {
  return str
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&apos;');
}

export function generateRss(manifest: BlogManifest, options: RssOptions): string {
  const { siteUrl, title, description = '', language = 'en' } = options;

  const items = manifest.posts
    .map(
      (post) => `    <item>
      <title>${escapeXml(post.title)}</title>
      <link>${siteUrl}/blog/${post.slug}</link>
      <guid>${siteUrl}/blog/${post.slug}</guid>
      <description>${escapeXml(post.summary)}</description>
      <author>${escapeXml(post.author)}</author>
      <pubDate>${new Date(post.date).toUTCString()}</pubDate>
      ${post.tags.map((t) => `<category>${escapeXml(t)}</category>`).join('\n      ')}
    </item>`,
    )
    .join('\n');

  return `<?xml version="1.0" encoding="UTF-8"?>
<rss version="2.0" xmlns:atom="http://www.w3.org/2005/Atom">
  <channel>
    <title>${escapeXml(title)}</title>
    <link>${siteUrl}</link>
    <description>${escapeXml(description)}</description>
    <language>${language}</language>
    <atom:link href="${siteUrl}/blog/rss.xml" rel="self" type="application/rss+xml"/>
${items}
  </channel>
</rss>`;
}
