#!/bin/bash
# Update all test scripts with fresh authentication token

NEW_TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiI1ZTc4MmI0ZC0yOWVkLTRlMWQtOTAzOS02YzhmNjk4YWVlYTkiLCJlbWFpbCI6Im5pcm9zaGhoMkBnbWFpbC5jb20iLCJ1bmlxdWVfbmFtZSI6Ik5pcm9zaGFuYSBTaW5oYXJhIFJhbGFsYWdlIiwicm9sZSI6IkV2ZW50T3JnYW5pemVyIiwiZmlyc3ROYW1lIjoiTmlyb3NoYW5hIiwibGFzdE5hbWUiOiJTaW5oYXJhIFJhbGFsYWdlIiwiaXNBY3RpdmUiOiJ0cnVlIiwianRpIjoiNTk3ODk1YzEtOTI0My00ZmE2LTgxYTEtMjJhNjQ3M2M5YzFlIiwiaWF0IjoxNzY0ODkxMjk3LCJuYmYiOjE3NjQ4OTEyOTcsImV4cCI6MTc2NDg5MzA5NywiaXNzIjoiaHR0cHM6Ly9sYW5rYWNvbm5lY3QtYXBpLXN0YWdpbmcuYXp1cmV3ZWJzaXRlcy5uZXQiLCJhdWQiOiJodHRwczovL2xhbmthY29ubmVjdC1zdGFnaW5nLmF6dXJld2Vic2l0ZXMubmV0In0.PbbaqS8Sdh3YBPce2LNNX8aX1loC1RMVR4X4Do5QKCA"
EXPIRY="2025-12-05T00:04:57Z"

# Update quick-test.sh
sed -i "s|^TOKEN=.*|TOKEN=\"$NEW_TOKEN\"|" quick-test.sh

# Update all test scenarios
for file in test-scenario-*.sh; do
    if [ -f "$file" ]; then
        # Update expiry comment
        sed -i "s|expires at .*)|expires at $EXPIRY)|" "$file"
        # Update token
        sed -i "s|^AUTH_TOKEN=.*|AUTH_TOKEN=\"$NEW_TOKEN\"|" "$file"
        echo "Updated: $file"
    fi
done

echo "All test scripts updated with fresh token (expires: $EXPIRY)"
