const { Client } = require('pg');
const client = new Client({ connectionString: 'postgresql://postgres:postgres@localhost:5432/tradeflow_dev' });

async function check() {
  await client.connect();
  const res = await client.query("SELECT table_name FROM information_schema.tables WHERE table_schema='public'");
  console.log('Tables:', res.rows.map(r => r.table_name).sort());
  const resCol = await client.query("SELECT column_name FROM information_schema.columns WHERE table_name='Roles'");
  console.log('Roles cols:', resCol.rows.map(r => r.column_name));
  const resPerm = await client.query("SELECT column_name FROM information_schema.columns WHERE table_name='RolePermissions'");
  console.log('RolePermissions cols:', resPerm.rows.map(r => r.column_name));
  await client.end();
}

check().catch(console.error);
