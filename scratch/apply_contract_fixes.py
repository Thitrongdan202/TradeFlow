import re

# 1. Update App.razor
app_path = r'C:/Users/thitr/.gemini/antigravity/scratch/TradeFlow/src/TradeFlow.Web/Components/App.razor'
with open(app_path, 'r', encoding='utf-8') as f:
    app_content = f.read()

# Replace title logic
old_title_snippet = """                        var title = docTitle || 'Hop_dong_lao_dong_HDLD-2026-DRAFT';
                        if (title.endsWith('_')) { title += 'HDLD-2026-DRAFT'; }"""

new_title_snippet = """                        var rawTitle = docTitle || 'Hop_Dong_Lao_Dong';
                        rawTitle = rawTitle.replace(/_+$/, '');
                        if (!rawTitle.includes('HDLD')) { rawTitle += '_HDLD-2026-DRAFT'; }
                        var title = rawTitle;"""

if old_title_snippet in app_content:
    app_content = app_content.replace(old_title_snippet, new_title_snippet)
    print("App.razor title snippet replaced successfully")
else:
    print("WARNING: App.razor title snippet not matched")

# Replace printStyles
old_styles_pattern = re.compile(r"var printStyles = '<style>.*?</style>';", re.DOTALL)
new_print_styles = """var printStyles = '<style>' +
                            '@@page { size: A4 ' + orientation + '; margin: 0; }' +
                            '*, *::before, *::after { box-sizing: border-box !important; -webkit-print-color-adjust: exact !important; print-color-adjust: exact !important; }' +
                            'html, html[data-bs-theme="light"], html[data-theme="light"], html[data-bs-theme="dark"], html[data-theme="dark"], body, .tf-print-isolated-body { margin: 0 !important; padding: 0 !important; background: #ffffff !important; background-color: #ffffff !important; color: #000000 !important; font-family: "Times New Roman", Times, serif !important; font-size: 13pt !important; line-height: 1.25 !important; color-scheme: light !important; }' +
                            'body.tf-print-isolated-body { margin: 0 !important; padding: 0 !important; }' +
                            '.no-print, .tf-no-print, [data-no-print="true"], .tf-a4-page-divider, .tf-modal-stepper, .tf-preview-toolbar, .modal-header, .modal-footer { display: none !important; visibility: hidden !important; height: 0 !important; margin: 0 !important; padding: 0 !important; }' +
                            '.tf-preview-viewport-scroll, .tf-preview-zoom-wrapper, #print-employment-contract, .tf-contract-modal-body, .modal-body, .modal-content, .modal-dialog, .modal { background: transparent !important; background-color: transparent !important; border: none !important; border-radius: 0 !important; box-shadow: none !important; padding: 0 !important; margin: 0 auto !important; min-height: 0 !important; height: auto !important; width: 100% !important; max-width: 100% !important; transform: none !important; overflow: visible !important; }' +
                            '.tf-document-paper, .a4-contract-sheet, .a4-contract-page, .tf-a4-page, .tf-inspect-a4-sheet, .tf-a4-preview-canvas, .invoice-print-sheet, .don-dat-hang-sheet, .quotation-print-sheet, .voucher-a4-sheet, .stocktake-print-sheet { width: 210mm !important; max-width: 210mm !important; height: 297mm !important; min-height: 297mm !important; max-height: 297mm !important; margin: 0 auto !important; padding: 15mm 20mm 15mm 30mm !important; box-shadow: none !important; border: none !important; border-radius: 0 !important; transform: none !important; background: #ffffff !important; background-color: #ffffff !important; color: #000000 !important; box-sizing: border-box !important; display: flex !important; flex-direction: column !important; justify-content: space-between !important; position: relative !important; overflow: hidden !important; }' +
                            '.tf-a4-page-1, .tf-a4-page-2 { break-after: page !important; page-break-after: always !important; }' +
                            '.tf-a4-page-footer { margin-top: auto !important; padding-top: 4pt !important; border-top: 1px solid #cbd5e1 !important; display: flex !important; justify-content: space-between !important; font-size: 9pt !important; color: #64748b !important; }' +
                            '.clause-heading, .tf-clause-heading, .tf-clause-title, .tf-article-heading, [data-clause-heading], h1, h2, h3, h4, h5, h6 { break-after: avoid-page !important; page-break-after: avoid !important; }' +
                            '.clause-paragraph, .tf-clause-paragraph, .tf-clause-content, p { orphans: 3 !important; widows: 3 !important; text-align: justify !important; margin-bottom: 4pt !important; }' +
                            'table { width: 100% !important; border-collapse: collapse !important; color: #000000 !important; background-color: transparent !important; }' +
                            'th, td { color: #000000 !important; border-color: #000000 !important; }' +
                            'tr, .tf-signature-block, .signature-block, .tf-signature-section, .tf-print-avoid-break { break-inside: avoid !important; page-break-inside: avoid !important; }' +
                            '.tf-doc-party-box, .tf-doc-summary-box, .tf-legal-party-block, .tf-legal-summary-block, .bg-light, .table-light, thead.table-light th { background: transparent !important; background-color: transparent !important; }' +
                            '.tf-clause-item, .tf-article-block, .tf-inspect-clause, .tf-a4-party-block { break-inside: auto !important; page-break-inside: auto !important; margin-bottom: 3mm !important; }' +
                            '.tf-doc-national-header { text-align: center !important; margin-bottom: 14pt !important; font-family: "Times New Roman", Times, serif !important; line-height: 1.25 !important; }' +
                            '.tf-doc-nat-title { font-weight: 700 !important; font-size: 13pt !important; text-transform: uppercase !important; letter-spacing: 0.5px !important; margin-bottom: 2pt !important; text-align: center !important; color: #000000 !important; }' +
                            '.tf-doc-nat-motto { font-weight: 700 !important; font-size: 13pt !important; margin-bottom: 4pt !important; text-align: center !important; color: #000000 !important; }' +
                            '.tf-doc-nat-line { width: 140px !important; height: 1.5px !important; background-color: #000000 !important; margin: 0 auto 8pt auto !important; display: block !important; }' +
                            '.tf-doc-nat-date { font-style: italic !important; font-size: 13pt !important; color: #1e293b !important; text-align: center !important; }' +
                            '</style>';"""

