#!/bin/bash
# Docker Build Verification Script
# Verifies that the Next.js standalone build is correctly structured in the container

set -e

IMAGE_NAME="${1:-lankaconnect-web:test}"
CONTAINER_NAME="verify-next-build-$$"

echo "🔍 Docker Build Verification Script"
echo "=================================="
echo ""

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if image exists
if ! docker image inspect "$IMAGE_NAME" &> /dev/null; then
    echo -e "${RED}❌ Error: Image '$IMAGE_NAME' not found${NC}"
    echo "   Build the image first with: docker build -t $IMAGE_NAME -f web/Dockerfile web/"
    exit 1
fi

echo -e "${GREEN}✓ Image found: $IMAGE_NAME${NC}"
echo ""

# Create temporary container
echo "📦 Creating temporary container..."
docker create --name "$CONTAINER_NAME" "$IMAGE_NAME" > /dev/null

# Cleanup function
cleanup() {
    echo ""
    echo "🧹 Cleaning up..."
    docker rm "$CONTAINER_NAME" > /dev/null 2>&1 || true
}
trap cleanup EXIT

echo ""
echo "🔍 Verification Tests"
echo "===================="

# Test 1: Check server.js exists
echo ""
echo "Test 1: server.js exists"
if docker exec "$CONTAINER_NAME" test -f /app/server.js 2>/dev/null || \
   docker cp "$CONTAINER_NAME:/app/server.js" /tmp/verify-test.tmp &>/dev/null; then
    echo -e "${GREEN}✓ PASS${NC}: server.js found at /app/server.js"
    rm -f /tmp/verify-test.tmp
else
    echo -e "${RED}✗ FAIL${NC}: server.js not found"
    exit 1
fi

# Test 2: Check .next directory exists
echo ""
echo "Test 2: .next directory structure"
if docker cp "$CONTAINER_NAME:/app/.next" /tmp/verify-next &>/dev/null; then
    echo -e "${GREEN}✓ PASS${NC}: .next directory exists"

    # List contents
    echo ""
    echo "   Contents of /app/.next/:"
    ls -la /tmp/verify-next/ | awk '{print "   " $0}'

    rm -rf /tmp/verify-next
else
    echo -e "${RED}✗ FAIL${NC}: .next directory not found"
    exit 1
fi

# Test 3: Check BUILD_ID exists
echo ""
echo "Test 3: BUILD_ID file"
if docker cp "$CONTAINER_NAME:/app/.next/BUILD_ID" /tmp/verify-buildid &>/dev/null; then
    BUILD_ID=$(cat /tmp/verify-buildid)
    echo -e "${GREEN}✓ PASS${NC}: BUILD_ID exists: $BUILD_ID"
    rm -f /tmp/verify-buildid
else
    echo -e "${RED}✗ FAIL${NC}: BUILD_ID not found"
    exit 1
fi

# Test 4: Check server directory
echo ""
echo "Test 4: Server runtime files"
if docker cp "$CONTAINER_NAME:/app/.next/server" /tmp/verify-server &>/dev/null; then
    SERVER_FILES=$(find /tmp/verify-server -type f | wc -l)
    echo -e "${GREEN}✓ PASS${NC}: server/ directory exists with $SERVER_FILES files"
    rm -rf /tmp/verify-server
else
    echo -e "${RED}✗ FAIL${NC}: server/ directory not found"
    exit 1
fi

# Test 5: Check static directory
echo ""
echo "Test 5: Static assets"
if docker cp "$CONTAINER_NAME:/app/.next/static" /tmp/verify-static &>/dev/null; then
    STATIC_FILES=$(find /tmp/verify-static -type f | wc -l)
    echo -e "${GREEN}✓ PASS${NC}: static/ directory exists with $STATIC_FILES files"

    # Check for chunks
    if [ -d /tmp/verify-static/chunks ]; then
        echo -e "${GREEN}✓ PASS${NC}: static/chunks/ exists (JS bundles)"
    else
        echo -e "${YELLOW}⚠ WARN${NC}: static/chunks/ not found"
    fi

    rm -rf /tmp/verify-static
else
    echo -e "${RED}✗ FAIL${NC}: static/ directory not found"
    exit 1
fi

# Test 6: Check required manifests
echo ""
echo "Test 6: Required manifest files"
MANIFESTS=(
    "build-manifest.json"
    "routes-manifest.json"
    "required-server-files.json"
)

ALL_MANIFESTS_OK=true
for manifest in "${MANIFESTS[@]}"; do
    if docker cp "$CONTAINER_NAME:/app/.next/$manifest" /tmp/verify-manifest &>/dev/null; then
        SIZE=$(wc -c < /tmp/verify-manifest)
        echo -e "${GREEN}✓ PASS${NC}: $manifest exists ($SIZE bytes)"
        rm -f /tmp/verify-manifest
    else
        echo -e "${RED}✗ FAIL${NC}: $manifest not found"
        ALL_MANIFESTS_OK=false
    fi
done

if [ "$ALL_MANIFESTS_OK" = false ]; then
    exit 1
fi

# Test 7: Check public directory
echo ""
echo "Test 7: Public assets"
if docker cp "$CONTAINER_NAME:/app/public" /tmp/verify-public &>/dev/null; then
    PUBLIC_FILES=$(find /tmp/verify-public -type f | wc -l)
    echo -e "${GREEN}✓ PASS${NC}: public/ directory exists with $PUBLIC_FILES files"
    rm -rf /tmp/verify-public
else
    echo -e "${YELLOW}⚠ WARN${NC}: public/ directory not found (may be empty)"
fi

# Test 8: Check node_modules
echo ""
echo "Test 8: Node.js dependencies"
if docker cp "$CONTAINER_NAME:/app/node_modules" /tmp/verify-modules &>/dev/null; then
    MODULE_COUNT=$(ls /tmp/verify-modules | wc -l)
    echo -e "${GREEN}✓ PASS${NC}: node_modules/ exists with $MODULE_COUNT packages"
    rm -rf /tmp/verify-modules
else
    echo -e "${RED}✗ FAIL${NC}: node_modules/ not found"
    exit 1
fi

# Test 9: Check package.json
echo ""
echo "Test 9: Package configuration"
if docker cp "$CONTAINER_NAME:/app/package.json" /tmp/verify-package &>/dev/null; then
    echo -e "${GREEN}✓ PASS${NC}: package.json exists"

    # Check for next dependency
    if grep -q '"next"' /tmp/verify-package; then
        NEXT_VERSION=$(grep '"next"' /tmp/verify-package | head -1)
        echo -e "${GREEN}✓ PASS${NC}: Next.js dependency found: $NEXT_VERSION"
    fi

    rm -f /tmp/verify-package
else
    echo -e "${RED}✗ FAIL${NC}: package.json not found"
    exit 1
fi

# Summary
echo ""
echo "=================================="
echo -e "${GREEN}✅ All verification tests passed!${NC}"
echo ""
echo "The Docker image has a complete .next directory with:"
echo "  ✓ Server runtime files (BUILD_ID, manifests)"
echo "  ✓ Server-side code (server/ directory)"
echo "  ✓ Static assets (static/ directory with CSS/JS)"
echo "  ✓ Public assets"
echo "  ✓ Node.js dependencies"
echo ""
echo "Next steps:"
echo "  1. Start the container: docker run -p 3000:3000 $IMAGE_NAME"
echo "  2. Check logs for 'Ready in Xms'"
echo "  3. Test health endpoint: curl http://localhost:3000/api/health"
echo ""
