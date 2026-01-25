# LankaConnect UI/UX Style Guide

**MANDATORY FOR ALL UI WORK - READ BEFORE BUILDING ANY COMPONENT**

**Last Updated:** 2026-01-24
**Status:** Active - All agents MUST follow this guide

---

## 🎯 Purpose

This style guide ensures **consistent look and feel** across all LankaConnect modules:
- Events
- Marketplace
- Business Profile
- Forum
- Admin Dashboard

**Every UI component MUST follow these patterns. NO exceptions.**

---

## 🎨 COLOR PALETTE

### Primary Colors
```css
--primary-blue: #1E40AF;      /* Primary brand color */
--primary-blue-light: #3B82F6; /* Hover states, accents */
--primary-blue-dark: #1E3A8A;  /* Active states, headers */

--secondary-green: #10B981;    /* Success, positive actions */
--secondary-purple: #8B5CF6;   /* Premium features, highlights */
```

### Semantic Colors
```css
--success: #10B981;   /* Green - Success messages, confirmations */
--warning: #F59E0B;   /* Orange - Warnings, pending states */
--error: #EF4444;     /* Red - Errors, destructive actions */
--info: #3B82F6;      /* Blue - Info messages, tips */
```

### Neutral Colors
```css
--gray-50: #F9FAFB;   /* Backgrounds */
--gray-100: #F3F4F6;  /* Subtle backgrounds */
--gray-200: #E5E7EB;  /* Borders, dividers */
--gray-300: #D1D5DB;  /* Disabled states */
--gray-400: #9CA3AF;  /* Placeholder text */
--gray-500: #6B7280;  /* Secondary text */
--gray-600: #4B5563;  /* Primary text - light backgrounds */
--gray-700: #374151;  /* Headings */
--gray-800: #1F2937;  /* Dark headings */
--gray-900: #111827;  /* Primary text - dark mode */
```

### Usage Examples
```tsx
// ✅ CORRECT: Use semantic colors
<Button variant="success">Save Changes</Button>
<Alert type="error">Payment failed. Please try again.</Alert>

// ❌ WRONG: Hardcoded hex colors
<button style={{ backgroundColor: '#10B981' }}>Save</button>
```

---

## 📐 TYPOGRAPHY

### Font Families
```css
--font-primary: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
--font-mono: 'Monaco', 'Courier New', monospace;
```

### Font Sizes (Responsive)
```css
/* Headings */
--text-5xl: 3rem;      /* 48px - Hero headings */
--text-4xl: 2.25rem;   /* 36px - Page titles */
--text-3xl: 1.875rem;  /* 30px - Section headings */
--text-2xl: 1.5rem;    /* 24px - Card headings */
--text-xl: 1.25rem;    /* 20px - Sub-headings */
--text-lg: 1.125rem;   /* 18px - Large body text */

/* Body */
--text-base: 1rem;     /* 16px - Default body text */
--text-sm: 0.875rem;   /* 14px - Small text, labels */
--text-xs: 0.75rem;    /* 12px - Captions, hints */
```

### Font Weights
```css
--font-light: 300;
--font-normal: 400;
--font-medium: 500;
--font-semibold: 600;
--font-bold: 700;
```

### Line Heights
```css
--leading-tight: 1.25;    /* Headings */
--leading-normal: 1.5;    /* Body text */
--leading-relaxed: 1.75;  /* Long-form content */
```

### Typography Components
```tsx
// Headings
<h1 className="text-4xl font-bold text-gray-900">Page Title</h1>
<h2 className="text-3xl font-semibold text-gray-800">Section Heading</h2>
<h3 className="text-2xl font-medium text-gray-700">Card Heading</h3>

// Body text
<p className="text-base text-gray-600 leading-normal">
  This is regular body text with normal line height.
</p>

// Small text
<span className="text-sm text-gray-500">
  Last updated 5 minutes ago
</span>
```

---

## 📏 SPACING SYSTEM

**4px base unit** - All spacing MUST be multiples of 4px

### Spacing Scale
```css
--space-1: 0.25rem;  /* 4px */
--space-2: 0.5rem;   /* 8px */
--space-3: 0.75rem;  /* 12px */
--space-4: 1rem;     /* 16px */
--space-5: 1.25rem;  /* 20px */
--space-6: 1.5rem;   /* 24px */
--space-8: 2rem;     /* 32px */
--space-10: 2.5rem;  /* 40px */
--space-12: 3rem;    /* 48px */
--space-16: 4rem;    /* 64px */
--space-20: 5rem;    /* 80px */
```

