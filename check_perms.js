const fs = require('fs');
const txt = fs.readFileSync('src/TradeFlow.Infrastructure/Persistence/DatabaseSeeder.cs', 'utf8');
const perms = txt.match(/"Permission:[^"]+"/g) || [];
console.log(Array.from(new Set(perms)).filter(p => p.includes('Employee') || p.includes('HR') || p.includes('Request')));
