# Newsletter Signup API Integration Requirements

## Current Status

The newsletter signup form in the Footer component is currently a **frontend-only mock implementation**. It validates email format and shows success/error messages but does NOT save emails anywhere.

## Location

**Component**: `C:\Work\LankaConnect\web\src\presentation\components\layout\Footer.tsx`
**Lines**: 91-111 (handleNewsletterSubmit function)

## Current Implementation

```typescript
const handleNewsletterSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
  e.preventDefault();

  if (!email || !email.includes('@')) {
    setSubscribeStatus('error');
    return;
  }

  setSubscribeStatus('loading');

  // TODO: Replace this with actual API call
  setTimeout(() => {
    setSubscribeStatus('success');
    setEmail('');

    setTimeout(() => {
      setSubscribeStatus('idle');
    }, 3000);
  }, 1000);
};
```

## Backend API Requirements

### Endpoint Specification

**Method**: `POST`
**Path**: `/api/newsletter/subscribe`
**Content-Type**: `application/json`

### Request Body

```json
{
  "email": "user@example.com",
  "source": "footer",
  "timestamp": "2025-11-08T21:00:00.000Z",
  "userAgent": "Mozilla/5.0...",
  "ipAddress": "192.168.1.1" // Optional
}
```

### Response Format

**Success (200)**:
```json
{
  "success": true,
  "message": "Successfully subscribed to newsletter",
  "subscriberId": "uuid-string"
}
```

**Error (400)**:
```json
{
  "success": false,
  "error": "Invalid email format",
  "code": "INVALID_EMAIL"
}
```

**Error (409)**:
```json
{
  "success": false,
  "error": "Email already subscribed",
  "code": "ALREADY_SUBSCRIBED"
}
```

**Error (500)**:
```json
{
  "success": false,
  "error": "Internal server error",
  "code": "SERVER_ERROR"
}
```

## Database Schema

### Newsletter Subscribers Table

```sql
CREATE TABLE newsletter_subscribers (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  email VARCHAR(255) UNIQUE NOT NULL,
  status VARCHAR(20) DEFAULT 'active', -- active, unsubscribed, bounced
  source VARCHAR(50), -- footer, popup, landing-page
  subscribed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  unsubscribed_at TIMESTAMP,
  last_email_sent TIMESTAMP,
  email_count INTEGER DEFAULT 0,
  ip_address INET,
  user_agent TEXT,
  verification_token VARCHAR(255),
  verified BOOLEAN DEFAULT false,
  verified_at TIMESTAMP,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_newsletter_email ON newsletter_subscribers(email);
CREATE INDEX idx_newsletter_status ON newsletter_subscribers(status);
CREATE INDEX idx_newsletter_subscribed_at ON newsletter_subscribers(subscribed_at);
```

## Frontend Integration Code

### Step 1: Create API Service

**File**: `C:\Work\LankaConnect\web\src\infrastructure\api\services\newsletter.service.ts`

```typescript
import { apiClient } from '../client';

export interface NewsletterSubscribeRequest {
  email: string;
  source?: string;
}

export interface NewsletterSubscribeResponse {
  success: boolean;
  message?: string;
  subscriberId?: string;
  error?: string;
  code?: string;
}

export const newsletterService = {
  async subscribe(data: NewsletterSubscribeRequest): Promise<NewsletterSubscribeResponse> {
    const response = await apiClient.post<NewsletterSubscribeResponse>(
      '/newsletter/subscribe',
      {
        ...data,
        timestamp: new Date().toISOString(),
        userAgent: navigator.userAgent,
      }
    );
    return response.data;
  },

  async unsubscribe(token: string): Promise<{ success: boolean }> {
    const response = await apiClient.post('/newsletter/unsubscribe', { token });
    return response.data;
  },

  async verify(token: string): Promise<{ success: boolean }> {
    const response = await apiClient.post('/newsletter/verify', { token });
    return response.data;
  },
};
```

### Step 2: Update Footer Component

**File**: `C:\Work\LankaConnect\web\src\presentation\components\layout\Footer.tsx`

Replace lines 91-111 with:

```typescript
import { newsletterService } from '@/infrastructure/api/services/newsletter.service';

const handleNewsletterSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
  e.preventDefault();

  if (!email || !email.includes('@')) {
    setSubscribeStatus('error');
    return;
  }

  setSubscribeStatus('loading');

  try {
    const response = await newsletterService.subscribe({
      email,
      source: 'footer',
    });

    if (response.success) {
      setSubscribeStatus('success');
      setEmail('');

      // Reset status after 3 seconds
      setTimeout(() => {
        setSubscribeStatus('idle');
      }, 3000);
    } else {
      setSubscribeStatus('error');
      console.error('Newsletter subscription failed:', response.error);
    }
  } catch (error) {
    setSubscribeStatus('error');
    console.error('Newsletter subscription error:', error);
  }
};
```

## Email Verification Flow (Optional but Recommended)

### Step 1: Send Verification Email

When a user subscribes, send a verification email:

```
Subject: Confirm your LankaConnect newsletter subscription

Hi there!

Thank you for subscribing to the LankaConnect newsletter. To complete your subscription, please click the link below:

[Verify Email Address] (https://lankaconnect.com/newsletter/verify?token=abc123)

If you didn't subscribe to this newsletter, you can safely ignore this email.

Best regards,
The LankaConnect Team
```

### Step 2: Verification Endpoint

**Method**: `GET`
**Path**: `/newsletter/verify?token=abc123`

### Step 3: Verification Page

**File**: `C:\Work\LankaConnect\web\src\app\newsletter\verify\page.tsx`

```typescript
'use client';

import { useEffect, useState } from 'react';
import { useSearchParams } from 'next/navigation';
import { newsletterService } from '@/infrastructure/api/services/newsletter.service';

export default function VerifyNewsletterPage() {
  const [status, setStatus] = useState<'loading' | 'success' | 'error'>('loading');
  const searchParams = useSearchParams();
  const token = searchParams.get('token');

  useEffect(() => {
    const verify = async () => {
      if (!token) {
        setStatus('error');
        return;
      }

      try {
        const response = await newsletterService.verify(token);
        setStatus(response.success ? 'success' : 'error');
      } catch (error) {
        setStatus('error');
      }
    };

    verify();
  }, [token]);

  return (
    <div className="min-h-screen flex items-center justify-center">
      {status === 'loading' && <p>Verifying your subscription...</p>}
      {status === 'success' && <p>✓ Email verified! You're subscribed.</p>}
      {status === 'error' && <p>✗ Verification failed. Invalid or expired link.</p>}
    </div>
  );
}
```

## Security Considerations

1. **Rate Limiting**: Limit signup attempts to 5 per IP address per hour
2. **Email Validation**: Use regex + DNS check to verify email validity
3. **CAPTCHA**: Consider adding reCAPTCHA for production
4. **GDPR Compliance**: Add checkbox for consent (EU users)
5. **Unsubscribe Link**: Include in every newsletter email
6. **Data Encryption**: Encrypt emails at rest in database
7. **Audit Log**: Track all subscription/unsubscription events

## Email Service Integration

Recommended email service providers:

1. **SendGrid** (recommended)
   - REST API
   - Good deliverability
   - Free tier: 100 emails/day

2. **Amazon SES**
   - Cost-effective
   - Requires AWS setup

3. **Mailchimp**
   - Marketing automation
   - Higher cost

## Testing

### Manual Testing Checklist

- [ ] Valid email subscribes successfully
- [ ] Invalid email shows error message
- [ ] Duplicate email shows appropriate message
- [ ] Loading state displays during API call
- [ ] Success message shows and auto-dismisses after 3 seconds
- [ ] Email field clears after successful subscription
- [ ] Error message persists until user corrects input
- [ ] Network error handled gracefully

### Automated Tests

**File**: `C:\Work\LankaConnect\web\src\__tests__\integration\newsletter.test.ts`

```typescript
import { newsletterService } from '@/infrastructure/api/services/newsletter.service';

describe('Newsletter Service', () => {
  it('should subscribe valid email', async () => {
    const response = await newsletterService.subscribe({
      email: 'test@example.com',
      source: 'footer',
    });
    expect(response.success).toBe(true);
  });

  it('should reject invalid email', async () => {
    const response = await newsletterService.subscribe({
      email: 'invalid-email',
      source: 'footer',
    });
    expect(response.success).toBe(false);
    expect(response.code).toBe('INVALID_EMAIL');
  });

  it('should reject duplicate email', async () => {
    await newsletterService.subscribe({
      email: 'duplicate@example.com',
      source: 'footer',
    });

    const response = await newsletterService.subscribe({
      email: 'duplicate@example.com',
      source: 'footer',
    });

    expect(response.success).toBe(false);
    expect(response.code).toBe('ALREADY_SUBSCRIBED');
  });
});
```

## Deployment Checklist

- [ ] Backend API endpoint implemented and deployed
- [ ] Database table created with proper indexes
- [ ] Email service configured (SendGrid/SES/etc.)
- [ ] Frontend code updated to call API
- [ ] Error handling implemented
- [ ] Rate limiting configured
- [ ] HTTPS enforced
- [ ] GDPR consent added (if applicable)
- [ ] Verification email template created
- [ ] Unsubscribe functionality implemented
- [ ] Analytics tracking added (optional)
- [ ] Load testing performed

## Future Enhancements

1. **Double Opt-In**: Require email verification before sending newsletters
2. **Preferences**: Allow users to select newsletter frequency/topics
3. **Welcome Email**: Send welcome email immediately after subscription
4. **Admin Dashboard**: View subscriber count, growth, engagement
5. **Segmentation**: Group subscribers by location, interests, etc.
6. **A/B Testing**: Test different newsletter content/subject lines
7. **Scheduled Sends**: Queue newsletters for optimal send times
8. **Bounce Handling**: Automatically unsubscribe bounced emails

---

**Last Updated**: 2025-11-08
**Status**: Awaiting Backend Implementation
**Priority**: Medium (P2)