### Common Spacing Patterns
```tsx
// ✅ CORRECT: Consistent spacing
<div className="space-y-6">        {/* 24px vertical spacing between children */}
  <Card className="p-6">           {/* 24px padding inside card */}
    <h3 className="mb-4">Title</h3> {/* 16px margin bottom */}
    <p className="mb-6">Content</p> {/* 24px margin bottom */}
    <Button>Action</Button>
  </Card>
</div>

// ❌ WRONG: Random spacing values
<div style={{ marginBottom: '17px' }}>  {/* Not multiple of 4 */}
```

---

## 🧩 REUSABLE COMPONENTS

### Button Component
**Location:** `web/src/presentation/components/common/Button.tsx`

```tsx
// Usage examples
<Button variant="primary" size="md">Primary Action</Button>
<Button variant="secondary" size="md">Secondary Action</Button>
<Button variant="outline" size="md">Outline Button</Button>
<Button variant="ghost" size="md">Ghost Button</Button>
<Button variant="danger" size="md">Delete</Button>

// Sizes
<Button size="sm">Small</Button>
<Button size="md">Medium</Button>  {/* Default */}
<Button size="lg">Large</Button>

// States
<Button loading={true}>Processing...</Button>
<Button disabled={true}>Disabled</Button>

// With icons
<Button icon={<PlusIcon />}>Add Item</Button>
<Button iconPosition="right" icon={<ArrowRightIcon />}>Next</Button>
```

### Input Component
**Location:** `web/src/presentation/components/common/Input.tsx`

```tsx
// Text input
<Input
  label="Email Address"
  type="email"
  placeholder="you@example.com"
  required
  error={errors.email}
/>

// With icon
<Input
  label="Search"
  type="text"
  icon={<SearchIcon />}
  placeholder="Search products..."
/>

// Number input
<Input
  label="Price"
  type="number"
  min={0}
  step={0.01}
  prefix="$"
/>

// Textarea
<Textarea
  label="Description"
  rows={4}
  maxLength={500}
  error={errors.description}
/>
```

### Card Component
**Location:** `web/src/presentation/components/common/Card.tsx`

```tsx
// Basic card
<Card>
  <CardHeader>
    <CardTitle>Product Details</CardTitle>
    <CardDescription>View and edit product information</CardDescription>
  </CardHeader>
  <CardContent>
    {/* Card body */}
  </CardContent>
  <CardFooter>
    <Button variant="primary">Save</Button>
    <Button variant="ghost">Cancel</Button>
  </CardFooter>
</Card>

// Clickable card
<Card hoverable onClick={() => navigate(`/products/${id}`)}>
  <CardContent>
    <h3 className="text-xl font-semibold">{product.name}</h3>
    <p className="text-gray-600">{product.description}</p>
  </CardContent>
</Card>
```

### Modal Component
**Location:** `web/src/presentation/components/common/Modal.tsx`

```tsx
// Confirmation modal
<Modal
  isOpen={isOpen}
  onClose={() => setIsOpen(false)}
  title="Delete Product"
  variant="danger"
>
  <p>Are you sure you want to delete this product? This action cannot be undone.</p>

  <ModalFooter>
    <Button variant="danger" onClick={handleDelete}>Delete</Button>
    <Button variant="ghost" onClick={() => setIsOpen(false)}>Cancel</Button>
  </ModalFooter>
</Modal>

// Form modal
<Modal
  isOpen={isOpen}
  onClose={() => setIsOpen(false)}
  title="Add Product"
  size="lg"
>
  <form onSubmit={handleSubmit}>
    <Input label="Product Name" {...register('name')} />
    <Input label="Price" type="number" {...register('price')} />
    <Textarea label="Description" {...register('description')} />

    <ModalFooter>
      <Button type="submit" variant="primary">Save</Button>
      <Button type="button" variant="ghost" onClick={() => setIsOpen(false)}>Cancel</Button>
    </ModalFooter>
  </form>
</Modal>
```

### Alert Component
**Location:** `web/src/presentation/components/common/Alert.tsx`

```tsx
// Success alert
<Alert type="success" dismissible>
  Product successfully added to cart!
</Alert>

// Error alert
<Alert type="error" title="Payment Failed">
  Your payment could not be processed. Please check your card details and try again.
</Alert>

// Warning alert
<Alert type="warning" icon={<WarningIcon />}>
  Only 3 items left in stock
</Alert>

// Info alert
<Alert type="info">
  Free shipping on orders over $75
</Alert>
```

