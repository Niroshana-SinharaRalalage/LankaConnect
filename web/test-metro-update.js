const axios = require('axios');

const API_URL = 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api';

async function test() {
  try {
    // Login with test credentials
    const loginRes = await axios.post(`${API_URL}/auth/login`, {
      email: 'testmetro@test.com',
      password: 'Test@Metro!123'
    });

    console.log('Login response:', {
      userId: loginRes.data.user?.userId,
      hasToken: !!loginRes.data.accessToken
    });

    const token = loginRes.data.accessToken;
    const userId = loginRes.data.user?.userId;

    // Try to update metro areas
    const metroRes = await axios.put(
      `${API_URL}/users/${userId}/preferred-metro-areas`,
      {
        metroAreaIds: ['39000000-0000-0000-0000-000000000001']
      },
      {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      }
    );

    console.log('Metro update success:', metroRes.status);
  } catch (error) {
    console.error('Error:', error.response?.status, error.response?.data);
  }
}

test();
