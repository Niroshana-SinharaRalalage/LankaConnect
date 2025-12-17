const https = require('https');

const token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiI1ZTc4MmI0ZC0yOWVkLTRlMWQtOTAzOS02YzhmNjk4YWVlYTkiLCJlbWFpbCI6Im5pcm9zaGhoMkBnbWFpbC5jb20iLCJ1bmlxdWVfbmFtZSI6Ik5pcm9zaGFuYSBTaW5oYXJhIFJhbGFsYWdlIiwicm9sZSI6IkV2ZW50T3JnYW5pemVyIiwiZmlyc3ROYW1lIjoiTmlyb3NoYW5hIiwibGFzdE5hbWUiOiJTaW5oYXJhIFJhbGFsYWdlIiwiaXNBY3RpdmUiOiJ0cnVlIiwianRpIjoiNTAxMzk4ZTMtY2VkNi00ZmU1LWE4YWItMmYxYTFiNTg4NjQ4IiwiaWF0IjoxNzY1NTc1NTk1LCJuYmYiOjE3NjU1NzU1OTUsImV4cCI6MTc2NTU3NzM5NSwiaXNzIjoiaHR0cHM6Ly9sYW5rYWNvbm5lY3QtYXBpLXN0YWdpbmcuYXp1cmV3ZWJzaXRlcy5uZXQiLCJhdWQiOiJodHRwczovL2xhbmthY29ubmVjdC1zdGFnaW5nLmF6dXJld2Vic2l0ZXMubmV0In0.j5pRP6OLrC-Ui5F-df0vWkb2vH2nEFbkVEeD-cdmz70";

const eventId = "68f675f1-327f-42a9-be9e-f66148d826c3";
const data = JSON.stringify({
    attendees: [{name: "Test Attendee", age: 25}],
    email: "niroshhh2@gmail.com",
    phoneNumber: "+94771234567",
    successUrl: "https://lankaconnect-staging.azurewebsites.net/events/payment/success",
    cancelUrl: "https://lankaconnect-staging.azurewebsites.net/events/payment/cancel"
});

const options = {
    hostname: 'lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io',
    port: 443,
    path: `/api/events/${eventId}/rsvp`,
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
        'Content-Length': Buffer.byteLength(data),
        'Authorization': `Bearer ${token}`
    }
};

const req = https.request(options, (res) => {
    console.log(`Status: ${res.statusCode}`);
    let body = '';
    res.on('data', (chunk) => {
        body += chunk;
    });
    res.on('end', () => {
        console.log('Response:', body);
    });
});

req.on('error', (e) => {
    console.error(`Error: ${e.message}`);
});

req.write(data);
req.end();