if old_styles_pattern.search(app_content):
    app_content = old_styles_pattern.sub(new_print_styles, app_content)
    print("App.razor printStyles updated successfully")
else:
    print("WARNING: App.razor printStyles pattern not matched")

with open(app_path, 'w', encoding='utf-8') as f:
    f.write(app_content)

# 2. Update EmploymentContractFormModal.razor
modal_path = r'C:/Users/thitr/.gemini/antigravity/scratch/TradeFlow/src/TradeFlow.Web/Components/Pages/Contracts/EmploymentContractFormModal.razor'
with open(modal_path, 'r', encoding='utf-8') as f:
    modal_content = f.read()

# Update print call in PrintContract()
old_print_call = 'await JS.InvokeVoidAsync("tradeflow.printDocument", "print-employment-contract", $"Hop_Dong_Lao_Dong_{ContractNumber}");'
new_print_call = 'await JS.InvokeVoidAsync("tradeflow.printDocument", "print-employment-contract", $"Hop_Dong_Lao_Dong_{(string.IsNullOrWhiteSpace(ContractNumber) ? "HDLD-2026-DRAFT" : ContractNumber.Trim())}");'

if old_print_call in modal_content:
    modal_content = modal_content.replace(old_print_call, new_print_call)
    print("EmploymentContractFormModal.razor print call updated")

