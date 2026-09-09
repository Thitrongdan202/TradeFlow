const fs = require('fs');
let code = fs.readFileSync('src/TradeFlow.Web/Components/Pages/Sales/Invoices/Detail.razor', 'utf8');

// Find the precise IssueInvoice button string
let issueBtnMatch = '<button class="tf-btn tf-btn-warning" @onclick="IssueInvoice" disabled="@_isProcessing">Phát hành</button>';
if(!code.includes(issueBtnMatch)) {
    // maybe utf-8 encoding issue
    issueBtnMatch = '<button class="tf-btn tf-btn-warning" @onclick="IssueInvoice" disabled="@_isProcessing">Ph\u00E1t h\u00E0nh</button>';
}

let newHtml = `<a href="/hoa-don/sua/@Id" class="tf-btn tf-btn-outline-primary" style="margin-right:8px;">Sửa</a>\n                        <button class="tf-btn tf-btn-warning" @onclick="IssueInvoice" disabled="@_isProcessing">Phát hành</button>`;

if(code.includes(issueBtnMatch)) {
    code = code.replace(issueBtnMatch, newHtml);
} else {
    // just replace disabled="@_isProcessing">Ph
    let looseMatch = /<button class="tf-btn tf-btn-warning" @onclick="IssueInvoice" disabled="@_isProcessing">Ph.+t h.+nh<\/button>/i;
    code = code.replace(looseMatch, newHtml);
}

fs.writeFileSync('src/TradeFlow.Web/Components/Pages/Sales/Invoices/Detail.razor', code);
