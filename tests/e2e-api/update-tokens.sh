#!/bin/bash
# Update all test scripts with fresh authentication token

NEW_TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiI1ZTc4MmI0ZC0yOWVkLTRlMWQtOTAzOS02YzhmNjk4YWVlYTkiLCJlbWFpbCI6Im5pcm9zaGhoMkBnbWFpbC5jb20iLCJ1bmlxdWVfbmFtZSI6Ik5pcm9zaGFuYSBTaW5oYXJhIFJhbGFsYWdlIiwicm9sZSI6IkV2ZW50T3JnYW5pemVyIiwiZmlyc3ROYW1lIjoiTmlyb3NoYW5hIiwibGFzdE5hbWUiOiJTaW5oYXJhIFJhbGFsYWdlIiwiaXNBY3RpdmUiOiJ0cnVlIiwianRpIjoiZmFmOTk0Y2YtZjQ1Mi00YjdmLWExZGYtMGRmOWFjZTE0OTU0IiwiaWF0IjoxNzY0OTAyMDAyLCJuYmYiOjE3NjQ5MDIwMDIsImV4cCI6MTc2NDkwMzgwMiwiaXNzIjoiaHR0cHM6Ly9sYW5rYWNvbm5lY3QtYXBpLXN0YWdpbmcuYXp1cmV3ZWJzaXRlcy5uZXQiLCJhdWQiOiJodHRwczovL2xhbmthY29ubmVjdC1zdGFnaW5nLmF6dXJld2Vic2l0ZXMubmV0In0.cMZsBhVlGiQWxXZgFgr3s-vSUegOBGYjjF1vXAfhVrs"
EXPIRY="2025-12-05T03:03:22Z"

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
