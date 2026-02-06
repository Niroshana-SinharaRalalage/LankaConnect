const { Client } = require('pg');

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

async function checkEmailQueue() {
  try {
    await client.connect();
    console.log('✅ Connected to database\n');

    // Check recent email messages
    const result = await client.query(`
      SELECT
        "Id",
        template_name,
        subject,
        to_emails,
        status,
        "CreatedAt",
        error_message
      FROM communications.email_messages
      WHERE "CreatedAt" > NOW() - INTERVAL '1 hour'
      ORDER BY "CreatedAt" DESC
      LIMIT 20
    `);

    console.log(`Found ${result.rows.length} email messages in last hour:\n`);

    result.rows.forEach((row, index) => {
      console.log(`${index + 1}. Email ID: ${row.Id}`);
      console.log(`   Template: ${row.template_name || 'N/A'}`);
      console.log(`   Subject: ${row.subject?.value || row.subject || 'N/A'}`);
      console.log(`   To: ${JSON.stringify(row.to_emails)}`);
      console.log(`   Status: ${row.status}`);
      console.log(`   Created: ${row.CreatedAt}`);
      if (row.error_message) {
        console.log(`   Error: ${row.error_message}`);
      }
      console.log('');
    });

    // Check for event-published template specifically
    const eventPublishedEmails = result.rows.filter(r => r.template_name === 'event-published');
    console.log(`\n📧 Event Publication Emails: ${eventPublishedEmails.length}`);

    if (eventPublishedEmails.length === 0) {
      console.log('❌ NO event-published emails found in the last hour');
      console.log('   This means EventPublishedEvent is NOT being raised or handled');
    }

  } catch (error) {
    console.error('❌ ERROR:', error.message);
  } finally {
    await client.end();
  }
}

checkEmailQueue();
