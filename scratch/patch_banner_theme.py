import re

with open('src/TradeFlow.Web/Components/Pages/Attendance/Index.razor.css', 'r', encoding='utf-8') as f:
    css = f.read()

target_pattern = r'/\* --- ADMIN EXEMPTION BANNER --- \*/\s*\.tf-admin-exempt-banner\s*\{.*?\n\s*\.tf-exempt-content p\s*\{.*?\n\}'

replacement = """/* --- ADMIN EXEMPTION BANNER --- */
.tf-admin-exempt-banner {
    display: flex;
    align-items: center;
    gap: 1.25rem;
    padding: 1.15rem 1.5rem;
    margin-bottom: 1.5rem;
    background: var(--tf-surface-card, #ffffff);
    border: 1px solid var(--tf-border, #e2e8f0);
    border-radius: 12px;
    box-shadow: var(--tf-shadow-sm, 0 1px 3px rgba(0, 0, 0, 0.05));
    transition: background-color 0.2s ease, border-color 0.2s ease, box-shadow 0.2s ease;
}

.tf-exempt-icon {
    width: 44px;
    height: 44px;
    border-radius: 10px;
    background: rgba(37, 99, 235, 0.08);
    border: 1px solid rgba(37, 99, 235, 0.18);
    display: flex;
    align-items: center;
    justify-content: center;
    color: var(--tf-primary, #2563eb);
    flex-shrink: 0;
    transition: all 0.2s ease;
}

.tf-exempt-content h4 {
    margin: 0 0 0.25rem 0;
    font-size: 1rem;
    font-weight: 600;
    color: var(--tf-text-primary, #0f172a);
    line-height: 1.4;
    transition: color 0.2s ease;
}

.tf-exempt-content p {
    margin: 0;
    font-size: 0.875rem;
    color: var(--tf-text-secondary, #64748b);
    line-height: 1.5;
    transition: color 0.2s ease;
}

/* --- DARK MODE SUPPORT FOR EXEMPTION BANNER --- */
[data-bs-theme="dark"] .tf-admin-exempt-banner,
[data-theme="dark"] .tf-admin-exempt-banner {
    background: linear-gradient(135deg, rgba(30, 41, 59, 0.7) 0%, rgba(15, 23, 42, 0.9) 100%);
    border-color: rgba(56, 189, 248, 0.25);
    box-shadow: 0 4px 15px rgba(0, 0, 0, 0.35);
}

[data-bs-theme="dark"] .tf-exempt-icon,
[data-theme="dark"] .tf-exempt-icon {
    background: rgba(56, 189, 248, 0.15);
    border-color: rgba(56, 189, 248, 0.3);
    color: #38bdf8;
}

[data-bs-theme="dark"] .tf-exempt-content h4,
[data-theme="dark"] .tf-exempt-content h4 {
    color: #f8fafc;
}

[data-bs-theme="dark"] .tf-exempt-content p,
[data-theme="dark"] .tf-exempt-content p {
    color: #94a3b8;
}"""

new_css, count = re.subn(target_pattern, replacement, css, flags=re.DOTALL)
print(f"Substitutions made: {count}")
if count == 1:
    with open('src/TradeFlow.Web/Components/Pages/Attendance/Index.razor.css', 'w', encoding='utf-8') as f:
        f.write(new_css)
    print("Updated Index.razor.css successfully!")
else:
    print("Error: Could not match target pattern!")
