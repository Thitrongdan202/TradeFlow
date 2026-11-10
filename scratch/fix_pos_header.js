const fs = require('fs');
let s = fs.readFileSync('src/TradeFlow.Web/Components/Pages/Sales/Pos/PosIndex.razor', 'utf8');

const target1 = '<header class="tf-sales-header">';
const repl1 = '<div class="tf-sales-toolbar tf-sales-header">';

const target2 = '</header>';
const repl2 = '</div>';

if (s.includes(target1) && s.includes(target2)) {
    s = s.replace(target1, repl1);
    s = s.replace(target2, repl2);
    fs.writeFileSync('src/TradeFlow.Web/Components/Pages/Sales/Pos/PosIndex.razor', s, 'utf8');
    console.log('Successfully updated PosIndex.razor to div.tf-sales-toolbar');
} else {
    console.error('Target not found in PosIndex.razor');
}
