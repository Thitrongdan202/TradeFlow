const fs = require('fs');
let content = fs.readFileSync('src/TradeFlow.Web/Components/Layout/NavMenu.razor', 'utf8');

console.log('NavMenu length:', content.length);
console.log('Contains IsInvoiceOpen:', content.includes('IsInvoiceOpen'));
console.log('Contains IsCatalogOpen:', content.includes('IsCatalogOpen'));
console.log('Contains IsWarehouseOpen:', content.includes('IsWarehouseOpen'));
console.log('Contains IsPartnersOpen:', content.includes('IsPartnersOpen'));
console.log('Contains IsDocumentsOpen:', content.includes('IsDocumentsOpen'));
console.log('Contains IsEmployeesOpen:', content.includes('IsEmployeesOpen'));
console.log('Contains IsSettingsOpen:', content.includes('IsSettingsOpen'));
