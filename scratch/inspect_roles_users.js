const { Client } = require('pg');

async function main() {
    const client = new Client({
        connectionString: 'postgresql://postgres:postgres@localhost:5432/tradeflow_dev'
    });
    await client.connect();

    console.log('=== ROLES ===');
    const rolesRes = await client.query('SELECT "Id", "Name", "Description", "IsActive", "ColorHex", "Icon" FROM "Roles" ORDER BY "Name"');
    console.table(rolesRes.rows);

    console.log('=== USER ROLES ===');
    const urRes = await client.query(`
        SELECT u."UserName", r."Name" as "RoleName"
        FROM "UserRoles" ur
        JOIN "Users" u ON ur."UserId" = u."Id"
        JOIN "Roles" r ON ur."RoleId" = r."Id"
        ORDER BY u."UserName", r."Name"
    `);
    console.table(urRes.rows);

    console.log('=== TEMPORARY ROLE ASSIGNMENTS ===');
    const traRes = await client.query('SELECT * FROM "TemporaryRoleAssignments"');
    console.table(traRes.rows);

    await client.end();
}

main().catch(console.error);
