/**
 * Remote functions for IOB (Insulin on Board) data
 *
 * Note: IOB calculations are provided through the ChartDataClient endpoint.
 * This file is kept for backward compatibility.
 */
import { query } from '$app/server';
import { z } from 'zod';
import { error } from '@sveltejs/kit';

/**
 * Hourly IOB data structure
 */
export interface HourlyIobData {
	timeSlot: number;
	hour: number;
	minute: number;
	timeLabel: string;
	totalIOB: number;
	bolusIOB: number;
	basalIOB: number;
}

const hourlyIobSchema = z.object({
	intervalMinutes: z.number().optional().default(5),
	hours: z.number().optional().default(24),
	startTime: z.number().optional(),
});

/**
 * Get hourly IOB data for charting
 *
 * Note: This endpoint is not currently implemented in V4.
 * IOB data should be retrieved from the chart data endpoint instead.
 */
export const getHourlyIob = query(hourlyIobSchema, async () => {
	try {
		// Return empty data structure - IOB is calculated server-side
		// and returned through the chart data endpoint
		return {
			data: [] as HourlyIobData[],
		};
	} catch (err) {
		console.error('Error loading IOB data:', err);
		throw error(500, 'Failed to load IOB data');
	}
});
