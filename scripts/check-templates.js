const { Client } = require('pg');

const client = new Client({
  host: 'lankaconnect-staging-db.postgres.database.azure.com',
  database: 'LankaConnectDB',
  user: 'adminuser',
  password: '1qaz!QAZ',
  ssl: { rejectUnauthorized: false }
});

async function checkTemplates() {
  try {
    await client.connect();

    const result = await client.query(`
      SELECT
        name,
        subject_template,
        LENGTH(html_template) as html_len,
        html_template LIKE '%EventDateTime%' as has_datetime,
        html_template LIKE '%EventImageUrl%' as has_image_url,
        html_template LIKE '%HasEventImage%' as has_event_image,
        is_active,
        created_at,
        updated_at
      FROM communications.email_templates
      WHERE name IN ('ticket-confirmation', 'registration-confirmation')
      ORDER BY name
    `);

    console.log(JSON.stringify(result.rows, null, 2));

    await client.end();
  } catch (error) {
    console.error('Error:', error.message);
    process.exit(1);
  }
}

checkTemplates();
