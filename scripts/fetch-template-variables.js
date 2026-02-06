const { Client } = require('pg');

const client = new Client({
  host: 'lankaconnect-staging-db.postgres.database.azure.com',
  database: 'LankaConnectDB',
  user: 'adminuser',
  password: '1qaz!QAZ',
  ssl: { rejectUnauthorized: false }
});

async function fetchTemplateVariables() {
  try {
    await client.connect();

    const result = await client.query(`
      SELECT
        name,
        html_template
      FROM communications.email_templates
      WHERE name = 'ticket-confirmation'
    `);

    if (result.rows.length === 0) {
      console.log('Template not found');
      return;
    }

    const template = result.rows[0].html_template;

    // Extract all {{variable}} patterns
    const variableRegex = /\{\{([^}]+)\}\}/g;
    const variables = new Set();
    let match;

    while ((match = variableRegex.exec(template)) !== null) {
      variables.add(match[1]);
    }

    console.log('Variables found in ticket-confirmation template:');
    console.log(Array.from(variables).sort());

    await client.end();
  } catch (error) {
    console.error('Error:', error.message);
    process.exit(1);
  }
}

fetchTemplateVariables();