---

## 📱 RESPONSIVE DESIGN

### Breakpoints
```css
--screen-sm: 640px;   /* Mobile landscape */
--screen-md: 768px;   /* Tablet portrait */
--screen-lg: 1024px;  /* Tablet landscape, small desktop */
--screen-xl: 1280px;  /* Desktop */
--screen-2xl: 1536px; /* Large desktop */
```

### Mobile-First Approach
```tsx
// ✅ CORRECT: Mobile-first responsive classes
<div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
  {/* 1 column on mobile, 2 on tablet, 3 on desktop */}
</div>

<h1 className="text-2xl md:text-3xl lg:text-4xl">
  {/* Responsive font sizes */}
</h1>

<div className="p-4 md:p-6 lg:p-8">
  {/* Responsive padding */}
</div>
```

### Touch Targets
All interactive elements MUST have minimum 44x44px touch target (mobile)

```tsx
// ✅ CORRECT: Minimum 44px height
<button className="h-11 px-4">  {/* 44px height */}
  Click Me
</button>

// ❌ WRONG: Too small for mobile
<button className="h-6 px-2">  {/* 24px height - too small */}
  Click
</button>
```

---

## 📋 FORM PATTERNS

### Form Layout
```tsx
// Standard form layout
<form className="space-y-6" onSubmit={handleSubmit}>
  <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
    <Input label="First Name" {...register('firstName')} required />
    <Input label="Last Name" {...register('lastName')} required />
  </div>

  <Input label="Email" type="email" {...register('email')} required />

  <Textarea label="Message" rows={4} {...register('message')} />

  <div className="flex justify-end space-x-4">
    <Button type="button" variant="ghost">Cancel</Button>
    <Button type="submit" variant="primary" loading={isSubmitting}>
      Submit
    </Button>
  </div>
</form>
```

### Validation & Error Messages
```tsx
// Inline validation
<Input
  label="Email"
  type="email"
  {...register('email', {
    required: 'Email is required',
    pattern: {
      value: /^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}$/i,
      message: 'Invalid email address'
    }
  })}
  error={errors.email?.message}
/>

// Form-level errors
{errors.submit && (
  <Alert type="error" className="mb-6">
    {errors.submit.message}
  </Alert>
)}
```

### Loading States
```tsx
// Form loading state
<form onSubmit={handleSubmit}>
  <Input label="Name" disabled={isSubmitting} />
  <Input label="Email" disabled={isSubmitting} />

  <Button type="submit" loading={isSubmitting} disabled={isSubmitting}>
    {isSubmitting ? 'Saving...' : 'Save Changes'}
  </Button>
</form>
```

---

## 🗺️ LAYOUT SYSTEM

### Container
```tsx
// Page container
<div className="container mx-auto px-4 sm:px-6 lg:px-8 py-8">
  {/* Page content */}
</div>

// Max-width containers
<div className="max-w-7xl mx-auto">  {/* Full width */}
<div className="max-w-5xl mx-auto">  {/* Large content */}
<div className="max-w-3xl mx-auto">  {/* Medium content */}
<div className="max-w-2xl mx-auto">  {/* Narrow content, forms */}
```

### Grid Layouts
```tsx
// Product grid
<div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
  {products.map(product => (
    <ProductCard key={product.id} product={product} />
  ))}
</div>

// Sidebar layout
<div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
  <aside className="lg:col-span-3">
    {/* Sidebar */}
  </aside>
  <main className="lg:col-span-9">
    {/* Main content */}
  </main>
</div>
```

---

## 🚦 NAVIGATION PATTERNS

### Header
```tsx
<header className="bg-white border-b border-gray-200 sticky top-0 z-50">
  <div className="container mx-auto px-4 sm:px-6 lg:px-8">
    <div className="flex items-center justify-between h-16">
      <Logo />
      <Navigation />
      <UserMenu />
    </div>
  </div>
</header>
```

### Navigation Links
```tsx
// Active/inactive states
<Link
  href="/marketplace"
  className={`px-3 py-2 rounded-md text-sm font-medium ${
    isActive
      ? 'bg-primary-blue text-white'
      : 'text-gray-600 hover:bg-gray-100 hover:text-gray-900'
  }`}
>
  Marketplace
</Link>
```

