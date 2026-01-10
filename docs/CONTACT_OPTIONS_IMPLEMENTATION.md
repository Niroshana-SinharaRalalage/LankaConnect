# Contact Options Implementation Guide

## Free Customer Support Setup (No Email Inbox Needed!)

### Option 1: Contact Form (Recommended for Email Support)

**Cost:** $0 (uses existing Azure Communication Services)

#### Backend Implementation

```csharp
// File: src/LankaConnect.Application/DTOs/ContactFormDto.cs

public class ContactFormDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? Phone { get; set; }

    [Required]
    [StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Message { get; set; } = string.Empty;
}
```

```csharp
// File: src/LankaConnect.API/Controllers/ContactController.cs

[ApiController]
[Route("api/[controller]")]
public class ContactController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<ContactController> _logger;
    private readonly IConfiguration _configuration;

    public ContactController(
        IEmailService emailService,
        ILogger<ContactController> logger,
        IConfiguration configuration)
    {
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> SendContactMessage([FromBody] ContactFormDto dto)
    {
        try
        {
            // Get your personal email from configuration
            var supportEmail = _configuration["ContactForm:SupportEmail"]
                ?? "yourname@gmail.com";  // ← Change to your email

            // Send to your personal inbox
            await _emailService.SendEmailAsync(
                to: supportEmail,
                subject: $"[LankaConnect Contact] {dto.Subject}",
                body: $@"
                    <h2>New Contact Form Submission</h2>
                    <p><strong>From:</strong> {dto.Name}</p>
                    <p><strong>Email:</strong> {dto.Email}</p>
                    <p><strong>Phone:</strong> {dto.Phone ?? "Not provided"}</p>
                    <p><strong>Subject:</strong> {dto.Subject}</p>
                    <hr>
                    <h3>Message:</h3>
                    <p>{dto.Message.Replace("\n", "<br>")}</p>
                    <hr>
                    <p><small>Reply to this email will go to: {dto.Email}</small></p>
                "
            );

            // Send auto-reply to user
            await _emailService.SendEmailAsync(
                to: dto.Email,
                from: "noreply@lankaconnect.app",
                subject: "Thanks for contacting LankaConnect!",
                body: $@"
                    <h2>Hi {dto.Name},</h2>

                    <p>Thanks for reaching out to us! We've received your message
                    and will get back to you within 24 hours.</p>

                    <p><strong>Your message:</strong></p>
                    <blockquote>{dto.Message.Replace("\n", "<br>")}</blockquote>

                    <p>Meanwhile, you can also reach us on:</p>
                    <ul>
                        <li>WhatsApp: <a href='https://wa.me/94XXXXXXXXX'>+94 XX XXX XXXX</a></li>
                        <li>Phone: +94 XX XXX XXXX</li>
                        <li>Facebook: <a href='https://facebook.com/lankaconnect'>@lankaconnect</a></li>
                    </ul>

                    <p>Best regards,<br>
                    LankaConnect Team</p>
                "
            );

            _logger.LogInformation(
                "Contact form submission from {Email}: {Subject}",
                dto.Email, dto.Subject
            );

            return Ok(new { message = "Message sent successfully. We'll get back to you soon!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send contact form message");
            return StatusCode(500, "Failed to send message. Please try again or contact us via WhatsApp.");
        }
    }
}
```

```json
// File: src/LankaConnect.API/appsettings.Production.json

{
  "ContactForm": {
    "SupportEmail": "yourname@gmail.com",  // ← Your personal email
    "WhatsAppNumber": "+94XXXXXXXXX"      // ← Your WhatsApp number
  }
}
```

#### Frontend Implementation

