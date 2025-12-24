const { Client } = require('pg');
const fs = require('fs');

const client = new Client({
  host: 'lankaconnect-staging-db.postgres.database.azure.com',
  database: 'LankaConnectDB',
  user: 'adminuser',
  password: '1qaz!QAZ',
  ssl: { rejectUnauthorized: false }
});

async function fetchTemplate() {
  try {
    await client.connect();

    const result = await client.query(`
      SELECT name, subject_template, html_template, text_template, is_active
      FROM communications.email_templates
      WHERE name = 'ticket-confirmation'
    `);

    if (result.rows.length === 0) {
      console.error('❌ Template not found: ticket-confirmation');
      process.exit(1);
    }

    const template = result.rows[0];

    console.log('=== TICKET CONFIRMATION TEMPLATE ===');
    console.log('Name:', template.name);
    console.log('Active:', template.is_active);
    console.log('Subject:', template.subject_template);
    console.log('\nHTML Template Length:', template.html_template.length, 'chars');
    console.log('\nText Template Length:', template.text_template.length, 'chars');

    // Save to files for inspection
    fs.writeFileSync('scripts/ticket-confirmation-html.html', template.html_template);
    fs.writeFileSync('scripts/ticket-confirmation-text.txt', template.text_template);
    fs.writeFileSync('scripts/ticket-confirmation-subject.txt', template.subject_template);

    console.log('\n✅ Template files saved to scripts/ folder');

    // Extract variables
    const htmlVars = [...new Set((template.html_template.match(/\{\{([^}#/]+)\}\}/g) || []))];
    const conditionals = [...new Set((template.html_template.match(/\{\{#([^}]+)\}\}/g) || []))];

    console.log('\nVariables:', htmlVars.length);
    console.log(htmlVars);
    console.log('\nConditionals:', conditionals.length);
    console.log(conditionals);

    // Check for unclosed tags
    const openTags = template.html_template.match(/\{\{#([^}]+)\}\}/g) || [];
    const closeTags = template.html_template.match(/\{\{\/([^}]+)\}\}/g) || [];

    console.log('\nOpen tags:', openTags.length);
    console.log('Close tags:', closeTags.length);

    if (openTags.length !== closeTags.length) {
      console.error('\n❌ SYNTAX ERROR: Unclosed conditional tags!');
    } else {
      console.log('\n✅ All conditional tags properly closed');
    }

    await client.end();
  } catch (error) {
    console.error('Error:', error.message);
    process.exit(1);
  }
}

fetchTemplate();
