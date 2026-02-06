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

# ===================================================================
# CRITICAL: Next.js Standalone COPY Strategy
# ===================================================================
# ARCHITECTURE: .next/standalone does NOT contain .next/ subdirectory
# server.js expects .next/ as SIBLING directory in runtime environment
#
# Required structure in /app/:
#   /app/server.js           ← From .next/standalone/server.js
#   /app/node_modules/       ← From .next/standalone/node_modules
#   /app/package.json        ← From .next/standalone/package.json
#   /app/.next/static/       ← From .next/static/ (BUILD OUTPUT)
#   /app/public/             ← From public/ (STATIC ASSETS)
#
# See: docs/ARCHITECTURE_NEXTJS_STANDALONE_DOCKER_COPY_STRATEGY.md
# ===================================================================

# Step 1: Copy public assets (images, fonts, etc.)
COPY --from=builder --chown=nextjs:nodejs /app/public ./public

# Step 2: Copy standalone output (server.js, node_modules, package.json)
# This is the minimal server bundle created by output: 'standalone'
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./

# Step 3: Copy static assets to .next/static
# This creates /app/.next/static/ with all CSS/JS bundles and BUILD_ID
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static

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
