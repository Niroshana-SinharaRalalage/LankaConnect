# Frontend Architecture: Epic 1 & Epic 2 Implementation

**Date**: 2025-11-05
**Version**: 1.0
**Author**: System Architect
**Status**: Draft for Implementation

---

## Executive Summary

This document outlines the complete frontend architecture for implementing Epic 1 (Authentication & User Management) and Epic 2 (Events Management) user interfaces for the LankaConnect platform. The frontend will be built using **Next.js 14** with **TypeScript**, following modern React patterns, TDD principles, and Clean Architecture guidelines.

**Timeline**: 4-5 weeks (160-200 development hours)
**Approach**: Incremental TDD with Zero Tolerance for errors
**Backend Integration**: REST APIs (staging environment available)

---

## 1. Technology Stack

### Core Framework
- **Next.js 14** (App Router) - React framework with server-side rendering
- **TypeScript 5.3+** - Type safety throughout
- **React 18** - UI library

### State Management
- **TanStack Query (React Query) v5** - Server state management
- **Zustand** - Client state management (auth, UI state)
- **Context API** - Theme, i18n

### UI Framework & Styling
- **shadcn/ui** - Headless component library (built on Radix UI)
- **Tailwind CSS 3.4** - Utility-first CSS framework
- **Lucide React** - Icon library
- **Class Variance Authority (CVA)** - Component variants

### Form Handling & Validation
- **React Hook Form** - Form state management
- **Zod** - Schema validation (matches backend FluentValidation)

### Testing Framework
- **Vitest** - Unit testing (faster than Jest, native ESM support)
- **React Testing Library** - Component testing
- **Playwright** - E2E testing
- **MSW (Mock Service Worker)** - API mocking

### API Client
- **Axios** - HTTP client with interceptors
- **TypeScript codegen** - Generate types from OpenAPI/Swagger

### Additional Tools
- **next-auth** - Authentication library
- **date-fns** - Date manipulation
- **react-hot-toast** - Notifications
- **zod-i18n** - Internationalization for validation messages

---

## 2. Project Structure

```
client/
├── public/
│   ├── images/
│   └── locales/
├── src/
│   ├── app/                      # Next.js App Router
│   │   ├── (auth)/              # Auth layout group
│   │   │   ├── login/
│   │   │   ├── register/
│   │   │   ├── forgot-password/
│   │   │   ├── reset-password/
│   │   │   └── verify-email/
│   │   ├── (dashboard)/         # Protected layout group
│   │   │   ├── profile/
│   │   │   ├── events/
│   │   │   │   ├── page.tsx     # Events list
│   │   │   │   ├── [id]/        # Event details
│   │   │   │   └── create/      # Create event
│   │   │   └── settings/
│   │   ├── layout.tsx            # Root layout
│   │   ├── page.tsx              # Home page
│   │   └── providers.tsx         # Client providers
│   ├── components/               # Reusable components
│   │   ├── ui/                   # shadcn/ui components
│   │   ├── auth/                 # Auth-specific components
│   │   ├── events/               # Event-specific components
│   │   ├── forms/                # Form components
│   │   ├── layout/               # Layout components
│   │   └── common/               # Shared components
│   ├── lib/                      # Utilities and configurations
│   │   ├── api/                  # API client and endpoints
│   │   ├── hooks/                # Custom React hooks
│   │   ├── stores/               # Zustand stores
│   │   ├── utils/                # Helper functions
│   │   ├── validations/          # Zod schemas
│   │   └── constants/            # Constants and enums
│   ├── types/                    # TypeScript types
│   │   ├── api.ts                # API types (generated)
│   │   ├── auth.ts
│   │   └── events.ts
│   ├── styles/
│   │   └── globals.css           # Global styles
│   └── middleware.ts             # Next.js middleware (auth)
├── tests/
│   ├── unit/                     # Unit tests
│   ├── integration/              # Integration tests
│   └── e2e/                      # Playwright E2E tests
├── .env.local                    # Environment variables
├── next.config.js
├── tailwind.config.ts
├── tsconfig.json
├── vitest.config.ts
└── playwright.config.ts
```

---

## 3. Epic 1: Authentication & User Management

### 3.1 Components Breakdown

#### Phase 1.1: Login & Registration (Week 1)

**Components**:
1. **`LoginPage`** (`app/(auth)/login/page.tsx`)
   - Email/password form
   - Microsoft Entra SSO button
   - "Forgot Password" link
   - "Don't have an account" link

2. **`RegisterPage`** (`app/(auth)/register/page.tsx`)
   - Registration form (name, email, password, confirm password)
   - Email verification notice
   - "Already have an account" link

3. **`LoginForm`** (`components/auth/login-form.tsx`)
   - Email input with validation
   - Password input with show/hide toggle
   - Remember me checkbox
   - Submit button with loading state
   - Error display

4. **`RegisterForm`** (`components/auth/register-form.tsx`)
   - Form fields with React Hook Form
   - Password strength indicator
   - Terms and conditions checkbox
   - Submit button

5. **`SocialLoginButtons`** (`components/auth/social-login-buttons.tsx`)
   - Microsoft Entra button
   - OAuth redirect handling

#### Phase 1.2: Password Reset Flow (Week 1-2)

