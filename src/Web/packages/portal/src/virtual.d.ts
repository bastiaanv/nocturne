declare module 'virtual:blog-manifest' {
  import type { BlogManifest } from '@nocturne/cms/blog/types';
  const manifest: BlogManifest;
  export default manifest;
}

declare module '*.svx' {
  import type { Component } from 'svelte';
  const component: Component;
  export default component;
}
