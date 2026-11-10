const fs = require('fs');
let css = fs.readFileSync('src/TradeFlow.Web/wwwroot/app.css', 'utf8');

const targetStart = '/* ============================================================\r\n\r\n   FLY-OUT SUBMENU (Desktop Horizontal Fly-Out & Hover Interaction)\r\n\r\n   ============================================================ */';
const targetStartUnix = '/* ============================================================\n\n   FLY-OUT SUBMENU (Desktop Horizontal Fly-Out & Hover Interaction)\n\n   ============================================================ */';

const targetEnd = '/* ============================================================\r\n\r\n   SIDEBAR BOTTOM PINNED DOCK ("Bảng điều khiển")';
const targetEndUnix = '/* ============================================================\n\n   SIDEBAR BOTTOM PINNED DOCK ("Bảng điều khiển")';

let isCrlf = css.includes('\r\n');
let sIdx = css.indexOf(targetStart);
if (sIdx === -1) sIdx = css.indexOf(targetStartUnix);

let eIdx = css.indexOf(targetEnd);
if (eIdx === -1) eIdx = css.indexOf(targetEndUnix);

console.log('sIdx:', sIdx, 'eIdx:', eIdx);
if (sIdx === -1 || eIdx === -1) {
  console.error('Target not found!');
  process.exit(1);
}

