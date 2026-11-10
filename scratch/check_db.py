import psycopg2

conn = psycopg2.connect('host=localhost dbname=tradeflow_dev user=postgres password=postgres')
cur = conn.cursor()
cur.execute("SELECT table_name FROM information_schema.tables WHERE table_schema='public'")
rows = cur.fetchall()
tables = [r[0] for r in rows]
print('Existing tables:', sorted(tables))

cur.execute("SELECT column_name FROM information_schema.columns WHERE table_name='Roles'")
print('Roles cols:', [r[0] for r in cur.fetchall()])

cur.execute("SELECT column_name FROM information_schema.columns WHERE table_name='RolePermissions'")
print('RolePermissions cols:', [r[0] for r in cur.fetchall()])

conn.close()
