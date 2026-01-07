# Multi-Stage Docker Build for Next.js 16 Application
# Optimized for Azure Container Apps deployment

# Stage 1: Dependencies
# Install ALL dependencies (including devDependencies needed for build)
FROM node:20-alpine AS deps
WORKDIR /app

# Copy package files
COPY package.json package-lock.json ./

# Install ALL dependencies (devDependencies needed for build: autoprefixer, tailwindcss, etc.)
RUN npm ci --ignore-scripts

# Stage 2: Builder
# Build the Next.js application
FROM node:20-alpine AS builder
WORKDIR /app

# Copy ALL dependencies from deps stage (including devDependencies)
COPY --from=deps /app/node_modules ./node_modules

# Copy application source
COPY . .

# Disable Next.js telemetry during build
ENV NEXT_TELEMETRY_DISABLED=1

# Build the application with standalone output
# This creates a minimal production build in .next/standalone
RUN npm run build

# Stage 3: Runner
# Create minimal production image
FROM node:20-alpine AS runner
WORKDIR /app

# Set production environment
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1

# Create non-root user for security
# Container Apps best practice: run as non-root
RUN addgroup --system --gid 1001 nodejs && \
    adduser --system --uid 1001 nextjs

# Copy standalone output (Next.js creates web/ subdirectory due to monorepo structure)
# The standalone build is at .next/standalone/web/ not .next/standalone/
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone/web ./

# Copy static files (CSS, JS bundles)
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static

# Copy public files (images, fonts, etc.) if they exist
COPY --from=builder /app/public ./public

# Switch to non-root user
USER nextjs

# Expose port 3000
EXPOSE 3000

# Set port environment variable
ENV PORT=3000
ENV HOSTNAME="0.0.0.0"

# Health check for Container Apps
# Checks /api/health endpoint every 30 seconds
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD node -e "require('http').get('http://localhost:3000/api/health', (res) => { process.exit(res.statusCode === 200 ? 0 : 1); }).on('error', () => process.exit(1));"

# Start the Next.js server
CMD ["node", "server.js"]
