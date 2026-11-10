import psycopg2

conn = psycopg2.connect('host=localhost port=5432 dbname=tradeflow_dev user=postgres password=postgres')
cur = conn.cursor()
cur.execute('''
    SELECT u."Id", u."UserName", u."Email", u."FullName", u."Status", u."MustChangePassword", u."PasswordChangedAt", u."EncryptedPassword", r."Name"
    FROM "AspNetUsers" u
    LEFT JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
    LEFT JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
    ORDER BY u."UserName"
''')
for r in cur.fetchall():
    print(r)
conn.close()
