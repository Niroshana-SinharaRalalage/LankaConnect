# Email Template Comparison: event-published vs event-details

## Template 1: event-published (EXISTS in DB)

**Purpose:** Automatic notification when event is published (Draft → Published)

**Subject:**
```
New Event: {{EventTitle}} in {{EventCity}}, {{EventState}}
```

**Variables Used:**
- EventTitle
- EventDescription
- EventStartDate
- EventStartTime
- EventLocation
- EventCity
- EventState
- IsFree / IsPaid (conditionals)
- TicketPrice
- EventUrl

**HTML Preview:**
```html
<!DOCTYPE html>
<html>
<head>
    <title>New Event Announcement</title>
</head>
<body>
    <h1>New Event Announcement</h1>
    
    <h2>{{EventTitle}}</h2>
    <p>{{EventDescription}}</p>
    
    <div class="event-details">
        <h3>Event Details</h3>
        <p><strong>Date:</strong> {{EventStartDate}} at {{EventStartTime}}</p>
        <p><strong>Location:</strong> {{EventLocation}}</p>
        <p><strong>Admission:</strong> 
            {{#IsFree}}FREE{{/IsFree}}
            {{#IsPaid}}{{TicketPrice}}{{/IsPaid}}
        </p>
    </div>
    
    <a href="{{EventUrl}}">View Event & Register</a>
    
    <footer>
        You're receiving this because you subscribed to events in {{EventCity}}, {{EventState}}
    </footer>
</body>
</html>
```

**Message Type:** "You have a NEW event in your area!"

---

## Template 2: event-details (MISSING from DB)

**Purpose:** Manual notification sent by organizer with custom message

**Subject:**
```
Event Details: {{EventTitle}}
```

**Variables Provided by EventNotificationEmailJob:**
- EventTitle
- EventDate (combined date+time)
- EventLocation
- EventDetailsUrl
- IsFreeEvent
- PricingDetails
- HasSignUpLists (optional)
- SignUpListsUrl (optional)
- HasOrganizerContact (optional)
- OrganizerName (optional)
- OrganizerEmail (optional)
- OrganizerPhone (optional)

**What's in my emergency SQL script:**
```html
<!DOCTYPE html>
<html>
<head>
    <title>Event Details</title>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>{{EventTitle}}</h1>
        </div>
        
        <div class="content">
            <p>Hello,</p>
            <p>{{Message}}</p> <!-- Custom message from organizer -->
            
            <div class="event-details">
                <h2>Event Information</h2>
                <p><strong>Event:</strong> {{EventTitle}}</p>
                <p><strong>Date & Time:</strong> {{EventDateTime}}</p>
                <p><strong>Location:</strong> {{EventLocation}}</p>
                <p><strong>Description:</strong> {{EventDescription}}</p>
            </div>
            
            <a href="{{EventUrl}}">View Event Details</a>
        </div>
        
        <footer>
            © 2026 LankaConnect
        </footer>
    </div>
</body>
</html>
```

**Message Type:** "The organizer has an update for you"

---

## KEY DIFFERENCES

| Aspect | event-published | event-details |
|--------|----------------|---------------|
| **Trigger** | Automatic (event published) | Manual (organizer clicks button) |
| **Custom Message** | ❌ No | ✅ Yes - organizer can write message |
| **Description** | ✅ Full event description | ⚠️ Job doesn't provide it |
| **Date Format** | Separate date + time | Combined EventDate |
| **Location Detail** | City + State shown | Address only |
| **Organizer Contact** | ❌ Not shown | ✅ Optional contact info |
| **Sign-up Lists** | ❌ Not shown | ✅ Optional link |
| **Footer Message** | "You subscribed to {{City}}" | Generic footer |

---

## PROBLEM WITH CURRENT CODE

**EventNotificationEmailJob provides:**
```javascript
{
    "EventTitle": "Christmas Dinner Dance",
    "EventDate": "December 25, 2025 7:00 PM",  // Combined
    "EventLocation": "123 Main St, Boston, MA",
    "EventDetailsUrl": "https://...",
    "IsFreeEvent": false,
    "PricingDetails": "$50.00"
}
```

**event-published template expects:**
```javascript
{
    "EventTitle": "...",
    "EventDescription": "...",     // ❌ MISSING
    "EventStartDate": "December 25, 2025",  // ❌ MISSING (has EventDate instead)
    "EventStartTime": "7:00 PM",   // ❌ MISSING (has EventDate instead)
    "EventLocation": "...",
    "EventCity": "Boston",         // ❌ MISSING
    "EventState": "MA",            // ❌ MISSING
    "EventUrl": "...",             // ❌ WRONG KEY (has EventDetailsUrl)
    "IsFree": false,               // ❌ WRONG KEY (has IsFreeEvent)
    "TicketPrice": "$50.00"        // ❌ WRONG KEY (has PricingDetails)
}
```

**Result if we use event-published:** Template will render with blank fields for Description, City, State, and wrong formatting.

