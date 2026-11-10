const { Client } = require('pg');
const client = new Client({
    connectionString: 'postgresql://postgres:postgres@localhost:5432/tradeflow_dev'
});
(async () => {
    try {
        await client.connect();
        const res = await client.query('UPDATE "Users" SET "LockoutEnd" = NULL, "AccessFailedCount" = 0');
        console.log('Unlocked all users in Users, row count:', res.rowCount);
    } catch (err) {
        console.error(err);
    } finally {
        await client.end();
    }
})();
