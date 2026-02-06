#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Check if 'All Locations' is properly handled by populating all metro areas
"""

import psycopg2
import sys
import io

if sys.stdout.encoding != 'utf-8':
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

conn_string = "host=lankaconnect-staging-db.postgres.database.azure.com dbname=LankaConnectDB user=adminuser password=1qaz!QAZ sslmode=require"

newsletter_id = 'a595d9bc-bc1b-4a17-b138-9c1f081a5992'

try:
    conn = psycopg2.connect(conn_string)
    cur = conn.cursor()

    print('=' * 100)
    print('PART 1: CHECK NEWSLETTER METRO AREAS JUNCTION TABLE')
    print('=' * 100)

    cur.execute('''
        SELECT COUNT(*)
        FROM communications.newsletter_metro_areas
        WHERE newsletter_id = %s::uuid
    ''', (newsletter_id,))

    newsletter_metro_count = cur.fetchone()[0]

    print(f'\nNewsletter ID: {newsletter_id}')
    print(f'target_all_locations: TRUE (verified earlier)')
    print(f'Metro areas in junction table: {newsletter_metro_count}')
    print(f'Expected (if properly handled): 84')

    if newsletter_metro_count == 0:
        print('\nRESULT: BROKEN - Newsletter has target_all_locations=TRUE but 0 metros in junction table!')
    elif newsletter_metro_count == 84:
        print('\nRESULT: CORRECT - All 84 metros populated')
    else:
        print(f'\nRESULT: PARTIAL - Only {newsletter_metro_count} metros, should be 84')

    print('\n' + '=' * 100)
    print('PART 2: CHECK SUBSCRIBER METRO AREAS JUNCTION TABLE')
    print('=' * 100)

    cur.execute('''
        SELECT
            ns.id,
            ns.email,
            ns.receive_all_locations,
            COUNT(nsma.metro_area_id) as metro_count
        FROM communications.newsletter_subscribers ns
        LEFT JOIN communications.newsletter_subscriber_metro_areas nsma ON ns.id = nsma.subscriber_id
        WHERE ns.is_confirmed = TRUE AND ns.is_active = TRUE
        GROUP BY ns.id, ns.email, ns.receive_all_locations
        ORDER BY ns.email
    ''')

    subscribers = cur.fetchall()
    print(f'\nTotal Confirmed Active Subscribers: {len(subscribers)}\n')

    all_good = True
    for sub in subscribers:
        print(f'Subscriber: {sub[1]}')
        print(f'  receive_all_locations: {sub[2]}')
        print(f'  Metro areas in junction table: {sub[3]}')

        if sub[2]:  # receive_all_locations = TRUE
            print(f'  Expected: 84')
            if sub[3] == 84:
                print(f'  RESULT: CORRECT - All metros populated')
            else:
                print(f'  RESULT: BROKEN - Should have 84 metros!')
                all_good = False
        else:
            print(f'  RESULT: OK - User selected {sub[3]} specific metro(s)')
        print()

    print('=' * 100)
    print('PART 3: CHECK ALL NEWSLETTERS WITH target_all_locations = TRUE')
    print('=' * 100)

    cur.execute('''
        SELECT
            n.id,
            n.title,
            n.target_all_locations,
            COUNT(nm.metro_area_id) as metro_count
        FROM communications.newsletters n
        LEFT JOIN communications.newsletter_metro_areas nm ON n.id = nm.newsletter_id
        WHERE n.target_all_locations = TRUE
        GROUP BY n.id, n.title, n.target_all_locations
        ORDER BY n.created_at DESC
    ''')

    all_location_newsletters = cur.fetchall()
    print(f'\nTotal newsletters with target_all_locations=TRUE: {len(all_location_newsletters)}\n')

    broken_count = 0
    for nl in all_location_newsletters:
        print(f'Newsletter: {nl[1][:50]}...' if len(nl[1]) > 50 else f'Newsletter: {nl[1]}')
        print(f'  ID: {nl[0]}')
        print(f'  Metro areas: {nl[3]} (expected: 84)')

        if nl[3] == 0:
            print(f'  RESULT: BROKEN - 0 metros!')
            broken_count += 1
        elif nl[3] == 84:
            print(f'  RESULT: CORRECT')
        else:
            print(f'  RESULT: PARTIAL - Should be 84')
            broken_count += 1
        print()

    print('=' * 100)
    print('PART 4: CHECK ALL SUBSCRIBERS WITH receive_all_locations = TRUE')
    print('=' * 100)

    cur.execute('''
        SELECT
            ns.id,
            ns.email,
            ns.receive_all_locations,
            COUNT(nsma.metro_area_id) as metro_count
        FROM communications.newsletter_subscribers ns
        LEFT JOIN communications.newsletter_subscriber_metro_areas nsma ON ns.id = nsma.subscriber_id
        WHERE ns.receive_all_locations = TRUE
        GROUP BY ns.id, ns.email, ns.receive_all_locations
        ORDER BY ns.created_at DESC
    ''')

    all_location_subscribers = cur.fetchall()
    print(f'\nTotal subscribers with receive_all_locations=TRUE: {len(all_location_subscribers)}\n')

    if len(all_location_subscribers) == 0:
        print('No subscribers with receive_all_locations=TRUE found.')
        print('This is OK - users have only selected specific metros.')
    else:
        sub_broken_count = 0
        for sub in all_location_subscribers:
            print(f'Subscriber: {sub[1]}')
            print(f'  Metro areas: {sub[3]} (expected: 84)')

            if sub[3] == 0:
                print(f'  RESULT: BROKEN - 0 metros!')
                sub_broken_count += 1
            elif sub[3] == 84:
                print(f'  RESULT: CORRECT')
            else:
                print(f'  RESULT: PARTIAL - Should be 84')
                sub_broken_count += 1
            print()

    print('=' * 100)
    print('SUMMARY')
    print('=' * 100)
    print(f'\n1. Newsletter Creation with "All Locations":')
    print(f'   - Total newsletters with target_all_locations=TRUE: {len(all_location_newsletters)}')
    print(f'   - Broken (0 or partial metros): {broken_count}')
    print(f'   - Status: {"ALL BROKEN - None populate metros correctly!" if broken_count == len(all_location_newsletters) and len(all_location_newsletters) > 0 else "Some working correctly"}')

    print(f'\n2. Subscriber Registration with "All Locations":')
    print(f'   - Total subscribers with receive_all_locations=TRUE: {len(all_location_subscribers)}')
    if len(all_location_subscribers) > 0:
        print(f'   - Broken (0 or partial metros): {sub_broken_count}')
        print(f'   - Status: {"ALL BROKEN - None populate metros correctly!" if sub_broken_count == len(all_location_subscribers) else "Some working correctly"}')
    else:
        print(f'   - Status: No subscribers selected "All Locations" yet (cannot test)')

    print(f'\n3. Matching Logic Impact:')
    print(f'   - Newsletter.MetroAreaIds (empty) ∩ Subscriber.MetroAreaIds (specific) = NO MATCH')
    print(f'   - This causes 0 recipients resolved → Job exits early → No emails sent')

    print(f'\n4. Root Cause:')
    print(f'   - When user selects "All Locations" in UI, backend does NOT populate all 84 metro areas')
    print(f'   - Boolean flag target_all_locations/receive_all_locations is set correctly')
    print(f'   - BUT junction tables remain EMPTY')
    print(f'   - Matching logic relies on junction tables, not boolean flags')

    print(f'\n5. Fix Required:')
    print(f'   - CreateNewsletterCommandHandler: If target_all_locations=TRUE, populate all 84 metros')
    print(f'   - Subscriber creation endpoint: If receive_all_locations=TRUE, populate all 84 metros')
    print(f'   - Both need to query events.metro_areas and insert into junction tables')

    cur.close()
    conn.close()

except Exception as e:
    print(f'\nERROR: {e}')
    import traceback
    traceback.print_exc()
    sys.exit(1)