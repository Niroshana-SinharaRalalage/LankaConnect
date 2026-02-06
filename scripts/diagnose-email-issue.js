// Phase 6A.49: Email Issue Diagnostics
const { Client } = require('pg');

const client = new Client({
  host: 'lankaconnect-staging-db.postgres.database.azure.com',
  database: 'LankaConnectDB',
  user: 'adminuser',
  password: '1qaz!QAZ',
  ssl: { rejectUnauthorized: false }
});

async function runDiagnostics() {
  try {
    await client.connect();
    console.log('=== PHASE 6A.49 EMAIL DIAGNOSTICS ===\n');

    const eventId = '9e3722f5-c255-4dcc-b167-afef56bc5592';

    // Test 1: Check registrations
    console.log('Test 1: Checking registrations...');
    const regs = await client.query(`
      SELECT
        "Id",
        "UserId",
        "Status",
        "PaymentStatus",
        "CreatedAt",
        "UpdatedAt"
      FROM events.registrations
      WHERE "EventId" = $1
      ORDER BY "CreatedAt" DESC
      LIMIT 5
    `, [eventId]);

    if (regs.rows.length > 0) {
      console.log('✓ Found', regs.rows.length, 'registration(s)');
      regs.rows.forEach(r => {
        console.log(`  - ${r.Id}: Status=${r.Status}, PaymentStatus=${r.PaymentStatus}, Created=${r.CreatedAt}`);
      });
    } else {
      console.log('✗ NO REGISTRATIONS FOUND - User did not complete registration!');
    }

    // Test 2: Check template structure
    console.log('\nTest 2: Checking ticket-confirmation template...');
    const template = await client.query(`
      SELECT
        name,
        is_active,
        LENGTH(html_template) as html_length,
        LENGTH(subject_template) as subject_length,
        (LENGTH(html_template) - LENGTH(REPLACE(html_template, '{{#', ''))) / 3 as opening_tags,
        (LENGTH(html_template) - LENGTH(REPLACE(html_template, '{{/', ''))) / 3 as closing_tags,
        updated_at
      FROM communications.email_templates
      WHERE name = 'ticket-confirmation'
    `);

    if (template.rows.length > 0) {
      const t = template.rows[0];
      console.log(`✓ Template found: ${t.name}`);
      console.log(`  Active: ${t.is_active}`);
      console.log(`  HTML length: ${t.html_length} chars`);
      console.log(`  Opening tags: ${t.opening_tags}`);
      console.log(`  Closing tags: ${t.closing_tags}`);
      console.log(`  Updated: ${t.updated_at}`);

      if (t.opening_tags === t.closing_tags) {
        console.log('  ✓ Tags are balanced');
      } else {
        console.log(`  ✗ UNBALANCED TAGS! (${t.opening_tags} opening, ${t.closing_tags} closing)`);
      }
    }

    // Test 3: Check domain events table (if exists)
    console.log('\nTest 3: Checking if domain events are being stored...');
    try {
      const events = await client.query(`
        SELECT COUNT(*) as count
        FROM information_schema.tables
        WHERE table_schema = 'events'
        AND table_name = 'DomainEvents'
      `);

      if (events.rows[0].count > 0) {
        const recentEvents = await client.query(`
          SELECT *
          FROM events.domain_events
          WHERE "EventType" LIKE '%PaymentCompleted%'
          ORDER BY "CreatedAt" DESC
          LIMIT 5
        `);
        console.log(`  Found ${recentEvents.rows.length} PaymentCompleted events`);
      } else {
        console.log('  ⚠ DomainEvents table does not exist (events processed in-memory)');
      }
    } catch (err) {
      console.log('  ⚠ Could not check domain events:', err.message);
    }

    console.log('\n=== DIAGNOSIS COMPLETE ===\n');

    await client.end();
  } catch (err) {
    console.error('Error:', err.message);
    process.exit(1);
  }
}

runDiagnostics();
