const { Client } = require('pg');
const fs = require('fs');
const path = require('path');

const client = new Client({
  host: 'lankaconnect-staging-db.postgres.database.azure.com',
  database: 'LankaConnectDB',
  user: 'adminuser',
  password: '1qaz!QAZ',
  port: 5432,
  ssl: {
    rejectUnauthorized: false
  }
});

async function executeFix() {
  try {
    console.log('Connecting to Azure PostgreSQL...');
    await client.connect();
    console.log('✅ Connected successfully!\n');

    // Step 1: Verify current state
    console.log('Step 1: Checking current template state...');
    const checkResult = await client.query(`
      SELECT name, subject_template, is_active, created_at
      FROM communications.email_templates
      WHERE name = 'event-published'
    `);

    if (checkResult.rows.length === 0) {
      console.error('❌ ERROR: Template "event-published" not found!');
      process.exit(1);
    }

    const currentTemplate = checkResult.rows[0];
    console.log('Current template state:');
    console.log('  Name:', currentTemplate.name);
    console.log('  Subject:', currentTemplate.subject_template || 'NULL');
    console.log('  Is Active:', currentTemplate.is_active);
    console.log('  Created:', currentTemplate.created_at);
    console.log('');

    // Step 2: Execute fix
    console.log('Step 2: Updating template subject...');
    const updateResult = await client.query(`
      UPDATE communications.email_templates
      SET
        subject_template = 'New Event: {{EventTitle}} in {{EventCity}}, {{EventState}}',
        updated_at = NOW()
      WHERE name = 'event-published'
        AND (subject_template IS NULL OR subject_template = '' OR subject_template != 'New Event: {{EventTitle}} in {{EventCity}}, {{EventState}}')
    `);

    console.log(`✅ Rows updated: ${updateResult.rowCount}`);
    console.log('');

    // Step 3: Verify fix
    console.log('Step 3: Verifying update...');
    const verifyResult = await client.query(`
      SELECT
        name,
        subject_template,
        LENGTH(subject_template) as subject_length,
        CASE
          WHEN subject_template IS NULL THEN 'ERROR: NULL'
          WHEN subject_template = '' THEN 'ERROR: EMPTY'
          WHEN subject_template LIKE '%{{EventTitle}}%' AND
               subject_template LIKE '%{{EventCity}}%' AND
               subject_template LIKE '%{{EventState}}%' THEN 'SUCCESS: VALID (All placeholders present)'
          WHEN subject_template LIKE '%{{EventTitle}}%' THEN 'WARNING: PARTIAL (Missing city/state placeholders)'
          ELSE 'ERROR: INVALID (No placeholders)'
        END as subject_status,
        is_active,
        updated_at
      FROM communications.email_templates
      WHERE name = 'event-published'
    `);

    const verifiedTemplate = verifyResult.rows[0];
    console.log('Updated template state:');
    console.log('  Name:', verifiedTemplate.name);
    console.log('  Subject:', verifiedTemplate.subject_template);
    console.log('  Subject Length:', verifiedTemplate.subject_length);
    console.log('  Status:', verifiedTemplate.subject_status);
    console.log('  Is Active:', verifiedTemplate.is_active);
    console.log('  Updated At:', verifiedTemplate.updated_at);
    console.log('');

    if (verifiedTemplate.subject_status.startsWith('SUCCESS')) {
      console.log('✅ DATABASE FIX COMPLETED SUCCESSFULLY!');
      console.log('✅ Template "event-published" now has valid subject with all placeholders');
      process.exit(0);
    } else {
      console.error('❌ VERIFICATION FAILED:', verifiedTemplate.subject_status);
      process.exit(1);
    }

  } catch (error) {
    console.error('❌ ERROR:', error.message);
    console.error('Stack:', error.stack);
    process.exit(1);
  } finally {
    await client.end();
  }
}

executeFix();
