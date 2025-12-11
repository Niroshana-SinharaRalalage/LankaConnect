# LankaConnect Events API Specification

**Version:** 1.0
**Base URL:** `https://api.lankaconnect.com/api/events` (Production)
**Base URL:** `https://localhost:7001/api/events` (Development)
**Authentication:** Bearer Token (JWT) for authenticated endpoints

---

## Table of Contents

1. [Public Endpoints](#public-endpoints)
2. [Authenticated Endpoints](#authenticated-endpoints)
3. [Admin Endpoints](#admin-endpoints)
4. [Data Models](#data-models)
5. [Enumerations](#enumerations)
6. [Error Handling](#error-handling)

---

## Public Endpoints

### 1. Get All Events (with Filtering)

**Endpoint:** `GET /api/events`
**Authentication:** None
**Description:** Retrieve a list of events with optional filtering

#### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| status | EventStatus | No | Filter by event status (Draft, Published, Active, etc.) |
| category | EventCategory | No | Filter by event category |
| startDateFrom | DateTime | No | Filter events starting from this date |
| startDateTo | DateTime | No | Filter events starting before this date |
| isFreeOnly | boolean | No | Filter for free events only |
| city | string | No | Filter by city name |

#### Response

**Status:** 200 OK
**Content-Type:** application/json

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "Sri Lankan Cultural Festival",
    "description": "Annual celebration of Sri Lankan culture and heritage",
    "startDate": "2025-02-15T10:00:00Z",
    "endDate": "2025-02-15T18:00:00Z",
    "organizerId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "capacity": 500,
    "currentRegistrations": 127,
    "status": "Published",
    "category": "Cultural",
    "createdAt": "2025-01-15T08:30:00Z",
    "updatedAt": "2025-01-20T14:22:00Z",
    "address": "123 Main Street",
    "city": "Colombo",
    "state": "Western Province",
    "zipCode": "00100",
    "country": "Sri Lanka",
    "latitude": 6.9271,
    "longitude": 79.8612,
    "ticketPriceAmount": 1500.00,
    "ticketPriceCurrency": "LKR",
    "isFree": false,
    "images": [
      {
        "id": "9a3f2e1d-5c4b-4a2e-8f7d-1e2c3b4a5f6e",
        "imageUrl": "https://storage.lankaconnect.com/events/img1.jpg",
        "displayOrder": 1,
        "uploadedAt": "2025-01-15T09:00:00Z"
      }
    ],
    "videos": []
  }
]
```

**Error Responses:**
- `400 Bad Request` - Invalid parameters
- `500 Internal Server Error` - Server error

---

### 2. Search Events (Full-Text Search)

**Endpoint:** `GET /api/events/search`
**Authentication:** None
**Description:** Search events using PostgreSQL full-text search (Epic 2 Phase 3)

#### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| searchTerm | string | **Yes** | Search term to match against titles and descriptions |
| page | integer | No | Page number (default: 1) |
| pageSize | integer | No | Items per page (default: 20, max: 100) |
| category | EventCategory | No | Filter by category |
| isFreeOnly | boolean | No | Filter for free events only |
| startDateFrom | DateTime | No | Filter events starting from this date |

#### Response

**Status:** 200 OK
**Content-Type:** application/json

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "title": "Sri Lankan Cultural Festival",
      "description": "Annual celebration of Sri Lankan culture and heritage",
      "startDate": "2025-02-15T10:00:00Z",
      "endDate": "2025-02-15T18:00:00Z",
      "location": {
        "address": {
          "street": "123 Main Street",
          "city": "Colombo",
          "state": "Western Province",
          "zipCode": "00100",
          "country": "Sri Lanka"
        },
        "coordinates": {
          "latitude": 6.9271,
          "longitude": 79.8612
        }
      },
      "category": "Cultural",
      "status": "Published",
      "organizerId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
      "organizerName": "Sri Lanka Cultural Association",
      "capacity": 500,
      "currentRegistrations": 127,
      "ticketPrice": 1500.00,
      "isFree": false,
      "createdAt": "2025-01-15T08:30:00Z",
      "updatedAt": "2025-01-20T14:22:00Z",
      "images": [],
      "videos": [],
      "searchRelevance": 0.8543
    }
  ],
  "totalCount": 45,
  "page": 1,
  "pageSize": 20,
  "totalPages": 3,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

**Error Responses:**
- `400 Bad Request` - Missing or invalid search term
- `500 Internal Server Error` - Server error

---

### 3. Get Event By ID

**Endpoint:** `GET /api/events/{id}`
**Authentication:** None
**Description:** Retrieve details for a specific event. Automatically records view analytics.

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | Guid | **Yes** | Event unique identifier |

#### Response

**Status:** 200 OK
**Content-Type:** application/json

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Sri Lankan Cultural Festival",
  "description": "Annual celebration of Sri Lankan culture and heritage...",
  "startDate": "2025-02-15T10:00:00Z",
  "endDate": "2025-02-15T18:00:00Z",
  "organizerId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "capacity": 500,
  "currentRegistrations": 127,
  "status": "Published",
  "category": "Cultural",
  "createdAt": "2025-01-15T08:30:00Z",
  "updatedAt": "2025-01-20T14:22:00Z",
  "address": "123 Main Street",
  "city": "Colombo",
  "state": "Western Province",
  "zipCode": "00100",
  "country": "Sri Lanka",
  "latitude": 6.9271,
  "longitude": 79.8612,
  "ticketPriceAmount": 1500.00,
  "ticketPriceCurrency": "LKR",
  "isFree": false,
  "images": [
    {
      "id": "9a3f2e1d-5c4b-4a2e-8f7d-1e2c3b4a5f6e",
      "imageUrl": "https://storage.lankaconnect.com/events/img1.jpg",
      "displayOrder": 1,
      "uploadedAt": "2025-01-15T09:00:00Z"
    }
  ],
  "videos": [
    {
      "id": "1b2c3d4e-5f6a-7b8c-9d0e-1f2a3b4c5d6e",
      "videoUrl": "https://storage.lankaconnect.com/events/video1.mp4",
      "thumbnailUrl": "https://storage.lankaconnect.com/events/thumb1.jpg",
      "duration": "00:02:30",
      "format": "mp4",
      "fileSizeBytes": 15728640,
      "displayOrder": 1,
      "uploadedAt": "2025-01-15T10:00:00Z"
    }
  ]
}
```

**Error Responses:**
- `404 Not Found` - Event not found
- `400 Bad Request` - Invalid ID format
- `500 Internal Server Error` - Server error

**Notes:**
- This endpoint fires a fire-and-forget analytics tracking call to record event views
- Analytics tracking includes user ID (if authenticated), IP address, and user agent

---

### 4. Get Nearby Events (Spatial Query)

**Endpoint:** `GET /api/events/nearby`
**Authentication:** None
**Description:** Find events within a specified radius of a location using PostGIS spatial queries (Epic 2 Phase 3)

#### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| latitude | decimal | **Yes** | Latitude coordinate (-90 to 90) |
| longitude | decimal | **Yes** | Longitude coordinate (-180 to 180) |
| radiusKm | double | **Yes** | Search radius in kilometers (0.1 to 1000) |
| category | EventCategory | No | Filter by category |
| isFreeOnly | boolean | No | Filter for free events only |
| startDateFrom | DateTime | No | Filter events starting from this date |

#### Example Request

```
GET /api/events/nearby?latitude=6.9271&longitude=79.8612&radiusKm=10&category=Cultural
```

#### Response

**Status:** 200 OK
**Content-Type:** application/json

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "Sri Lankan Cultural Festival",
    "description": "Annual celebration...",
    "startDate": "2025-02-15T10:00:00Z",
    "endDate": "2025-02-15T18:00:00Z",
    "organizerId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "capacity": 500,
    "currentRegistrations": 127,
    "status": "Published",
    "category": "Cultural",
    "address": "123 Main Street",
    "city": "Colombo",
    "latitude": 6.9271,
    "longitude": 79.8612,
    "isFree": false,
    "images": [],
    "videos": []
  }
]
```

**Error Responses:**
- `400 Bad Request` - Invalid coordinates or radius
- `500 Internal Server Error` - Server error

---

### 5. Get Event ICS Calendar File

**Endpoint:** `GET /api/events/{id}/ics`
**Authentication:** None
**Description:** Export event as ICS calendar file for importing into Google Calendar, Apple Calendar, Outlook, etc.

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | Guid | **Yes** | Event unique identifier |

#### Response

**Status:** 200 OK
**Content-Type:** text/calendar
**Content-Disposition:** attachment; filename="event-{id}.ics"

```ics
BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//LankaConnect//Events//EN
BEGIN:VEVENT
UID:3fa85f64-5717-4562-b3fc-2c963f66afa6
DTSTAMP:20250115T083000Z
DTSTART:20250215T100000Z
DTEND:20250215T180000Z
SUMMARY:Sri Lankan Cultural Festival
DESCRIPTION:Annual celebration of Sri Lankan culture and heritage
LOCATION:123 Main Street, Colombo, Western Province 00100, Sri Lanka
GEO:6.9271;79.8612
STATUS:CONFIRMED
END:VEVENT
END:VCALENDAR
```

**Error Responses:**
- `404 Not Found` - Event not found
- `400 Bad Request` - Invalid ID format
- `500 Internal Server Error` - Server error

---

### 6. Get Event Waiting List

**Endpoint:** `GET /api/events/{id}/waiting-list`
**Authentication:** None
**Description:** Retrieve the waiting list for an event (when capacity is full)

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | Guid | **Yes** | Event unique identifier |

#### Response

**Status:** 200 OK
**Content-Type:** application/json

```json
[
  {
    "userId": "8d7c6b5a-4e3d-2c1b-0a9f-8e7d6c5b4a3f",
    "position": 1,
    "joinedAt": "2025-01-20T15:30:00Z"
  },
  {
    "userId": "9e8d7c6b-5a4e-3d2c-1b0a-9f8e7d6c5b4a",
    "position": 2,
    "joinedAt": "2025-01-21T09:15:00Z"
  }
]
```

**Error Responses:**
- `404 Not Found` - Event not found
- `400 Bad Request` - Invalid ID format
- `500 Internal Server Error` - Server error

---

### 7. Record Event Share (Analytics)

**Endpoint:** `POST /api/events/{id}/share`
**Authentication:** Optional
**Description:** Record a social share for analytics tracking

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | Guid | **Yes** | Event unique identifier |

#### Request Body

```json
{
  "platform": "facebook"
}
```

**Platform values:** "facebook", "twitter", "linkedin", "whatsapp", "email", "copy-link", etc.

#### Response

**Status:** 200 OK

---

## Authenticated Endpoints

All authenticated endpoints require a Bearer token in the Authorization header:

```
Authorization: Bearer <JWT_TOKEN>
```

### 8. Create Event

**Endpoint:** `POST /api/events`
**Authentication:** Required (Organizers only)
**Description:** Create a new event

#### Request Body

```json
{
  "title": "New Sri Lankan Event",
  "description": "Event description...",
  "startDate": "2025-03-15T10:00:00Z",
  "endDate": "2025-03-15T18:00:00Z",
  "capacity": 200,
  "category": "Community",
  "address": "456 Temple Road",
  "city": "Kandy",
  "state": "Central Province",
  "zipCode": "20000",
  "country": "Sri Lanka",
  "ticketPriceAmount": 500.00,
  "ticketPriceCurrency": "LKR"
}
```

#### Response

**Status:** 201 Created
**Location:** /api/events/{newEventId}
**Content-Type:** application/json

```json
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

**Error Responses:**
- `400 Bad Request` - Validation errors
- `401 Unauthorized` - Missing or invalid token
- `500 Internal Server Error` - Server error

---

### 9. Update Event

**Endpoint:** `PUT /api/events/{id}`
**Authentication:** Required (Owner only)
**Description:** Update an existing event

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | Guid | **Yes** | Event unique identifier |

#### Request Body

```json
{
  "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Updated Event Title",
  "description": "Updated description...",
  "startDate": "2025-03-15T10:00:00Z",
  "endDate": "2025-03-15T18:00:00Z",
  "capacity": 250,
  "category": "Community"
}
```

**Note:** The eventId in the body must match the id in the URL path.

#### Response

**Status:** 200 OK

**Error Responses:**
- `400 Bad Request` - Validation errors or ID mismatch
- `401 Unauthorized` - Missing or invalid token
- `403 Forbidden` - Not the event owner
- `404 Not Found` - Event not found
- `500 Internal Server Error` - Server error

---

### 10. Delete Event

**Endpoint:** `DELETE /api/events/{id}`
**Authentication:** Required (Owner only)
**Description:** Delete an event (only draft or cancelled events can be deleted)

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | Guid | **Yes** | Event unique identifier |

#### Response

**Status:** 200 OK

**Error Responses:**
- `400 Bad Request` - Cannot delete (event is not draft/cancelled)
- `401 Unauthorized` - Missing or invalid token
- `403 Forbidden` - Not the event owner
- `404 Not Found` - Event not found
- `500 Internal Server Error` - Server error

---

### 11. Submit Event for Approval

**Endpoint:** `POST /api/events/{id}/submit`
**Authentication:** Required (Owner only)
**Description:** Submit event for admin approval (changes status to UnderReview)

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | Guid | **Yes** | Event unique identifier |

#### Response

**Status:** 200 OK

**Error Responses:**
- `400 Bad Request` - Invalid status transition
- `401 Unauthorized` - Missing or invalid token
- `403 Forbidden` - Not the event owner
- `500 Internal Server Error` - Server error

---

### 12. Publish Event

**Endpoint:** `POST /api/events/{id}/publish`
**Authentication:** Required (Owner only)
**Description:** Publish an event (make it visible to public)

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | Guid | **Yes** | Event unique identifier |

#### Response

**Status:** 200 OK

**Error Responses:**
- `400 Bad Request` - Invalid status transition
- `401 Unauthorized` - Missing or invalid token
- `403 Forbidden` - Not the event owner
- `500 Internal Server Error` - Server error

---

### 13. Cancel Event

**Endpoint:** `POST /api/events/{id}/cancel`
**Authentication:** Required (Owner only)
**Description:** Cancel an event with reason

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | Guid | **Yes** | Event unique identifier |

#### Request Body

```json
{
  "reason": "Venue unavailable due to unforeseen circumstances"
}
```

#### Response

**Status:** 200 OK

**Error Responses:**
- `400 Bad Request` - Invalid status transition or missing reason
- `401 Unauthorized` - Missing or invalid token
- `403 Forbidden` - Not the event owner
- `500 Internal Server Error` - Server error

---

### 14. Postpone Event

**Endpoint:** `POST /api/events/{id}/postpone`
**Authentication:** Required (Owner only)
**Description:** Postpone an event with reason

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | Guid | **Yes** | Event unique identifier |

#### Request Body

```json
{
  "reason": "Postponed to accommodate more participants"
}
```

#### Response

**Status:** 200 OK

**Error Responses:**
- `400 Bad Request` - Invalid status transition or missing reason
- `401 Unauthorized` - Missing or invalid token
- `403 Forbidden` - Not the event owner
- `500 Internal Server Error` - Server error

---

### 15. RSVP to Event

**Endpoint:** `POST /api/events/{id}/rsvp`
**Authentication:** Required
**Description:** Register for an event

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | Guid | **Yes** | Event unique identifier |

#### Request Body

```json
{
  "userId": "8d7c6b5a-4e3d-2c1b-0a9f-8e7d6c5b4a3f",
  "quantity": 2
}
```

#### Response

**Status:** 200 OK

**Error Responses:**
- `400 Bad Request` - Event full, invalid quantity, or already registered
- `401 Unauthorized` - Missing or invalid token
- `500 Internal Server Error` - Server error

---

### 16. Cancel RSVP

**Endpoint:** `DELETE /api/events/{id}/rsvp`
**Authentication:** Required
**Description:** Cancel an event registration

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | Guid | **Yes** | Event unique identifier |

#### Response

**Status:** 200 OK

**Error Responses:**
- `400 Bad Request` - No active RSVP found
- `401 Unauthorized` - Missing or invalid token
- `500 Internal Server Error` - Server error

**Note:** User ID is extracted from the JWT token automatically.

---

### 17. Update RSVP Quantity

**Endpoint:** `PUT /api/events/{id}/rsvp`
**Authentication:** Required
**Description:** Update the quantity of an existing RSVP

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | Guid | **Yes** | Event unique identifier |

#### Request Body

```json
{
  "userId": "8d7c6b5a-4e3d-2c1b-0a9f-8e7d6c5b4a3f",
  "newQuantity": 3
}
```

#### Response

**Status:** 200 OK

**Error Responses:**
- `400 Bad Request` - No active RSVP found or exceeds capacity
- `401 Unauthorized` - Missing or invalid token
- `500 Internal Server Error` - Server error

---

### 18. Get My RSVPs

**Endpoint:** `GET /api/events/my-rsvps`
**Authentication:** Required
**Description:** Get all RSVPs for the authenticated user

#### Response

**Status:** 200 OK
**Content-Type:** application/json

```json
[
  {
    "id": "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d",
    "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "userId": "8d7c6b5a-4e3d-2c1b-0a9f-8e7d6c5b4a3f",
    "quantity": 2,
    "status": "Confirmed",
    "createdAt": "2025-01-15T10:30:00Z",
    "updatedAt": "2025-01-16T14:20:00Z",
    "eventTitle": "Sri Lankan Cultural Festival",
    "eventStartDate": "2025-02-15T10:00:00Z",
    "eventEndDate": "2025-02-15T18:00:00Z",
    "eventStatus": "Published"
  }
]
```

**Error Responses:**
- `401 Unauthorized` - Missing or invalid token
- `500 Internal Server Error` - Server error

---

### 19. Get Upcoming Events

**Endpoint:** `GET /api/events/upcoming`
**Authentication:** Required
**Description:** Get upcoming events for the authenticated user (events they have RSVP'd to)

#### Response

**Status:** 200 OK
**Content-Type:** application/json

Returns an array of `EventDto` objects for events the user has registered for.

**Error Responses:**
- `401 Unauthorized` - Missing or invalid token
- `500 Internal Server Error` - Server error

---

### 20. Add to Waiting List

**Endpoint:** `POST /api/events/{id}/waiting-list`
**Authentication:** Required
**Description:** Add authenticated user to event waiting list

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | Guid | **Yes** | Event unique identifier |

#### Response

**Status:** 200 OK

**Error Responses:**
- `400 Bad Request` - Event not full, already on waiting list, or already registered
- `401 Unauthorized` - Missing or invalid token
- `500 Internal Server Error` - Server error

---

### 21. Remove from Waiting List

**Endpoint:** `DELETE /api/events/{id}/waiting-list`
**Authentication:** Required
**Description:** Remove authenticated user from event waiting list

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | Guid | **Yes** | Event unique identifier |

#### Response

**Status:** 200 OK

**Error Responses:**
- `400 Bad Request` - Not on waiting list
- `401 Unauthorized` - Missing or invalid token
- `500 Internal Server Error` - Server error

---

### 22. Promote from Waiting List

**Endpoint:** `POST /api/events/{id}/waiting-list/promote`
**Authentication:** Required
**Description:** Promote authenticated user from waiting list to confirmed registration (when spot becomes available)

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | Guid | **Yes** | Event unique identifier |

#### Response

**Status:** 200 OK

**Error Responses:**
- `400 Bad Request` - Not on waiting list, event still full, or not first in line
- `401 Unauthorized` - Missing or invalid token
- `500 Internal Server Error` - Server error

---

### 23. Add Image to Event

**Endpoint:** `POST /api/events/{id}/images`
**Authentication:** Required (Owner only)
**Description:** Add an image to event gallery (Epic 2 Phase 2)

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | Guid | **Yes** | Event unique identifier |

#### Request Body (multipart/form-data)

```
Content-Type: multipart/form-data

image: [binary file data]
```

**Supported formats:** JPG, PNG, GIF
**Max file size:** 5MB

#### Response

**Status:** 200 OK
**Content-Type:** application/json

```json
{
  "id": "9a3f2e1d-5c4b-4a2e-8f7d-1e2c3b4a5f6e",
  "imageUrl": "https://storage.lankaconnect.com/events/img1.jpg",
  "displayOrder": 1,
  "uploadedAt": "2025-01-15T09:00:00Z"
}
```

**Error Responses:**
- `400 Bad Request` - No file uploaded or invalid format
- `401 Unauthorized` - Missing or invalid token
- `403 Forbidden` - Not the event owner
- `500 Internal Server Error` - Server error

---

### 24. Replace Event Image

**Endpoint:** `PUT /api/events/{eventId}/images/{imageId}`
**Authentication:** Required (Owner only)
**Description:** Replace an existing event image

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| eventId | Guid | **Yes** | Event unique identifier |
| imageId | Guid | **Yes** | Image unique identifier |

#### Request Body (multipart/form-data)

```
Content-Type: multipart/form-data

image: [binary file data]
```

#### Response

**Status:** 200 OK

**Error Responses:**
- `400 Bad Request` - No file uploaded or invalid format
- `401 Unauthorized` - Missing or invalid token
- `403 Forbidden` - Not the event owner
- `404 Not Found` - Image not found
- `500 Internal Server Error` - Server error

---

### 25. Delete Event Image

**Endpoint:** `DELETE /api/events/{eventId}/images/{imageId}`
**Authentication:** Required (Owner only)
**Description:** Delete an image from event gallery

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| eventId | Guid | **Yes** | Event unique identifier |
| imageId | Guid | **Yes** | Image unique identifier |

#### Response

**Status:** 200 OK

**Error Responses:**
- `400 Bad Request` - Invalid IDs
- `401 Unauthorized` - Missing or invalid token
- `403 Forbidden` - Not the event owner
- `500 Internal Server Error` - Server error

---

### 26. Reorder Event Images

**Endpoint:** `PUT /api/events/{id}/images/reorder`
**Authentication:** Required (Owner only)
**Description:** Reorder images in event gallery

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | Guid | **Yes** | Event unique identifier |

#### Request Body

```json
{
  "newOrders": {
    "9a3f2e1d-5c4b-4a2e-8f7d-1e2c3b4a5f6e": 2,
    "1b2c3d4e-5f6a-7b8c-9d0e-1f2a3b4c5d6e": 1
  }
}
```

**Note:** Key is image ID, value is the new display order.

#### Response

**Status:** 200 OK

**Error Responses:**
- `400 Bad Request` - Invalid order data
- `401 Unauthorized` - Missing or invalid token
- `403 Forbidden` - Not the event owner
- `500 Internal Server Error` - Server error

---

### 27. Add Video to Event

**Endpoint:** `POST /api/events/{id}/videos`
**Authentication:** Required (Owner only)
**Description:** Add a video to event gallery (Epic 2 Phase 2)

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | Guid | **Yes** | Event unique identifier |

#### Request Body (multipart/form-data)

```
Content-Type: multipart/form-data

video: [binary video file]
thumbnail: [binary image file]
```

**Supported video formats:** MP4, MOV, AVI
**Max video size:** 100MB
**Thumbnail format:** JPG, PNG
**Max thumbnail size:** 2MB

#### Response

**Status:** 200 OK
**Content-Type:** application/json

```json
{
  "id": "1b2c3d4e-5f6a-7b8c-9d0e-1f2a3b4c5d6e",
  "videoUrl": "https://storage.lankaconnect.com/events/video1.mp4",
  "thumbnailUrl": "https://storage.lankaconnect.com/events/thumb1.jpg",
  "duration": "00:02:30",
  "format": "mp4",
  "fileSizeBytes": 15728640,
  "displayOrder": 1,
  "uploadedAt": "2025-01-15T10:00:00Z"
}
```

**Error Responses:**
- `400 Bad Request` - No files uploaded or invalid format
- `401 Unauthorized` - Missing or invalid token
- `403 Forbidden` - Not the event owner
- `500 Internal Server Error` - Server error

---

### 28. Delete Event Video

**Endpoint:** `DELETE /api/events/{eventId}/videos/{videoId}`
**Authentication:** Required (Owner only)
**Description:** Delete a video from event gallery

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| eventId | Guid | **Yes** | Event unique identifier |
| videoId | Guid | **Yes** | Video unique identifier |

#### Response

**Status:** 200 OK

**Error Responses:**
- `400 Bad Request` - Invalid IDs
- `401 Unauthorized` - Missing or invalid token
- `403 Forbidden` - Not the event owner
- `500 Internal Server Error` - Server error

---

## Admin Endpoints

All admin endpoints require authentication AND the "AdminOnly" policy.

### 29. Get Pending Events for Approval

**Endpoint:** `GET /api/events/admin/pending`
**Authentication:** Required (Admin only)
**Description:** Get all events pending admin approval

#### Response

**Status:** 200 OK
**Content-Type:** application/json

Returns an array of `EventDto` objects with status "UnderReview".

**Error Responses:**
- `401 Unauthorized` - Missing or invalid token
- `403 Forbidden` - Not an admin
- `500 Internal Server Error` - Server error

---

### 30. Approve Event

**Endpoint:** `POST /api/events/admin/{id}/approve`
**Authentication:** Required (Admin only)
**Description:** Approve an event (changes status to Published)

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | Guid | **Yes** | Event unique identifier |

#### Request Body

```json
{
  "approvedByAdminId": "2c3d4e5f-6a7b-8c9d-0e1f-2a3b4c5d6e7f"
}
```

#### Response

**Status:** 200 OK

**Error Responses:**
- `400 Bad Request` - Invalid status transition
- `401 Unauthorized` - Missing or invalid token
- `403 Forbidden` - Not an admin
- `500 Internal Server Error` - Server error

---

### 31. Reject Event

**Endpoint:** `POST /api/events/admin/{id}/reject`
**Authentication:** Required (Admin only)
**Description:** Reject an event with reason (changes status back to Draft)

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | Guid | **Yes** | Event unique identifier |

#### Request Body

```json
{
  "rejectedByAdminId": "2c3d4e5f-6a7b-8c9d-0e1f-2a3b4c5d6e7f",
  "reason": "Event description does not meet community guidelines"
}
```

#### Response

**Status:** 200 OK

**Error Responses:**
- `400 Bad Request` - Invalid status transition or missing reason
- `401 Unauthorized` - Missing or invalid token
- `403 Forbidden` - Not an admin
- `500 Internal Server Error` - Server error

---

## Data Models

### EventDto

```typescript
interface EventDto {
  id: string;                        // Guid
  title: string;
  description: string;
  startDate: string;                 // ISO 8601 DateTime
  endDate: string;                   // ISO 8601 DateTime
  organizerId: string;               // Guid
  capacity: number;
  currentRegistrations: number;
  status: EventStatus;
  category: EventCategory;
  createdAt: string;                 // ISO 8601 DateTime
  updatedAt?: string;                // ISO 8601 DateTime (nullable)

  // Location (all nullable - not all events have physical locations)
  address?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  country?: string;
  latitude?: number;                 // decimal
  longitude?: number;                // decimal

  // Pricing (nullable - free events)
  ticketPriceAmount?: number;        // decimal
  ticketPriceCurrency?: Currency;
  isFree: boolean;

  // Media galleries
  images: EventImageDto[];
  videos: EventVideoDto[];
}
```

### EventSearchResultDto

```typescript
interface EventSearchResultDto {
  id: string;
  title: string;
  description: string;
  startDate: string;
  endDate: string;
  location?: EventLocation;
  category: EventCategory;
  status: EventStatus;
  organizerId: string;
  organizerName?: string;
  capacity?: number;
  currentRegistrations: number;
  ticketPrice?: number;
  isFree: boolean;
  createdAt: string;
  updatedAt?: string;
  images: EventImageDto[];
  videos: EventVideoDto[];
  searchRelevance: number;           // 0.0 to 1.0
}
```

### EventImageDto

```typescript
interface EventImageDto {
  id: string;                        // Guid
  imageUrl: string;
  displayOrder: number;
  uploadedAt: string;                // ISO 8601 DateTime
}
```

### EventVideoDto

```typescript
interface EventVideoDto {
  id: string;                        // Guid
  videoUrl: string;
  thumbnailUrl: string;
  duration?: string;                 // TimeSpan (nullable)
  format: string;
  fileSizeBytes: number;             // long
  displayOrder: number;
  uploadedAt: string;                // ISO 8601 DateTime
}
```

### RsvpDto

```typescript
interface RsvpDto {
  id: string;                        // Guid
  eventId: string;                   // Guid
  userId: string;                    // Guid
  quantity: number;
  status: RegistrationStatus;
  createdAt: string;                 // ISO 8601 DateTime
  updatedAt?: string;                // ISO 8601 DateTime (nullable)

  // Event information for convenience
  eventTitle?: string;
  eventStartDate?: string;           // ISO 8601 DateTime (nullable)
  eventEndDate?: string;             // ISO 8601 DateTime (nullable)
  eventStatus?: EventStatus;
}
```

### WaitingListEntryDto

```typescript
interface WaitingListEntryDto {
  userId: string;                    // Guid
  position: number;
  joinedAt: string;                  // ISO 8601 DateTime
}
```

### PagedResult<T>

```typescript
interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
```

### EventLocation

```typescript
interface EventLocation {
  address: Address;
  coordinates?: GeoCoordinate;
}

interface Address {
  street: string;
  city: string;
  state: string;
  zipCode: string;
  country: string;
}

interface GeoCoordinate {
  latitude: number;                  // decimal
  longitude: number;                 // decimal
}
```

---

## Enumerations

### EventStatus

```typescript
enum EventStatus {
  Draft = 0,
  Published = 1,
  Active = 2,
  Postponed = 3,
  Cancelled = 4,
  Completed = 5,
  Archived = 6,
  UnderReview = 7
}
```

**String values for API:**
- "Draft"
- "Published"
- "Active"
- "Postponed"
- "Cancelled"
- "Completed"
- "Archived"
- "UnderReview"

---

### EventCategory

```typescript
enum EventCategory {
  Religious = 0,
  Cultural = 1,
  Community = 2,
  Educational = 3,
  Social = 4,
  Business = 5,
  Charity = 6,
  Entertainment = 7
}
```

**String values for API:**
- "Religious"
- "Cultural"
- "Community"
- "Educational"
- "Social"
- "Business"
- "Charity"
- "Entertainment"

---

### RegistrationStatus

```typescript
enum RegistrationStatus {
  Pending = 0,
  Confirmed = 1,
  Waitlisted = 2,
  CheckedIn = 3,
  Completed = 4,
  Cancelled = 5,
  Refunded = 6
}
```

**String values for API:**
- "Pending"
- "Confirmed"
- "Waitlisted"
- "CheckedIn"
- "Completed"
- "Cancelled"
- "Refunded"

---

### Currency

```typescript
enum Currency {
  USD = 1,
  LKR = 2,
  GBP = 3,
  EUR = 4,
  CAD = 5,
  AUD = 6
}
```

**String values for API:**
- "USD"
- "LKR"
- "GBP"
- "EUR"
- "CAD"
- "AUD"

---

## Error Handling

All error responses follow the RFC 7807 Problem Details specification:

### Error Response Format

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Title": ["The Title field is required."],
    "StartDate": ["Start date must be in the future."]
  },
  "traceId": "0HMVFE42A5P7L:00000001"
}
```

### Common HTTP Status Codes

| Code | Meaning | When Used |
|------|---------|-----------|
| 200 | OK | Successful GET, PUT, DELETE, POST operations |
| 201 | Created | Successful POST that creates a resource |
| 400 | Bad Request | Validation errors, malformed requests |
| 401 | Unauthorized | Missing or invalid authentication token |
| 403 | Forbidden | Authenticated but not authorized (e.g., not event owner) |
| 404 | Not Found | Resource does not exist |
| 500 | Internal Server Error | Unexpected server errors |

### Error Handling Best Practices

1. **Always check status codes** before processing responses
2. **Parse error messages** from the `errors` object for validation failures
3. **Handle 401** by redirecting to login or refreshing token
4. **Handle 403** by showing "Access Denied" message
5. **Handle 404** by showing "Not Found" message
6. **Log 500 errors** and show user-friendly message
7. **Use traceId** when reporting errors to support

---

## Frontend Integration Examples

### Example: Fetch All Events with Filters

```typescript
async function fetchEvents(filters: {
  status?: EventStatus;
  category?: EventCategory;
  city?: string;
  isFreeOnly?: boolean;
  startDateFrom?: Date;
  startDateTo?: Date;
}): Promise<EventDto[]> {
  const params = new URLSearchParams();

  if (filters.status) params.append('status', filters.status);
  if (filters.category) params.append('category', filters.category);
  if (filters.city) params.append('city', filters.city);
  if (filters.isFreeOnly !== undefined) params.append('isFreeOnly', String(filters.isFreeOnly));
  if (filters.startDateFrom) params.append('startDateFrom', filters.startDateFrom.toISOString());
  if (filters.startDateTo) params.append('startDateTo', filters.startDateTo.toISOString());

  const response = await fetch(`${API_BASE_URL}/api/events?${params}`);

  if (!response.ok) {
    throw new Error(`Failed to fetch events: ${response.status}`);
  }

  return await response.json();
}
```

### Example: Search Events

```typescript
async function searchEvents(
  searchTerm: string,
  page: number = 1,
  pageSize: number = 20,
  filters?: {
    category?: EventCategory;
    isFreeOnly?: boolean;
    startDateFrom?: Date;
  }
): Promise<PagedResult<EventSearchResultDto>> {
  const params = new URLSearchParams({
    searchTerm,
    page: String(page),
    pageSize: String(pageSize)
  });

  if (filters?.category) params.append('category', filters.category);
  if (filters?.isFreeOnly !== undefined) params.append('isFreeOnly', String(filters.isFreeOnly));
  if (filters?.startDateFrom) params.append('startDateFrom', filters.startDateFrom.toISOString());

  const response = await fetch(`${API_BASE_URL}/api/events/search?${params}`);

  if (!response.ok) {
    throw new Error(`Search failed: ${response.status}`);
  }

  return await response.json();
}
```

### Example: Get Nearby Events

```typescript
async function getNearbyEvents(
  latitude: number,
  longitude: number,
  radiusKm: number,
  filters?: {
    category?: EventCategory;
    isFreeOnly?: boolean;
    startDateFrom?: Date;
  }
): Promise<EventDto[]> {
  const params = new URLSearchParams({
    latitude: String(latitude),
    longitude: String(longitude),
    radiusKm: String(radiusKm)
  });

  if (filters?.category) params.append('category', filters.category);
  if (filters?.isFreeOnly !== undefined) params.append('isFreeOnly', String(filters.isFreeOnly));
  if (filters?.startDateFrom) params.append('startDateFrom', filters.startDateFrom.toISOString());

  const response = await fetch(`${API_BASE_URL}/api/events/nearby?${params}`);

  if (!response.ok) {
    throw new Error(`Failed to fetch nearby events: ${response.status}`);
  }

  return await response.json();
}
```

### Example: RSVP to Event

```typescript
async function rsvpToEvent(
  eventId: string,
  userId: string,
  quantity: number = 1,
  authToken: string
): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/api/events/${eventId}/rsvp`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${authToken}`
    },
    body: JSON.stringify({ userId, quantity })
  });

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.title || 'RSVP failed');
  }
}
```

### Example: Upload Event Image

```typescript
async function uploadEventImage(
  eventId: string,
  imageFile: File,
  authToken: string
): Promise<EventImageDto> {
  const formData = new FormData();
  formData.append('image', imageFile);

  const response = await fetch(`${API_BASE_URL}/api/events/${eventId}/images`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${authToken}`
    },
    body: formData
  });

  if (!response.ok) {
    throw new Error(`Image upload failed: ${response.status}`);
  }

  return await response.json();
}
```

---

## Notes

1. **All DateTime values** are in ISO 8601 format (UTC): `2025-01-15T10:00:00Z`
2. **All Guid values** are formatted as: `3fa85f64-5717-4562-b3fc-2c963f66afa6`
3. **Enum values** can be sent as either integers or strings; responses always use strings
4. **Nullable fields** may be omitted from responses if null
5. **Authentication tokens** should be stored securely (HttpOnly cookies or secure storage)
6. **CORS is enabled** for configured origins in development and production
7. **Rate limiting** may be applied to prevent abuse
8. **File uploads** use multipart/form-data encoding
9. **Pagination** is available on the search endpoint (more endpoints may be paginated in future)
10. **Analytics tracking** is automatic and non-blocking for view and share events

---

## API Versioning

Currently on **Version 1.0**. Future versions will be indicated in the URL path:
- v1: `/api/events`
- v2: `/api/v2/events` (when introduced)

---

## Support

For API support and questions:
- Email: api-support@lankaconnect.com
- Documentation: https://docs.lankaconnect.com/api
- Status Page: https://status.lankaconnect.com

---

**Last Updated:** 2025-01-15
**API Version:** 1.0
