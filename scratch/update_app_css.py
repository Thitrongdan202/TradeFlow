path = r"C:\Users\thitr\.gemini\antigravity\scratch\TradeFlow\src\TradeFlow.Web\wwwroot\app.css"

with open(path, "r", encoding="utf-8") as f:
    text = f.read()

# 1. Update .tf-topbar to have justify-content: space-between
target_topbar = """.tf-topbar {

  height: var(--tf-topbar-height);

  background-color: var(--tf-topbar-bg);

  border-bottom: 1px solid var(--tf-topbar-border);

  display: flex;

  align-items: center;

  padding: 0 var(--tf-space-6);

  position: sticky;

  top: 0;

  z-index: var(--tf-z-sticky);

  gap: var(--tf-space-4);

}"""

replacement_topbar = """.tf-topbar {

  height: var(--tf-topbar-height);

  background-color: var(--tf-topbar-bg);

  border-bottom: 1px solid var(--tf-topbar-border);

  display: flex;

  align-items: center;

  justify-content: space-between;

  padding: 0 var(--tf-space-6);

  position: sticky;

  top: 0;

  z-index: var(--tf-z-sticky);

  gap: var(--tf-space-4);

}

.tf-topbar-left {

  display: flex;

  align-items: center;

  gap: var(--tf-space-3);

}"""

if target_topbar in text:
    text = text.replace(target_topbar, replacement_topbar, 1)
    print("Updated .tf-topbar with justify-content: space-between")
else:
    print("Direct target_topbar not found, checking with normalize")

# 2. Add .tf-topbar-clock-pill styles if not present
if ".tf-topbar-clock-pill" not in text:
    clock_css = """
.tf-topbar-clock-pill {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 6px 12px;
  border-radius: 8px;
  background-color: #f8fafc;
  border: 1px solid #e2e8f0;
  color: #475569;
  font-size: 13px;
  font-weight: 500;
  user-select: none;
  white-space: nowrap;
}

[data-bs-theme="dark"] .tf-topbar-clock-pill {
  background-color: #242933;
  border-color: #383e4b;
  color: #94a3b8;
}

@media (max-width: 900px) {
  .tf-topbar-clock-pill {
    display: none;
  }
}
"""
    # Insert right before /* ============================================================
    # TOPBAR
    idx = text.find("/* ============================================================\n\n   TOPBAR")
    if idx == -1:
        idx = text.find(".tf-topbar-warehouse-pill {")
    if idx != -1:
        text = text[:idx] + clock_css + "\n" + text[idx:]
        print("Added .tf-topbar-clock-pill CSS")

with open(path, "w", encoding="utf-8", newline="") as f:
    f.write(text)

print("Updated app.css successfully.")
