'use client';

import React from 'react';
import { useEvents } from '@/presentation/hooks/useEvents';
import { EventStatus } from '@/infrastructure/api/types/events.types';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';

export default function TestEventsPage() {
  const [directApiResult, setDirectApiResult] = React.useState<any>(null);
  const [directApiError, setDirectApiError] = React.useState<any>(null);

  const { data: events, isLoading, error, fetchStatus, status } = useEvents({
    status: EventStatus.Published,
    startDateFrom: new Date().toISOString(),
  });

  // Try direct API call without React Query
  React.useEffect(() => {
    eventsRepository.getEvents({
      status: EventStatus.Published,
      startDateFrom: new Date().toISOString(),
    })
      .then((result) => {
        console.log('✅ Direct API call SUCCESS:', result);
        setDirectApiResult(result);
      })
      .catch((err) => {
        console.error('❌ Direct API call ERROR:', err);
        setDirectApiError(err);
      });
  }, []);

  console.log('=== DEBUG INFO ===');
  console.log('API URL:', process.env.NEXT_PUBLIC_API_URL);
  console.log('React Query - Events:', events);
  console.log('React Query - Loading:', isLoading);
  console.log('React Query - Error:', error);
  console.log('React Query - Status:', status);
  console.log('React Query - Fetch Status:', fetchStatus);
  console.log('Direct API - Result:', directApiResult);
  console.log('Direct API - Error:', directApiError);

  return (
    <div style={{ padding: '20px', fontFamily: 'monospace' }}>
      <h1>Events API Test Page</h1>

      <div style={{ marginTop: '20px', padding: '10px', background: '#f0f0f0' }}>
        <h2>Configuration</h2>
        <p><strong>API URL:</strong> {process.env.NEXT_PUBLIC_API_URL || 'NOT SET'}</p>
      </div>

      <div style={{ marginTop: '20px', padding: '10px', background: '#f0f0f0' }}>
        <h2>React Query Status</h2>
        <p><strong>Loading:</strong> {isLoading ? 'YES' : 'NO'}</p>
        <p><strong>Status:</strong> {status}</p>
        <p><strong>Fetch Status:</strong> {fetchStatus}</p>
        <p><strong>Error:</strong> {error ? JSON.stringify(error, null, 2) : 'None'}</p>
        <p><strong>Events Count:</strong> {events?.length || 0}</p>
      </div>

      <div style={{ marginTop: '20px', padding: '10px', background: '#e0e0ff' }}>
        <h2>Direct API Call Status</h2>
        <p><strong>Result:</strong> {directApiResult ? `${directApiResult.length} events` : 'Pending...'}</p>
        <p><strong>Error:</strong> {directApiError ? JSON.stringify(directApiError, null, 2) : 'None'}</p>
      </div>

      {error && (
        <div style={{ marginTop: '20px', padding: '10px', background: '#ffcccc' }}>
          <h2>Error Details</h2>
          <pre>{JSON.stringify(error, null, 2)}</pre>
        </div>
      )}

      {events && events.length > 0 && (
        <div style={{ marginTop: '20px', padding: '10px', background: '#ccffcc' }}>
          <h2>Events ({events.length})</h2>
          <ul>
            {events.map((event) => (
              <li key={event.id}>
                <strong>{event.title}</strong> - {event.city}, {event.state}
              </li>
            ))}
          </ul>
        </div>
      )}

      {isLoading && (
        <div style={{ marginTop: '20px', padding: '10px', background: '#ffffcc' }}>
          <h2>Loading...</h2>
          <p>Fetching events from API...</p>
        </div>
      )}
    </div>
  );
}
