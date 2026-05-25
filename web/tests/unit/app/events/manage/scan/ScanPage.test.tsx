import { describe, it } from 'vitest';

/**
 * Phase 6A.141 — Scanner page tests are intentionally skipped at the page level
 * because the Next.js 16 client-component params pattern (`params: Promise<{id:string}>`
 * unwrapped via `React.use(params)`) is hard to test in vitest. `use(promise)` suspends
 * synchronously and the test environment doesn't flush microtasks before assertions
 * fire, so the page never renders past the Suspense fallback.
 *
 * The internal logic is covered indirectly by:
 *  - Backend handler tests (Phases A-F): all 8 rejection reason codes + happy path + race-loser.
 *  - Operator browser UAT in Phase I against staging: visual confirmation of the
 *    green/red/yellow panels, audio + vibrate, manual-entry modal, camera-denied path.
 *
 * A future refactor splitting the page into an outer thin wrapper (`<ScanTicketPage
 * params>` → `use(params)` → `<ScannerView eventId={id}/>`) would let us test
 * `<ScannerView>` directly with a synchronous eventId prop. Out of scope for this phase.
 */
describe.skip('ScanTicketPage', () => {
  it('placeholder — see operator UAT cells in MASTER_TODO_PHASE_6A_141_*.md', () => {
    // intentionally empty
  });
});