**Components**:
1. **`ForgotPasswordPage`** (`app/(auth)/forgot-password/page.tsx`)
   - Email input
   - Submit button
   - Success message display

2. **`ResetPasswordPage`** (`app/(auth)/reset-password/page.tsx`)
   - Token validation
   - New password form
   - Password confirmation
   - Success redirect

3. **`ForgotPasswordForm`** (`components/auth/forgot-password-form.tsx`)
   - Email validation
   - API integration

4. **`ResetPasswordForm`** (`components/auth/reset-password-form.tsx`)
   - Password strength validation
   - Confirmation matching

#### Phase 1.3: Email Verification Flow (Week 2)

**Components**:
1. **`VerifyEmailPage`** (`app/(auth)/verify-email/page.tsx`)
   - Token extraction from URL
   - Verification status display
   - Success/error states

2. **`EmailVerificationBanner`** (`components/auth/email-verification-banner.tsx`)
   - Shows when email is not verified
   - Resend verification button
   - Rate limiting indicator (5-minute cooldown)

3. **`ResendVerificationButton`** (`components/auth/resend-verification-button.tsx`)
   - Handles resend API call
   - Shows countdown timer
   - Disabled state during cooldown

#### Phase 1.4: User Profile (Week 2)

**Components**:
1. **`ProfilePage`** (`app/(dashboard)/profile/page.tsx`)
   - Display user information
   - Edit profile form
   - Password change section
   - Email verification status

2. **`ProfileForm`** (`components/profile/profile-form.tsx`)
   - Editable user fields
   - Avatar upload (future)
   - Save button

3. **`ChangePasswordForm`** (`components/profile/change-password-form.tsx`)
   - Current password
   - New password
   - Confirm new password

#### Phase 1.5: Protected Routes & Auth Guard (Week 2)

**Infrastructure**:
1. **`middleware.ts`** - Next.js middleware for route protection
2. **`auth-provider.tsx`** - Authentication context
3. **`useAuth` hook** - Access auth state and actions
4. **Token management** - Refresh token handling
5. **API interceptors** - Axios request/response interceptors

---

### 3.2 API Integration (Epic 1)

**Endpoints**:
```typescript
// lib/api/auth.ts
export const authApi = {
  register: (data: RegisterDto) => axios.post('/api/Auth/register', data),
  login: (data: LoginDto) => axios.post('/api/Auth/login', data),
  loginEntra: () => window.location.href = '/api/Auth/login/entra',
  refresh: (refreshToken: string) => axios.post('/api/Auth/refresh', { refreshToken }),
  logout: () => axios.post('/api/Auth/logout'),
  getProfile: () => axios.get('/api/Auth/profile'),
  forgotPassword: (email: string) => axios.post('/api/Auth/forgot-password', { email }),
  resetPassword: (data: ResetPasswordDto) => axios.post('/api/Auth/reset-password', data),
  verifyEmail: (token: string) => axios.post('/api/Auth/verify-email', { token }),
  resendVerification: () => axios.post('/api/Auth/resend-verification'),
};
```

### 3.3 State Management (Epic 1)

**Zustand Store** (`lib/stores/auth-store.ts`):
```typescript
interface AuthStore {
  user: User | null;
  accessToken: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;

  setAuth: (user: User, tokens: Tokens) => void;
  clearAuth: () => void;
  updateUser: (user: Partial<User>) => void;
}
```

**React Query Hooks**:
- `useLogin` - Login mutation
- `useRegister` - Registration mutation
- `useProfile` - Profile query
- `useUpdateProfile` - Update profile mutation
- `useForgotPassword` - Forgot password mutation
- `useResetPassword` - Reset password mutation
- `useVerifyEmail` - Verify email mutation
- `useResendVerification` - Resend verification mutation

---

## 4. Epic 2: Events Management

### 4.1 Components Breakdown

#### Phase 2.1: Events List & Search (Week 3)

**Components**:
1. **`EventsPage`** (`app/(dashboard)/events/page.tsx`)
   - Events list container
   - Search and filters
   - Pagination
   - Category tabs

2. **`EventsList`** (`components/events/events-list.tsx`)
   - Grid/list view toggle
   - Event cards
   - Loading skeleton
   - Empty state

3. **`EventCard`** (`components/events/event-card.tsx`)
   - Event image/placeholder
   - Title, date, location
   - Category badge
   - Price tag
   - "Register" button
   - "Share" button

4. **`EventsSearch`** (`components/events/events-search.tsx`)
   - Search input with debounce
   - Full-text search integration
   - Search suggestions

5. **`EventsFilters`** (`components/events/events-filters.tsx`)
   - Category dropdown
   - "Free only" checkbox
   - Date range picker
   - Location filter
   - "Apply filters" button

6. **`EventsPagination`** (`components/events/events-pagination.tsx`)
   - Page numbers
   - Previous/Next buttons
   - Items per page selector

#### Phase 2.2: Event Details & Registration (Week 3-4)

**Components**:
1. **`EventDetailsPage`** (`app/(dashboard)/events/[id]/page.tsx`)
   - Event details display
   - Registration form
   - Related events
   - Share functionality

