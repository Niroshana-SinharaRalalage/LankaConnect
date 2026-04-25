#!/usr/bin/env python3
"""Phase 7C.2b follow-up — identify every email template on staging that
still contains a legacy flat location token, so we can size the gap
(Issue 3: Event Details + Newsletter emails still render "4314 Clark Ave,
Cleveland" flat)."""
import psycopg2

conn_params = {
    "host": "lankaconnect-staging-db.postgres.database.azure.com",
    "database": "LankaConnectDB",
    "user": "adminuser",
    "password": "1qaz!QAZ",
    "sslmode": "require",
}


def main():
    conn = psycopg2.connect(**conn_params)
    cur = conn.cursor()

    cur.execute(
        """
        SELECT name,
               length(html_template) AS len,
               (html_template LIKE '%{{EventLocation}}%') AS has_event_location,
               (html_template LIKE '%{{Location}}%' AND html_template NOT LIKE '%{{EventLocation}}%') AS has_location_variant,
               (html_template LIKE '%{{LocationName}}%') AS has_decomposed
          FROM communications.email_templates
      ORDER BY name;
        """
    )
    rows = cur.fetchall()
    print(f"{'template':72} {'len':>7}  flat  var  decomp")
    print("-" * 100)
    flat_total = 0
    decomp_total = 0
    unrelated = 0
    for name, length, flat, var, decomp in rows:
        flag_flat = "YES" if flat else "   "
        flag_var = "YES" if var else "   "
        flag_dec = "YES" if decomp else "   "
        print(f"{name:72} {length:>7}  {flag_flat}  {flag_var}   {flag_dec}")
        if flat or var:
            flat_total += 1
        if decomp:
            decomp_total += 1
        if not (flat or var or decomp):
            unrelated += 1
    print("-" * 100)
    print(f"templates with legacy flat token: {flat_total}")
    print(f"templates already decomposed:     {decomp_total}")
    print(f"templates with no location refs:  {unrelated}")
    cur.close()
    conn.close()


if __name__ == "__main__":
    main()
