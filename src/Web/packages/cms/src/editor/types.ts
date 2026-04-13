import type { Extension } from '@tiptap/core';

export interface ContentItem {
	id: string;
	title: string;
	status: 'draft' | 'published';
	updatedAt: string;
	metadata: Record<string, unknown>;
}

export interface ContentData {
	id: string;
	content: string;
	metadata: Record<string, unknown>;
}

export interface EditorCallbacks {
	list: () => Promise<ContentItem[]>;
	load: (id: string) => Promise<ContentData>;
	save: (id: string, content: string, metadata: Record<string, unknown>) => Promise<void>;
	publish: (id: string) => Promise<void>;
	create: (metadata: Record<string, unknown>) => Promise<string>;
	delete?: (id: string) => Promise<void>;
}

export interface ContentTypeConfig {
	mode: 'blog' | 'email';
	label: string;
	extensions?: Extension[];
	metadataFields: MetadataField[];
	preview: 'markdown' | 'email';
}

export interface MetadataField {
	key: string;
	label: string;
	type: 'text' | 'textarea' | 'date' | 'tags' | 'select' | 'toggle';
	required?: boolean;
	options?: string[];
	placeholder?: string;
}

export const blogMetadataFields: MetadataField[] = [
	{ key: 'title', label: 'Title', type: 'text', required: true, placeholder: 'Post title' },
	{ key: 'slug', label: 'Slug', type: 'text', required: true, placeholder: 'url-friendly-slug' },
	{ key: 'date', label: 'Date', type: 'date', required: true },
	{ key: 'author', label: 'Author', type: 'text', required: true },
	{ key: 'category', label: 'Category', type: 'text', required: true },
	{ key: 'tags', label: 'Tags', type: 'tags' },
	{
		key: 'summary',
		label: 'Summary',
		type: 'textarea',
		required: true,
		placeholder: 'Brief description',
	},
	{ key: 'image', label: 'Cover Image', type: 'text', placeholder: '/blog/image.png' },
	{ key: 'draft', label: 'Draft', type: 'toggle' },
];

export const emailMetadataFields: MetadataField[] = [
	{
		key: 'name',
		label: 'Template Name',
		type: 'text',
		required: true,
		placeholder: 'welcome-email',
	},
	{
		key: 'subject',
		label: 'Subject Line',
		type: 'text',
		required: true,
		placeholder: 'Email subject',
	},
	{
		key: 'locale',
		label: 'Locale',
		type: 'select',
		options: ['en', 'es', 'fr', 'de', 'it', 'pt', 'nl', 'ru', 'zh', 'ja', 'ko'],
	},
];
