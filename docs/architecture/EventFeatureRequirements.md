# Event Management Feature Requirements

## Overview
This document outlines the detailed requirements for the **Event Management** module within the Community App. The module allows users to create, register, and manage community events, including registrations, sign-ups, payments, notifications, and analytics.

---

## 1. Event Creation
- Event creators (organizers) can create and manage events.
- Each event includes the following details:
  - Event title, description, location, and date/time.
  - Event type (Free or Paid).
  - Event capacity limit (optional).
  - Registration type:
    - **Simple registration:** Only number of participants.
    - **Detailed registration:** Full participant information (Name, Email, Phone, Age, Gender, Address, etc.).
  - Sign-up lists (optional): Food, gifts, or other contributions.
  - Ticket/pass types and pricing (e.g., Adult, Child, Food Pass).
- Event creators can edit or delete events.
- Organizers can enable/disable sign-ups, payments, or detailed registration requirements.

---

## 2. Registration
- Users can register for an event by providing required details.
- Registration types:
  - **Free Event:** Registration without payment.
  - **Paid Event:** Requires payment (Stripe integration).
- Upon successful registration:
  - A confirmation message is shown.
  - Users receive an email with their event pass (includes QR code for check-in).
- Registered users can cancel their registration (refund policy applies per event terms).

---

## 3. Sign-Up Management

### Overview
Events can include multiple sign-up lists to manage participant contributions. Each sign-up list can contain items across three priority categories. Event organizers can choose which category combinations work best for their event.

### Sign-Up Categories (Priority-Based System)
Each sign-up list can include items from one or more of the following categories:

1. **Mandatory Items** (Required)
   - Items that MUST be brought by participants
   - Example: "Main dish for 10 people", "Table decorations"
   - Organizers can require commitment to at least one mandatory item before registration

2. **Preferred Items** (Highly Desired)
   - Items that are strongly encouraged but not required
   - Example: "Desserts", "Beverages", "Appetizers"
   - Helps organizers prioritize what they need most

3. **Suggested Items** (Optional)
   - Items that would be nice to have but are completely optional
   - Example: "Extra chairs", "Paper plates", "Decorative items"
   - Provides flexibility for participants to contribute

### Item Structure
Each item in a sign-up list includes:
- **Item Description**: What needs to be brought (e.g., "Vegetable Salad")
- **Quantity**: How many units/servings are needed (e.g., "2 large bowls")
- **Category**: Mandatory, Preferred, or Suggested
- **Current Commitments**: Track who has committed to bringing this item

### Sign-Up List Creation
When creating a sign-up list, organizers can:
1. **Choose Categories**: Select which category types to enable (can select one, two, or all three)
   - Example 1: Potluck event might enable all three categories
   - Example 2: Essential supplies event might only use "Mandatory Items"
   - Example 3: Community picnic might use "Preferred" and "Suggested" only

2. **Add Items per Category**: For each enabled category, add multiple items with:
   - Item description
   - Required quantity
   - Optional notes

3. **Set Rules** (Optional):
   - Require at least X mandatory items to be fulfilled before event
   - Limit commitments per user
   - Allow multiple users to commit to same item (if quantity > 1)

### User Interaction
- Users can view all sign-up lists for an event
- Items are grouped by category (Mandatory → Preferred → Suggested)
- Users can commit to bringing one or more items
- Once committed, the button shows "You are signed up"
- Users can view who else has committed to items (for transparency)
- Users can cancel their commitments before the event deadline

### Example Use Cases

**Example 1: Potluck Dinner**
- **Mandatory Items**: Main Dishes (Chicken Curry x2, Fish Fry x1)
- **Preferred Items**: Side Dishes (Rice x3, Salad x2)
- **Suggested Items**: Desserts, Beverages, Paper plates

**Example 2: Temple Decoration Event**
- **Mandatory Items**: Flowers (Lotus x50, Jasmine x100), Oil lamps x20
- **Preferred Items**: Incense sticks x10 boxes, Candles x50
- **Suggested Items**: Decorative fabric, Ribbons

**Example 3: Community Cleanup**
- **Mandatory Items**: Garbage bags (100), Gloves (50 pairs)
- **Preferred Items**: (not used)
- **Suggested Items**: Brooms, Rakes, Water bottles

