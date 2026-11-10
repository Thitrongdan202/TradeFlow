const fs = require('fs');
const path = require('path');

const filePath = path.join(__dirname, '../src/TradeFlow.Web/Components/App.razor');
let appRazor = fs.readFileSync(filePath, 'utf8');

const target = 'window.tradeflowFlyout = {';
if (!appRazor.includes(target)) {
    console.error('Target not found in App.razor!');
    process.exit(1);
}

if (appRazor.includes('window.tradeflow = {')) {
    console.log('window.tradeflow already exists in App.razor');
    process.exit(0);
}

const tradeflowObj = `        window.tradeflow = {
            isMobile: function () {
                return window.innerWidth < 1024;
            },
            getSidebarCollapsed: function () {
                try {
                    return localStorage.getItem('tf_sidebar_collapsed') === 'true';
                } catch (e) {
                    return false;
                }
            },
            setSidebarCollapsed: function (val) {
                try {
                    localStorage.setItem('tf_sidebar_collapsed', val ? 'true' : 'false');
                } catch (e) { }
            },
            toggleSidebar: function () {
                var isMob = window.innerWidth < 1024;
                var sidebar = document.querySelector('.tf-sidebar');
                var wrapper = document.querySelector('.tf-main-wrapper');
                var app = document.querySelector('.tf-app');
                var hamburgerBtn = document.querySelector('.tf-sidebar-toggle');
                var bottomBtn = document.querySelector('.tf-nav-collapse-btn');
                var backdrop = document.querySelector('.tf-sidebar-backdrop');
                if (isMob) {
                    if (!sidebar) return;
                    var isOpen = sidebar.classList.toggle('open');
                    if (backdrop) {
                        if (isOpen) backdrop.classList.add('active');
                        else backdrop.classList.remove('active');
                    }
                    if (hamburgerBtn) hamburgerBtn.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
                } else {
                    if (!sidebar) return;
                    var isCurrentlyCollapsed = sidebar.classList.contains('is-collapsed');
                    var willBeCollapsed = !isCurrentlyCollapsed;
                    if (willBeCollapsed) {
                        sidebar.classList.add('is-collapsed');
                        if (wrapper) wrapper.classList.add('is-collapsed');
                        if (app) app.classList.add('is-sidebar-collapsed');
                    } else {
                        sidebar.classList.remove('is-collapsed');
                        if (wrapper) wrapper.classList.remove('is-collapsed');
                        if (app) app.classList.remove('is-sidebar-collapsed');
                    }
                    try {
                        localStorage.setItem('tf_sidebar_collapsed', willBeCollapsed ? 'true' : 'false');
                    } catch (e) { }
                    var titleText = willBeCollapsed ? 'Mở rộng thanh menu (Ctrl+B)' : 'Thu gọn thanh menu (Ctrl+B)';
                    var labelText = willBeCollapsed ? 'Mở rộng thanh menu' : 'Thu gọn thanh menu';
                    if (hamburgerBtn) {
                        hamburgerBtn.setAttribute('aria-expanded', willBeCollapsed ? 'false' : 'true');
                        hamburgerBtn.setAttribute('title', titleText);
                    }
                    if (bottomBtn) {
                        bottomBtn.setAttribute('aria-expanded', willBeCollapsed ? 'false' : 'true');
                        bottomBtn.setAttribute('title', titleText);
                        bottomBtn.setAttribute('aria-label', labelText);
                        var svg = bottomBtn.querySelector('svg');
                        if (svg) svg.style.transform = willBeCollapsed ? 'rotate(180deg)' : 'rotate(0deg)';
                        var labelSpan = bottomBtn.querySelector('.tf-nav-label');
                        if (labelSpan) labelSpan.textContent = willBeCollapsed ? 'Mở rộng' : 'Thu gọn';
                    }
                }
            },
            closeMobileSidebar: function () {
                var sidebar = document.querySelector('.tf-sidebar');
                if (sidebar) sidebar.classList.remove('open');
                var backdrop = document.querySelector('.tf-sidebar-backdrop');
                if (backdrop) backdrop.classList.remove('active');
                var hamburgerBtn = document.querySelector('.tf-sidebar-toggle');
                if (hamburgerBtn) hamburgerBtn.setAttribute('aria-expanded', 'false');
            },
            initSidebar: function () {
                var isMob = window.innerWidth < 1024;
                if (isMob) return;
                var isCollapsed = false;
                try {
                    isCollapsed = localStorage.getItem('tf_sidebar_collapsed') === 'true';
                } catch (e) { }
                var sidebar = document.querySelector('.tf-sidebar');
                var wrapper = document.querySelector('.tf-main-wrapper');
                var app = document.querySelector('.tf-app');
                var hamburgerBtn = document.querySelector('.tf-sidebar-toggle');
                var bottomBtn = document.querySelector('.tf-nav-collapse-btn');
                if (sidebar) {
                    if (isCollapsed) {
                        sidebar.classList.add('is-collapsed');
                        if (wrapper) wrapper.classList.add('is-collapsed');
                        if (app) app.classList.add('is-sidebar-collapsed');
                    } else {
                        sidebar.classList.remove('is-collapsed');
                        if (wrapper) wrapper.classList.remove('is-collapsed');
                        if (app) app.classList.remove('is-sidebar-collapsed');
                    }
                }
                var titleText = isCollapsed ? 'Mở rộng thanh menu (Ctrl+B)' : 'Thu gọn thanh menu (Ctrl+B)';
                var labelText = isCollapsed ? 'Mở rộng thanh menu' : 'Thu gọn thanh menu';
                if (hamburgerBtn) {
                    hamburgerBtn.setAttribute('aria-expanded', isCollapsed ? 'false' : 'true');
                    hamburgerBtn.setAttribute('title', titleText);
                }
                if (bottomBtn) {
                    bottomBtn.setAttribute('aria-expanded', isCollapsed ? 'false' : 'true');
                    bottomBtn.setAttribute('title', titleText);
                    bottomBtn.setAttribute('aria-label', labelText);
                    var svg = bottomBtn.querySelector('svg');
                    if (svg) svg.style.transform = isCollapsed ? 'rotate(180deg)' : 'rotate(0deg)';
                    var labelSpan = bottomBtn.querySelector('.tf-nav-label');
                    if (labelSpan) labelSpan.textContent = isCollapsed ? 'Mở rộng' : 'Thu gọn';
                }
            },
            applyTheme: function () {
                var theme = 'light';
                try {
                    var cookieMatch = document.cookie.match(/(?:^|;\\s*)(?:tf_theme|tradeflow_theme)=([^;]+)/);
                    if (cookieMatch && (cookieMatch[1] === 'dark' || cookieMatch[1] === 'light')) {
                        theme = cookieMatch[1];
                    } else {
                        var stored = localStorage.getItem('tf_theme') || localStorage.getItem('tradeflow_theme');
                        if (stored === 'dark' || stored === 'light') {
                            theme = stored;
                        }
                    }
                } catch (e) {}

                document.documentElement.setAttribute('data-bs-theme', theme);
                document.documentElement.setAttribute('data-theme', theme);
                if (theme === 'dark') {
                    document.documentElement.classList.add('dark');
                } else {
                    document.documentElement.classList.remove('dark');
                }
                return theme;
            },
            getTheme: function () {
                try {
                    var cookieMatch = document.cookie.match(/(?:^|;\\s*)(?:tf_theme|tradeflow_theme)=([^;]+)/);
                    if (cookieMatch && (cookieMatch[1] === 'dark' || cookieMatch[1] === 'light')) {
                        return cookieMatch[1];
                    }
                    var stored = localStorage.getItem('tf_theme') || localStorage.getItem('tradeflow_theme');
                    if (stored === 'dark' || stored === 'light') return stored;
                } catch (e) {}
                return document.documentElement.getAttribute('data-bs-theme') || 'light';
            },
            setTheme: function (theme) {
                if (theme !== 'dark' && theme !== 'light') theme = 'light';
                try {
                    localStorage.setItem('tf_theme', theme);
                    localStorage.setItem('tradeflow_theme', theme);
                    document.cookie = 'tf_theme=' + theme + ';path=/;max-age=31536000;SameSite=Lax';
                    document.cookie = 'tradeflow_theme=' + theme + ';path=/;max-age=31536000;SameSite=Lax';
                } catch (e) {}

                document.documentElement.setAttribute('data-bs-theme', theme);
                document.documentElement.setAttribute('data-theme', theme);
                if (theme === 'dark') {
                    document.documentElement.classList.add('dark');
                } else {
                    document.documentElement.classList.remove('dark');
                }

                window.dispatchEvent(new CustomEvent('tf-theme-changed', { detail: { theme: theme } }));
                return theme;
            },
            toggleTheme: function () {
                var current = window.tradeflow.getTheme();
                var next = current === 'dark' ? 'light' : 'dark';
                return window.tradeflow.setTheme(next);
            },
            downloadPdfBase64: function (base64Data, fileName) {
                var link = document.createElement('a');
                link.download = fileName;
                link.href = 'data:application/pdf;base64,' + base64Data;
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
            },
            downloadPdfFromPost: function (url, postData, defaultFilename) {
                return new Promise(function (resolve, reject) {
                    try {
                        fetch(url, {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json', 'Accept': 'application/pdf' },
                            credentials: 'same-origin',
                            body: JSON.stringify(postData)
                        })
                        .then(function (res) {
                            if (!res.ok) throw new Error('HTTP ' + res.status);
                            return res.blob();
                        })
                        .then(function (blob) {
                            var blobUrl = window.URL.createObjectURL(blob);
                            var a = document.createElement('a');
                            a.style.display = 'none';
                            a.href = blobUrl;
                            a.download = defaultFilename || 'Hop_Dong_Lao_Dong_Draft.pdf';
                            document.body.appendChild(a);
                            a.click();
                            setTimeout(function () {
                                document.body.removeChild(a);
                                window.URL.revokeObjectURL(blobUrl);
                                resolve(true);
                            }, 1000);
                        })
                        .catch(function (err) {
                            console.error('[tradeflow.downloadPdfFromPost] Error:', err);
                            reject(err);
                        });
                    } catch (e) {
                        reject(e);
                    }
                });
            },
            downloadPdf: function (url, defaultFilename) {
                return new Promise(function (resolve, reject) {
                    try {
                        var a = document.createElement('a');
                        a.href = url;
                        a.download = defaultFilename || '';
                        document.body.appendChild(a);
                        a.click();
                        document.body.removeChild(a);
                        resolve(true);
                    } catch (e) {
                        reject(e);
                    }
                });
            },
            autoResizeTextarea: function (el, minH, maxH) {
                if (!el) return;
                minH = minH || 180;
                maxH = maxH || 450;
                el.style.height = 'auto';
                var scrollH = el.scrollHeight;
                var targetH = Math.min(Math.max(scrollH, minH), maxH);
                el.style.height = targetH + 'px';
                el.style.overflowY = scrollH > maxH ? 'auto' : 'hidden';
            }
        };

`;

appRazor = appRazor.replace(target, tradeflowObj + target);
fs.writeFileSync(filePath, appRazor, 'utf8');
console.log('App.razor successfully patched with window.tradeflow!');