2. **`EventDetails`** (`components/events/event-details.tsx`)
   - Hero image
   - Title, date, time
   - Location with map
   - Full description
   - Organizer info
   - Capacity and availability
   - Category and tags

3. **`EventRegistrationForm`** (`components/events/event-registration-form.tsx`)
   - Attendee information
   - Number of tickets
   - Payment integration (future)
   - Terms acceptance
   - Submit button

4. **`EventAttendanceStatus`** (`components/events/event-attendance-status.tsx`)
   - "Already registered" badge
   - "Checked in" status
   - "Cancelled" status

5. **`WaitingListForm`** (`components/events/waiting-list-form.tsx`)
   - Email input
   - Notification preferences
   - Join waiting list button

6. **`EventShare`** (`components/events/event-share.tsx`)
   - Social media share buttons
   - Copy link button
   - Share count display

7. **`EventCalendar`** (`components/events/event-calendar.tsx`)
   - "Add to Calendar" dropdown
   - Google Calendar link
   - Outlook Calendar link
   - Download .ics file

#### Phase 2.3: Event Creation & Management (Week 4-5, Admin)

**Components**:
1. **`CreateEventPage`** (`app/(dashboard)/events/create/page.tsx`)
   - Event creation form
   - Preview mode
   - Save draft
   - Publish button

2. **`EventForm`** (`components/events/event-form.tsx`)
   - Title, description
   - Date/time pickers
   - Location input with autocomplete
   - Category selection
   - Capacity input
   - Price input
   - Image upload
   - Tags input

3. **`EventFormPreview`** (`components/events/event-form-preview.tsx`)
   - Live preview of event card
   - Preview of details page

---

### 4.2 API Integration (Epic 2)

**Endpoints**:
```typescript
// lib/api/events.ts
export const eventsApi = {
  // Search and list
  searchEvents: (params: SearchEventsDto) =>
    axios.get('/api/Events/search', { params }),

  // CRUD
  getEvent: (id: string) => axios.get(`/api/Events/${id}`),
  createEvent: (data: CreateEventDto) => axios.post('/api/Events', data),
  updateEvent: (id: string, data: UpdateEventDto) =>
    axios.put(`/api/Events/${id}`, data),
  deleteEvent: (id: string) => axios.delete(`/api/Events/${id}`),

  // Categories
  getCategories: () => axios.get('/api/Events/categories'),

  // Registration
  registerForEvent: (id: string, data: RegisterDto) =>
    axios.post(`/api/Events/${id}/register`, data),
  cancelRegistration: (id: string) =>
    axios.delete(`/api/Events/${id}/registration`),

  // Attendance
  checkIn: (eventId: string, registrationId: string) =>
    axios.post(`/api/Events/${eventId}/attendance/${registrationId}/check-in`),

  // Waiting list
  joinWaitingList: (id: string, data: WaitingListDto) =>
    axios.post(`/api/Events/${id}/waiting-list`, data),

  // Sharing
  shareEvent: (id: string) => axios.post(`/api/Events/${id}/share`),

  // Calendar
  getIcsFile: (id: string) => axios.get(`/api/Events/${id}/ics`),
};
```

### 4.3 State Management (Epic 2)

**React Query Hooks**:
- `useSearchEvents` - Search with pagination
- `useEvent` - Get single event
- `useCategories` - Get categories
- `useCreateEvent` - Create event mutation
- `useUpdateEvent` - Update event mutation
- `useDeleteEvent` - Delete event mutation
- `useRegisterForEvent` - Register mutation
- `useCancelRegistration` - Cancel registration mutation
- `useJoinWaitingList` - Join waiting list mutation
- `useShareEvent` - Share event mutation

---

## 5. Common Components Library

### 5.1 UI Components (shadcn/ui)

**Core Components**:
- `Button` - Primary, secondary, outline, ghost variants
- `Input` - Text, email, password, search inputs
- `Form` - Form container with error handling
- `FormField` - Individual form field wrapper
- `Label` - Form labels
- `Select` - Dropdown select
- `Checkbox` - Checkbox input
- `RadioGroup` - Radio button group
- `Textarea` - Multi-line text input
- `DatePicker` - Date selection with calendar
- `Dialog` - Modal dialogs
- `Dropdown Menu` - Context menus
- `Toast` - Notifications
- `Card` - Content container
- `Badge` - Status badges
- `Avatar` - User avatars
- `Tabs` - Tabbed content
- `Skeleton` - Loading skeletons
- `Alert` - Alert messages
- `Separator` - Visual separators

### 5.2 Layout Components

**Components**:
1. **`Header`** (`components/layout/header.tsx`)
   - Logo
   - Navigation menu
   - Search bar
   - User menu
   - Notifications icon

2. **`Footer`** (`components/layout/footer.tsx`)
   - Links
   - Social media
   - Copyright

3. **`Sidebar`** (`components/layout/sidebar.tsx`)
   - Navigation links
   - Collapsible on mobile

4. **`MobileNav`** (`components/layout/mobile-nav.tsx`)
   - Bottom navigation (mobile)
   - 5 main sections

5. **`PageHeader`** (`components/layout/page-header.tsx`)
   - Page title
   - Breadcrumbs
   - Action buttons

### 5.3 Form Components

