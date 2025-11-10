'use client';

import React from 'react';
import { QueryClient, QueryClientProvider, useQuery } from '@tanstack/react-query';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import { EventStatus } from '@/infrastructure/api/types/events.types';

// Create a fresh query client for this test
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
      staleTime: 0,
    },
  },
});

function SimpleTest() {
  const { data, isLoading, error, status, fetchStatus } = useQuery({
    queryKey: ['simple-test'],
    queryFn: async () => {
      console.log('✅ Simple queryFn STARTING');
      const result = await eventsRepository.getEvents({
        status: EventStatus.Published,
      });
      console.log('✅ Simple queryFn SUCCESS:', result.length);
      return result;
    },
  });

  console.log('Simple Test Render:', { data: data?.length, isLoading, error, status, fetchStatus });

  return (
    <div style={{ padding: '20px', fontFamily: 'monospace' }}>
      <h1>Simple Test (Fresh Query Client)</h1>
      <p>Status: {status}</p>
      <p>Fetch Status: {fetchStatus}</p>
      <p>Loading: {isLoading ? 'YES' : 'NO'}</p>
      <p>Error: {error ? String(error) : 'None'}</p>
      <p>Events: {data?.length || 0}</p>

      {data && data.length > 0 && (
        <div style={{ marginTop: '20px', background: '#ccffcc', padding: '10px' }}>
          <h2>SUCCESS! Events Loaded:</h2>
          <ul>
            {data.slice(0, 5).map(event => (
              <li key={event.id}>{event.title}</li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}

export default function SimpleTestPage() {
  return (
    <QueryClientProvider client={queryClient}>
      <SimpleTest />
    </QueryClientProvider>
  );
}
