/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  // Enable standalone output for Docker deployment
  // This creates a minimal production build with only necessary files
  // Required for Azure Container Apps deployment
  output: 'standalone',
  // Allow large file uploads (videos up to 500 MB) through Server Actions
  experimental: {
    serverActions: {
      bodySizeLimit: '520mb', // 500 MB video + overhead
    },
  },
  images: {
    remotePatterns: [
      {
        protocol: 'http',
        hostname: 'localhost',
      },
      {
        protocol: 'https',
        hostname: 'lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io',
      },
      {
        protocol: 'https',
        hostname: 'lankaconnectstrgaccount.blob.core.windows.net',
      },
      {
        protocol: 'https',
        hostname: 'lankaconnectprodstorage.blob.core.windows.net',
      },
      {
        protocol: 'https',
        hostname: 'lankaconnect-api-prod.graystone-d581eaeb.eastus2.azurecontainerapps.io',
      },
    ],
  },
  env: {
    NEXT_PUBLIC_API_URL: process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api',
  },
};

module.exports = nextConfig;
