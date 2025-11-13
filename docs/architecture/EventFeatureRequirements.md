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
- Events can include multiple sign-up categories (e.g., food, gifts, logistics).
- Sign-ups can be:
  - **Predefined list:** Specific items participants can select (e.g., 10 food dishes).
  - **Open sign-up:** Participants can decide what to bring.
- Users can view, join, or cancel sign-ups.
- Once a user signs up, the button updates to “You are signed up.”
- Users can view the list of all current sign-ups for transparency.
- This feature is similar to [SignupGenius](https://www.signupgenius.com/).

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
