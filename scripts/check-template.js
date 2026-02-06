const { Client } = require('pg');

const client = new Client({
  host: 'lankaconnect-staging-db.postgres.database.azure.com',
  database: 'LankaConnectDB',
  user: 'adminuser',
  password: '1qaz!QAZ',
  ssl: { rejectUnauthorized: false }
});

async function checkTemplate() {
  try {
    await client.connect();

    // Check ALL templates that could be registration related
    console.log('=== ALL TEMPLATES ===');
    const allResult = await client.query(`
      SELECT "Id", name, is_active, created_at, updated_at,
             CASE WHEN html_template LIKE '%#8B1538%' THEN 'BRANDED' ELSE 'OLD BLUE' END as template_type
      FROM communications.email_templates
      ORDER BY name
    `);
    console.log(JSON.stringify(allResult.rows, null, 2));

    // Check if there are duplicates
    console.log('\n=== DUPLICATE CHECK ===');
    const dupResult = await client.query(`
      SELECT name, COUNT(*) as count
      FROM communications.email_templates
      GROUP BY name
      HAVING COUNT(*) > 1
    `);
    console.log('Duplicates:', dupResult.rows.length > 0 ? JSON.stringify(dupResult.rows) : 'None');

    // Get the exact template being used
    console.log('\n=== REGISTRATION-CONFIRMATION TEMPLATE DETAILS ===');
    const regResult = await client.query(`
      SELECT "Id", name, is_active, created_at, updated_at,
             html_template LIKE '%linear-gradient%' as has_gradient,
             html_template LIKE '%lankaconnect-logo%' as has_logo,
             html_template LIKE '%Reply to this email%' as has_reply_text
      FROM communications.email_templates
      WHERE name = 'registration-confirmation'
    `);
    console.log(JSON.stringify(regResult.rows, null, 2));

    await client.end();
  } catch (err) {
    console.error('Error:', err.message);
  }
}

checkTemplate();
