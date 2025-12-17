#!/bin/bash

# Test set-primary endpoint on staging
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiI1ZTc4MmI0ZC0yOWVkLTRlMWQtOTAzOS02YzhmNjk4YWVlYTkiLCJlbWFpbCI6Im5pcm9zaGhoMkBnbWFpbC5jb20iLCJ1bmlxdWVfbmFtZSI6Ik5pcm9zaGFuYSBTaW5oYXJhIFJhbGFsYWdlIiwicm9sZSI6IkV2ZW50T3JnYW5pemVyIiwiZmlyc3ROYW1lIjoiTmlyb3NoYW5hIiwibGFzdE5hbWUiOiJTaW5oYXJhIFJhbGFsYWdlIiwiaXNBY3RpdmUiOiJ0cnVlIiwianRpIjoiNGI5ZDdkZTMtNTQ1OC00NmVlLThjNzgtNGIzZjRhMjZkYTMwIiwiaWF0IjoxNzY0ODIzMjMwLCJuYmYiOjE3NjQ4MjMyMzAsImV4cCI6MTc2NDgyNTAzMCwiaXNzIjoiaHR0cHM6Ly9sYW5rYWNvbm5lY3QtYXBpLXN0YWdpbmcuYXp1cmV3ZWJzaXRlcy5uZXQiLCJhdWQiOiJodHRwczovL2xhbmthY29ubmVjdC1zdGFnaW5nLmF6dXJld2Vic2l0ZXMubmV0In0.5zSubeP3fRK4cOlTBOW_T2Qayg9EM3aUZhsFm-wQ8Qs"

EVENT_ID="0458806b-8672-4ad5-a7cb-f5346f1b282a"
IMAGE_ID="3f55cb35-2bb9-4748-8e0c-f843fc5d5723"

echo "Testing set-primary on staging..."
curl -X POST \
  "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events/${EVENT_ID}/images/${IMAGE_ID}/set-primary" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -w "\n\nStatus: %{http_code}\n" \
  2>&1
