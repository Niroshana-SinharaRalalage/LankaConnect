/**
 * Health Check Endpoint for Azure Container Apps
 *
 * Purpose: Provide liveness and readiness probes for Container Apps health monitoring
 *
 * Container Apps uses this endpoint to:
 * - Liveness probe: Check if container is alive (restart if failing)
 * - Readiness probe: Check if container is ready to receive traffic
 *
 * Returns:
 * - 200 OK: Service is healthy and ready
 * - 500 Internal Server Error: Service is unhealthy
 */

import { NextResponse } from 'next/server';

export async function GET() {
  try {
    const startTime = process.uptime();
    const memoryUsage = process.memoryUsage();

    // Format uptime in human-readable format
    const uptimeSeconds = Math.floor(startTime);
    const hours = Math.floor(uptimeSeconds / 3600);
    const minutes = Math.floor((uptimeSeconds % 3600) / 60);
    const seconds = uptimeSeconds % 60;
    const uptimeFormatted = `${hours}h ${minutes}m ${seconds}s`;

    // Convert memory to MB
    const memoryMB = {
      rss: Math.round(memoryUsage.rss / 1024 / 1024),
      heapTotal: Math.round(memoryUsage.heapTotal / 1024 / 1024),
      heapUsed: Math.round(memoryUsage.heapUsed / 1024 / 1024),
      external: Math.round(memoryUsage.external / 1024 / 1024),
    };

    const healthData = {
      status: 'healthy',
      service: 'lankaconnect-ui',
      timestamp: new Date().toISOString(),
      uptime: uptimeFormatted,
      uptimeSeconds,
      memory: memoryMB,
      environment: process.env.NEXT_PUBLIC_ENV || 'unknown',
      nodeVersion: process.version,
    };

    console.log('[Health] Health check passed:', healthData);

    return NextResponse.json(healthData, {
      status: 200,
      headers: {
        'Content-Type': 'application/json',
        'Cache-Control': 'no-cache, no-store, must-revalidate',
      },
    });
  } catch (error) {
    console.error('[Health] Health check failed:', error);

    return NextResponse.json(
      {
        status: 'unhealthy',
        service: 'lankaconnect-ui',
        timestamp: new Date().toISOString(),
        error: error instanceof Error ? error.message : 'Unknown error',
      },
      {
        status: 500,
        headers: {
          'Content-Type': 'application/json',
          'Cache-Control': 'no-cache, no-store, must-revalidate',
        },
      }
    );
  }
}
