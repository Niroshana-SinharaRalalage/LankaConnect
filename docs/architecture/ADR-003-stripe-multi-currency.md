# ADR-003: Stripe Multi-Currency Strategy

| | |
|---|---|
| **Status** | Accepted (2026-04-26) |
| **Date** | 2026-04-26 |
| **Decision Owner** | Niroshana (Founder/Architect) |
| **Reviewers** | system-architect agent |
| **Supersedes** | — |
| **Related** | ADR-001 (i18n scope), ADR-002 (tenancy) |

## Phase A Scope Clarification (per D3 resolution 2026-04-26)

Commerce (LankaSeyla / LankaMart / LankaNivasa) launches **USA-only / USD-only at Phase 3**. LankaEvents continues its existing multi-country Stripe pattern unchanged. This ADR specifies the **architectural foundation** for multi-currency, with **implementation deferred** as follows:

| Concern | Phase A foundation | Production implementation |
|---|---|---|
| `IPaymentCheckoutService` accepts `Currency` parameter | ✅ Built in W5/W7 | Used USD-only until first non-USA Commerce storefront |
| Money value object replacing `decimal` | ✅ Built in W5 | All existing rows backfilled as USD |
| Stripe metadata stamping (`storefront_id`, `originating_module`, `customer_country`) | ✅ Built in W7 + ArchTest enforcement | Active from Commerce launch |
| Stripe multi-currency presentment (charge in customer's currency) | Configuration-ready | Enabled when first non-USD storefront launches |
| Stripe Tax | US coverage required at Commerce launch | Other countries enabled per launch schedule |
| LK VAT custom tax table (Stripe Tax doesn't cover Sri Lanka) | NOT in Phase A; NOT in Phase 3 | Built when LK Commerce launch is scheduled |
| SCA / 3DS for UK/EU | Stripe Checkout (hosted) selected — gets it for free | No retrofit in Phase A; Events already uses Checkout |
| FX-volatility-on-refund accounting | Disclosed (this ADR) | Reconciliation report built when first non-USD refunds occur |
| Bank multi-currency settlement | **Assumed YES** per D1; verification deferred to pre-Phase-3 | If bank rejects, fallback to multi-currency US business account |

## Context

Phase A makes the Stripe abstraction currency-aware. Phase 3 launches Commerce in USD initially, with planned expansion to LKR (Sri Lanka), INR (India), GBP (UK). Existing Events flows (event registration payments, donations, sponsor payments, add-on purchases) currently charge USD only.

Stripe offers several patterns for multi-currency platforms. The choice has long-term consequences: Stripe account architecture is hard to change after live transactions exist.

## Options Considered

### Option A: Single Stripe account, multi-currency settlement (RECOMMENDED)

One Stripe account. The account is configured to accept charges in any of Stripe's 135+ supported presentment currencies. Charges in non-settlement currencies are converted to the settlement currency (USD) at payout time, with Stripe's standard FX fee (~1–2%).

- LankaConnect IS the seller of record for all transactions (Events tickets, Seyla/Mart/Nivasa products)
- Customer pays in their local currency; Stripe handles FX
- Founder receives USD payout to USD bank account

### Option B: Stripe Connect (Standard or Express)

LankaConnect operates as a platform; each "seller" (or storefront, or country) is a connected Stripe account. Platform charges an application fee per transaction. Each connected account settles to its own bank in its own currency.

### Option C: Multiple Stripe accounts (one per country/storefront)

Manually create separate Stripe accounts per country. Backend selects SDK initialization based on transaction context.

### Option D: Hybrid — single account for Events, Connect for Commerce

Events stays simple (single account). Commerce uses Connect to support future third-party vendors.

## Decision

**Adopt Option A (Single Stripe account with multi-currency settlement) for Phase A and Phase 3.**

Revisit and migrate to Option B (Stripe Connect) **only when** LankaConnect becomes a true marketplace (third-party sellers list products on Seyla/Mart/Nivasa). At that point, the abstraction layer (`IPaymentCheckoutService`) protects existing code from the migration.

### Implementation specifics

- Stripe account configured with multi-currency presentment + USD settlement
- `IPaymentCheckoutService.CreateCheckoutSessionAsync(CheckoutRequest)` accepts `Money` (carries Currency)
- Refunds happen in original charge currency (Stripe enforces this constraint regardless of strategy)
- Stripe Tax enabled for automatic per-country tax calculation (eliminates custom tax tables)
- Webhook handlers route by event type, not by currency — single webhook endpoint
- Stripe metadata on every charge: `storefront_id` (per ADR-002), `originating_module` (Events/Commerce/...), `customer_country`

## Consequences

### Positive

- Single Stripe account = single dashboard, single set of credentials, single webhook endpoint
- Solo-founder operational simplicity
- Customer pays in their currency without Stripe Checkout currency-switcher friction
- Stripe Tax handles per-country tax (US states, UK VAT, Sri Lanka VAT, etc.) — saves weeks of tax-rule maintenance per country
- Refund/dispute/chargeback flow is unified
- One reconciliation job, one accounting integration point
- Settlement to one USD bank account — accountant-friendly
- Migration to Connect later is non-destructive: charges already created stay; new charges flow through Connect

### Negative / Trade-offs

- ~1–2% FX fee on non-USD charges at payout time (cost of doing business; cheaper than maintaining multi-account complexity)
- All transactions appear under one entity in Stripe — if regulatory scrutiny ever requires per-country segregation, migration to Connect needed
- Bank account must accept multi-currency or USD-only with FX losses absorbed
- Refunds in original currency means USD bank balance fluctuates with FX

### Risks

- **Risk: regulatory pressure to separate per-country revenue (tax, money-laundering, sanctions).** Mitigation: Stripe metadata tags enable per-country reporting today; can migrate to Connect within ~4 weeks if required.
- **Risk: bank account FX policy.** Mitigation: confirm with US bank that USD-only settlement of multi-currency charges is acceptable before going live.
- **Risk: a country's local payment methods (e.g., LKR card networks not supported by global Stripe).** Mitigation: investigate per-country acceptance before launch; Stripe supports LKR but verify card-network coverage.

## Rejected Alternatives

- **Option B (Connect) for Phase A**: Premature. Connect adds significant SDK complexity, KYC requirements per connected account, application fee plumbing. Justified only when third-party vendors join. Today, founder is the only seller.
- **Option C (multi-account)**: Operationally horrific for solo founder. Multiple dashboards, multiple webhook URLs, multiple secrets, no unified reporting. Rejected.
- **Option D (hybrid)**: Adds complexity without clear benefit for Phase A timeframe. Defer.

## Pre-Launch Verification

Before Phase 3 Commerce launch:

- [ ] Confirm Stripe account FX policy and payout currency
- [ ] Enable Stripe Tax for US (and any country going live)
- [ ] **Verify Stripe Tax coverage for Sri Lanka VAT** (likely uncovered; document fallback below if so)
- [ ] Confirm card-network coverage for target country (e.g., visa/master/amex acceptance for LKR)
- [ ] Test refund flow in non-USD currency on Stripe test mode
- [ ] Document accountant reconciliation procedure for multi-currency
- [ ] **Verify Strong Customer Authentication (3DS) flow works for UK/EU launches**

## Architect Review v3 — Required Additions

### Bank rejection fallback

**If the US bank rejects multi-currency settlement to a USD-only account**:
- Open a multi-currency US business account (Mercury, Bluevine, or large bank's "global account" product)
- **Do NOT open a Stripe Connect account** — Connect is for marketplace platforms with third-party sellers, not for solving bank reject. Wrong tool.
- Settlement to multi-currency account preserves original currency until you choose to convert; FX is on your terms, not Stripe's.
- Decision criteria: if FX volume > $10K/mo or volatility risk material, multi-currency bank account justified; otherwise USD bank with Stripe FX is simpler.

### Metadata stamping contract

Every caller of `IPaymentCheckoutService.CreateCheckoutSessionAsync(CheckoutRequest)` MUST stamp the following metadata fields. ArchTest enforces presence:

```csharp
new CheckoutRequest {
    Amount = new Money(50.00m, Currency.USD),
    Metadata = new Dictionary<string, string> {
        ["storefront_id"] = currentStorefront.Id.ToString(),  // required (per ADR-002)
        ["originating_module"] = "Events",                    // required (Events|Commerce|Donations|Sponsor|AddOn)
        ["customer_country"] = customer.Country.Iso3166Alpha2,  // required
        ["correlation_id"] = telemetryContext.CorrelationId   // recommended
    },
    ...
};
```

The ArchTest rule (per `tests/architecture/`):
- Every `.CreateCheckoutSessionAsync(...)` call site is reviewed for these three required keys
- Reflective test inspects `CheckoutRequest.Metadata` at integration test time; fails if any required key missing

### FX volatility on refund — accounting treatment

Refunds happen in original charge currency (Stripe enforces). Customer charged in LKR settled to USD bank → refund in LKR. This means:

1. Original charge: customer LKR 1,500 → bank USD ~$5.00 (at FX rate T0)
2. Settlement: USD ~$4.90 after Stripe FX fee
3. Refund 30 days later: customer LKR 1,500 → bank pays USD ~$5.05 (at FX rate T1)
4. **Net loss to platform**: ~$0.15 per refund + Stripe FX fee on the way back

**Accounting treatment**:
- Refund FX losses booked to "Foreign Exchange Loss" account
- Reconciliation report (monthly) shows charge-vs-refund FX differential
- For Sri Lanka launches with significant refund rates, consider per-month batched accounting reserve

### SCA / 3DS support requirement

UK and EU launches require Strong Customer Authentication (PSD2). Stripe Checkout (hosted) handles 3DS challenges automatically. Stripe Elements (embedded) requires explicit 3DS integration code.

**Decision for Phase 3 Commerce v1**: use Stripe Checkout (hosted) — gets SCA for free. Revisit Elements for embedded checkout only when conversion data justifies it.

**Verification**: test 3DS flow with Stripe test card `4000 0027 6000 3184` (always requires 3DS) before any UK/EU launch.

### Stripe Tax coverage caveat

Stripe Tax automatically calculates and collects:
- US sales tax (state + local) ✅
- EU VAT ✅
- UK VAT ✅
- Australia GST ✅
- Canada GST/PST/HST ✅
- **Sri Lanka VAT** ❌ (NOT supported as of 2026)
- **India GST** — partial support; verify before launch

**Fallback for uncovered jurisdictions**:
- Maintain custom `tax_rates` table with country-region-rate triples
- TaxCalculationService computes tax in domain layer; Stripe Checkout receives pre-calculated `tax_amount` line item
- Quarterly review: if Stripe adds coverage, migrate

## References

- Architect review: 2026-04-26 (Question 4 — Stripe strategy flagged as need-decision); 2026-04-26 v2 (review of ADR-003 — required 4 additions)
- Stripe documentation: Multi-currency settlement, Stripe Tax, 3DS / SCA