```typescript
// File: web/src/app/contact/page.tsx

'use client';

import { useState } from 'react';

export default function ContactPage() {
  const [formData, setFormData] = useState({
    name: '',
    email: '',
    phone: '',
    subject: '',
    message: ''
  });
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');

    try {
      const response = await fetch('/api/proxy/contact', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(formData)
      });

      if (response.ok) {
        setSuccess(true);
        setFormData({ name: '', email: '', phone: '', subject: '', message: '' });
      } else {
        setError('Failed to send message. Please try again.');
      }
    } catch (err) {
      setError('Network error. Please check your connection.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="max-w-2xl mx-auto p-6">
      <h1 className="text-3xl font-bold mb-6">Contact Us</h1>

      {/* Success Message */}
      {success && (
        <div className="bg-green-100 border border-green-400 text-green-700 px-4 py-3 rounded mb-4">
          ✅ Message sent! We'll get back to you within 24 hours.
        </div>
      )}

      {/* Error Message */}
      {error && (
        <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
          ❌ {error}
        </div>
      )}

      {/* Contact Form */}
      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label className="block text-sm font-medium mb-1">Name *</label>
          <input
            type="text"
            required
            value={formData.name}
            onChange={(e) => setFormData({ ...formData, name: e.target.value })}
            className="w-full px-3 py-2 border rounded"
          />
        </div>

        <div>
          <label className="block text-sm font-medium mb-1">Email *</label>
          <input
            type="email"
            required
            value={formData.email}
            onChange={(e) => setFormData({ ...formData, email: e.target.value })}
            className="w-full px-3 py-2 border rounded"
          />
        </div>

        <div>
          <label className="block text-sm font-medium mb-1">Phone (optional)</label>
          <input
            type="tel"
            value={formData.phone}
            onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
            className="w-full px-3 py-2 border rounded"
          />
        </div>

        <div>
          <label className="block text-sm font-medium mb-1">Subject *</label>
          <input
            type="text"
            required
            value={formData.subject}
            onChange={(e) => setFormData({ ...formData, subject: e.target.value })}
            className="w-full px-3 py-2 border rounded"
          />
        </div>

        <div>
          <label className="block text-sm font-medium mb-1">Message *</label>
          <textarea
            required
            rows={6}
            value={formData.message}
            onChange={(e) => setFormData({ ...formData, message: e.target.value })}
            className="w-full px-3 py-2 border rounded"
          />
        </div>

        <button
          type="submit"
          disabled={loading}
          className="w-full bg-blue-600 text-white py-2 px-4 rounded hover:bg-blue-700 disabled:bg-gray-400"
        >
          {loading ? 'Sending...' : 'Send Message'}
        </button>
      </form>

      {/* Alternative Contact Methods */}
      <div className="mt-8 p-4 bg-gray-50 rounded">
        <h2 className="text-xl font-semibold mb-4">Other Ways to Reach Us</h2>

        {/* WhatsApp */}
        <div className="mb-3">
          <a
            href="https://wa.me/94XXXXXXXXX?text=Hi, I need help with LankaConnect"
            target="_blank"
            rel="noopener noreferrer"
            className="flex items-center text-green-600 hover:text-green-700"
          >
            <span className="mr-2">📱</span>
            <span className="font-medium">WhatsApp:</span>
            <span className="ml-2">+94 XX XXX XXXX</span>
          </a>
        </div>

        {/* Phone */}
        <div className="mb-3">
          <a href="tel:+94XXXXXXXXX" className="flex items-center text-blue-600 hover:text-blue-700">
            <span className="mr-2">📞</span>
            <span className="font-medium">Phone:</span>
            <span className="ml-2">+94 XX XXX XXXX</span>
          </a>
        </div>

        {/* Social Media */}
        <div className="mb-3">
          <a
            href="https://facebook.com/lankaconnect"
            target="_blank"
            rel="noopener noreferrer"
            className="flex items-center text-blue-600 hover:text-blue-700"
          >
            <span className="mr-2">👍</span>
            <span className="font-medium">Facebook:</span>
            <span className="ml-2">@lankaconnect</span>
          </a>
        </div>
      </div>
    </div>
  );
}
```

---

### Option 2: WhatsApp Business Button (Highly Recommended for Sri Lanka!)

**Cost:** $0 (completely free)

#### Setup WhatsApp Business

```
1. Download WhatsApp Business app (iOS/Android)
2. Register with your business phone number: +94 XX XXX XXXX
3. Set up business profile:
   - Business name: LankaConnect
   - Category: Event Planning
   - Description: "Discover local events and businesses in Sri Lanka"
   - Business hours: Mon-Fri 9 AM - 6 PM
   - Website: https://lankaconnect.app

4. Enable features:
   - Quick replies (for common questions)
   - Away message (after hours)
   - Greeting message (first contact)
```

#### Add WhatsApp Button to Website

