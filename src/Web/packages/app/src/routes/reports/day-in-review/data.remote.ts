/**
 * Remote functions for day-in-review report
 * Fetches sensor glucose, boluses, and carb intakes for a specific day
 */
import { z } from 'zod';
import { getRequestEvent, query } from '$app/server';
import { error } from '@sveltejs/kit';
import { getAll as getApsSnapshots } from '$api/generated/apssnapshots.generated.remote';
import { getProfileSummary } from '$api/generated/profiles.generated.remote';
import { getLocalDayBoundariesUtc } from '$lib/utils/timezone';

/**
 * Get day-in-review data for a specific date
 */
export const getDayInReviewData = query(
	z.string(), // date string in ISO format (YYYY-MM-DD)
	async (dateParam) => {
		if (!dateParam) {
			throw error(400, 'Date parameter is required');
		}

		const date = new Date(dateParam);
		if (isNaN(date.getTime())) {
			throw error(400, 'Invalid date parameter');
		}

		const { locals } = getRequestEvent();
		const { apiClient } = locals;

		// Resolve the user's timezone from their profile to compute correct day boundaries
		const profile = await getProfileSummary(undefined);
		const timezone = profile?.therapySettings?.[0]?.timezone;
		const { start: dayStart, end: dayEnd } = getLocalDayBoundariesUtc(dateParam, timezone);

		// Fetch v4 data + APS snapshots for historical predictions
		const [entriesResponse, bolusResponse, carbResponse, apsResponse] = await Promise.all([
			apiClient.sensorGlucose.getAll(dayStart, dayEnd, 10000),
			apiClient.bolus.getAll(dayStart, dayEnd, 1000),
			apiClient.nutrition.getCarbIntakes(dayStart, dayEnd, 1000),
			getApsSnapshots({ from: dayStart.getTime(), to: dayEnd.getTime(), limit: 1000, sort: 'timestamp_asc' }),
		]);

		const entries = entriesResponse.data ?? [];
		const boluses = bolusResponse.data ?? [];
		const carbIntakes = carbResponse.data ?? [];
		const apsSnapshots = apsResponse.data ?? [];

		// TODO: apiClient.statistics has been removed. These methods need to be migrated
		// to use apiClient.summary or apiClient.retrospective clients when available.
		const analysis = null;
		const treatmentSummary = null;
		const insulinDelivery = null;

		return {
			date: dateParam,
			entries,
			boluses,
			carbIntakes,
			analysis,
			treatmentSummary,
			insulinDelivery,
			apsSnapshots,
			dateRange: {
				from: dayStart.toISOString(),
				to: dayEnd.toISOString(),
			},
		};
	}
);

