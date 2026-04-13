import { describe, it, expect } from 'vitest';
import { generateRss } from './rss.ts';
import type { BlogManifest } from './types.ts';

describe('generateRss', () => {
  const manifest: BlogManifest = {
    posts: [
      {
        title: 'First Post',
        slug: 'first-post',
        date: '2026-04-12',
        tags: ['news'],
        category: 'announcements',
        author: 'Rhys',
        summary: 'The first post',
      },
      {
        title: 'Second Post',
        slug: 'second-post',
        date: '2026-04-10',
        tags: ['dev'],
        category: 'engineering',
        author: 'Rhys',
        summary: 'The second post',
      },
    ],
    tags: ['dev', 'news'],
    categories: ['announcements', 'engineering'],
  };

  it('generates valid XML', () => {
    const xml = generateRss(manifest, { siteUrl: 'https://nocturne.health', title: 'Nocturne Blog' });
    expect(xml).toContain('<?xml version="1.0" encoding="UTF-8"?>');
    expect(xml).toContain('<rss version="2.0"');
  });

  it('includes all posts as items', () => {
    const xml = generateRss(manifest, { siteUrl: 'https://nocturne.health', title: 'Nocturne Blog' });
    expect(xml).toContain('<title>First Post</title>');
    expect(xml).toContain('<title>Second Post</title>');
  });

  it('generates correct post links', () => {
    const xml = generateRss(manifest, { siteUrl: 'https://nocturne.health', title: 'Nocturne Blog' });
    expect(xml).toContain('<link>https://nocturne.health/blog/first-post</link>');
  });

  it('includes pubDate for each item', () => {
    const xml = generateRss(manifest, { siteUrl: 'https://nocturne.health', title: 'Nocturne Blog' });
    expect(xml).toContain('<pubDate>');
  });
});
