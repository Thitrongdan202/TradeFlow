const { Client } = require('pg');
const client = new Client({ connectionString: 'postgresql://postgres:postgres@localhost:5432/tradeflow_dev' });

async function run() {
    await client.connect();
    const cols = await client.query("SELECT column_name, data_type FROM information_schema.columns WHERE table_name = 'AspNetUsers' AND column_name IN ('EncryptedPassword', 'PasswordChangedAt', 'MustChangePassword', 'Status')");
    console.log('Columns in AspNetUsers:', cols.rows);

    const users = await client.query('SELECT "UserName", "Status", "MustChangePassword", "PasswordChangedAt", "EncryptedPassword" FROM "AspNetUsers" ORDER BY "UserName" LIMIT 10');
    console.log('Users sample:', users.rows);

    await client.end();
}

run().catch(console.error);
