const { Client } = require('pg');

const client = new Client({
  host: 'lankaconnect-staging-db.postgres.database.azure.com',
  database: 'LankaConnectDB',
  user: 'adminuser',
  password: '1qaz!QAZ',
  ssl: { rejectUnauthorized: false }
});

async function checkTemplateContent() {
  try {
    await client.connect();

    const result = await client.query(`
      SELECT
        name,
        subject_template,
        SUBSTRING(html_template, 1, 1000) as html_preview,
        html_template LIKE '%EventStartDate%' as has_old_format,
        html_template LIKE '%EventDateTime%' as has_new_format,
        html_template LIKE '%AttendeeCount%' as has_attendee_count,
        updated_at
      FROM communications.email_templates
      WHERE name = 'ticket-confirmation'
    `);

    console.log(JSON.stringify(result.rows, null, 2));

    await client.end();
  } catch (error) {
    console.error('Error:', error.message);
    process.exit(1);
  }
}

checkTemplateContent();
