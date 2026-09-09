const fs = require('fs');
let code = fs.readFileSync('src/TradeFlow.Web/Components/Pages/Sales/Invoices/List.razor', 'utf8');

let replaceStr = `<td class="tf-text-right">
                                <a href="/hoa-don/chi-tiet/@inv.Id" class="tf-btn tf-btn-sm tf-btn-outline-primary">Chi tiết</a>
                            </td>`;

let newHtml = `<td class="tf-text-right">
                                @if (inv.Status == InvoiceStatus.Draft)
                                {
                                    <a href="/hoa-don/sua/@inv.Id" class="tf-btn tf-btn-sm tf-btn-outline-secondary" style="margin-right:4px;">Sửa</a>
                                }
                                <a href="/hoa-don/chi-tiet/@inv.Id" class="tf-btn tf-btn-sm tf-btn-outline-primary">Chi tiết</a>
                            </td>`;

if(code.includes(replaceStr)) {
    code = code.replace(replaceStr, newHtml);
} else {
    // try regex
    let looseMatch = /<td class="tf-text-right">\s*<a href="\/hoa-don\/chi-tiet\/@inv\.Id" class="tf-btn tf-btn-sm tf-btn-outline-primary">Chi ti.+t<\/a>\s*<\/td>/;
    code = code.replace(looseMatch, newHtml);
}

fs.writeFileSync('src/TradeFlow.Web/Components/Pages/Sales/Invoices/List.razor', code);
