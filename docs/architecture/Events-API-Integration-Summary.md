# Events API Integration - Quick Reference

## Architecture Decision

**Pattern:** Repository + React Query Hooks

```
Component → Hook (React Query) → Repository → ApiClient → Backend API
```

## File Locations

### Types & Constants
- `web/src/infrastructure/api/types/events.types.ts` - Event DTOs
- `web/src/infrastructure/api/types/common.types.ts` - Shared types (PagedResult)
- `web/src/domain/constants/events.constants.ts` - Enums (EventStatus, EventCategory, etc.)

### Repository
- `web/src/infrastructure/api/repositories/events.repository.ts` - EventsRepository class
- Exports: `eventsRepository` singleton

### React Query Hooks

**Queries:**
- `web/src/presentation/hooks/queries/events/useEvents.ts`
- `web/src/presentation/hooks/queries/events/useEventById.ts`
- `web/src/presentation/hooks/queries/events/useSearchEvents.ts`
- `web/src/presentation/hooks/queries/events/useNearbyEvents.ts`
- `web/src/presentation/hooks/queries/events/useUserRsvps.ts`
- `web/src/presentation/hooks/queries/events/useUpcomingEvents.ts`

**Mutations:**
- `web/src/presentation/hooks/mutations/events/useCreateEvent.ts`
- `web/src/presentation/hooks/mutations/events/useUpdateEvent.ts`
- `web/src/presentation/hooks/mutations/events/useDeleteEvent.ts`
- `web/src/presentation/hooks/mutations/events/useRsvpToEvent.ts`
- `web/src/presentation/hooks/mutations/events/useCancelRsvp.ts`
- `web/src/presentation/hooks/mutations/events/useUploadEventImage.ts`

## Key Backend Endpoints

### Public Queries
- `GET /api/events` - List events with filters
- `GET /api/events/{id}` - Get event details
- `GET /api/events/search` - Full-text search (PostgreSQL FTS)
- `GET /api/events/nearby` - Geospatial query

### Authenticated Mutations
- `POST /api/events` - Create event
- `PUT /api/events/{id}` - Update event
- `DELETE /api/events/{id}` - Delete event
- `POST /api/events/{id}/rsvp` - RSVP to event
- `DELETE /api/events/{id}/rsvp` - Cancel RSVP
- `POST /api/events/{id}/images` - Upload image
- `GET /api/events/{id}/ics` - Export to calendar

## Usage Examples

### Query Events
```typescript
import { useEvents } from '@/presentation/hooks/queries/events/useEvents';

function EventList() {
  const { data: events, isLoading, error } = useEvents({
    category: 'Cultural',
    isFreeOnly: true,
    city: 'Colombo'
  });

  if (isLoading) return <Skeleton />;
  if (error) return <ErrorDisplay error={error} />;

  return events.map(event => <EventCard key={event.id} event={event} />);
}
```

### Get Single Event
```typescript
import { useEventById } from '@/presentation/hooks/queries/events/useEventById';

function EventDetails({ eventId }: { eventId: string }) {
  const { data: event, isLoading } = useEventById(eventId);

  if (isLoading) return <Skeleton />;

  return <EventDetailsView event={event} />;
}
```

### Create Event
```typescript
import { useCreateEvent } from '@/presentation/hooks/mutations/events/useCreateEvent';

function CreateEventForm() {
  const { mutate: createEvent, isPending } = useCreateEvent();

  const handleSubmit = (data: CreateEventRequest) => {
    createEvent(data, {
      onSuccess: (eventId) => {
        toast.success('Event created!');
        router.push(`/events/${eventId}`);
      },
      onError: (error) => {
        toast.error(error.message);
      },
    });
  };

  return <form onSubmit={handleSubmit}>...</form>;
}
```

### RSVP to Event (with Optimistic Updates)
```typescript
import { useRsvpToEvent } from '@/presentation/hooks/mutations/events/useRsvpToEvent';

function RsvpButton({ eventId, userId }: Props) {
  const { mutate: rsvp, isPending } = useRsvpToEvent();

  const handleRsvp = () => {
    rsvp(
      { eventId, userId, quantity: 1 },
      {
        onSuccess: () => toast.success('RSVP confirmed!'),
        onError: (error) => toast.error(error.message),
      }
    );
  };

  return (
    <Button onClick={handleRsvp} disabled={isPending}>
      {isPending ? 'Processing...' : 'RSVP'}
    </Button>
  );
}
```

