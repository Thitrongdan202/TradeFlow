import psycopg2

conn = psycopg2.connect('host=127.0.0.1 port=5432 dbname=tradeflow user=postgres password=postgres')
cur = conn.cursor()

cur.execute('SELECT "Id", "UserName" FROM "AspNetUsers"')
print('Users:', cur.fetchall())

cur.execute('SELECT "Id", "Name", "Description" FROM "AspNetRoles"')
print('Roles:', cur.fetchall())

cur.execute('''
    SELECT u."UserName", r."Name" 
    FROM "AspNetUserRoles" ur 
    JOIN "AspNetUsers" u ON ur."UserId" = u."Id" 
    JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
''')
print('UserRoles:', cur.fetchall())

cur.execute('''
    SELECT r."Name", COUNT(*) 
    FROM "RolePermissions" rp 
    JOIN "AspNetRoles" r ON rp."RoleId" = r."Id" 
    GROUP BY r."Name"
''')
print('Permissions per role:', cur.fetchall())

cur.execute('''
    SELECT r."Name", rp."Resource", rp."Action" 
    FROM "RolePermissions" rp 
    JOIN "AspNetRoles" r ON rp."RoleId" = r."Id" 
    WHERE r."Name" = 'Employee'
''')
print('Employee perms:', cur.fetchall())

cur.execute('''
    SELECT r."Name", rp."Resource", rp."Action" 
    FROM "RolePermissions" rp 
    JOIN "AspNetRoles" r ON rp."RoleId" = r."Id" 
    WHERE r."Name" = 'Sales'
''')
print('Sales perms:', cur.fetchall())

cur.execute('''
    SELECT r."Name", rp."Resource", rp."Action" 
    FROM "RolePermissions" rp 
    JOIN "AspNetRoles" r ON rp."RoleId" = r."Id" 
    WHERE r."Name" = 'Manager'
''')
print('Manager perms:', cur.fetchall())
