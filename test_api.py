import requests
import json

# Auth token
TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiI1ZTc4MmI0ZC0yOWVkLTRlMWQtOTAzOS02YzhmNjk4YWVlYTkiLCJlbWFpbCI6Im5pcm9zaGhoQGdtYWlsLmNvbSIsInVuaXF1ZV9uYW1lIjoiTmlyb3NoYW5hIFNpbmhhcmFnZSIsInJvbGUiOiJFdmVudE9yZ2FuaXplciIsImZpcnN0TmFtZSI6Ik5pcm9zaGFuYSIsImxhc3ROYW1lIjoiU2luaGFyYWdlIiwiaXNBY3RpdmUiOiJ0cnVlIiwiaXNFbWFpbFZlcmlmaWVkIjoidHJ1ZSIsImp0aSI6ImMwZjQxYjdlLWFhY2QtNGIyOS1hMjM0LTk1M2Q4NDZlYjkyNSIsImlhdCI6MTc2OTM1NjMyNywiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbW9iaWxlcGhvbmUiOiI4NjA5NzgwMTI0IiwibmJmIjoxNzY5MzU2MzI3LCJleHAiOjE3NjkzNTgxMjcsImlzcyI6Imh0dHBzOi8vbGFua2Fjb25uZWN0LWFwaS1zdGFnaW5nLmF6dXJld2Vic2l0ZXMubmV0IiwiYXVkIjoiaHR0cHM6Ly9sYW5rYWNvbm5lY3Qtc3RhZ2luZy5henVyZXdlYnNpdGVzLm5ldCJ9.8wl7SSawqh7wN6ru4gyo6AClbBiJigX9gbWRyQks068"

BASE_URL = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"

headers = {
    "Authorization": f"Bearer {TOKEN}",
    "Content-Type": "application/json"
}

# Get all events
print("Fetching events...")
response = requests.get(f"{BASE_URL}/api/Events", headers=headers)
events = response.json()

print(f"Found {len(events)} events")
print("\nSearching for events with null pricing (ticketPriceAmount=null AND pricingType=null)...")

# Find events with null pricing
null_pricing_events = []
for event in events:
    if event.get('ticketPriceAmount') is None and event.get('pricingType') is None:
        null_pricing_events.append({
            'id': event['id'],
            'title': event['title'],
            'isFree': event.get('isFree'),
            'ticketPriceAmount': event.get('ticketPriceAmount'),
            'pricingType': event.get('pricingType'),
            'status': event.get('status')
        })

print(f"\nFound {len(null_pricing_events)} events with null pricing:")
for event in null_pricing_events[:5]:  # Show first 5
    print(json.dumps(event, indent=2))

# Also search for "Christmas" in titles
print("\n\nSearching for Christmas events...")
christmas_events = [e for e in events if 'christmas' in e.get('title', '').lower()]
print(f"Found {len(christmas_events)} Christmas events:")
for event in christmas_events:
    print(json.dumps({
        'id': event['id'],
        'title': event['title'],
        'isFree': event.get('isFree'),
        'ticketPriceAmount': event.get('ticketPriceAmount'),
        'pricingType': event.get('pricingType'),
        'status': event.get('status')
    }, indent=2))