**Components**:
1. **`PasswordInput`** (`components/forms/password-input.tsx`)
   - Show/hide toggle
   - Strength indicator

2. **`SearchInput`** (`components/forms/search-input.tsx`)
   - Debounced input
   - Clear button
   - Loading state

3. **`DateRangePicker`** (`components/forms/date-range-picker.tsx`)
   - Start and end date
   - Preset ranges

4. **`ImageUpload`** (`components/forms/image-upload.tsx`)
   - Drag & drop
   - Preview
   - Crop functionality

5. **`FormError`** (`components/forms/form-error.tsx`)
   - Error message display
   - Icon
   - Dismissible

---

## 6. Testing Strategy

### 6.1 Unit Tests (Target: 90% Coverage)

**Test Structure**:
```typescript
// tests/unit/components/auth/login-form.test.tsx
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { LoginForm } from '@/components/auth/login-form';

describe('LoginForm', () => {
  it('should render email and password inputs', () => {
    render(<LoginForm />);
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
  });

  it('should show validation errors for invalid email', async () => {
    const user = userEvent.setup();
    render(<LoginForm />);

    const emailInput = screen.getByLabelText(/email/i);
    await user.type(emailInput, 'invalid-email');
    await user.tab(); // Trigger blur

    expect(await screen.findByText(/invalid email/i)).toBeInTheDocument();
  });

  it('should submit form with valid credentials', async () => {
    const onSubmit = vi.fn();
    const user = userEvent.setup();
    render(<LoginForm onSubmit={onSubmit} />);

    await user.type(screen.getByLabelText(/email/i), 'test@example.com');
    await user.type(screen.getByLabelText(/password/i), 'Password123!');
    await user.click(screen.getByRole('button', { name: /login/i }));

    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalledWith({
        email: 'test@example.com',
        password: 'Password123!'
      });
    });
  });
});
```

**Test Categories**:
- Component rendering
- User interactions
- Form validation
- Error handling
- Loading states
- API integration (mocked)

### 6.2 Integration Tests

**Test Scenarios**:
- Complete authentication flow (login → profile → logout)
- Registration → email verification → login
- Password reset flow (forgot → reset → login)
- Event search → filter → view details → register
- Event creation → preview → publish

**Example**:
```typescript
// tests/integration/auth-flow.test.tsx
describe('Authentication Flow', () => {
  it('should complete full registration and login flow', async () => {
    const { user } = renderWithProviders(<App />);

    // Navigate to registration
    await user.click(screen.getByText(/join community/i));

    // Fill registration form
    await user.type(screen.getByLabelText(/email/i), 'new@example.com');
    await user.type(screen.getByLabelText(/password/i), 'Password123!');
    await user.type(screen.getByLabelText(/confirm password/i), 'Password123!');
    await user.click(screen.getByRole('button', { name: /register/i }));

    // Verify email verification banner appears
    expect(await screen.findByText(/verify your email/i)).toBeInTheDocument();

    // Mock email verification
    mockEmailVerification('token123');

    // Navigate to verification link
    await navigateToVerifyEmail('token123');

    // Verify success and redirect to home
    expect(await screen.findByText(/email verified/i)).toBeInTheDocument();
  });
});
```

### 6.3 E2E Tests (Playwright)

**Critical Paths**:
1. User registration and login
2. Password reset
3. Event search and registration
4. Profile update

**Example**:
```typescript
// tests/e2e/auth.spec.ts
import { test, expect } from '@playwright/test';

test('user can register and login', async ({ page }) => {
  await page.goto('/register');

  // Fill registration form
  await page.fill('[name="email"]', 'e2e@example.com');
  await page.fill('[name="password"]', 'Password123!');
  await page.fill('[name="confirmPassword"]', 'Password123!');
  await page.click('button:has-text("Register")');

  // Wait for success message
  await expect(page.locator('text=Registration successful')).toBeVisible();

  // Verify redirect to login
  await expect(page).toHaveURL('/login');

  // Login with new credentials
  await page.fill('[name="email"]', 'e2e@example.com');
  await page.fill('[name="password"]', 'Password123!');
  await page.click('button:has-text("Login")');

  // Verify successful login
  await expect(page).toHaveURL('/');
  await expect(page.locator('text=Welcome')).toBeVisible();
});
```

---

## 7. Security Considerations

### 7.1 Authentication Security

1. **Token Storage**:
   - Store access token in memory (React state/Zustand)
   - Store refresh token in HttpOnly cookie (set by backend)
   - Never store tokens in localStorage

2. **XSS Prevention**:
   - All user input sanitized
   - Use `dangerouslySetInnerHTML` only with sanitized HTML (DOMPurify)
   - Content Security Policy headers

3. **CSRF Protection**:
   - Backend CSRF tokens for state-changing operations
   - SameSite cookie attribute

4. **Password Security**:
   - Client-side validation (min 8 chars, uppercase, lowercase, digit, special char)
   - Never log passwords
   - Password strength indicator

5. **OAuth/Entra Security**:
   - State parameter validation
   - PKCE (Proof Key for Code Exchange) if supported
   - Redirect URL validation

### 7.2 Input Validation