### Breadcrumbs
```tsx
<nav aria-label="Breadcrumb" className="mb-6">
  <ol className="flex items-center space-x-2 text-sm text-gray-600">
    <li><Link href="/">Home</Link></li>
    <li className="flex items-center">
      <ChevronRightIcon className="w-4 h-4 mx-2" />
      <Link href="/marketplace">Marketplace</Link>
    </li>
    <li className="flex items-center">
      <ChevronRightIcon className="w-4 h-4 mx-2" />
      <span className="text-gray-900 font-medium">Product Name</span>
    </li>
  </ol>
</nav>
```

---

## ♿ ACCESSIBILITY REQUIREMENTS

### Semantic HTML
```tsx
// ✅ CORRECT: Semantic elements
<header>...</header>
<nav>...</nav>
<main>...</main>
<article>...</article>
<aside>...</aside>
<footer>...</footer>

// ❌ WRONG: Divs everywhere
<div className="header">...</div>
<div className="nav">...</div>
```

### ARIA Labels
```tsx
// Buttons with icons only
<button aria-label="Close modal" onClick={onClose}>
  <CloseIcon />
</button>

// Search input
<input
  type="search"
  aria-label="Search products"
  placeholder="Search..."
/>

// Loading spinner
<div role="status" aria-live="polite">
  <Spinner />
  <span className="sr-only">Loading...</span>
</div>
```

### Keyboard Navigation
- All interactive elements MUST be keyboard accessible
- Focus states MUST be visible
- Tab order MUST be logical

```tsx
// Focus visible
<button className="focus:outline-none focus:ring-2 focus:ring-primary-blue focus:ring-offset-2">
  Click Me
</button>
```

---

## 🎭 ANIMATION & TRANSITIONS

### Transition Speeds
```css
--transition-fast: 150ms;
--transition-base: 200ms;
--transition-slow: 300ms;
```

### Common Transitions
```tsx
// Hover transitions
<button className="transition-colors duration-200 hover:bg-primary-blue-light">
  Hover Me
</button>

// Loading spinners
<div className="animate-spin">
  <Spinner />
</div>

// Fade in/out
<div className="transition-opacity duration-300 opacity-0 group-hover:opacity-100">
  Tooltip
</div>
```

---

## 📊 DATA DISPLAY PATTERNS

### Tables
```tsx
<Table>
  <TableHeader>
    <TableRow>
      <TableHead>Product</TableHead>
      <TableHead>Price</TableHead>
      <TableHead>Stock</TableHead>
      <TableHead className="text-right">Actions</TableHead>
    </TableRow>
  </TableHeader>
  <TableBody>
    {products.map(product => (
      <TableRow key={product.id}>
        <TableCell className="font-medium">{product.name}</TableCell>
        <TableCell>${product.price}</TableCell>
        <TableCell>
          <Badge variant={product.stock > 0 ? 'success' : 'error'}>
            {product.stock > 0 ? 'In Stock' : 'Out of Stock'}
          </Badge>
        </TableCell>
        <TableCell className="text-right">
          <Button size="sm" variant="ghost">Edit</Button>
        </TableCell>
      </TableRow>
    ))}
  </TableBody>
</Table>
```

### Badges
```tsx
<Badge variant="success">Active</Badge>
<Badge variant="warning">Pending</Badge>
<Badge variant="error">Rejected</Badge>
<Badge variant="info">New</Badge>
```

---

## ✅ PRE-BUILD CHECKLIST

Before building ANY new UI component, verify:

- [ ] Read this entire UI_STYLE_GUIDE.md
- [ ] Checked if similar component exists in `/web/src/presentation/components/`
- [ ] Using design tokens (colors, spacing, typography) from this guide
- [ ] Following existing component patterns
- [ ] Mobile-first responsive design
- [ ] Minimum 44x44px touch targets
- [ ] Keyboard accessible
- [ ] ARIA labels for screen readers
- [ ] Error states defined
- [ ] Loading states defined
- [ ] Focus states visible
- [ ] Wrote component tests

---

## 🚨 ENFORCEMENT

**All UI code MUST pass these checks before merge:**

1. **Design Review**: Matches this style guide
2. **Accessibility Audit**: Passes WCAG 2.1 AA
3. **Responsive Test**: Works on mobile (320px), tablet (768px), desktop (1024px)
4. **Browser Test**: Works on Chrome, Firefox, Safari, Edge
5. **Component Tests**: 90% coverage with visual regression tests

---

**Questions? Ask user before deviating from this guide.**
**Last Updated:** 2026-01-24
