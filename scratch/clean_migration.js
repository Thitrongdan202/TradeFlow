const fs = require('fs');
let content = fs.readFileSync('C:/Users/thitr/.gemini/antigravity/scratch/TradeFlow/src/TradeFlow.Infrastructure/Migrations/20261109183403_AddInternalSupportSystem.cs', 'utf8');

// 1. Remove AddColumn for ColorHex, Icon, Scope
const upColsRegex = /migrationBuilder\.AddColumn<string>[\s\S]*?defaultValue: ""\);\s*migrationBuilder\.AddColumn<string>[\s\S]*?defaultValue: ""\);\s*migrationBuilder\.AddColumn<int>[\s\S]*?defaultValue: 0\);\s*/;
content = content.replace(upColsRegex, '');

// 2. Remove CreateTable for TemporaryRoleAssignments
const tempRoleRegex = /migrationBuilder\.CreateTable\(\s*name: "TemporaryRoleAssignments"[\s\S]*?\}\);\s*/;
content = content.replace(tempRoleRegex, '');

// 3. Remove indexes for TemporaryRoleAssignments
const tempIndexRegex = /migrationBuilder\.CreateIndex\(\s*name: "IX_TemporaryRoleAssignments[\s\S]*?\);\s*/g;
content = content.replace(tempIndexRegex, '');

// 4. Remove DropTable for TemporaryRoleAssignments and DropColumns from Down
const downDropsRegex = /migrationBuilder\.DropTable\(\s*name: "TemporaryRoleAssignments"\);\s*/;
content = content.replace(downDropsRegex, '');

const downColsRegex = /migrationBuilder\.DropColumn\(\s*name: "ColorHex"[\s\S]*?table: "RolePermissions"\);\s*/;
content = content.replace(downColsRegex, '');

fs.writeFileSync('C:/Users/thitr/.gemini/antigravity/scratch/TradeFlow/src/TradeFlow.Infrastructure/Migrations/20261109183403_AddInternalSupportSystem.cs', content, 'utf8');
console.log('Migration cleaned up successfully!');
