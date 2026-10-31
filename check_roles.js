const fs = require('fs');
const path = require('path');

const seeder = fs.readFileSync('src/TradeFlow.Infrastructure/Persistence/DatabaseSeeder.cs', 'utf8');
const rolesMatch = seeder.match(/new IdentityRole[^\)]+\)/g);
console.log('Roles from Seeder:');
const roleLines = seeder.split('\n').filter(l => l.includes('Role') || l.includes('Claim') || l.includes('Policy') || l.includes('HR'));
console.log(roleLines.slice(0, 30).join('\n'));
