import psycopg2

try:
    conn = psycopg2.connect("host=localhost dbname=tradeflow_dev user=postgres password=postgres")
    cur = conn.cursor()
    cur.execute("SELECT column_name, data_type, is_nullable FROM information_schema.columns WHERE table_name = 'Users' ORDER BY ordinal_position;")
    rows = cur.fetchall()
    print("Columns in 'Users' table:")
    for col, dtype, nullable in rows:
        print(f"  {col}: {dtype} (nullable: {nullable})")
    
    cur.execute("SELECT Id, UserName, Email, Status FROM \"Users\";")
    print("\nExisting users:")
    for user in cur.fetchall():
        print(f"  {user}")

    cur.execute("SELECT column_name, data_type FROM information_schema.columns WHERE table_name = 'Employees' ORDER BY ordinal_position;")
    print("\nColumns in 'Employees' table:")
    for col, dtype in cur.fetchall():
        print(f"  {col}: {dtype}")

    conn.close()
except Exception as e:
    print(f"Error: {e}")
