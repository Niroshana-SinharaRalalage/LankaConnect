# LankaConnect Cultural Intelligence API Documentation

## Overview

The LankaConnect Cultural Intelligence API Gateway provides comprehensive access to sophisticated cultural algorithms designed for Sri Lankan diaspora communities. Our API leverages advanced cultural calendar integration, geographic community analysis, and multi-language optimization to deliver culturally intelligent solutions.

## Key Features

- **Event Recommendation Engine**: AI-powered cultural event recommendations with Buddhist/Hindu calendar integration
- **Cultural Communication Optimization**: Email timing and language optimization with Poyaday conflict avoidance
- **Cultural Calendar Intelligence**: Precise Buddhist/Hindu lunar calendar calculations and festival scheduling
- **Diaspora Community Analytics**: Geographic community clustering and cultural preference analysis
- **Multi-Language Support**: Comprehensive Sinhala, Tamil, and English localization
- **Tiered Access Control**: Free, Premium, and Enterprise tiers with escalating capabilities

## Authentication

All API requests require an API key included in the `X-API-Key` header:

```http
X-API-Key: your-api-key-here
X-API-Version: 1.0
```

### API Key Tiers

| Tier | Rate Limit | Features | Use Case |
|------|------------|----------|----------|
| **Free** | 100 req/min | Basic cultural intelligence | Development & testing |
| **Premium** | 1,000 req/min | Advanced analytics, premium algorithms | Production applications |
| **Enterprise** | 5,000 req/min | White-label, custom integrations | Large-scale deployments |

