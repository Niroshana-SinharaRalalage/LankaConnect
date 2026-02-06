#!/usr/bin/env python3
"""
Debug script to analyze search_vector tokens for events with 'Varuni' in title.
This helps diagnose inconsistent search behavior.
"""
import psycopg2

conn_string = "host=lankaconnect-staging-db.postgres.database.azure.com dbname=LankaConnectDB user=adminuser password=1qaz!QAZ sslmode=require"

try:
    conn = psycopg2.connect(conn_string)
    cursor = conn.cursor()

    # Find events with "Varuni" in title and analyze their search_vector
    print("=" * 80)
    print("SEARCH VECTOR ANALYSIS FOR EVENTS WITH 'Varuni' IN TITLE")
    print("=" * 80)

    cursor.execute("""
        SELECT
            "Id",
            title,
            "Status",
            search_vector::text as search_vector_text,
            LENGTH(title) as title_length
        FROM events.events
        WHERE title ILIKE '%Varuni%'
        ORDER BY title;
    """)

    events = cursor.fetchall()
    print(f"\nFound {len(events)} events matching 'Varuni':\n")

    for event in events:
        event_id, title, status, search_vector, title_len = event
        print(f"ID: {event_id}")
        print(f"Title: '{title}'")
        print(f"Title Length: {title_len} chars")
        print(f"Status: {status}")
        print(f"Search Vector: {search_vector}")
        print("-" * 80)

    # Test websearch_to_tsquery with both search terms
    print("\n" + "=" * 80)
    print("WEBSEARCH_TO_TSQUERY ANALYSIS")
    print("=" * 80)

    test_queries = [
        "Sample group tier testing - Varu",
        "Sample group tier testing - Varuni",
        "Varuni",
        "Varuni11",
        "Varu"
    ]

    for query in test_queries:
        cursor.execute("""
            SELECT websearch_to_tsquery('english', %s)::text;
        """, (query,))
        result = cursor.fetchone()[0]
        print(f"\nQuery: '{query}'")
        print(f"  tsquery: {result}")

    # Test which events match each search
    print("\n" + "=" * 80)
    print("SEARCH RESULTS COMPARISON")
    print("=" * 80)

    for query in ["Sample group tier testing - Varu", "Sample group tier testing - Varuni"]:
        cursor.execute("""
            SELECT "Id", title, "Status"
            FROM events.events
            WHERE search_vector @@ websearch_to_tsquery('english', %s)
              AND "Status" IN ('Published', 'Cancelled')
            ORDER BY title;
        """, (query,))

        results = cursor.fetchall()
        print(f"\nSearch: '{query}'")
        print(f"  Results: {len(results)} events")
        for r in results:
            print(f"    - [{r[2]}] {r[1]}")

    # Check for any whitespace/encoding issues in titles
    print("\n" + "=" * 80)
    print("CHECKING FOR WHITESPACE/ENCODING ISSUES")
    print("=" * 80)

    cursor.execute("""
        SELECT
            "Id",
            title,
            encode(title::bytea, 'hex') as title_hex,
            array_to_string(regexp_split_to_array(title, ''), ' ') as char_by_char
        FROM events.events
        WHERE title ILIKE '%Varuni%'
        ORDER BY title;
    """)

    for row in cursor.fetchall():
        event_id, title, hex_val, char_by_char = row
        print(f"\nID: {event_id}")
        print(f"Title: '{title}'")
        print(f"Hex: {hex_val[:100]}..." if len(hex_val) > 100 else f"Hex: {hex_val}")

    cursor.close()
    conn.close()
    print("\n" + "=" * 80)
    print("Analysis complete!")

except Exception as e:
    print(f"Error: {e}")
    import traceback
    traceback.print_exc()