**Validation Strategy**:
- Client-side: Zod schemas (immediate feedback)
- Server-side: FluentValidation (authoritative)
- Sanitize all user input before display
- Prevent SQL injection (parameterized queries on backend)
- Prevent XSS (escape HTML)

**Example Zod Schema**:
```typescript
// lib/validations/auth.ts
import { z } from 'zod';

export const loginSchema = z.object({
  email: z.string().email('Invalid email address'),
  password: z.string().min(1, 'Password is required'),
});

export const registerSchema = z.object({
  email: z.string().email('Invalid email address'),
  password: z
    .string()
    .min(8, 'Password must be at least 8 characters')
    .regex(/[A-Z]/, 'Password must contain at least one uppercase letter')
    .regex(/[a-z]/, 'Password must contain at least one lowercase letter')
    .regex(/[0-9]/, 'Password must contain at least one digit')
    .regex(/[@$!%*?&#]/, 'Password must contain at least one special character'),
  confirmPassword: z.string(),
}).refine((data) => data.password === data.confirmPassword, {
  message: "Passwords don't match",
  path: ['confirmPassword'],
});
```

---

## 8. Performance Optimization

### 8.1 Code Splitting

- **Route-based splitting**: Automatic with Next.js App Router
- **Component lazy loading**: Use `React.lazy()` for large components
- **Dynamic imports**: Load heavy libraries only when needed

```typescript
// Dynamic import for event calendar
const EventCalendar = dynamic(() => import('@/components/events/event-calendar'), {
  loading: () => <Skeleton className="h-96" />,
  ssr: false,
});
```

### 8.2 Image Optimization

- Use Next.js `<Image>` component
- Lazy load images below the fold
- Responsive images (multiple sizes)
- WebP format with fallbacks
- Placeholder blur (LQIP - Low Quality Image Placeholder)

### 8.3 Data Fetching

- **React Query caching**: Cache API responses
- **Stale-while-revalidate**: Show cached data while fetching new
- **Prefetching**: Prefetch data on hover (event cards)
- **Infinite scroll**: Use `useInfiniteQuery` for events list
- **Debouncing**: Debounce search input (300ms)

```typescript
// lib/hooks/use-search-events.ts
export function useSearchEvents(searchTerm: string) {
  const debouncedSearchTerm = useDebounce(searchTerm, 300);

  return useQuery({
    queryKey: ['events', 'search', debouncedSearchTerm],
    queryFn: () => eventsApi.searchEvents({ searchTerm: debouncedSearchTerm }),
    enabled: debouncedSearchTerm.length >= 3,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}
```

### 8.4 Bundle Size

- Tree shaking (automatic with Next.js)
- Analyze bundle with `@next/bundle-analyzer`
- Remove unused dependencies
- Use dynamic imports for heavy components

---

## 9. Deployment Strategy

### 9.1 Hosting

**Recommended**: **Azure Static Web Apps** (to match backend on Azure)

**Alternatives**:
- Vercel (best Next.js experience)
- Netlify
- Azure App Service

### 9.2 Environment Configuration

**Environment Variables**:
```bash
# .env.local (development)
NEXT_PUBLIC_API_URL=https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io
NEXT_PUBLIC_API_TIMEOUT=30000
NEXT_PUBLIC_ENTRA_CLIENT_ID=xxx
NEXT_PUBLIC_ENTRA_AUTHORITY=https://xxx.ciamlogin.com/
```

**Environments**:
- **Local**: `http://localhost:3000` → Staging API
- **Staging**: `https://lankaconnect-staging.azurestaticapps.net` → Staging API
- **Production**: `https://lankaconnect.azurestaticapps.net` → Production API

### 9.3 CI/CD Pipeline

**GitHub Actions Workflow**:
```yaml
name: Deploy Frontend to Azure Static Web Apps

on:
  push:
    branches:
      - develop  # Deploy staging
      - master   # Deploy production
  pull_request:
    types: [opened, synchronize, reopened, closed]

jobs:
  build_and_deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Set up Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'
          cache-dependency-path: client/package-lock.json

      - name: Install dependencies
        working-directory: ./client
        run: npm ci

      - name: Run tests
        working-directory: ./client
        run: npm test -- --coverage

      - name: Build application
        working-directory: ./client
        run: npm run build
        env:
          NEXT_PUBLIC_API_URL: ${{ secrets.API_URL }}

      - name: Deploy to Azure Static Web Apps
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: "upload"
          app_location: "client"
          output_location: ".next"
```

---

## 10. Implementation Phases (TDD Approach)

### Phase 1: Project Setup (Week 1, Days 1-2)

**Tasks**:
1. Initialize Next.js 14 project with TypeScript
2. Install dependencies (shadcn/ui, TanStack Query, Zustand, etc.)
3. Set up Tailwind CSS and global styles
4. Configure Vitest and React Testing Library
5. Set up Playwright for E2E tests
6. Configure ESLint and Prettier
7. Set up folder structure
8. Initialize Git repository in `client/` directory
9. Create README with setup instructions
10. Configure environment variables

**Deliverables**:
- Working Next.js project
- Test configuration
- CI/CD pipeline (basic)

**TDD Approach**:
- Write smoke test first (renders home page)
- Implement minimal home page
- Test passes

---

### Phase 2: Epic 1 - Authentication UI (Week 1-2)