### Search Events
```typescript
import { useSearchEvents } from '@/presentation/hooks/queries/events/useSearchEvents';

function EventSearch() {
  const [searchTerm, setSearchTerm] = useState('');
  const { data, isLoading } = useSearchEvents({
    searchTerm,
    page: 1,
    pageSize: 20,
  });

  return (
    <>
      <Input value={searchTerm} onChange={e => setSearchTerm(e.target.value)} />
      {data?.items.map(event => <EventCard key={event.id} event={event} />)}
    </>
  );
}
```

## Type Definitions

### Core Event Type
```typescript
interface EventDto {
  id: string;
  title: string;
  description: string;
  startDate: string;
  endDate: string;
  organizerId: string;
  capacity: number;
  currentRegistrations: number;
  status: EventStatus;
  category: EventCategory;

  // Location (nullable)
  city?: string | null;
  latitude?: number | null;
  longitude?: number | null;

  // Pricing (nullable)
  ticketPriceAmount?: number | null;
  isFree: boolean;

  // Media
  images: EventImageDto[];
  videos: EventVideoDto[];
}
```

### Event Enums
```typescript
enum EventStatus {
  Draft = 'Draft',
  Published = 'Published',
  Cancelled = 'Cancelled',
  Completed = 'Completed',
  // ... more
}

enum EventCategory {
  Cultural = 'Cultural',
  Religious = 'Religious',
  Social = 'Social',
  Educational = 'Educational',
  // ... more
}
```

## Error Handling

All hooks return errors typed as `ApiError`:

```typescript
const { data, error } = useEvents();

if (error instanceof ValidationError) {
  // Handle validation errors (400)
  console.log(error.validationErrors);
} else if (error instanceof UnauthorizedError) {
  // Redirect to login (401)
  router.push('/login');
} else if (error instanceof NotFoundError) {
  // Show 404 page (404)
} else {
  // Generic error handling
  toast.error(error.message);
}
```

## React Query Configuration

Default settings in `web/src/app/providers.tsx`:

```typescript
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000, // 5 minutes
      refetchOnWindowFocus: false,
      retry: 1,
    },
  },
});
```

## Performance Features

### Optimistic Updates
RSVP mutations update UI instantly before server confirms:
```typescript
onMutate: async ({ eventId, quantity }) => {
  // Update UI immediately
  queryClient.setQueryData([EVENT_BY_ID_QUERY_KEY, eventId], (old) => ({
    ...old,
    currentRegistrations: old.currentRegistrations + quantity,
  }));
},
```

### Prefetching
Prefetch event details on card hover:
```typescript
const handleMouseEnter = () => {
  queryClient.prefetchQuery({
    queryKey: [EVENT_BY_ID_QUERY_KEY, event.id],
    queryFn: () => eventsRepository.getEventById(event.id),
  });
};
```

### Infinite Scrolling
For large event lists:
```typescript
const { data, fetchNextPage, hasNextPage } = useInfiniteEvents({
  category: 'Cultural',
});
```

## Testing

### Repository Tests
```typescript
describe('EventsRepository', () => {
  it('should fetch events', async () => {
    const events = await eventsRepository.getEvents();
    expect(events).toBeDefined();
  });
});
```

### Hook Tests
```typescript
import { renderHook } from '@testing-library/react';
import { wrapper } from '@/test-utils';

it('should fetch events', async () => {
  const { result } = renderHook(() => useEvents(), { wrapper });
  await waitFor(() => expect(result.current.isSuccess).toBe(true));
});
```

## Integration with Feed

Convert EventDto to FeedItem:
```typescript
import { eventDtoToFeedItem } from '@/domain/models/FeedItem';

const feedItems = events.map(eventDtoToFeedItem);
```

## Migration Phases

1. **Phase 1:** Types, Repository, Unit Tests
2. **Phase 2:** Query Hooks, EventList/EventCard Components
3. **Phase 3:** Mutation Hooks, Optimistic Updates
4. **Phase 4:** Feed Integration, Event Mappers
5. **Phase 5:** Advanced Features (Infinite Scroll, Real-time)

## Next Steps

1. Review architecture document
2. Create implementation issues in GitHub
3. Start with Phase 1 (types and repository)
4. Code review after each phase

---

**Full Documentation:** `docs/architecture/Events-API-Integration-Architecture.md`