```typescript
// File: web/src/components/WhatsAppButton.tsx

'use client';

export function WhatsAppButton() {
  const phoneNumber = '94XXXXXXXXX';  // Your business number (no + or -)
  const message = encodeURIComponent('Hi, I need help with LankaConnect');

  return (
    <a
      href={`https://wa.me/${phoneNumber}?text=${message}`}
      target="_blank"
      rel="noopener noreferrer"
      className="fixed bottom-4 right-4 bg-green-500 text-white p-4 rounded-full shadow-lg hover:bg-green-600 transition z-50"
      aria-label="Chat on WhatsApp"
    >
      <svg className="w-6 h-6" fill="currentColor" viewBox="0 0 24 24">
        <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413Z"/>
      </svg>
    </a>
  );
}
```

```typescript
// File: web/src/app/layout.tsx

import { WhatsAppButton } from '@/components/WhatsAppButton';

export default function RootLayout({ children }: { children: React.Node }) {
  return (
    <html>
      <body>
        {children}
        <WhatsAppButton />  {/* Floating button on all pages */}
      </body>
    </html>
  );
}
```

---

## Migration Path: Free → Paid Email

### When to Upgrade to Microsoft 365

**Trigger Points:**

1. **Support volume > 20 emails/day**
   - Gmail starts feeling overwhelming
   - Need better organization

2. **Need team collaboration**
   - Multiple people handling support
   - Shared inbox needed

3. **Professional image important**
   - yourname@gmail.com feels unprofessional
   - Want support@lankaconnect.app

4. **Budget allows $6/month**
   - Ready to invest in professional tools

### Migration Steps (30 minutes)

```bash
# ===================================================================
# Step 1: Purchase Microsoft 365 Business Basic
# ===================================================================

# Go to: https://www.microsoft.com/microsoft-365/business
# Select: Business Basic ($6/user/month)
# Purchase for 1 user (support@lankaconnect.app)

# ===================================================================
# Step 2: Verify Domain with Microsoft 365
# ===================================================================

# Microsoft 365 Admin Center → Domains → Add domain
# Domain: lankaconnect.app
# Verify ownership: Add TXT record to Namecheap DNS

Type: TXT
Host: @
Value: MS=ms12345678 (provided by Microsoft)
TTL: 300

# ===================================================================
# Step 3: Configure DNS for Email
# ===================================================================

# Add MX records (in Namecheap DNS):

Type: MX
Host: @
Value: lankaconnect-app.mail.protection.outlook.com
Priority: 0
TTL: 300

# Add SPF record (REPLACE existing SPF from Azure):

Type: TXT
Host: @
Value: v=spf1 include:spf.protection.outlook.com include:azurecomm.net ~all
TTL: 300

# Add DKIM records (provided by Microsoft 365):

Type: CNAME
Host: selector1._domainkey
Value: selector1-lankaconnect-app._domainkey.lankaconnect.onmicrosoft.com
TTL: 300

Type: CNAME
Host: selector2._domainkey
Value: selector2-lankaconnect-app._domainkey.lankaconnect.onmicrosoft.com
TTL: 300

# Add DMARC record (update existing):

Type: TXT
Host: _dmarc
Value: v=DMARC1; p=none; rua=mailto:support@lankaconnect.app
TTL: 300

# ===================================================================
# Step 4: Create Email Accounts
# ===================================================================

# Microsoft 365 Admin Center → Users → Add user

Email accounts to create:
  - support@lankaconnect.app (primary support)
  - info@lankaconnect.app (general inquiries)
  - noreply@lankaconnect.app (keep for Azure Comm Services!)

# ===================================================================
# Step 5: Update Website Contact Info
# ===================================================================

# Update contact page:
# OLD: yourname@gmail.com
# NEW: support@lankaconnect.app

# Update auto-reply emails:
# "Questions? Email us at support@lankaconnect.app"

# ===================================================================
# Step 6: Set Up Email Forwarding (Transition Period)
# ===================================================================

# Gmail → Settings → Forwarding
# Forward: yourname@gmail.com → support@lankaconnect.app
# Keep for 1 month during transition

# ===================================================================
# Step 7: Test Everything
# ===================================================================

# Send test email to support@lankaconnect.app
# Verify:
# ✅ Email received in Outlook
# ✅ Can reply from support@lankaconnect.app
# ✅ Azure Communication Services still works (noreply@)
# ✅ No disruption to app emails
```

**Migration Downtime:** 0 seconds ✅

**Code Changes Required:** 0 (just update contact page) ✅

---

## Cost Comparison

### Launch Phase (FREE Options)

```
Azure Communication Services: $0-5/month
  - noreply@lankaconnect.app
  - All transactional emails

