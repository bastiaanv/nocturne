import type { BlogPostMeta, BlogManifest } from './types.ts';

interface LoadResult {
  content: unknown;
  meta: BlogPostMeta | undefined;
}

interface BlogLoaderParams {
  params: { slug: string };
}

interface BlogLoaderResult {
  load: (event: BlogLoaderParams) => Promise<LoadResult>;
  entries: () => Array<{ slug: string }>;
}

export function createBlogLoader(
  modules: Record<string, () => Promise<{ default: unknown; metadata: Record<string, unknown> }>>,
  manifest: BlogManifest,
): BlogLoaderResult {
  function entries(): Array<{ slug: string }> {
    return manifest.posts.map((post) => ({ slug: post.slug }));
  }

  async function load({ params }: BlogLoaderParams): Promise<LoadResult> {
    const path = Object.keys(modules).find((p) => p.endsWith(`/${params.slug}.svx`));
    if (!path) throw new Error(`Blog post not found: ${params.slug}`);

    const mod = await modules[path]();
    const meta = manifest.posts.find((p) => p.slug === params.slug);

    return {
      content: mod.default,
      meta,
    };
  }

  return { load, entries };
}
