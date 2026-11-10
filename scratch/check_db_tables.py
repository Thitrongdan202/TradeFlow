import psycopg2

conn = psycopg2.connect('host=localhost port=5432 dbname=tradeflow_dev user=postgres password=postgres')
cur = conn.cursor()
cur.execute("SELECT table_name FROM information_schema.tables WHERE table_schema='public' ORDER BY table_name;")
tables = [row[0] for row in cur.fetchall()]
print(f"Total tables: {len(tables)}")
role_tables = [t for t in tables if any(k in t.lower() for k in ['role', 'perm', 'user', 'temp'])]
print(f"Security tables: {role_tables}")

# Check columns of AspNetRoles
cur.execute("SELECT column_name, data_type FROM information_schema.columns WHERE table_name='AspNetRoles';")
print("\nAspNetRoles columns:")
for col, dt in cur.fetchall():
    print(f"  {col}: {dt}")

# Check columns of RolePermissions
cur.execute("SELECT column_name, data_type FROM information_schema.columns WHERE table_name='RolePermissions';")
print("\nRolePermissions columns:")
for col, dt in cur.fetchall():
    print(f"  {col}: {dt}")

# Check current roles
cur.execute("SELECT \"Id\", \"Name\", \"Description\", \"IsSystem\", \"IsActive\" FROM \"AspNetRoles\";")
print("\nCurrent Roles in DB:")
for r in cur.fetchall():
    print(f"  {r}")

conn.close()