This feature is inspired by [SignupGenius](https://www.signupgenius.com/) with enhanced category-based organization.

---

## 4. Passes & Tickets
Each event can include multiple **passes** or **tickets**, each with unique pricing and features. Users can choose one or more passes during registration.  

### Example Event Pass Configuration

#### **Event Pass (Main Entry Ticket)**
- **Adult Pass:** $20 per person  
  - Includes general admission to the event.  
  - Valid for one adult attendee.  
  - Optional add-ons available (e.g., food ticket, parking).  
- **Child Pass:** $10 per person  
  - Admission for children under 12 years old.  
  - Requires accompanying adult registration.  

#### **Food Purchase Ticket**
- **Hoppers Meal:** $15  
  - Includes a Sri Lankan-style hopper meal with sides.  
  - Redeemable at food counters.  
- **Fried Rice Meal:** $15  
  - Includes a fried rice meal with curry options.  

### Additional Ticket Features
- Each pass includes:
  - **QR Code** for easy check-in and validation.  
  - **PDF Download Option** for offline access.  
  - **Resend Ticket Option** via email.  
  - **Print-Friendly Format** for users who prefer physical copies.  
- The system calculates the total cost dynamically based on selected passes and redirects the user to the payment page (Stripe).  
- Organizers can configure multiple ticket tiers and categories during event creation or later.  

---

## 5. Waitlist Support
- If an event reaches capacity, users can join a **waitlist**.
- When a spot becomes available, the next user on the waitlist receives a notification and an option to register.

---

## 6. Notifications
- Email/SMS notifications for:
  - Registration confirmation and cancellation.
  - Event reminders (e.g., 24 hours before event).
  - Sign-up changes (e.g., new or canceled items).
  - Waitlist promotions (when a spot becomes available).

---

## 7. Organizer Dashboard
- Dashboard available to event creators with analytics:
  - Total registrations.
  - Payments received (for paid events).
  - Cancellations and refunds.
  - Sign-up statistics.
- Ability to export event data (CSV/Excel).
- Dashboard should be mobile-friendly and integrated within the app.

---

## 8. Payment & Financial Features
- **Stripe Integration:** For secure payments.
- **Early Bird Pricing:** Discounted rate available until a specified date.
- **Group Discounts:** Reduced rate when registering multiple participants in one order.
- **Refund Handling:** Per organizer’s rules.
- **Revenue Reports:** Display total income, pending refunds, and payout summaries.
- **Multiple Ticket Pricing:** Support tiered ticket prices for different passes or times.

---

## 9. User Interface
- **Event Listing Page:**
  - Displays upcoming and past events.
  - Shows Register/Sign-Up buttons based on status.
- **Event Details Page:**
  - Displays full event information.
  - Shows user’s registration status and pass details.
- **Registration/Sign-Up Buttons:**
  - Updates dynamically (e.g., “Registered,” “Signed Up,” “Join Waitlist”).

---

## 10. Payment Integration
- Integrate with **Stripe** for paid event registrations.
- Support multiple ticket types in one transaction.
- Send receipts via email.
- Handle refunds per organizer’s configuration.

---

## 11. Technical Considerations
- Backend: .NET 8 Minimal API (recommended).
- Database: Azure SQL or PostgreSQL.
- File Storage: Azure Blob Storage (for images/passes).
- Notifications: Twilio/SendGrid integration for SMS/Email.
- Authentication: Integrated with community app’s existing user auth.

---

## 12. Event Notification Recipients (Email Group Management)

- During event creation, the organizer can specify one or more email groups (lists of email addresses) that should receive notifications related to the event.

  - Example: The organizer enters friends@community.org, volunteers@community.org, or manually adds individual emails separated by commas.

  - The system stores these emails under the event configuration.

- When sending notifications (such as event announcements, updates, or reminders), the system should:

  - Retrieve the email group specified during event creation.

  - Retrieve all registered participants’ emails for that event.

  - Combine both lists, remove duplicates, and generate a consolidated email recipient list.

  - Send notifications to the consolidated list.

- Notifications may include:

  - Event updates or changes.

  - Reminders (e.g., “Event happening tomorrow”).

  - Cancellation or rescheduling notices.

  - Post-event thank-you messages.

- The feature should support:

  - Manual updates to the email group (add/remove addresses).

  - Import from file (CSV of email addresses).

  - Validation to ensure all email addresses are properly formatted.

  - Optional opt-out mechanism for recipients who don’t want further notifications.

- Technical Considerations:

  - Use SendGrid or a similar service to handle large batch email sends.

  - Maintain an email delivery log (success/failure).

  - Handle unsubscribe preferences in compliance with email standards (CAN-SPAM).

---

## 13. Future Enhancements (Optional)
- Event check-in via QR code scanning.
- Event photo sharing and gallery integration.
- Post-event feedback form and rating system.
