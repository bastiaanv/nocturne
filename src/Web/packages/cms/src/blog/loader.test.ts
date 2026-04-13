import { describe, it, expect } from 'vitest';
import { createBlogLoader } from './loader.ts';
import type { BlogManifest } from './types.ts';

const mockManifest: BlogManifest = {
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

const mockModules: Record<string, () => Promise<{ default: unknown; metadata: Record<string, unknown> }>> = {
  '/content/blog/first-post.svx': async () => ({
    default: 'FirstPostComponent',
    metadata: { title: 'First Post' },
  }),
  '/content/blog/second-post.svx': async () => ({
    default: 'SecondPostComponent',
    metadata: { title: 'Second Post' },
  }),
};

describe('createBlogLoader', () => {
  it('returns load and entries functions', () => {
    const loader = createBlogLoader(mockModules, mockManifest);
    expect(loader).toHaveProperty('load');
    expect(loader).toHaveProperty('entries');
    expect(typeof loader.load).toBe('function');
    expect(typeof loader.entries).toBe('function');
  });

  it('entries returns all post slugs', () => {
    const loader = createBlogLoader(mockModules, mockManifest);
    const entries = loader.entries();
    expect(entries).toEqual([{ slug: 'first-post' }, { slug: 'second-post' }]);
  });

  it('load returns content and meta for a valid slug', async () => {
    const loader = createBlogLoader(mockModules, mockManifest);
    const result = await loader.load({ params: { slug: 'first-post' } });
    expect(result.content).toBe('FirstPostComponent');
    expect(result.meta?.title).toBe('First Post');
  });

  it('load throws for an unknown slug', async () => {
    const loader = createBlogLoader(mockModules, mockManifest);
    await expect(loader.load({ params: { slug: 'nonexistent' } })).rejects.toThrow();
  });
});
