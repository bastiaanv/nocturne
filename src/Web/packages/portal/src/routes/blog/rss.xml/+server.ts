import { generateRss } from '@nocturne/cms/blog/rss';
import manifest from 'virtual:blog-manifest';

export const prerender = true;

export function GET() {
  const xml = generateRss(manifest, {
    siteUrl: 'https://nocturne.health',
    title: 'Nocturne Blog',
    description: 'Updates and announcements from the Nocturne project',
  });

  return new Response(xml, {
    headers: {
      'Content-Type': 'application/xml',
      'Cache-Control': 'max-age=3600',
    },
  });
}
