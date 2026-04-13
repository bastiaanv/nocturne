<script lang="ts">
  import ContentEditor from '@nocturne/cms/editor/ContentEditor.svelte';
  import { blogMetadataFields } from '@nocturne/cms/editor/types';
  import type { ContentTypeConfig, EditorCallbacks, ContentItem, ContentData } from '@nocturne/cms/editor/types';

  const STORAGE_KEY = 'nocturne-studio-blog';

  function getStorage(): Record<string, ContentData> {
    try {
      return JSON.parse(localStorage.getItem(STORAGE_KEY) || '{}');
    } catch {
      return {};
    }
  }

  function setStorage(data: Record<string, ContentData>) {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
  }

  const config: ContentTypeConfig = {
    mode: 'blog',
    label: 'Blog Posts',
    metadataFields: blogMetadataFields,
    preview: 'markdown',
  };

  const callbacks: EditorCallbacks = {
    async list(): Promise<ContentItem[]> {
      const storage = getStorage();
      return Object.entries(storage).map(([id, data]) => ({
        id,
        title: String(data.metadata.title || 'Untitled'),
        status: data.metadata.draft === false ? 'published' as const : 'draft' as const,
        updatedAt: String(data.metadata.date || ''),
        metadata: data.metadata,
      }));
    },
    async load(id: string): Promise<ContentData> {
      const storage = getStorage();
      return storage[id] ?? { id, content: '', metadata: {} };
    },
    async save(id: string, content: string, metadata: Record<string, unknown>) {
      const storage = getStorage();
      storage[id] = { id, content, metadata };
      setStorage(storage);
    },
    async publish(id: string) {
      const storage = getStorage();
      if (storage[id]) {
        storage[id].metadata.draft = false;
        setStorage(storage);
      }
    },
    async create(metadata: Record<string, unknown>): Promise<string> {
      const id = crypto.randomUUID();
      const storage = getStorage();
      storage[id] = { id, content: '', metadata };
      setStorage(storage);
      return id;
    },
    async delete(id: string) {
      const storage = getStorage();
      delete storage[id];
      setStorage(storage);
    },
  };
</script>

<svelte:head>
  <title>Studio - Nocturne</title>
</svelte:head>

<ContentEditor {config} {callbacks} />