[Get Your API Key →](https://lankaconnect.com/cultural-intelligence-api/register)

## Base URL

```
https://api.lankaconnect.com/api/v1/
```

## Core Endpoints

### 1. Cultural Event Recommendations

#### Get Personalized Event Recommendations

```http
POST /cultural-events/recommendations
Content-Type: application/json
X-API-Key: your-api-key

{
  "userId": "123e4567-e89b-12d3-a456-426614174000",
  "maxResults": 10,
  "includeCulturalScoring": true,
  "includeGeographicScoring": true,
  "culturalFilter": {
    "culturalBackground": "Sri Lankan Buddhist",
    "preferredLanguages": ["Sinhala", "English"],
    "avoidReligiousConflicts": true,
    "preferDiasporaFriendlyEvents": true
  },
  "geographicFilter": {
    "latitude": 37.7749,
    "longitude": -122.4194,
    "maxDistanceKm": 50,
    "preferredRegion": "San Francisco Bay Area"
  }
}
```

**Response:**
```json
{
  "recommendations": [
    {
      "eventId": "event-123",
      "title": "Vesak Day Celebration 2024",
      "description": "Traditional Vesak observance with lantern ceremony",
      "startDate": "2024-05-23T10:00:00Z",
      "endDate": "2024-05-23T18:00:00Z",
      "location": "Fremont Buddhist Temple, CA",
      "recommendationScore": 0.95,
      "culturalScore": {
        "overallScore": 0.92,
        "appropriatenessScore": 0.98,
        "diasporaFriendlinessScore": 0.89,
        "conflictLevel": "None",
        "culturalFactors": [
          "Aligns with full moon Poyaday",
          "Traditional Buddhist observance",
          "Family-friendly timing"
        ]
      },
      "geographicScore": {
        "proximityScore": 0.85,
        "communityDensityScore": 0.91,
        "accessibilityScore": 0.87,
        "distanceKm": 12.3
      },
      "recommendationReasons": [
        "Perfect cultural alignment with Buddhist calendar",
        "High Sri Lankan community attendance expected",
        "Family-oriented celebration format"
      ]
    }
  ],
  "totalCount": 15,
  "metadata": {
    "version": "1.0",
    "generatedAt": "2024-01-15T10:30:00Z",
    "processingTimeMs": 234,
    "algorithmVersion": "CulturalIntelligence-v2.1",
    "dataSources": [
      "EventRecommendationEngine",
      "CulturalCalendar", 
      "UserPreferences",
      "GeographicProximityService"
    ]
  }
}
```

#### Analyze Cultural Appropriateness

```http
POST /cultural-events/analyze-appropriateness
Content-Type: application/json

{
  "eventId": "event-123",
  "userId": "user-456",
  "culturalBackground": "Sri Lankan Tamil",
  "analysisDate": "2024-01-15T00:00:00Z",
  "targetAudiences": ["Sri Lankan Tamil", "Hindu", "General"]
}
```

**Response:**
```json
{
  "appropriatenessScore": 0.87,
  "conflictLevel": "Low",
  "recommendations": [
    "Consider Tamil language signage",
    "Include Hindu blessing if appropriate",
    "Ensure vegetarian food options"
  ],
  "calendarValidation": {
    "isPoyadayConflict": false,
    "isFestivalConflict": false,
    "conflictDetails": [],
    "recommendation": "Timing is culturally appropriate"
  },
  "culturalAnalysis": {
    "eventNature": "Cultural/Religious",
    "culturalFactors": [
      "Multi-religious participation welcome",
      "Family-oriented event structure",
      "Traditional elements present"
    ],
    "audienceAppropriatenessScores": {
      "Sri Lankan Tamil": 0.89,
      "Hindu": 0.85,
      "General": 0.82
    },
    "religiousConsiderations": {
      "isBuddhistAppropriate": true,
      "isHinduAppropriate": true,
      "isChristianAppropriate": true,
      "religiousGuidelines": [
        "Inclusive approach recommended",
        "Respect for all faith traditions"
      ]
    }
  }
}
```

### 2. Cultural Communications

#### Optimize Email Timing

```http
POST /cultural-communications/optimize-timing
Content-Type: application/json

{
  "recipientId": "recipient-123",
  "emailType": "EventNotification",
  "culturalBackground": "Sri Lankan Buddhist",
  "preferredTiming": {
    "timeZone": "America/Los_Angeles",
    "preferredDays": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"],
    "avoidReligiousDays": true,
    "preferredStartTime": "09:00:00",
    "preferredEndTime": "17:00:00"
  },
  "schedulingWindow": {
    "startDate": "2024-01-15T00:00:00Z",
    "endDate": "2024-02-15T00:00:00Z"
  }
}
```

#### Multi-Language Optimization

```http
POST /cultural-communications/multi-language
Content-Type: application/json

{
  "recipientId": "recipient-456",
  "emailType": "WelcomeEmail",
  "languagePreferences": ["Tamil", "English"],
  "culturalContext": {
    "region": "Toronto",
    "communityType": "Urban",
    "educationLevel": "University"
  }
}
```

### 3. Cultural Calendar Intelligence

#### Get Poyaday Calculations

```http
POST /cultural-calendar/poya-days
Content-Type: application/json

{
  "year": 2024,
  "calculationType": "FullMoonPoyadays",
  "timeZone": "Asia/Colombo"
}
```

**Response:**
```json
{
  "poyadayDates": [
    {
      "date": "2024-01-25T00:00:00Z",
      "poyadayType": "Duruthu",
      "buddhistSignificance": "First sermon after enlightenment commemoration",
      "isFullMoon": true,
      "sinhalaName": "දුරුතු පොහොය"
    },
    {
      "date": "2024-05-23T00:00:00Z",
      "poyadayType": "Vesak",
      "buddhistSignificance": "Birth, enlightenment, and passing of Buddha",
      "isFullMoon": true,
      "sinhalaName": "වෙසක් පොහොය"
    }
  ],
  "year": 2024,
  "calculationMethod": "Astronomical",
  "accuracyEstimate": 0.99
}
```

#### Validate Event Scheduling

```http
POST /cultural-calendar/validate-scheduling
Content-Type: application/json

{
  "proposedEvents": [
    {
      "eventId": "event-123",
      "title": "Business Conference",
      "startDate": "2024-05-23T09:00:00Z",
      "endDate": "2024-05-23T17:00:00Z",
      "eventType": "Business",
      "targetAudience": ["Sri Lankan Buddhist", "Hindu", "General"]
    }
  ],
  "validationCriteria": {
    "checkBuddhistCalendar": true,
    "checkHinduCalendar": true,
    "checkCulturalConflicts": true,
    "requireAppropriatenessScoring": true
  }
}
```

### 4. Diaspora Community Analytics

#### Analyze Community Clusters

```http
POST /diaspora/community-clustering
Content-Type: application/json

{
  "centerPoint": {
    "latitude": 43.7184,
    "longitude": -79.3776
  },
  "analysisRadius": 50,
  "clusteringParameters": {
    "minCommunitySize": 100,
    "includeDemographics": true,
    "includeCulturalMetrics": true,
    "analyzeGenerationalDifferences": true
  }
}
```

**Response:**
```json
{
  "communityCluster": {
    "regionName": "Toronto GTA",
    "sriLankanPopulation": 25000,
    "communityDensity": 0.92,
    "majorNeighborhoods": ["Scarborough", "Markham", "Richmond Hill"]
  },
  "demographics": {
    "totalPopulation": 25000,
    "averageAge": 34.5,
    "educationLevels": {
      "High School": 0.25,
      "University": 0.45,
      "Graduate": 0.20,
      "Other": 0.10
    },
    "incomeDistribution": {
      "Lower": 0.15,
      "Middle": 0.55,
      "Upper Middle": 0.25,
      "High": 0.05
    }
  },
  "culturalMetrics": {
    "languageRetention": {
      "sinhalaRetention": 0.72,
      "tamilRetention": 0.68,
      "englishProficiency": 0.95,
      "generationalDifferences": {
        "FirstGeneration": 0.90,
        "SecondGeneration": 0.60,
        "ThirdGeneration": 0.35
      }
    },
    "traditionalObservance": 0.78,
    "communityEngagement": 0.84
  },
  "generationalBreakdown": {
    "FirstGeneration": 8500,
    "SecondGeneration": 12000,
    "ThirdGeneration": 4500
  }
}
```

## SDK and Integration Patterns

### JavaScript/Node.js SDK

```javascript
const LankaConnectCulturalIntelligence = require('@lankaconnect/cultural-intelligence');

const client = new LankaConnectCulturalIntelligence({
  apiKey: 'your-api-key',
  tier: 'premium', // 'free', 'premium', 'enterprise'
  baseUrl: 'https://api.lankaconnect.com'
});

// Get event recommendations
const recommendations = await client.events.getRecommendations({
  userId: 'user-123',
  culturalBackground: 'Sri Lankan Buddhist',
  location: { lat: 37.7749, lng: -122.4194 },
  preferences: {
    avoidReligiousConflicts: true,
    preferDiasporaEvents: true,
    languages: ['Sinhala', 'English']
  }
});

// Optimize email timing
const timing = await client.communications.optimizeTiming({
  recipientId: 'recipient-456',
  emailType: 'EventReminder',
  avoidPoyadays: true,
  timeZone: 'America/New_York'
});

// Get cultural calendar
const calendar = await client.calendar.getPoyadays({
  year: 2024,
  region: 'NorthAmerica'
});

// Analyze diaspora communities
const community = await client.diaspora.analyzeCommunity({
  location: 'Toronto, Canada',
  radius: 50,
  includeGenerational: true
});
```

### Python SDK

```python
from lankaconnect_cultural_intelligence import CulturalIntelligenceClient

client = CulturalIntelligenceClient(
    api_key='your-api-key',
    tier='premium'
)

# Event recommendations with cultural intelligence
recommendations = await client.events.get_recommendations(
    user_id='user-123',
    cultural_background='Sri Lankan Tamil',
    location={'lat': 43.7184, 'lng': -79.3776},
    preferences={
        'avoid_religious_conflicts': True,
        'prefer_diaspora_events': True,
        'languages': ['Tamil', 'English']
    }
)

# Cultural calendar validation
validation = await client.calendar.validate_scheduling(
    events=[{
        'title': 'Community Gathering',
        'start_date': '2024-05-23T10:00:00Z',
        'target_audience': ['Sri Lankan Tamil', 'Hindu']
    }]
)

# Diaspora analytics
analytics = await client.diaspora.get_community_analytics(
    location='Melbourne, Australia',
    time_range={'start': '2023-01-01', 'end': '2024-01-01'},
    segments=['generation', 'cultural_retention']
)
```

### REST API Client Examples

#### cURL
```bash
# Get event recommendations
curl -X POST https://api.lankaconnect.com/api/v1/cultural-events/recommendations \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key" \
  -H "X-API-Version: 1.0" \
  -d '{
    "userId": "user-123",
    "maxResults": 10,
    "culturalFilter": {
      "culturalBackground": "Sri Lankan Buddhist",
      "avoidReligiousConflicts": true
    }
  }'

# Validate cultural scheduling
curl -X POST https://api.lankaconnect.com/api/v1/cultural-calendar/validate-scheduling \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key" \
  -d '{
    "proposedEvents": [{
      "title": "Business Meeting",
      "startDate": "2024-05-23T14:00:00Z",
      "targetAudience": ["Sri Lankan Buddhist"]
    }]
  }'
```

## Error Handling

The API uses standard HTTP status codes and provides detailed error information:

```json
{
  "error": "ValidationError",
  "message": "Invalid cultural background specified",
  "details": {
    "field": "culturalBackground",
    "supportedValues": ["Sri Lankan Buddhist", "Sri Lankan Tamil", "Sri Lankan Hindu", "Sri Lankan Christian"]
  },
  "documentation": "https://docs.lankaconnect.com/cultural-intelligence-api/cultural-backgrounds"
}
```

### Common Status Codes

- `200` - Success
- `400` - Bad Request (validation errors)
- `401` - Unauthorized (missing/invalid API key)
- `403` - Forbidden (insufficient permissions)
- `429` - Rate Limit Exceeded
- `500` - Internal Server Error

## Rate Limiting

Rate limits are enforced per API key and tier:

```http
HTTP/1.1 429 Too Many Requests
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 1642234567
Content-Type: application/json

{
  "error": "RateLimitExceeded",
  "message": "Rate limit exceeded. Please try again later.",
  "upgradeInfo": "https://lankaconnect.com/cultural-intelligence-api/pricing"
}
```

## Cultural Intelligence Features

### Buddhist Calendar Integration
- Accurate Poyaday calculations using astronomical algorithms
- Festival timing optimization for Vesak, Poson, Esala, etc.
- Cultural appropriateness scoring for religious events

### Hindu Calendar Support
- Tamil calendar integration with Thaipusam, Diwali timing
- Regional variations for Sri Lankan Tamil community
- Multi-religious event planning guidance

### Diaspora Community Intelligence
- Geographic clustering analysis for major cities
- Generational cultural retention patterns
- Community engagement optimization

### Multi-Language Optimization
- Sinhala (සිංහල) script support with Unicode
- Tamil (தமிழ்) localization for Tamil community
- Cultural context-aware English adaptations
- Automatic language preference detection

## Integration Examples

### WhatsApp Business API Integration

```javascript
// Optimize WhatsApp message timing using Cultural Intelligence
const optimalTiming = await client.communications.optimizeTiming({
  recipientId: customer.id,
  communicationType: 'WhatsApp',
  culturalBackground: customer.culturalBackground,
  avoidReligiousDays: true
});

// Send WhatsApp message at optimal time
await whatsappClient.sendMessage({
  to: customer.phone,
  message: await client.communications.getCulturallyAdaptedMessage({
    templateId: 'event_reminder',
    culturalContext: customer.culturalProfile,
    language: customer.preferredLanguage
  }),
  scheduledTime: optimalTiming.recommendedTime
});
```

### Google Calendar Integration

```javascript
// Validate Google Calendar event against cultural calendar
const validation = await client.calendar.validateScheduling({
  proposedEvents: [{
    title: googleEvent.summary,
    startDate: googleEvent.start.dateTime,
    targetAudience: await inferAudienceFromAttendees(googleEvent.attendees)
  }]
});

if (validation.hasConflicts) {
  // Suggest alternative timing
  const alternatives = await client.calendar.suggestAlternatives({
    originalEvent: googleEvent,
    avoidConflicts: true,
    culturalPreferences: userPreferences
  });
}
```

## Support and Resources

- **Developer Portal**: [https://developers.lankaconnect.com](https://developers.lankaconnect.com)
- **API Documentation**: [https://docs.lankaconnect.com/cultural-intelligence-api](https://docs.lankaconnect.com/cultural-intelligence-api)
- **SDK Downloads**: [https://github.com/LankaConnect/cultural-intelligence-sdks](https://github.com/LankaConnect/cultural-intelligence-sdks)
- **Community Forum**: [https://community.lankaconnect.com](https://community.lankaconnect.com)
- **Support Email**: [api-support@lankaconnect.com](mailto:api-support@lankaconnect.com)

### Getting Started
1. [Register for API Key](https://lankaconnect.com/cultural-intelligence-api/register)
2. [Choose Your Tier](https://lankaconnect.com/cultural-intelligence-api/pricing)
3. [Download SDK](https://github.com/LankaConnect/cultural-intelligence-sdks)
4. [Read Integration Guide](https://docs.lankaconnect.com/cultural-intelligence-api/integration-guide)
5. [Join Developer Community](https://community.lankaconnect.com)

---

**LankaConnect Cultural Intelligence API** - Empowering culturally intelligent applications for Sri Lankan diaspora communities worldwide.

*Last updated: January 2024*