#### Phase 2.1: Login & Registration (Week 1, Days 3-5)

**TDD Workflow**:

**Test 1**: Login form validation
```typescript
// RED: Write failing test
it('should show error for invalid email', () => {
  // Test fails
});

// GREEN: Implement validation
const loginSchema = z.object({
  email: z.string().email(),
});

// REFACTOR: Clean up code
```

**Test 2**: Login form submission
```typescript
// RED: Write failing test
it('should call login API on submit', () => {
  // Test fails
});

// GREEN: Implement login mutation
const useLogin = () => useMutation({
  mutationFn: authApi.login,
});

// REFACTOR: Extract hook
```

**Tasks**:
1. [TDD] Create LoginForm component with validation
2. [TDD] Implement login API integration
3. [TDD] Create RegisterForm component
4. [TDD] Implement registration API integration
5. [TDD] Add password strength indicator
6. [TDD] Create SocialLoginButtons component (Entra SSO)
7. [TDD] Implement token storage in Zustand store
8. [Integration Test] Complete login → profile flow
9. [E2E Test] User can register and login

**Acceptance Criteria**:
- User can login with email/password
- User can register new account
- User can login with Microsoft Entra
- Form validation works correctly
- Loading states display properly
- Errors display appropriately
- Tests: 90%+ coverage

#### Phase 2.2: Password Reset Flow (Week 2, Days 1-2)

**TDD Workflow** (same RED-GREEN-REFACTOR cycle)

**Tasks**:
1. [TDD] Create ForgotPasswordForm component
2. [TDD] Implement forgot password API integration
3. [TDD] Create ResetPasswordForm component
4. [TDD] Implement reset password API integration
5. [TDD] Add token validation
6. [Integration Test] Complete forgot → reset → login flow
7. [E2E Test] User can reset password

**Acceptance Criteria**:
- User can request password reset
- User receives email with reset link
- User can set new password
- Token validation works
- Tests: 90%+ coverage

#### Phase 2.3: Email Verification Flow (Week 2, Days 3-4)

**Tasks**:
1. [TDD] Create VerifyEmailPage component
2. [TDD] Implement email verification API integration
3. [TDD] Create EmailVerificationBanner component
4. [TDD] Create ResendVerificationButton with rate limiting
5. [TDD] Add countdown timer
6. [Integration Test] Verify email flow
7. [E2E Test] User can verify email and resend

**Acceptance Criteria**:
- User can verify email with token
- User can resend verification email
- Rate limiting (5-minute cooldown) works
- Banner shows for unverified users
- Tests: 90%+ coverage

#### Phase 2.4: User Profile (Week 2, Day 5)

**Tasks**:
1. [TDD] Create ProfilePage component
2. [TDD] Implement profile fetch API integration
3. [TDD] Create ProfileForm component
4. [TDD] Implement update profile API integration
5. [TDD] Create ChangePasswordForm component
6. [Integration Test] Update profile flow
7. [E2E Test] User can update profile

**Acceptance Criteria**:
- User can view profile
- User can edit profile information
- User can change password
- Email verification status shows
- Tests: 90%+ coverage

---

### Phase 3: Epic 2 - Events UI (Week 3-4)

#### Phase 3.1: Events List & Search (Week 3, Days 1-3)

**TDD Workflow**:

**Test 1**: Events list renders
```typescript
// RED: Write failing test
it('should display list of events', async () => {
  // Mock API response
  // Test fails
});

// GREEN: Implement EventsList component
const EventsList = () => {
  const { data } = useSearchEvents('');
  return data.map(event => <EventCard key={event.id} event={event} />);
};

// REFACTOR: Extract EventCard
```

**Tasks**:
1. [TDD] Create EventsPage layout
2. [TDD] Create EventsList component
3. [TDD] Create EventCard component
4. [TDD] Implement events search API integration
5. [TDD] Create EventsSearch component with debounce
6. [TDD] Create EventsFilters component
7. [TDD] Implement filter logic (category, free only, date range)
8. [TDD] Create EventsPagination component
9. [TDD] Add grid/list view toggle
10. [TDD] Add loading skeletons
11. [Integration Test] Search → filter → pagination flow
12. [E2E Test] User can search and filter events

**Acceptance Criteria**:
- User can view paginated events list
- User can search events (full-text search)
- User can filter by category, price, date
- User can toggle grid/list view
- Pagination works correctly
- Loading states display
- Tests: 90%+ coverage

#### Phase 3.2: Event Details & Registration (Week 3-4, Days 4-7)

**Tasks**:
1. [TDD] Create EventDetailsPage component
2. [TDD] Implement get event API integration
3. [TDD] Create EventDetails display component
4. [TDD] Create EventRegistrationForm component
5. [TDD] Implement register for event API integration
6. [TDD] Create EventAttendanceStatus component
7. [TDD] Create WaitingListForm component
8. [TDD] Implement waiting list API integration
9. [TDD] Create EventShare component
10. [TDD] Implement share event API integration
11. [TDD] Create EventCalendar component (.ics download)
12. [Integration Test] View event → register flow
13. [Integration Test] Join waiting list flow
14. [E2E Test] User can register for event
15. [E2E Test] User can download calendar file

