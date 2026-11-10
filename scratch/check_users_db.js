const { Client } = require('pg');
const client = new Client({ connectionString: 'postgresql://postgres:postgres@localhost:5432/tradeflow_dev' });
(async () => {
    await client.connect();
    const res = await client.query('SELECT u."UserName", u."EmployeeId", e."FullName", e."DepartmentId", d."Name" as dept_name FROM "Users" u LEFT JOIN "Employees" e ON u."EmployeeId" = e."Id" LEFT JOIN "Departments" d ON e."DepartmentId" = d."Id" WHERE u."UserName" IN (\'admin\', \'nv01\', \'quanly01\')');
    console.table(res.rows);
    const tickets = await client.query('SELECT "Id", "Code", "Title", "CreatedByUserName", "SubmitterDepartment" FROM "SupportTickets"');
    console.table(tickets.rows);
    await client.end();
})();