const replacement = `/* ============================================================
   TRADEFLOW CONTEXTUAL NAVIGATION PANEL & ICON RAIL ARCHITECTURE
   - Floating docked panel alongside sidebar
   - No automatic opening or flyout cascades on hover
   - Clean theme integration (Light & Dark Mode)
   - Zero horizontal overflow & responsive drawer sub-view
   ============================================================ */

.tf-sidebar {
  z-index: 1045 !important;
}

@media (min-width: 1024px) {
  .tf-sidebar,
  .tf-sidebar-inner {
    overflow: visible !important;
  }

  .tf-nav-scroll {
    overflow-y: auto !important;
    overflow-x: hidden !important;
  }

  .tf-nav-item {
    position: relative;
  }

  .tf-sidebar-backdrop {
    display: none !important;
  }
}

/* --- Contextual Backdrop (outside click capture) --- */
.tf-contextual-backdrop {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  z-index: 1040;
  background: transparent;
  cursor: default;
}

@media (max-width: 1023px) {
  .tf-contextual-backdrop {
    background: rgba(15, 23, 42, 0.45);
    backdrop-filter: blur(2px);
    -webkit-backdrop-filter: blur(2px);
  }
}

/* --- Floating Contextual Panel Container --- */
.tf-contextual-panel {
  position: absolute;
  left: calc(100% + 10px);
  top: 12px;
  bottom: 12px;
  width: 320px;
  background-color: var(--tf-bg-card, #ffffff);
  border: 1px solid var(--tf-border, #e2e8f0);
  border-radius: 14px;
  box-shadow: 0 16px 36px -4px rgba(16, 24, 40, 0.16), 0 4px 12px -2px rgba(16, 24, 40, 0.06);
  display: flex;
  flex-direction: column;
  z-index: 1050;
  overflow: hidden;
  animation: tfContextualSlideIn 0.18s cubic-bezier(0.16, 1, 0.3, 1) forwards;
}

[data-bs-theme="dark"] .tf-contextual-panel,
[data-theme="dark"] .tf-contextual-panel {
  background-color: var(--tf-bg-card, #222630);
  border-color: var(--tf-border, #383E4B);
  box-shadow: 0 20px 44px -4px rgba(0, 0, 0, 0.55), 0 0 0 1px rgba(255, 255, 255, 0.08);
}

@keyframes tfContextualSlideIn {
  from {
    opacity: 0;
    transform: translateX(-8px);
  }
  to {
    opacity: 1;
    transform: translateX(0);
  }
}

/* --- Contextual Panel Header --- */
.tf-contextual-header {
  padding: 14px 16px 12px;
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 10px;
  background: transparent;
}

.tf-contextual-header-info {
  flex: 1;
  min-width: 0;
}

.tf-contextual-title {
  font-size: 11.5px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  color: var(--tf-text-primary, #1e293b);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

[data-bs-theme="dark"] .tf-contextual-title,
[data-theme="dark"] .tf-contextual-title {
  color: #F0F2F6;
}

.tf-contextual-subtitle {
  font-size: 11.5px;
  font-weight: 400;
  color: var(--tf-text-muted, #64748b);
  margin-top: 2px;
  line-height: 1.35;
}

[data-bs-theme="dark"] .tf-contextual-subtitle,
[data-theme="dark"] .tf-contextual-subtitle {
  color: #929BAC;
}

.tf-contextual-close {
  width: 28px;
  height: 28px;
  border-radius: 6px;
  border: none;
  background: transparent;
  color: var(--tf-text-muted, #64748b);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.15s ease;
  flex-shrink: 0;
}

.tf-contextual-close:hover {
  background-color: var(--tf-bg-subtle, #f1f5f9);
  color: var(--tf-text-primary, #0f172a);
}

[data-bs-theme="dark"] .tf-contextual-close:hover,
[data-theme="dark"] .tf-contextual-close:hover {
  background-color: var(--tf-surface-variant, #1D2028);
  color: #ffffff;
}

.tf-contextual-mobile-back {
  display: none;
}

.tf-contextual-divider {
  height: 1px;
  background-color: var(--tf-border, #e2e8f0);
  margin: 0;
}

[data-bs-theme="dark"] .tf-contextual-divider,
[data-theme="dark"] .tf-contextual-divider {
  background-color: var(--tf-border, #383E4B);
}

/* --- Contextual Panel Body & Navigation Items --- */
.tf-contextual-body {
  flex: 1;
  padding: 10px;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 3px;
}

.tf-contextual-body::-webkit-scrollbar {
  width: 4px;
}
.tf-contextual-body::-webkit-scrollbar-track {
  background: transparent;
}
.tf-contextual-body::-webkit-scrollbar-thumb {
  background-color: var(--tf-border, #cbd5e1);
  border-radius: 4px;
}
[data-bs-theme="dark"] .tf-contextual-body::-webkit-scrollbar-thumb,
[data-theme="dark"] .tf-contextual-body::-webkit-scrollbar-thumb {
  background-color: var(--tf-border, #383E4B);
}

.tf-contextual-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 12px;
  border-radius: 8px;
  color: var(--tf-text-secondary, #334155);
  font-size: 13px;
  font-weight: 500;
  text-decoration: none;
  transition: all 0.15s ease;
  cursor: pointer;
  border: 1px solid transparent;
  user-select: none;
}

.tf-contextual-item:hover {
  background-color: var(--tf-sidebar-item-hover, #F7F8FA);
  color: var(--tf-text-primary, #0f172a);
  text-decoration: none;
}

[data-bs-theme="dark"] .tf-contextual-item {
  color: var(--tf-text-secondary, #B4BBC8);
}

[data-bs-theme="dark"] .tf-contextual-item:hover {
  background-color: var(--tf-surface-variant, #1D2028);
  color: #ffffff;
}

.tf-contextual-item.active {
  background-color: var(--tf-sidebar-item-active, #eff6ff) !important;
  color: var(--tf-sidebar-text-active, #2563eb) !important;
  font-weight: 600 !important;
  border-color: rgba(37, 99, 235, 0.15);
}

[data-bs-theme="dark"] .tf-contextual-item.active {
  background-color: rgba(37, 99, 235, 0.16) !important;
  color: #60a5fa !important;
  font-weight: 600 !important;
  border-color: rgba(96, 165, 250, 0.25);
}

.tf-contextual-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background-color: var(--tf-text-muted, #94a3b8);
  flex-shrink: 0;
  transition: all 0.15s ease;
}

.tf-contextual-item:hover .tf-contextual-dot {
  background-color: var(--tf-text-primary, #0f172a);
}

[data-bs-theme="dark"] .tf-contextual-item:hover .tf-contextual-dot {
  background-color: #ffffff;
}

.tf-contextual-item.active .tf-contextual-dot {
  background-color: var(--tf-sidebar-text-active, #2563eb);
  transform: scale(1.25);
  box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.2);
}

[data-bs-theme="dark"] .tf-contextual-item.active .tf-contextual-dot {
  background-color: #60a5fa;
  box-shadow: 0 0 0 3px rgba(96, 165, 250, 0.25);
}

.tf-contextual-text {
  flex: 1;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

/* --- Rail Item Panel-Open Highlight --- */
.tf-nav-item.is-panel-open > .tf-nav-link,
.tf-nav-link.panel-active {
  background-color: var(--tf-sidebar-item-hover, #F7F8FA);
  color: var(--tf-text-primary, #0f172a);
}

[data-bs-theme="dark"] .tf-nav-item.is-panel-open > .tf-nav-link,
[data-bs-theme="dark"] .tf-nav-link.panel-active {
  background-color: var(--tf-surface-variant, #1D2028);
  color: #ffffff;
}

.tf-nav-item.is-panel-open > .tf-nav-link .tf-nav-chevron {
  color: var(--tf-primary, #2563eb) !important;
  opacity: 1 !important;
  transform: rotate(90deg) !important;
}

/* --- Mobile Drawer Contextual View (< 1024px) --- */
@media (max-width: 1023px) {
  .tf-contextual-panel {
    position: absolute;
    left: 0;
    top: 0;
    right: 0;
    bottom: 0;
    width: 100%;
    height: 100%;
    border-radius: 0;
    border: none;
    z-index: 1100;
    box-shadow: none;
  }

  .tf-contextual-mobile-back {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    font-size: 11.5px;
    font-weight: 600;
    color: var(--tf-primary, #2563eb);
    background: transparent;
    border: none;
    padding: 0 0 4px;
    margin-bottom: 2px;
    cursor: pointer;
  }
}

`;

const formattedReplacement = isCrlf ? replacement.replace(/\n/g, '\r\n') : replacement;
css = css.substring(0, sIdx) + formattedReplacement + css.substring(eIdx);
fs.writeFileSync('src/TradeFlow.Web/wwwroot/app.css', css, 'utf8');
console.log('REPLACED CONTEXTUAL CSS SUCCESSFULLY');
