# LankaConnect Layout Redesign - Complete Implementation Plan

**Created:** 2025-11-16
**Status:** Planning Phase
**Based on:** Figma Design Screenshots

---

## Table of Contents
1. [Design Overview](#design-overview)
2. [Component Library Updates](#component-library-updates)
3. [Page-by-Page Implementation](#page-by-page-implementation)
4. [Implementation Phases](#implementation-phases)
5. [Technical Specifications](#technical-specifications)
6. [Testing Strategy](#testing-strategy)

---

## Design Overview

### Key Design Changes from Figma

#### 1. **Header/Navigation**
- Logo + tagline: "LankaConnect - Sri Lankan Community Hub"
- Horizontal nav: Events, Marketplace, Forums, Culture, Directory
- Centered search bar
- Notification bell with count badge
- User profile with avatar + name dropdown

#### 2. **Hero Section** (Landing Page)
- Full-width gradient: Orange → Red → Maroon → Brown → Green
- Left side: Badge, large heading, subheading, dual CTAs, stats bar
- Right side: 4 feature cards (rounded, shadowed)
- Stats: "25K+ Members", "1.2K+ Events", "500+ Businesses"

#### 3. **Quick Action Bar** (NEW)
- 6 icon buttons with labels
- Actions: Create Event, List Business, Start Discussion, Share News, Find Members, Local Services
- Color-coded circular icons with pastel backgrounds

#### 4. **Content Layout**
- Two-column: Main content (60-70%) + Sidebar (30-40%)
- Sections: Upcoming Events, Forum Highlights, Marketplace, News & Updates, Cultural Spotlight
- Full-width sections with gradients

#### 5. **Component Styles**
- **Cards:** White background, rounded-xl, subtle shadows
- **Badges:** Rounded-full, colored backgrounds (Cultural, Arts, Food & Culture, etc.)
- **Buttons:** Rounded, vibrant colors, icon support
- **Icons:** Circular backgrounds with icons
- **Product Cards:** Image, badges, rating stars, price, location
- **Event Cards:** Icon, details, category badge, register button

#### 6. **Footer**
- Vibrant gradient background (Orange → Red → Pink → Green)
- 4 columns: Community, Marketplace, Resources, About
- Newsletter section: "Stay Connected" centered
- Social media icons

---

## Component Library Updates

### New Components to Create

#### 1. **Badge Component** (`web/src/presentation/components/ui/Badge.tsx`)
```typescript
interface BadgeProps {
  variant: 'cultural' | 'arts' | 'food' | 'business' | 'community' | 'featured' | 'new' | 'hot' | 'default'
  children: React.ReactNode
  className?: string
}
```
- Rounded-full design
- Color variants matching Figma
- Small size option

#### 2. **Enhanced Card Component** (`web/src/presentation/components/ui/EnhancedCard.tsx`)
```typescript
interface EnhancedCardProps {
  children: React.ReactNode
  hover?: boolean
  shadow?: 'sm' | 'md' | 'lg'
  className?: string
}
```
- Elevated shadow on hover
- Smooth transitions
- Rounded-xl corners

#### 3. **IconButton Component** (`web/src/presentation/components/ui/IconButton.tsx`)
```typescript
interface IconButtonProps {
  icon: React.ReactNode
  label: string
  onClick: () => void
  variant: 'calendar' | 'business' | 'chat' | 'news' | 'members' | 'location'
  className?: string
}
```
- Circular background
- Pastel color variants
- Icon + label layout

#### 4. **StatCard Component** (`web/src/presentation/components/ui/StatCard.tsx`)
```typescript
interface StatCardProps {
  value: string
  label: string
  className?: string
}
```
- Large bold number
- Small label text
- White text on gradient

#### 5. **FeatureCard Component** (`web/src/presentation/components/features/landing/FeatureCard.tsx`)
```typescript
interface FeatureCardProps {
  icon: React.ReactNode
  title: string
  subtitle: string
  iconColor: string
}
```
- White rounded card
- Colored icon circle
- Title + subtitle layout

#### 6. **EventCard Component** (`web/src/presentation/components/features/events/EventCard.tsx`)
```typescript
interface EventCardProps {
  event: {
    icon: string
    title: string
    date: string
    time: string
    location: string
    attendees: number
    category: string
  }
  onRegister: () => void
}
```
- Left icon with background
- Event details
- Category badge (top-right)
- Register button (bottom-right)
- Attendee count

#### 7. **ForumPostCard Component** (`web/src/presentation/components/features/forums/ForumPostCard.tsx`)
```typescript
interface ForumPostCardProps {
  post: {
    avatar: string
    title: string
    author: string
    timeAgo: string
    category: string
    comments: number
    likes: number
    isHot?: boolean
  }
}
```
- User avatar (gradient background with initials)
- Title with "Hot" badge
- Category tag
- Engagement metrics (comments, likes)

#### 8. **ProductCard Component** (`web/src/presentation/components/features/marketplace/ProductCard.tsx`)
```typescript
interface ProductCardProps {
  product: {
    image: string
    badge?: 'Featured' | 'New' | 'Hot Deal'
    category: string
    title: string
    rating: number
    reviews: number
    location: string
    price: string
  }
  onView: () => void
}
```
- Product image with gradient overlay
- Badge overlay (top-right)
- Category, title, rating
- Location icon + text
- Price + View button

#### 9. **NewsItem Component** (`web/src/presentation/components/features/news/NewsItem.tsx`)
```typescript
interface NewsItemProps {
  news: {
    category: 'Business' | 'Community' | 'Culture'
    title: string
    description: string
    timeAgo: string
  }
}
```
- Category badge with color
- Title (bold)
- Description (truncated)
- Time ago

#### 10. **CulturalCard Component** (`web/src/presentation/components/features/culture/CulturalCard.tsx`)
```typescript
interface CulturalCardProps {
  item: {
    icon: React.ReactNode
    iconColor: string
    title: string
    description: string
  }
}
```
- White card on gradient background
- Circular icon with color
- Title + description

---

## Page-by-Page Implementation

### **PAGE 1: Landing Page** (`web/src/app/page.tsx`)

**Priority:** 🔴 **HIGHEST** (Most visible, public-facing)

#### Current State
- Simple hero with gradient
- 4 stat cards (horizontal)
- 3-column widgets (Calendar, Businesses, Stats)
- Activity feed with metro filtering
- Basic header/footer

#### New Design Requirements

##### Section 1: Header (Enhanced)
- Add search bar (centered between nav and user menu)
- Update navigation items: Events, Marketplace, Forums, Culture, Directory
- Enhance notification bell with animated badge
- Improve user dropdown design

##### Section 2: Hero Section (Complete Redesign)
- **Layout:** Two-column (60/40 split)
- **Left Column:**
  - Badge: "🌐 Connecting Sri Lankans Worldwide" (white text on semi-transparent bg)
  - H1: "Your Community, Your Heritage" (large, white, bold)
  - Subtitle: "Join the largest Sri Lankan diaspora platform. Discover events, connect with businesses, engage in discussions, and celebrate our rich culture together."
  - CTA Buttons:
    - Primary: "Get Started" (white bg, maroon text, arrow icon)
    - Secondary: Placeholder button (outline)
  - Stats Bar (bottom):
    - "25K+ Members" | "1.2K+ Events" | "500+ Businesses"
- **Right Column:**
  - 4 Feature Cards (2x2 grid):
    1. Avurudu Festival (orange icon) - "Tomorrow at 6:00 PM"
    2. Community Chat (pink icon) - "234 active discussions"
    3. Fresh Groceries (green icon) - "50+ Sri Lankan products"
    4. Cultural Events (yellow icon) - "Dance & Music classes"
- **Background:** Full-width gradient (Orange → Red → Maroon → Brown → Green)

##### Section 3: Quick Action Bar (NEW)
- 6 Icon Buttons (horizontal scroll on mobile):
  1. 📅 Create Event (orange bg)
  2. 🏪 List Business (teal bg)
  3. 💬 Start Discussion (pink bg)
  4. 📰 Share News (yellow bg)
  5. 👥 Find Members (blue bg)
  6. 📍 Local Services (purple bg)
- **Functionality:** Open modals or navigate to pages
- **Layout:** Flex wrap, center aligned

##### Section 4: Main Content (Two-Column Layout)
**Left Column (Main Content):**

**A. Upcoming Events Section**
- Title: "📅 Upcoming Events" with "View All →" link
- Event Cards (vertical stack):
  - Sinhala & Tamil New Year Celebration
    - Date: April 14, 2024 | Time: 6:00 PM - 11:00 PM
    - Location: Sri Lankan Community Center, Toronto
    - Attendees: 234 attending
    - Category Badge: "Cultural" (orange)
    - Register Button
  - Traditional Cooking Workshop
    - Date: April 20, 2024 | Time: 2:00 PM - 5:00 PM
    - Location: Lanka Kitchen, Vancouver
    - Attendees: 45 attending
    - Category Badge: "Food & Culture" (yellow-orange)
    - Register Button
  - Kandyan Dance Performance
    - Date: April 27, 2024 | Time: 7:30 PM - 9:30 PM
    - Location: Royal Theatre, Melbourne
    - Attendees: 189 attending
    - Category Badge: "Arts" (pink-red)
    - Register Button

**B. Marketplace Section (Full-Width)**
- Title: "🏪 Marketplace" with "Browse All →" link
- Product Cards (3-column grid):
  1. Ceylon Cinnamon Sticks - Premium Quality
     - Badge: "Featured" (green)
     - Category: Groceries
     - Rating: 4.9 ⭐ (127 reviews)
     - Location: Toronto, ON
     - Price: $24.99
     - View Button (green)
  2. Traditional Sri Lankan Batik Saree
     - Badge: "New" (green)
     - Category: Fashion
     - Rating: 4.8 ⭐ (89 reviews)
     - Location: Vancouver, BC
     - Price: $89.99
     - View Button (teal)
  3. Authentic Sri Lankan Curry Powder Set
     - Badge: "Hot Deal" (green)
     - Category: Groceries
     - Rating: 5 ⭐ (203 reviews)
     - Location: Melbourne, VIC
     - Price: $19.99
     - View Button (teal)

**Right Column (Sidebar):**

**C. Forum Highlights Section**
- Title: "💬 Forum Highlights" with arrow →
- Forum Post Cards:
  1. "Best places to buy Sri Lankan groceries?" 🔥 Hot
     - Author: Saman P. | 2h ago
     - Category: Food & Lifestyle
     - Engagement: 💬 24 comments | 👍 67 likes
  2. "Teaching Sinhala to kids abroad"
     - Author: Nisha R. | 5h ago
     - Category: Parenting
     - Engagement: 💬 18 comments | 👍 45 likes
  3. "Planning a trip to Sri Lanka - tips?" 🔥 Hot
     - Author: Kasun M. | 1d ago
     - Category: Travel
     - Engagement: 💬 56 comments | 👍 123 likes
  4. "Sri Lankan business networking group"
     - Author: Amara S. | 2d ago
     - Category: Business
     - Engagement: 💬 31 comments | 👍 89 likes

**D. News & Updates Section**
- Title: "📰 News & Updates" with arrow →
- News Items:
  1. Business: "New Sri Lankan restaurant opens in downtown"
     - Description: "Authentic cuisine from Colombo arrives..."
     - Time: 3h ago
  2. Community: "Community raises $50K for Sri Lankan schools"
     - Description: "Successful fundraiser helps education..."
     - Time: 1d ago
  3. Culture: "Cultural dance competition winners announced"
     - Description: "Young performers showcase talent..."
     - Time: 2d ago

##### Section 5: Cultural Spotlight (Full-Width with Gradient)
- **Background:** Gradient (Orange → Red → Pink → Magenta)
- **Content:**
  - Icon: 🎭
  - Title: "Cultural Spotlight" (white, large)
  - Subtitle: "Discover and preserve our rich heritage through interactive content and community contributions"
  - 4 Cards (horizontal grid):
    1. Traditional Arts (orange icon)
       - "Explore Kandyan dance, drumming, and ancient crafts"
    2. Sri Lankan Cuisine (pink icon)
       - "Authentic recipes and cooking techniques"
    3. Language Lessons (teal icon)
       - "Learn Sinhala and Tamil with native speakers"
    4. Heritage Sites (yellow icon)
       - "Virtual tours of historical landmarks"
  - CTA: "Explore Culture Hub →" button

##### Section 6: Newsletter Section (Full-Width)
- **Background:** Gradient continuation
- **Title:** "Stay Connected" (white, centered)
- **Subtitle:** "Get weekly updates about events, news, and community highlights"
- **Form:** Email input + Subscribe button (centered, white input, dark button)

##### Section 7: Footer (Enhanced with Gradient)
- **Background:** Vibrant gradient (Orange → Red → Pink → Green)
- **Layout:** 4 columns
  - Community: Events, Forums, Directory, Cultural Hub
  - Marketplace: Browse Listings, Sell Items, Businesses, Services
  - Resources: Help Center, Guidelines, Safety, Blog
  - About: Our Story, Contact Us, Careers, Press
- **Bottom Section:**
  - Logo + © 2024
  - Social icons (Facebook, Twitter, Instagram, YouTube, Email)

---

### **PAGE 2: Dashboard** (`web/src/app/(dashboard)/dashboard/page.tsx`)

**Priority:** 🟠 **HIGH** (Protected, frequently used)

#### Current State
- Custom header (not using reusable Header component)
- Quick actions section
- Tabbed interface (role-based tabs)
- Event lists
- Admin approvals table

#### New Design Requirements

##### Section 1: Header (Use Reusable Component)
- Replace custom header with reusable `Header` component
- Same design as landing page

##### Section 2: Welcome Banner (NEW)
- **Background:** Subtle gradient or pattern
- **Content:**
  - Large heading: "Welcome back, [User Name]!"
  - Subheading: "Here's what's happening in your community"
  - User role badge: "Event Organizer" or "Admin" (if applicable)

##### Section 3: Quick Actions (Enhanced)
- **Layout:** Card-based instead of just buttons
- **Cards:**
  - Create Event: Icon, title, description, button
  - Manage Profile: Icon, title, description, button
  - Browse Events: Icon, title, description, button
  - Community Feed: Icon, title, description, button
  - (Conditional) Upgrade to Event Organizer: Icon, title, description, button

##### Section 4: Dashboard Tabs (Enhanced Design)
- **Tab Design:** Rounded top corners, active tab has bottom border
- **Tab Panels:**
  - My Registered Events: Grid layout of enhanced event cards
  - My Created Events: Grid with edit/delete actions
  - Admin Tasks: Enhanced approvals table with filters
  - Notifications: List with category icons

##### Section 5: Stats Cards (NEW)
- **Layout:** 4-card grid (below welcome banner)
- **Cards:**
  - Registered Events Count
  - Upcoming Events Count
  - Community Connections Count
  - Profile Completion % (with progress bar)

##### Section 6: Footer
- Keep current footer design (to be enhanced later)

---

### **PAGE 3: Profile** (`web/src/app/(dashboard)/profile/page.tsx`)

**Priority:** 🟠 **HIGH** (User management)

#### Current State
- Sri Lankan flag stripe header
- Welcome section with avatar
- Profile sections (vertical stack)
- Basic form layouts

#### New Design Requirements

##### Section 1: Header
- Use reusable `Header` component

##### Section 2: Profile Header (Enhanced)
- **Background:** Full-width gradient (subtle, not as vibrant as hero)
- **Layout:** Centered
- **Content:**
  - Large user avatar with upload overlay on hover
  - User name (editable inline)
  - User role badge
  - Email (non-editable, gray)
  - Member since date
  - Profile completion progress bar

##### Section 3: Profile Sections (Card-Based Layout)
- **Layout:** Two-column grid (main content + sidebar)

**Left Column (Main Content):**
1. **Personal Information Card**
   - Name, email, phone
   - Editable fields
   - Save button

2. **Location Information Card**
   - City, state, zipcode
   - Map preview (placeholder)
   - Save button

3. **Cultural Interests Card**
   - Interest tags (selectable)
   - Visual tag layout
   - Save button

4. **Preferred Metro Areas Card**
   - Metro area selection with checkboxes
   - "Receive all locations" toggle
   - Save button

**Right Column (Sidebar):**
1. **Profile Strength Card**
   - Progress circle
   - Completion percentage
   - Suggestions for improvement

2. **Quick Links Card**
   - Dashboard
   - Notifications
   - Settings
   - Help Center

3. **Account Settings Card**
   - Change password
   - Email preferences
   - Privacy settings
   - Delete account (danger zone)

##### Section 4: Footer
- Keep current footer

---

### **PAGE 4: Notifications** (`web/src/app/(dashboard)/notifications/page.tsx`)

**Priority:** 🟡 **MEDIUM** (Important but less frequently used)

#### Current State
- Sidebar with category filters
- Notification list with type icons
- Mark all as read button

#### New Design Requirements

##### Section 1: Header
- Use reusable `Header` component

##### Section 2: Page Header (Enhanced)
- **Background:** Subtle gradient or pattern
- **Layout:** Center aligned
- **Content:**
  - 🔔 Bell icon (large)
  - Title: "Notifications"
  - Subtitle: "Stay updated with your community activities"
  - "Mark all as read" button (top-right)

##### Section 3: Notification Filters (Enhanced Sidebar)
- **Design:** Card-based filters
- **Layout:** Vertical pills with counts
- **Filters:**
  - All Notifications (12)
  - Role Upgrades (2)
  - Trial & Subscription (1)
  - System (3)
  - Events (6)

##### Section 4: Notification List (Enhanced Cards)
- **Card Design:**
  - Left icon (colored circle)
  - Title (bold)
  - Description
  - Time ago (gray, small)
  - Unread indicator (blue dot or background)
  - Hover effect (shadow increase)

##### Section 5: Empty State
- Illustration or icon
- "You're all caught up!" message
- Helpful links

##### Section 6: Footer
- Keep current footer

---

### **PAGE 5: Login** (`web/src/app/(auth)/login/page.tsx`)

**Priority:** 🟡 **MEDIUM** (Gateway page, important for first impression)

#### Current State
- Split panel layout
- Left: Branding with features
- Right: Login form
- Background image

#### New Design Requirements

##### Keep Split Panel Layout (Minor Enhancements)

**Left Panel:**
- **Background:** Keep gradient but match new brand vibrancy
- **Logo:** Larger logo at top
- **Heading:** "Welcome Back to LankaConnect"
- **Features (Enhanced Icons):**
  - 🎭 Cultural Events: "Join celebrations of our heritage"
  - 🤝 Community Network: "Connect with Sri Lankans worldwide"
  - 🏪 Local Businesses: "Support our community enterprises"

**Right Panel:**
- **Form Design:**
  - Enhanced input fields (larger, better focus states)
  - "Remember me" checkbox (better styling)
  - Social login buttons (if applicable): Google, Facebook
  - "Forgot password?" link (more prominent)
  - Sign up link: "Don't have an account? Sign up"

**Enhancements:**
- Better mobile responsiveness
- Loading states with spinner
- Error message styling (red, icon)
- Success states

---

### **PAGE 6: Register** (`web/src/app/(auth)/register/page.tsx`)

**Priority:** 🟡 **MEDIUM** (User acquisition)

#### Current State
- Split panel with animated gradient
- Left: Branding
- Right: Registration form

#### New Design Requirements

**Similar to Login but with:**
- **Heading:** "Join Our Community"
- **Features:**
  - ✨ Free to Join: "No cost to become a member"
  - 🎯 Personalized Experience: "Customize your interests"
  - 🔒 Secure & Private: "Your data is protected"

**Form Enhancements:**
- Step indicator (if multi-step)
- Better field validation (inline)
- Password strength indicator
- Terms & conditions checkbox (better styling)
- "Already have an account? Login" link

---

### **PAGE 7: Forgot Password** (`web/src/app/(auth)/forgot-password/page.tsx`)

**Priority:** 🟢 **LOW** (Utility page)

#### New Design Requirements
- Keep split panel layout
- Enhanced form design
- Better success state (check icon, message)
- Email sent illustration

---

### **PAGE 8: Reset Password** (`web/src/app/(auth)/reset-password/page.tsx`)

**Priority:** 🟢 **LOW** (Utility page)

#### New Design Requirements
- Keep split panel layout
- Enhanced form with password strength indicator
- Success state with "Go to Login" button

---

### **PAGE 9: Verify Email** (`web/src/app/(auth)/verify-email/page.tsx`)

**Priority:** 🟢 **LOW** (Utility page)

#### New Design Requirements
- Keep split panel layout
- Animated verification process (spinner/check)
- Success state illustration
- Error state with retry button

---

### **PAGE 10: Templates Gallery** (`web/src/app/templates/page.tsx`)

**Priority:** 🟡 **MEDIUM** (Feature page)

#### Current State
- Template cards in grid
- Category filters (tabs)
- Basic card design

#### New Design Requirements

##### Section 1: Header
- Use reusable `Header` component

##### Section 2: Page Header (Enhanced)
- **Background:** Gradient accent
- **Content:**
  - ✨ Sparkles icon (large)
  - Title: "Event Templates"
  - Subtitle: "Create stunning events in minutes with our ready-made templates"
  - "Create from Scratch" button (secondary)

##### Section 3: Category Filters (Enhanced)
- **Design:** Pill-shaped buttons with counts
- **Layout:** Horizontal scroll on mobile
- **Active State:** Gradient background

##### Section 4: Template Cards (Enhanced)
- **Card Design:**
  - Preview image/illustration
  - Category badge
  - Template title
  - Template description
  - "Use Template" button
  - Hover effect (lift + shadow)

##### Section 5: Help Section
- **Background:** Light background
- **Content:**
  - "Need help?" heading
  - "Contact Support" button
  - "Browse Tutorials" link

##### Section 6: Footer
- Keep current footer

---

### **PAGE 11: 404 Not Found** (`web/src/app/not-found.tsx`)

**Priority:** 🟢 **LOW** (Error page)

#### New Design Requirements
- Keep Sri Lankan flag stripe
- Enhanced 404 illustration
- Centered content
- Helpful links in card format
- Better button styling

---

### **PAGE 12: Error Boundary** (`web/src/app/error.tsx`)

**Priority:** 🟢 **LOW** (Error page)

#### New Design Requirements
- Centered error illustration
- Friendly error message
- "Try again" button (prominent)
- "Go home" button (secondary)
- Optional "Report error" button

---

## Implementation Phases

### **Phase 1: Foundation & Component Library** (Days 1-3)

**Goal:** Build reusable components and update design system

**Tasks:**
1. Update Tailwind config with new gradients and colors
2. Create Badge component with variants
3. Create EnhancedCard component
4. Create IconButton component
5. Create StatCard component
6. Create FeatureCard component
7. Update Button component with new variants
8. Add new icon assets

**Deliverables:**
- Updated Tailwind config
- 7 new/updated components
- Component documentation
- Storybook stories (if applicable)

---

### **Phase 2: Header & Footer Enhancements** (Days 4-5)

**Goal:** Update global navigation and footer

**Tasks:**
1. Enhance Header component:
   - Add search bar (centered)
   - Update navigation items
   - Improve notification bell
   - Enhance user dropdown
2. Enhance Footer component:
   - Add vibrant gradient background
   - Update link columns
   - Improve newsletter section
   - Enhance social icons
3. Test responsive behavior
4. Update all pages to use enhanced Header (remove custom headers)

**Deliverables:**
- Enhanced Header component
- Enhanced Footer component
- Updated pages using new components

---

### **Phase 3: Landing Page Redesign** (Days 6-10)

**Goal:** Complete landing page redesign with all new sections

**Tasks:**
1. **Hero Section:**
   - Create hero layout component
   - Implement left column (badge, heading, CTAs, stats)
   - Implement right column (4 feature cards)
   - Add gradient background
   - Test responsiveness

2. **Quick Action Bar:**
   - Create QuickActionBar component
   - Implement 6 icon buttons with modals/navigation
   - Test mobile scroll behavior

3. **Main Content Layout:**
   - Create two-column layout wrapper
   - Implement Upcoming Events section with EventCard components
   - Implement Forum Highlights sidebar with ForumPostCard components
   - Implement Marketplace section with ProductCard components
   - Implement News & Updates sidebar with NewsItem components
   - Test responsive breakpoints

4. **Cultural Spotlight Section:**
   - Create full-width gradient section
   - Implement 4 CulturalCard components
   - Add "Explore Culture Hub" CTA
   - Test responsive grid

5. **Newsletter Section:**
   - Update "Stay Connected" section design
   - Center email form
   - Add gradient background

6. **Integration & Testing:**
   - Connect to existing APIs
   - Implement loading states
   - Implement empty states
   - Test all interactions
   - Cross-browser testing
   - Mobile testing

**Deliverables:**
- Completely redesigned landing page
- 6 new feature components
- Placeholder data for new sections
- Responsive design across all breakpoints

---

### **Phase 4: Dashboard Page Redesign** (Days 11-13)

**Goal:** Modernize dashboard with new design

**Tasks:**
1. Replace custom header with reusable Header component
2. Create welcome banner component
3. Enhance quick actions with card layout
4. Create stats cards (4-card grid)
5. Enhance tab design
6. Update event cards to use new EventCard component
7. Enhance admin approvals table
8. Test all role-based visibility

**Deliverables:**
- Redesigned dashboard page
- New stats cards component
- Enhanced quick actions
- Updated tabs design

---

### **Phase 5: Profile Page Redesign** (Days 14-16)

**Goal:** Modernize profile page with two-column layout

**Tasks:**
1. Create enhanced profile header with gradient
2. Implement two-column layout (main + sidebar)
3. Create profile strength card (sidebar)
4. Create quick links card (sidebar)
5. Create account settings card (sidebar)
6. Enhance profile sections with better card designs
7. Improve form field styling
8. Test save functionality

**Deliverables:**
- Redesigned profile page
- New sidebar components
- Enhanced form styling

---

### **Phase 6: Notifications Page Redesign** (Days 17-18)

**Goal:** Enhance notifications page with better UX

**Tasks:**
1. Create enhanced page header
2. Improve sidebar filters with card design
3. Enhance notification cards
4. Improve empty state design
5. Test mark as read functionality
6. Test filter functionality

**Deliverables:**
- Redesigned notifications page
- Enhanced notification cards
- Improved filters

---

### **Phase 7: Auth Pages Enhancement** (Days 19-21)

**Goal:** Polish all authentication pages

**Tasks:**
1. Enhance Login page:
   - Update left panel branding
   - Enhance form design
   - Add social login buttons (if applicable)
   - Improve error/success states

2. Enhance Register page:
   - Update features section
   - Enhance form with better validation
   - Add password strength indicator
   - Add step indicator (if multi-step)

3. Enhance Forgot Password page:
   - Improve form design
   - Add success state illustration

4. Enhance Reset Password page:
   - Add password strength indicator
   - Improve success state

5. Enhance Verify Email page:
   - Add verification animation
   - Improve success/error states

6. Test complete auth flow

**Deliverables:**
- Enhanced all 5 auth pages
- Improved form components
- Better state handling

---

### **Phase 8: Templates Gallery Enhancement** (Days 22-23)

**Goal:** Modernize templates gallery

**Tasks:**
1. Create enhanced page header
2. Enhance category filters
3. Redesign template cards
4. Add help section
5. Test template selection flow

**Deliverables:**
- Redesigned templates page
- Enhanced template cards

---

### **Phase 9: Error Pages Enhancement** (Days 24)

**Goal:** Polish error pages

**Tasks:**
1. Enhance 404 page
2. Enhance error boundary
3. Add illustrations
4. Test error scenarios

**Deliverables:**
- Enhanced error pages

---

### **Phase 10: Testing, Optimization & Documentation** (Days 25-28)

**Goal:** Ensure quality and document everything

**Tasks:**
1. **Cross-Browser Testing:**
   - Chrome, Firefox, Safari, Edge
   - Test all pages
   - Fix browser-specific issues

2. **Responsive Testing:**
   - Mobile (iOS, Android)
   - Tablet (iPad, Android tablets)
   - Desktop (various screen sizes)
   - Fix responsive issues

3. **Accessibility Audit:**
   - WCAG 2.1 AA compliance
   - Keyboard navigation
   - Screen reader testing
   - Color contrast checks
   - ARIA labels

4. **Performance Optimization:**
   - Lazy load images
   - Code splitting
   - Optimize bundle size
   - Lighthouse audit
   - Fix performance issues

5. **Documentation:**
   - Update PROGRESS_TRACKER.md
   - Update STREAMLINED_ACTION_PLAN.md
   - Create design system documentation
   - Update component documentation
   - Create deployment guide

6. **Deployment:**
   - Deploy to staging
   - QA review
   - Fix issues
   - Deploy to production

**Deliverables:**
- Fully tested application
- Performance optimizations
- Complete documentation
- Production deployment

---

## Technical Specifications

### Color Palette

#### Primary Colors (Sri Lankan Flag)
```css
Saffron: #FF7900 (Orange)
Maroon: #8B1538 (Red/Maroon)
Lanka Green: #006400 (Green)
Gold: #FFD700 (Accent)
Cream: #FFF8DC (Backgrounds)
```

#### Extended Palette (from Figma)
```css
/* Gradients */
--gradient-hero: linear-gradient(135deg, #FF7900 0%, #8B1538 35%, #7C0A02 50%, #4A0404 65%, #006400 100%)
--gradient-footer: linear-gradient(90deg, #FF5722 0%, #E91E63 33%, #FF1744 66%, #00BCD4 100%)
--gradient-cultural: linear-gradient(90deg, #FF7900 0%, #E91E63 50%, #C2185B 100%)

/* Badge Colors */
--badge-cultural: #FF7900
--badge-arts: #E91E63
--badge-food: #FF9800
--badge-business: #00BCD4
--badge-community: #9C27B0
--badge-featured: #4CAF50
--badge-new: #00E676
--badge-hot: #FF1744

/* Background Colors */
--bg-orange-light: #FFF3E0
--bg-teal-light: #E0F7FA
--bg-pink-light: #FCE4EC
--bg-yellow-light: #FFF9C4
--bg-blue-light: #E3F2FD
--bg-purple-light: #F3E5F5
```

#### Typography
```css
/* Font Family */
font-family: 'Inter', sans-serif

/* Font Sizes */
--text-xs: 0.75rem (12px)
--text-sm: 0.875rem (14px)
--text-base: 1rem (16px)
--text-lg: 1.125rem (18px)
--text-xl: 1.25rem (20px)
--text-2xl: 1.5rem (24px)
--text-3xl: 1.875rem (30px)
--text-4xl: 2.25rem (36px)
--text-5xl: 3rem (48px)

/* Font Weights */
--font-normal: 400
--font-medium: 500
--font-semibold: 600
--font-bold: 700
```

#### Spacing
```css
/* Based on 8px grid */
--spacing-1: 0.25rem (4px)
--spacing-2: 0.5rem (8px)
--spacing-3: 0.75rem (12px)
--spacing-4: 1rem (16px)
--spacing-5: 1.25rem (20px)
--spacing-6: 1.5rem (24px)
--spacing-8: 2rem (32px)
--spacing-10: 2.5rem (40px)
--spacing-12: 3rem (48px)
--spacing-16: 4rem (64px)
```

#### Shadows
```css
--shadow-sm: 0 1px 2px 0 rgba(0, 0, 0, 0.05)
--shadow-md: 0 4px 6px -1px rgba(0, 0, 0, 0.1)
--shadow-lg: 0 10px 15px -3px rgba(0, 0, 0, 0.1)
--shadow-xl: 0 20px 25px -5px rgba(0, 0, 0, 0.1)
--shadow-2xl: 0 25px 50px -12px rgba(0, 0, 0, 0.25)

/* Card Shadow (from Figma) */
--shadow-card: 0 2px 8px rgba(0, 0, 0, 0.08)
--shadow-card-hover: 0 4px 16px rgba(0, 0, 0, 0.12)
```

#### Border Radius
```css
--radius-sm: 0.25rem (4px)
--radius-md: 0.5rem (8px)
--radius-lg: 0.75rem (12px)
--radius-xl: 1rem (16px)
--radius-2xl: 1.5rem (24px)
--radius-full: 9999px (circle)

/* Common Uses */
Cards: rounded-xl (16px)
Buttons: rounded-lg (12px)
Badges: rounded-full (circle)
Inputs: rounded-md (8px)
Icons: rounded-full (circle)
```

### Responsive Breakpoints

```css
/* Tailwind Defaults */
sm: 640px   /* Mobile landscape, small tablets */
md: 768px   /* Tablets */
lg: 1024px  /* Desktops */
xl: 1280px  /* Large desktops */
2xl: 1536px /* Extra large desktops */
```

#### Layout Breakpoints
```
Mobile: < 640px (1 column)
Tablet: 640px - 1024px (2 columns)
Desktop: > 1024px (3-4 columns)
```

### Component Specifications

#### Hero Section
```
Desktop:
- Height: 600px minimum
- Left column: 60% width
- Right column: 40% width
- Padding: 80px 120px

Tablet:
- Height: auto
- Left column: 50% width
- Right column: 50% width
- Padding: 60px 40px

Mobile:
- Height: auto
- Single column (stacked)
- Padding: 40px 20px
```

#### Quick Action Bar
```
Desktop:
- 6 buttons in row
- Icon size: 24px
- Button padding: 16px 24px
- Gap: 16px

Tablet:
- 3 buttons per row (2 rows)
- Same sizing

Mobile:
- 2 buttons per row (3 rows)
- Smaller padding: 12px 16px
```

#### Event Card
```
Width: 100% of container
Height: auto
Padding: 20px
Border radius: 16px
Shadow: 0 2px 8px rgba(0, 0, 0, 0.08)
Hover shadow: 0 4px 16px rgba(0, 0, 0, 0.12)

Icon circle:
- Size: 48px
- Border radius: 50%
- Padding: 12px

Badge:
- Position: absolute top-right
- Padding: 4px 12px
- Border radius: full
- Font size: 12px

Register button:
- Position: absolute bottom-right
- Padding: 8px 16px
- Border radius: 8px
```

#### Product Card
```
Width: 100% of container
Aspect ratio: 4:5 (image)
Border radius: 16px
Shadow: 0 2px 8px rgba(0, 0, 0, 0.08)

Image:
- Aspect ratio: 4:3
- Object fit: cover
- Gradient overlay: linear-gradient(180deg, transparent 0%, rgba(0,0,0,0.3) 100%)

Badge:
- Position: absolute top-right (8px, 8px)
- Padding: 4px 12px
- Border radius: full

Rating:
- Star size: 16px
- Gap: 2px
- Color: #FFD700
```

### Animation & Transitions

```css
/* Standard transitions */
--transition-fast: 150ms ease-in-out
--transition-normal: 300ms ease-in-out
--transition-slow: 500ms ease-in-out

/* Hover effects */
Cards: transform: translateY(-4px), shadow increase
Buttons: background darken, scale: 1.02
Links: color change, underline

/* Loading states */
Skeleton: shimmer animation
Spinner: rotate 360deg, 1s linear infinite

/* Page transitions */
Fade in: opacity 0 to 1, 300ms
Slide up: translateY(20px) to 0, 300ms
```

---

## Testing Strategy

### 1. Unit Testing
- Test all new components
- Test component variants and states
- Test form validation logic
- Test utility functions

### 2. Integration Testing
- Test page layouts
- Test navigation flows
- Test API integrations
- Test state management

### 3. E2E Testing
- Test complete user flows:
  - Registration → Email verification → Login → Profile setup
  - Browse events → Register → Dashboard
  - Create event → Publish → View on feed
  - Role upgrade request → Admin approval

### 4. Visual Regression Testing
- Screenshot comparison before/after
- Test responsive breakpoints
- Test different browsers

### 5. Accessibility Testing
- Keyboard navigation
- Screen reader testing
- Color contrast ratios
- Focus management
- ARIA labels

### 6. Performance Testing
- Lighthouse scores (90+ target)
- Page load times
- Time to interactive
- First contentful paint
- Largest contentful paint
- Bundle size analysis

### 7. Cross-Browser Testing
- Chrome (latest 2 versions)
- Firefox (latest 2 versions)
- Safari (latest 2 versions)
- Edge (latest 2 versions)

### 8. Device Testing
- iOS: iPhone SE, iPhone 12/13/14, iPad
- Android: Various screen sizes
- Desktop: 1920x1080, 1366x768, 1440x900

---

## Phase Tracking

### Phase Status Legend
- 🔴 Not Started
- 🟡 In Progress
- 🟢 Completed
- ⚠️ Blocked
- ✅ Approved & Deployed

### Current Status: 🔴 Not Started

| Phase | Status | Start Date | End Date | Notes |
|-------|--------|------------|----------|-------|
| Phase 1: Foundation & Component Library | 🔴 | TBD | TBD | Waiting for approval |
| Phase 2: Header & Footer Enhancements | 🔴 | TBD | TBD | Depends on Phase 1 |
| Phase 3: Landing Page Redesign | 🔴 | TBD | TBD | Depends on Phase 2 |
| Phase 4: Dashboard Page Redesign | 🔴 | TBD | TBD | Depends on Phase 1-2 |
| Phase 5: Profile Page Redesign | 🔴 | TBD | TBD | Depends on Phase 1-2 |
| Phase 6: Notifications Page Redesign | 🔴 | TBD | TBD | Depends on Phase 1-2 |
| Phase 7: Auth Pages Enhancement | 🔴 | TBD | TBD | Depends on Phase 1-2 |
| Phase 8: Templates Gallery Enhancement | 🔴 | TBD | TBD | Depends on Phase 1-2 |
| Phase 9: Error Pages Enhancement | 🔴 | TBD | TBD | Depends on Phase 1-2 |
| Phase 10: Testing & Documentation | 🔴 | TBD | TBD | Final phase |

---

## Next Steps

1. **Review this plan** and confirm approach
2. **Prioritize pages** if needed (currently ordered by importance)
3. **Set timeline** based on team availability
4. **Assign implementation** to Phase 1 (Foundation)
5. **Begin implementation** when approved

---

## Notes

- **Placeholder Content:** Using placeholder data for Cultural Spotlight and News & Updates sections
- **Quick Action Buttons:** Need to determine modal vs page navigation for each action
- **Backend APIs:** Future work - new endpoints for news and cultural content
- **Progressive Enhancement:** Build features incrementally, test frequently
- **Documentation:** Update tracking docs after each phase

---

## References

- **Figma Design:** Screenshot analysis (4 images provided)
- **Current Codebase:** Full analysis of 12 existing pages
- **Design System:** Sri Lankan flag colors, Inter font, Tailwind CSS
- **Architecture:** Next.js 15, React 19, Clean Architecture, DDD patterns

---

**Document Version:** 1.0
**Last Updated:** 2025-11-16
**Created By:** Claude Code
**Status:** Draft - Awaiting Approval