**Acceptance Criteria**:
- User can view event details
- User can register for event
- User can join waiting list when full
- User can share event
- User can add event to calendar
- Attendance status displays correctly
- Tests: 90%+ coverage

#### Phase 3.3: Event Creation & Management (Week 4-5, Days 1-5, Admin Feature)

**Tasks**:
1. [TDD] Create CreateEventPage component
2. [TDD] Create EventForm component
3. [TDD] Implement create event API integration
4. [TDD] Add image upload functionality
5. [TDD] Create EventFormPreview component
6. [TDD] Add form validation
7. [TDD] Implement update event API integration
8. [TDD] Implement delete event API integration
9. [Integration Test] Create → preview → publish flow
10. [Integration Test] Edit existing event flow
11. [E2E Test] Admin can create event
12. [E2E Test] Admin can edit event

**Acceptance Criteria**:
- Admin can create new event
- Admin can edit existing event
- Admin can delete event
- Form validation works
- Preview mode displays correctly
- Draft save functionality
- Tests: 90%+ coverage

---

### Phase 4: Polish & Optimization (Week 5)

#### Phase 4.1: UI/UX Polish (Week 5, Days 1-2)

**Tasks**:
1. Add loading skeletons for all pages
2. Add empty states
3. Add error boundaries
4. Improve mobile responsiveness
5. Add animations and transitions
6. Implement toast notifications
7. Add confirmation dialogs
8. Improve accessibility (ARIA labels, keyboard navigation)
9. Test with screen readers

**Acceptance Criteria**:
- All pages responsive on mobile/tablet/desktop
- Loading states consistent
- Error handling graceful
- WCAG 2.1 AA compliance
- Smooth animations

#### Phase 4.2: Performance Optimization (Week 5, Days 3-4)

**Tasks**:
1. Analyze bundle size with `@next/bundle-analyzer`
2. Implement code splitting for large components
3. Add prefetching for event details
4. Optimize images (next/image)
5. Implement infinite scroll for events (optional)
6. Add service worker for offline support (optional)
7. Run Lighthouse audit
8. Fix performance issues

**Acceptance Criteria**:
- Lighthouse score: 90+ Performance
- Bundle size < 500KB (initial load)
- TTI (Time to Interactive) < 3s
- FCP (First Contentful Paint) < 1.5s

#### Phase 4.3: Documentation & Deployment (Week 5, Day 5)

**Tasks**:
1. Update README with setup instructions
2. Document component API (Storybook optional)
3. Create user guide
4. Deploy to Azure Static Web Apps (staging)
5. Configure production environment
6. Set up monitoring (Application Insights)
7. Deploy to production
8. Update PROGRESS_TRACKER and documentation

**Deliverables**:
- Deployed staging environment
- Deployed production environment
- Complete documentation
- Monitoring configured

---

## 11. Risk Assessment & Mitigation

### Risk 1: Microsoft Entra Integration Complexity
**Impact**: High
**Likelihood**: Medium
**Mitigation**:
- Use well-tested next-auth library
- Test OAuth flow early (Week 1)
- Document redirect URLs and configuration
- Have fallback to email/password only

### Risk 2: API Response Times
**Impact**: Medium
**Likelihood**: Medium
**Mitigation**:
- Implement optimistic updates
- Use React Query caching aggressively
- Add loading skeletons
- Monitor API performance with Application Insights

### Risk 3: Mobile Performance
**Impact**: Medium
**Likelihood**: Medium
**Mitigation**:
- Test on real devices early
- Use Chrome DevTools device emulation
- Optimize bundle size
- Implement lazy loading

### Risk 4: Test Coverage Not Meeting 90%
**Impact**: Medium
**Likelihood**: Low
**Mitigation**:
- Write tests first (TDD)
- Monitor coverage after each PR
- Enforce coverage threshold in CI/CD
- Review uncovered code weekly

### Risk 5: Scope Creep
**Impact**: High
**Likelihood**: Medium
**Mitigation**:
- Stick to defined Epic 1 & 2 features only
- Defer "nice-to-have" features to Phase 2
- Regular check-ins with product owner
- Use feature flags for experimental features

---

## 12. Success Metrics

### Technical Metrics
- **Test Coverage**: 90%+ (unit + integration)
- **Build Time**: < 3 minutes
- **Bundle Size**: < 500KB (initial load)
- **Lighthouse Score**: 90+ (Performance, Accessibility, Best Practices, SEO)
- **Zero Tolerance**: 0 TypeScript errors, 0 ESLint errors

### User Experience Metrics
- **Page Load Time**: < 3 seconds (TTI)
- **Mobile Responsive**: 100% (all pages work on mobile)
- **Accessibility**: WCAG 2.1 AA compliant
- **Browser Support**: Chrome, Firefox, Safari, Edge (latest 2 versions)

### Feature Completion
- **Epic 1**: 100% (all 10 authentication features)
- **Epic 2**: 100% (events list, search, details, registration)
- **Documentation**: 100% (README, architecture docs, user guide)
- **Deployment**: 100% (staging and production live)

---

## 13. Next Steps After Phase Completion

