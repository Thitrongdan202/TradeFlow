const fs = require('fs');

const path = 'src/TradeFlow.Web/wwwroot/app.css';
let content = fs.readFileSync(path, 'utf8');

// 1. Motion tokens
content = content.replace(
  /--tf-motion-instant:\s+120ms;[^\r\n]*/,
  '--tf-motion-instant:     90ms; /* Button press, toggle switch, micro-icon feedback (90-120ms) */'
);
content = content.replace(
  /--tf-motion-fast:\s+160ms;[^\r\n]*/,
  '--tf-motion-fast:        120ms; /* Dropdown appearance, card elevation, badge, tab switch (90-140ms) */'
);
content = content.replace(
  /--tf-motion-normal:\s+220ms;[^\r\n]*/,
  '--tf-motion-normal:      140ms; /* Modal backdrop fade, page enter, subtle cross-fades (120-160ms) */'
);
content = content.replace(
  /--tf-motion-modal:\s+260ms;[^\r\n]*/,
  '--tf-motion-modal:       150ms; /* Modal & drawer dialog entrance (120-170ms) */'
);
content = content.replace(
  /--tf-motion-modal-exit:\s+180ms;[^\r\n]*/,
  '--tf-motion-modal-exit:  110ms; /* Modal & drawer dialog exit (100-140ms) */'
);
content = content.replace(
  /--tf-motion-content:\s+220ms;[^\r\n]*/,
  '--tf-motion-content:     140ms; /* Content container entrance */'
);

// 2. @keyframes tfModalScaleIn
content = content.replace(
  /@keyframes tfModalScaleIn\s*\{\s*from\s*\{\s*opacity:\s*0;\s*transform:\s*scale\(0\.96\)\s*translateY\(8px\);/g,
  '@keyframes tfModalScaleIn {\n  from {\n    opacity: 0;\n    transform: scale(0.98) translateY(4px);'
);

// 3. .modal.fade .modal-dialog
content = content.replace(
  /transform:\s*scale\(0\.96\)\s*translateY\(8px\);/g,
  'transform: scale(0.98) translateY(4px);'
);

// 4. .tf-modal will-change removal
content = content.replace(
  /(\.tf-modal\s*\{[^}]*?)\r?\n\s*will-change:\s*transform,\s*opacity;/g,
  '$1'
);

// 5. Hardware isolation for modal body
content = content.replace(
  /(\.tf-modal-body\s*\{[^}]*?overscroll-behavior:\s*contain;)/g,
  '$1\n  contain: layout paint;\n  transform: translateZ(0);\n  scroll-behavior: auto !important;'
);

// 6. Targeted transitions instead of transition: all on common buttons/tabs
content = content.replace(
  /transition:\s*all 0\.15s ease;/g,
  'transition: background-color var(--tf-motion-fast) ease, border-color var(--tf-motion-fast) ease, color var(--tf-motion-fast) ease, box-shadow var(--tf-motion-fast) ease;'
);
content = content.replace(
  /transition:\s*all 0\.2s ease;/g,
  'transition: background-color var(--tf-motion-fast) ease, border-color var(--tf-motion-fast) ease, color var(--tf-motion-fast) ease, box-shadow var(--tf-motion-fast) ease;'
);

// 7. Add styles for notification action pill and highlighted target card
if (!content.includes('.tf-btn-action-pill')) {
  content += `\n
/* ============================================================
   HR WORKFLOW NOTIFICATION & TARGET HIGHLIGHT STYLES
   ============================================================ */
.tf-btn-action-pill {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: 11.5px;
  font-weight: 600;
  padding: 3px 10px;
  border-radius: 9999px;
  background-color: var(--tf-primary);
  color: #ffffff !important;
  text-decoration: none !important;
  transition: background-color var(--tf-motion-fast) ease, transform var(--tf-motion-instant) ease;
  cursor: pointer;
  border: none;
}
.tf-btn-action-pill:hover {
  background-color: var(--tf-primary-hover);
  transform: translateY(-1px);
}
.tf-btn-action-pill.warning {
  background-color: #d97706;
}
.tf-btn-action-pill.warning:hover {
  background-color: #b45309;
}
.tf-btn-action-pill.danger {
  background-color: #dc2626;
}
.tf-btn-action-pill.danger:hover {
  background-color: #b91c1c;
}
.tf-btn-action-pill.success {
  background-color: #16a34a;
}
.tf-btn-action-pill.success:hover {
  background-color: #15803d;
}

.tf-req-card.highlighted-target {
  border-color: var(--tf-primary) !important;
  box-shadow: 0 0 0 2px rgba(37, 99, 235, 0.25) !important;
  animation: tfTargetPulse 1.5s ease-out 1;
}

@keyframes tfTargetPulse {
  0% { box-shadow: 0 0 0 0 rgba(37, 99, 235, 0.5); }
  70% { box-shadow: 0 0 0 6px rgba(37, 99, 235, 0); }
  100% { box-shadow: 0 0 0 2px rgba(37, 99, 235, 0.25); }
}
`;
}

fs.writeFileSync(path, content, 'utf8');
console.log('Successfully updated app.css');
