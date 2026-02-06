const { Client } = require('pg');

async function checkRegistrationData() {
  const client = new Client({
    connectionString: 'postgresql://lankaconnect_admin:7heB4F58-41*H-BBc*gb4c8h86C88GFd@lankaconnect-db-staging.postgres.database.azure.com:5432/lankaconnect_db?sslmode=require'
  });

  try {
    await client.connect();
    console.log('Connected to database\n');

    // Query registrations for the specific event
    const eventId = '0458806b-8672-4ad5-a7cb-f5346f1b282a';

    const result = await client.query(`
      SELECT
        r."Id",
        r."EventId",
        r."UserId",
        r."Status",
        r."Quantity",
        r.attendees,
        r.contact,
        r."CreatedAt",
        r."UpdatedAt"
      FROM events.registrations r
      WHERE r."EventId" = $1
      ORDER BY r."CreatedAt" DESC
      LIMIT 10;
    `, [eventId]);

    console.log(`Found ${result.rows.length} registrations for event ${eventId}\n`);

    result.rows.forEach((row, index) => {
      console.log(`\n--- Registration ${index + 1} ---`);
      console.log(`ID: ${row.Id}`);
      console.log(`UserId: ${row.UserId}`);
      console.log(`Status: ${row.Status}`);
      console.log(`Quantity: ${row.Quantity}`);
      console.log(`Attendees (JSONB): ${JSON.stringify(row.attendees, null, 2)}`);
      console.log(`Contact (JSONB): ${JSON.stringify(row.contact, null, 2)}`);
      console.log(`CreatedAt: ${row.CreatedAt}`);
    });

    // Also check if there are any NULL attendees arrays
    const nullCheck = await client.query(`
      SELECT COUNT(*) as count
      FROM events.registrations r
      WHERE r."EventId" = $1 AND r.attendees IS NULL;
    `, [eventId]);

    console.log(`\n\nRegistrations with NULL attendees: ${nullCheck.rows[0].count}`);

  } catch (err) {
    console.error('Error:', err.message);
    console.error('Stack:', err.stack);
  } finally {
    await client.end();
  }
}

checkRegistrationData();