### Phase 2 Features (Future)
1. **Epic 3**: Forums & Community
2. **Epic 4**: Business Directory
3. **Epic 5**: Education Platform
4. **Notifications System** (Email + Push)
5. **Real-time Chat** (WebSockets)
6. **Mobile Apps** (React Native)

### Enhancements (Future)
1. Internationalization (Sinhala, Tamil translations)
2. Dark mode support
3. PWA (Progressive Web App) features
4. Advanced analytics dashboard
5. Payment integration (Stripe)
6. Social feed (like Twitter)
7. Video streaming for events

---

## Appendix A: Dependencies

### Core Dependencies
```json
{
  "dependencies": {
    "next": "^14.2.0",
    "react": "^18.3.0",
    "react-dom": "^18.3.0",
    "typescript": "^5.4.0",
    "@tanstack/react-query": "^5.51.0",
    "zustand": "^4.5.0",
    "axios": "^1.7.0",
    "zod": "^3.23.0",
    "react-hook-form": "^7.52.0",
    "@hookform/resolvers": "^3.9.0",
    "next-auth": "^4.24.0",
    "date-fns": "^3.6.0",
    "react-hot-toast": "^2.4.0",
    "lucide-react": "^0.395.0",
    "clsx": "^2.1.0",
    "tailwind-merge": "^2.3.0",
    "class-variance-authority": "^0.7.0"
  },
  "devDependencies": {
    "@types/node": "^20.14.0",
    "@types/react": "^18.3.0",
    "@types/react-dom": "^18.3.0",
    "vitest": "^1.6.0",
    "@vitejs/plugin-react": "^4.3.0",
    "@testing-library/react": "^16.0.0",
    "@testing-library/user-event": "^14.5.0",
    "@testing-library/jest-dom": "^6.4.0",
    "@playwright/test": "^1.45.0",
    "msw": "^2.3.0",
    "eslint": "^8.57.0",
    "eslint-config-next": "^14.2.0",
    "prettier": "^3.3.0",
    "tailwindcss": "^3.4.0",
    "autoprefixer": "^10.4.0",
    "postcss": "^8.4.0"
  }
}
```

---

## Appendix B: File Templates

### Component Template
```typescript
// components/auth/login-form.tsx
'use client';

import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { loginSchema, type LoginInput } from '@/lib/validations/auth';
import { useLogin } from '@/lib/hooks/use-auth';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { FormError } from '@/components/forms/form-error';

export function LoginForm() {
  const { mutate: login, isPending, error } = useLogin();
  const { register, handleSubmit, formState: { errors } } = useForm<LoginInput>({
    resolver: zodResolver(loginSchema),
  });

  const onSubmit = (data: LoginInput) => {
    login(data);
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div>
        <label htmlFor="email">Email</label>
        <Input id="email" type="email" {...register('email')} />
        {errors.email && <FormError>{errors.email.message}</FormError>}
      </div>

      <div>
        <label htmlFor="password">Password</label>
        <Input id="password" type="password" {...register('password')} />
        {errors.password && <FormError>{errors.password.message}</FormError>}
      </div>

      {error && <FormError>{error.message}</FormError>}

      <Button type="submit" disabled={isPending}>
        {isPending ? 'Logging in...' : 'Login'}
      </Button>
    </form>
  );
}
```

### Test Template
```typescript
// tests/unit/components/auth/login-form.test.tsx
import { render, screen, waitFor } from '@/tests/utils';
import userEvent from '@testing-library/user-event';
import { LoginForm } from '@/components/auth/login-form';

describe('LoginForm', () => {
  it('should render all form fields', () => {
    render(<LoginForm />);

    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /login/i })).toBeInTheDocument();
  });

  it('should validate email format', async () => {
    const user = userEvent.setup();
    render(<LoginForm />);

    const emailInput = screen.getByLabelText(/email/i);
    await user.type(emailInput, 'invalid-email');
    await user.tab();

    expect(await screen.findByText(/invalid email/i)).toBeInTheDocument();
  });

  it('should submit form with valid data', async () => {
    const user = userEvent.setup();
    render(<LoginForm />);

    await user.type(screen.getByLabelText(/email/i), 'test@example.com');
    await user.type(screen.getByLabelText(/password/i), 'Password123!');
    await user.click(screen.getByRole('button', { name: /login/i }));

    await waitFor(() => {
      expect(mockLogin).toHaveBeenCalledWith({
        email: 'test@example.com',
        password: 'Password123!',
      });
    });
  });
});
```

---

## Conclusion

This architecture document provides a comprehensive blueprint for implementing the frontend for Epic 1 (Authentication) and Epic 2 (Events Management) of the LankaConnect platform. By following the TDD approach, Clean Architecture principles, and the incremental phase-by-phase plan, we can deliver a high-quality, well-tested, performant, and secure frontend application within the 4-5 week timeline.

**Key Success Factors**:
1. **TDD First**: Write tests before implementation
2. **Zero Tolerance**: No TypeScript/ESLint errors
3. **90% Coverage**: Maintain high test coverage
4. **Incremental Delivery**: Complete one phase before moving to next
5. **Regular Reviews**: Daily check-ins and weekly reviews

**Ready to Start**: Phase 1 (Project Setup) can begin immediately.

---

**Document Status**: Ready for Implementation
**Next Action**: Initialize Next.js project and begin Phase 1