# Update clause splitting
old_clauses_code = """    private IEnumerable<EmploymentContractClauseDto> GetEmpFormClausesPage1()
    {
        if (model.Clauses == null) return Enumerable.Empty<EmploymentContractClauseDto>();
        return model.Clauses.OrderBy(c => c.DisplayOrder).Take(1);
    }

    private IEnumerable<EmploymentContractClauseDto> GetEmpFormClausesPage2()
    {
        if (model.Clauses == null) return Enumerable.Empty<EmploymentContractClauseDto>();
        return model.Clauses.OrderBy(c => c.DisplayOrder).Skip(1).Take(5);
    }

    private IEnumerable<EmploymentContractClauseDto> GetEmpFormClausesPage3()
    {
        if (model.Clauses == null) return Enumerable.Empty<EmploymentContractClauseDto>();
        return model.Clauses.OrderBy(c => c.DisplayOrder).Skip(6);
    }"""

new_clauses_code = """    private IEnumerable<EmploymentContractClauseDto> GetEmpFormClausesPage1()
    {
        if (model.Clauses == null) return Enumerable.Empty<EmploymentContractClauseDto>();
        return model.Clauses.OrderBy(c => c.DisplayOrder).Take(1);
    }

    private IEnumerable<EmploymentContractClauseDto> GetEmpFormClausesPage2()
    {
        if (model.Clauses == null) return Enumerable.Empty<EmploymentContractClauseDto>();
        return model.Clauses.OrderBy(c => c.DisplayOrder).Skip(1).Take(6);
    }

    private IEnumerable<EmploymentContractClauseDto> GetEmpFormClausesPage3()
    {
        if (model.Clauses == null) return Enumerable.Empty<EmploymentContractClauseDto>();
        return model.Clauses.OrderBy(c => c.DisplayOrder).Skip(7);
    }"""

if old_clauses_code in modal_content:
    modal_content = modal_content.replace(old_clauses_code, new_clauses_code)
    print("EmploymentContractFormModal.razor clause splitting updated")
else:
    print("WARNING: EmploymentContractFormModal.razor clause code not matched")

with open(modal_path, 'w', encoding='utf-8') as f:
    f.write(modal_content)

# 3. Update EmploymentContractFormModal.razor.css
css_path = r'C:/Users/thitr/.gemini/antigravity/scratch/TradeFlow/src/TradeFlow.Web/Components/Pages/Contracts/EmploymentContractFormModal.razor.css'
with open(css_path, 'r', encoding='utf-8') as f:
    css_content = f.read()

print_css = """

/* ============================================================
   PRINT MEDIA RULES - ZERO GRAY BOXES & PERFECT A4 ISOLATION
   ============================================================ */
@media print {
  .tf-modal-stepper,
  .tf-preview-toolbar,
  .modal-header,
  .modal-footer,
  .no-print,
  .tf-a4-page-divider {
    display: none !important;
    visibility: hidden !important;
    height: 0 !important;
    margin: 0 !important;
    padding: 0 !important;
  }

  .tf-preview-viewport-scroll,
  .tf-preview-zoom-wrapper {
    background: transparent !important;
    background-color: transparent !important;
    border: none !important;
    box-shadow: none !important;
    border-radius: 0 !important;
    min-height: 0 !important;
    height: auto !important;
    padding: 0 !important;
    margin: 0 !important;
    overflow: visible !important;
    transform: none !important;
  }

  .tf-a4-preview-canvas,
  .tf-printable-contract-canvas {
    box-shadow: none !important;
    margin: 0 auto !important;
    background: #ffffff !important;
    background-color: #ffffff !important;
    color: #000000 !important;
  }

  .tf-signature-block,
  .signature-block {
    break-inside: avoid !important;
    page-break-inside: avoid !important;
  }
}
"""

if "@media print" not in css_content:
    css_content += print_css
    with open(css_path, 'w', encoding='utf-8') as f:
        f.write(css_content)
    print("EmploymentContractFormModal.razor.css print rules appended")
else:
    print("EmploymentContractFormModal.razor.css already has @media print")

print("All updates applied cleanly!")
