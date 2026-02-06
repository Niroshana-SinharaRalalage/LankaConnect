#!/bin/bash
# Template Discovery Diagnostic Script
# Usage: ./diagnose-template-discovery.sh <container-id-or-name>

CONTAINER=$1

if [ -z "$CONTAINER" ]; then
  echo "Usage: $0 <container-id-or-name>"
  exit 1
fi

echo "=== Template Discovery Diagnostics ==="
echo ""

echo "1. Container Runtime User:"
docker exec $CONTAINER id
echo ""

echo "2. Working Directory:"
docker exec $CONTAINER pwd
echo ""

echo "3. Templates Directory Existence:"
docker exec $CONTAINER ls -la /app/Templates/Email/ 2>&1 || echo "Directory not found or access denied"
echo ""

echo "4. Registration Confirmation Template Files:"
docker exec $CONTAINER ls -la /app/Templates/Email/registration-confirmation* 2>&1 || echo "Files not found or access denied"
echo ""

echo "5. Template File Contents (subject):"
docker exec $CONTAINER cat /app/Templates/Email/registration-confirmation-subject.txt 2>&1
echo ""

echo "6. Configuration Files:"
docker exec $CONTAINER ls -la /app/appsettings*.json
echo ""

echo "7. Environment Variables (Email/Template related):"
docker exec $CONTAINER env | grep -i -E '(email|template|aspnetcore)' || echo "No matching environment variables"
echo ""

echo "8. Test File.Exists() via .NET (if dotnet CLI available):"
docker exec $CONTAINER which dotnet && \
  docker exec $CONTAINER dotnet --version && \
  echo "Testing File.Exists('/app/Templates/Email/registration-confirmation-subject.txt')..." || \
  echo "dotnet CLI not available in container"
echo ""

echo "9. Process List:"
docker exec $CONTAINER ps aux 2>&1 | head -20
echo ""

echo "10. Application Logs (last 50 lines with template mentions):"
docker exec $CONTAINER sh -c "find /var/log -name '*.log' -type f 2>/dev/null | xargs grep -i template | tail -50" 2>&1 || \
  echo "Log files not found or not accessible"
echo ""

echo "11. Container Logs (last 100 lines):"
docker logs --tail 100 $CONTAINER 2>&1 | grep -i -E '(template|email|error|exception)' || echo "No relevant logs found"
echo ""

echo "=== Diagnostics Complete ==="
echo ""
echo "Key Things to Check:"
echo "  - Runtime user should be uid=1654 (appuser), NOT uid=0 (root)"
echo "  - Template files should be owned by appuser:appgroup with 755 permissions"
echo "  - Configuration should show TemplateBasePath=Templates/Email"
echo "  - Logs should show 'Found X template files' at startup"
echo ""
