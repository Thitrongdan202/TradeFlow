with open(r'C:/Users/thitr/.gemini/antigravity/scratch/TradeFlow/src/TradeFlow.Web/Components/Pages/Contracts/EmploymentContractFormModal.razor', 'r', encoding='utf-8') as f:
    c = f.read()

# Rebalance clauses back to Take(5) / Skip(6)
c = c.replace(
    'return model.Clauses.OrderBy(c => c.DisplayOrder).Skip(1).Take(6);',
    'return model.Clauses.OrderBy(c => c.DisplayOrder).Skip(1).Take(5);'
)
c = c.replace(
    'return model.Clauses.OrderBy(c => c.DisplayOrder).Skip(7);',
    'return model.Clauses.OrderBy(c => c.DisplayOrder).Skip(6);'
)

# Tighten signature block in FormModal
old_sig = """                                            <!-- Signatures block preview (Balanced 50%-50%, Non-split) -->
                                            <div class="row pt-4 mt-auto border-top tf-signature-block" style="break-inside: avoid !important; page-break-inside: avoid !important;">
                                                <div class="col-6 text-center">
                                                    <div class="fw-bold text-uppercase" style="font-size: 13pt;">NGƯỜI LAO ĐỘNG</div>
                                                    <div class="fst-italic smaller text-muted">(Ký và ghi rõ họ tên)</div>
                                                    <div style="height: 65px;" class="d-flex align-items-center justify-content-center">"""

new_sig = """                                            <!-- Signatures block preview (Balanced 50%-50%, Non-split) -->
                                            <div class="row pt-2 mt-auto border-top tf-signature-block" style="break-inside: avoid !important; page-break-inside: avoid !important;">
                                                <div class="col-6 text-center">
                                                    <div class="fw-bold text-uppercase" style="font-size: 13pt;">NGƯỜI LAO ĐỘNG</div>
                                                    <div class="fst-italic smaller text-muted">(Ký và ghi rõ họ tên)</div>
                                                    <div style="height: 48px;" class="d-flex align-items-center justify-content-center">"""

c = c.replace(old_sig, new_sig)

# Also employer side signature height
old_sig_emp = """                                                <div class="col-6 text-center">
                                                    <div class="fw-bold text-uppercase" style="font-size: 13pt;">BÊN SỬ DỤNG LAO ĐỘNG</div>
                                                    <div class="fst-italic smaller text-muted">(Ký, ghi rõ họ tên và đóng dấu)</div>
                                                    <div style="height: 65px;" class="d-flex align-items-center justify-content-center">"""

new_sig_emp = """                                                <div class="col-6 text-center">
                                                    <div class="fw-bold text-uppercase" style="font-size: 13pt;">BÊN SỬ DỤNG LAO ĐỘNG</div>
                                                    <div class="fst-italic smaller text-muted">(Ký, ghi rõ họ tên và đóng dấu)</div>
                                                    <div style="height: 48px;" class="d-flex align-items-center justify-content-center">"""

c = c.replace(old_sig_emp, new_sig_emp)

with open(r'C:/Users/thitr/.gemini/antigravity/scratch/TradeFlow/src/TradeFlow.Web/Components/Pages/Contracts/EmploymentContractFormModal.razor', 'w', encoding='utf-8') as f:
    f.write(c)

print("Updated EmploymentContractFormModal.razor successfully")
