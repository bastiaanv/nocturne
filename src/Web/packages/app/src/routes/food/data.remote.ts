/**
 * Remote functions for food database
 */
import { getRequestEvent, query, command } from '$app/server';
import { error } from '@sveltejs/kit';
import { z } from 'zod';

import type { FoodRecord, QuickPickRecord } from './types';
import type { Food } from '$lib/api';
import { FoodSchema } from '$lib/api/generated/schemas';

const quickPickRecordSchema = z.object({
	_id: z.string().optional(),
	type: z.literal('quickpick'),
	name: z.string(),
	foods: z.array(z.any()),
	carbs: z.number(),
	hideafteruse: z.boolean(),
	hidden: z.boolean(),
	position: z.number(),
});

/**
 * Get all food records and quickpicks
 * NOTE: The backend Food CRUD API endpoints (getFood2, createFood2, updateFood2) do not exist yet.
 * This function is a stub returning empty data until the API is implemented.
 */
export const getFoodData = query(async () => {
	try {
		// FIXME: Backend needs to implement GET /api/v4/foods endpoint
		// For now, return empty food database structure
		const foodList: FoodRecord[] = [];
		const quickPickList: QuickPickRecord[] = [];
		const categories: Record<string, Record<string, boolean>> = {};

		return {
			foodList,
			quickPickList,
			categories,
		};
	} catch (err) {
		console.error('Error loading food database:', err);
		throw error(500, 'Failed to load food database');
	}
});

/**
 * Create a new food record
 * NOTE: Backend Food CRUD endpoint does not exist yet.
 */
export const createFood = command(FoodSchema, async () => {
	try {
		// FIXME: Backend needs to implement POST /api/v4/foods endpoint
		console.error('createFood: API endpoint not implemented');
		return { success: false, error: 'Food API not yet implemented' };
	} catch (err) {
		console.error('Error creating food:', err);
		return { success: false, error: 'Failed to create food' };
	}
});

/**
 * Update an existing food record
 * NOTE: Backend Food CRUD endpoint does not exist yet.
 */
export const updateFood = command(FoodSchema, async (food) => {
	const f = food as Food;

	try {
		if (!f._id) {
			return { success: false, error: 'Food ID is required for update' };
		}
		// FIXME: Backend needs to implement PUT /api/v4/foods/{id} endpoint
		console.error('updateFood: API endpoint not implemented');
		return { success: false, error: 'Food API not yet implemented' };
	} catch (err) {
		console.error('Error updating food:', err);
		return { success: false, error: 'Failed to update food' };
	}
});

/**
 * Delete a food record by ID via V4 endpoint with attribution handling.
 * NOTE: Generated version is broken (missing foodId parameter in codegen), so kept here.
 */
export const deleteFood = command(
	z.object({
		id: z.string(),
		attributionMode: z.enum(['clear', 'remove']).optional(),
	}),
	async ({ id, attributionMode }) => {
		const { locals } = getRequestEvent();
		const { apiClient } = locals;

		try {
			await apiClient.foodsV4.deleteFood(id, attributionMode);
			return { success: true };
		} catch (err) {
			console.error('Error deleting food:', err);
			return { success: false, error: 'Failed to delete food' };
		}
	}
);

/**
 * Create a new quickpick record
 * NOTE: Backend Food CRUD endpoint does not exist yet.
 */
export const createQuickPick = command(quickPickRecordSchema.omit({ _id: true }), async () => {
	try {
		// FIXME: Backend needs to implement POST /api/v4/foods endpoint
		console.error('createQuickPick: API endpoint not implemented');
		return { success: false, error: 'Food API not yet implemented' };
	} catch (err) {
		console.error('Error creating quickpick:', err);
		return { success: false, error: 'Failed to create quickpick' };
	}
});

/**
 * Update an existing quickpick record
 * NOTE: Backend Food CRUD endpoint does not exist yet.
 */
export const updateQuickPick = command(quickPickRecordSchema, async (quickPick) => {
	try {
		if (!quickPick._id) {
			return { success: false, error: 'QuickPick ID is required for update' };
		}
		// FIXME: Backend needs to implement PUT /api/v4/foods/{id} endpoint
		console.error('updateQuickPick: API endpoint not implemented');
		return { success: false, error: 'Food API not yet implemented' };
	} catch (err) {
		console.error('Error updating quickpick:', err);
		return { success: false, error: 'Failed to update quickpick' };
	}
});

/**
 * Batch save quickpicks (delete marked ones, update positions)
 * NOTE: Backend Food CRUD endpoint does not exist yet.
 */
export const saveQuickPicks = command(
	z.object({
		toDelete: z.array(z.string()),
		toUpdate: z.array(quickPickRecordSchema),
	}),
	async () => {
		try {
			// FIXME: Backend needs to implement PUT /api/v4/foods/{id} and DELETE /api/v4/foods/{id} endpoints
			// For now, just acknowledge the request since the endpoints don't exist
			console.error('saveQuickPicks: API endpoints not fully implemented');
			return { success: false, error: 'Food API not yet implemented' };
		} catch (err) {
			console.error('Error saving quickpicks:', err);
			return { success: false, error: 'Failed to save quickpicks' };
		}
	}
);