Contact Form: $0/month
  - Sends to your Gmail
  - Auto-reply to users

WhatsApp Business: $0/month
  - Instant messaging
  - Most popular in Sri Lanka

Phone: $0/month
  - Your existing number

Social Media: $0/month
  - Facebook/Instagram

TOTAL: $0-5/month ✅
```

### Growth Phase (Professional Email)

```
Azure Communication Services: $0-5/month
  - noreply@lankaconnect.app (unchanged)
  - All transactional emails

Microsoft 365 Business Basic: $6/user/month
  - support@lankaconnect.app
  - info@lankaconnect.app
  - 50GB mailbox

WhatsApp Business: $0/month
  - Keep using!

TOTAL: $6-11/month ✅
```

---

## Recommended Launch Setup

### Step 1: Implement Contact Form

```bash
# Backend
1. Create ContactController.cs (see above)
2. Add ContactFormDto.cs
3. Configure SupportEmail in appsettings

# Frontend
4. Create contact page (web/src/app/contact/page.tsx)
5. Add WhatsAppButton component
6. Test form submission

Time: 2-3 hours
Cost: $0
```

### Step 2: Set Up WhatsApp Business

```bash
1. Download WhatsApp Business app
2. Register business number
3. Configure business profile
4. Add quick replies for FAQs
5. Add WhatsApp button to website

Time: 30 minutes
Cost: $0
```

### Step 3: Add Contact Info to Footer

```typescript
// web/src/components/Footer.tsx

export function Footer() {
  return (
    <footer className="bg-gray-800 text-white p-8">
      <div className="max-w-6xl mx-auto grid md:grid-cols-3 gap-8">
        {/* Contact Info */}
        <div>
          <h3 className="font-bold mb-4">Contact Us</h3>
          <ul className="space-y-2">
            <li>
              <a href="/contact" className="hover:text-blue-400">
                📧 Contact Form
              </a>
            </li>
            <li>
              <a href="https://wa.me/94XXXXXXXXX" className="hover:text-green-400">
                💬 WhatsApp: +94 XX XXX XXXX
              </a>
            </li>
            <li>
              <a href="tel:+94XXXXXXXXX" className="hover:text-blue-400">
                📞 Phone: +94 XX XXX XXXX
              </a>
            </li>
            <li>
              <a href="https://facebook.com/lankaconnect" className="hover:text-blue-400">
                👍 Facebook: @lankaconnect
              </a>
            </li>
          </ul>
        </div>

        {/* Other footer sections */}
      </div>
    </footer>
  );
}
```

### Step 4: Configure Auto-Responder

Emails sent via contact form will auto-reply with:
- Thank you message
- Expected response time (24 hours)
- Alternative contact methods (WhatsApp, phone, social)

---

## Summary

### Answer 1: How can users contact us without an inbox?

**Multiple FREE options:**

1. **Contact Form** → Sends to your Gmail → You reply from Gmail
2. **WhatsApp Business** → Direct messaging (HIGHLY recommended for Sri Lanka!)
3. **Phone Number** → Direct calls/SMS
4. **Social Media** → Facebook/Instagram messages

**You DON'T need a business email inbox to launch!**

### Answer 2: Can we go live with Option 1 and switch to Option 2 later?

**YES! Absolutely!** ✅

**Phase 1 (Launch):**
- Azure Communication Services only ($0-5/month)
- Contact form + WhatsApp + phone
- Total cost: $0-5/month

**Phase 2 (Growth, when needed):**
- Add Microsoft 365 ($6/month)
- Professional email addresses
- Keep Azure Communication Services (no migration!)
- Total cost: $6-11/month

**Migration effort:** 30 minutes
**Downtime:** 0 seconds
**Code changes:** 0 (just update contact page)

---

**Recommended Launch Plan:**
1. ✅ Use Azure Communication Services (noreply@lankaconnect.app)
2. ✅ Implement contact form (sends to your Gmail)
3. ✅ Set up WhatsApp Business (FREE, very popular in SL!)
4. ✅ Add phone number
5. ✅ Upgrade to Microsoft 365 later (when volume increases)

**Total Launch Cost: $0-5/month** ✅