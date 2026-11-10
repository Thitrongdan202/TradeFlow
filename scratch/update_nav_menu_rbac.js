const fs = require('fs');

const navMenuCode = `@rendermode InteractiveServer
@implements IDisposable
@using System.Security.Claims
@using Microsoft.AspNetCore.Components.Authorization
@using TradeFlow.Application.Common.Interfaces
@inject NavigationManager Nav
@inject AuthenticationStateProvider AuthStateProvider
@inject IPermissionService PermissionService

<nav class="tf-sidebar-inner" style="display:flex; flex-direction:column; height:100%;">
    @* Brand Header — Circular Badge & Modern Identity *@
    <div class="tf-brand-wrapper">
        <a href="/" class="tf-brand" title="TradeFlow — Hệ thống quản trị doanh nghiệp">
            <div class="tf-brand-logo">
                <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5"/>
                </svg>
            </div>
            <div class="tf-brand-text">
                <span class="tf-brand-name">TradeFlow</span>
                <span class="tf-brand-tagline">ERP &amp; Bán hàng</span>
            </div>
        </a>
        <button type="button" class="tf-mobile-close-btn" @onclick="OnCloseMobile" title="Đóng menu" aria-label="Đóng menu">
            &times;
        </button>
    </div>

    @* Navigation Menu Items *@
    <div class="tf-nav-scroll">

        @* 1. TỔNG QUAN (DASHBOARD) *@
        <div class="tf-nav-item">
            <NavLink class="tf-nav-link" href="" Match="NavLinkMatch.All" ActiveClass="active" title="Tổng quan" @onclick="HandleLinkClick">
                <span class="tf-nav-icon">
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                        <rect x="3" y="3" width="7" height="7" rx="1.5"/>
                        <rect x="14" y="3" width="7" height="7" rx="1.5"/>
                        <rect x="14" y="14" width="7" height="7" rx="1.5"/>
                        <rect x="3" y="14" width="7" height="7" rx="1.5"/>
                    </svg>
                </span>
                <span class="tf-nav-label">Tổng quan</span>
            </NavLink>
            <div class="tf-nav-flyout tf-nav-flyout-single">
                <div class="tf-nav-flyout-header" style="border-bottom:none; margin-bottom:0; padding:6px 12px; font-size:12px; font-weight:600; color:#1e293b; text-transform:none;">Tổng quan</div>
            </div>
        </div>

        @* 2. CHẤM CÔNG (STANDALONE DIRECT LINK) *@
        @if (Has("Attendance", "View") || _isAuthenticated)
        {
            <div class="tf-nav-item">
                <NavLink class="tf-nav-link" href="/cham-cong" Match="NavLinkMatch.Prefix" ActiveClass="active" title="Chấm công" @onclick="HandleLinkClick">
                    <span class="tf-nav-icon">
                        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                            <circle cx="12" cy="12" r="10"/>
                            <polyline points="12 6 12 12 16 14"/>
                        </svg>
                    </span>
                    <span class="tf-nav-label">Chấm công</span>
                </NavLink>
                <div class="tf-nav-flyout tf-nav-flyout-single">
                    <div class="tf-nav-flyout-header" style="border-bottom:none; margin-bottom:0; padding:6px 12px; font-size:12px; font-weight:600; color:#1e293b; text-transform:none;">Chấm công</div>
                </div>
            </div>
        }

        @* 3. BÁN HÀNG (STANDALONE DIRECT LINK) *@
        @if (Has("SalesOrders", "Create") || Has("SalesOrders", "View"))
        {
            <div class="tf-nav-item">
                <NavLink class="tf-nav-link" href="/ban-hang" Match="NavLinkMatch.All" ActiveClass="active" title="Bán hàng" @onclick="HandleLinkClick">
                    <span class="tf-nav-icon">
                        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                            <circle cx="9" cy="21" r="1"/>
                            <circle cx="20" cy="21" r="1"/>
                            <path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"/>
                        </svg>
                    </span>
                    <span class="tf-nav-label">Bán hàng</span>
                </NavLink>
                <div class="tf-nav-flyout tf-nav-flyout-single">
                    <div class="tf-nav-flyout-header" style="border-bottom:none; margin-bottom:0; padding:6px 12px; font-size:12px; font-weight:600; color:#1e293b; text-transform:none;">Bán hàng</div>
                </div>
            </div>
        }

        @* 4. HÓA ĐƠN & ĐƠN HÀNG *@
        @if (HasAny(("Invoices", "View"), ("SalesOrders", "View"), ("Quotations", "View")))
        {
            <div class="tf-nav-item @(IsInvoiceSectionActive() ? "is-active" : "")">
                <button type="button" class="tf-nav-link @(IsInvoiceSectionActive() ? "active" : "")" @onclick="ToggleInvoice" title="Hóa đơn">
                    <span class="tf-nav-icon">
                        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                            <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
                            <polyline points="14 2 14 8 20 8"/>
                            <line x1="16" y1="13" x2="8" y2="13"/>
                            <line x1="16" y1="17" x2="8" y2="17"/>
                            <polyline points="10 9 9 9 8 9"/>
                        </svg>
                    </span>
                    <span class="tf-nav-label">Hóa đơn</span>
                    <span class="tf-nav-chevron-wrap">
                        <svg class="tf-nav-chevron @(IsInvoiceOpen ? "expanded" : "")" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                            <polyline points="9 18 15 12 9 6"/>
                        </svg>
                    </span>
                </button>

                @* Desktop Fly-out Submenu *@
                <div class="tf-nav-flyout">
                    <div class="tf-nav-flyout-header">Hóa đơn &amp; Đơn hàng</div>
                    <div class="tf-nav-flyout-body">
                        @if (Has("Invoices", "View"))
                        {
                            <NavLink class="tf-flyout-link" href="/hoa-don" Match="NavLinkMatch.All" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-flyout-dot"></span>
                                <span>Danh sách hóa đơn</span>
                            </NavLink>
                        }
                        @if (Has("SalesOrders", "View"))
                        {
                            <NavLink class="tf-flyout-link" href="/ban-hang/don-ban-hang" Match="NavLinkMatch.All" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-flyout-dot"></span>
                                <span>Đơn bán hàng</span>
                            </NavLink>
                        }
                        @if (Has("Quotations", "View"))
                        {
                            <NavLink class="tf-flyout-link" href="/ban-hang/bao-gia" Match="NavLinkMatch.All" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-flyout-dot"></span>
                                <span>Báo giá</span>
                            </NavLink>
                        }
                    </div>
                </div>

                @* Mobile / Click Accordion Submenu *@
                <div class="tf-nav-sub @(IsInvoiceOpen ? "expanded" : "")">
                    @if (Has("Invoices", "View"))
                    {
                        <div class="tf-nav-sub-item">
                            <NavLink class="tf-nav-sub-link" href="/hoa-don" Match="NavLinkMatch.All" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-nav-sub-dot"></span>Danh sách hóa đơn
                            </NavLink>
                        </div>
                    }
                    @if (Has("SalesOrders", "View"))
                    {
                        <div class="tf-nav-sub-item">
                            <NavLink class="tf-nav-sub-link" href="/ban-hang/don-ban-hang" Match="NavLinkMatch.All" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-nav-sub-dot"></span>Đơn bán hàng
                            </NavLink>
                        </div>
                    }
                    @if (Has("Quotations", "View"))
                    {
                        <div class="tf-nav-sub-item">
                            <NavLink class="tf-nav-sub-link" href="/ban-hang/bao-gia" Match="NavLinkMatch.All" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-nav-sub-dot"></span>Báo giá
                            </NavLink>
                        </div>
                    }
                </div>
            </div>
        }

        @* 5. SẢN PHẨM & DANH MỤC *@
        @if (HasAny(("Products", "View"), ("ProductCategories", "View"), ("PriceLists", "View")))
        {
            <div class="tf-nav-item @(IsSectionActive("danh-muc") || IsSectionActive("bang-gia") ? "is-active" : "")">
                <button type="button" class="tf-nav-link @(IsSectionActive("danh-muc") || IsSectionActive("bang-gia") ? "active" : "")" @onclick="ToggleCatalog" title="Sản phẩm &amp; Danh mục">
                    <span class="tf-nav-icon">
                        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                            <path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"/>
                            <polyline points="3.27 6.96 12 12.01 20.73 6.96"/>
                            <line x1="12" y1="22.08" x2="12" y2="12"/>
                        </svg>
                    </span>
                    <span class="tf-nav-label">Sản phẩm</span>
                    <span class="tf-nav-chevron-wrap">
                        <svg class="tf-nav-chevron @(IsCatalogOpen ? "expanded" : "")" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                            <polyline points="9 18 15 12 9 6"/>
                        </svg>
                    </span>
                </button>

                @* Desktop Fly-out Submenu *@
                <div class="tf-nav-flyout">
                    <div class="tf-nav-flyout-header">Sản phẩm &amp; Danh mục</div>
                    <div class="tf-nav-flyout-body">
                        @if (Has("Products", "View"))
                        {
                            <NavLink class="tf-flyout-link" href="/danh-muc/san-pham" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-flyout-dot"></span>
                                <span>Danh sách sản phẩm</span>
                            </NavLink>
                        }
                        @if (Has("ProductCategories", "View"))
                        {
                            <NavLink class="tf-flyout-link" href="/danh-muc/danh-muc-san-pham" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-flyout-dot"></span>
                                <span>Danh mục sản phẩm</span>
                            </NavLink>
                        }
                        @if (Has("PriceLists", "View"))
                        {
                            <NavLink class="tf-flyout-link" href="/bang-gia" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-flyout-dot"></span>
                                <span>Bảng giá sản phẩm</span>
                            </NavLink>
                        }
                    </div>
                </div>

                @* Mobile / Click Accordion Submenu *@
                <div class="tf-nav-sub @(IsCatalogOpen ? "expanded" : "")">
                    @if (Has("Products", "View"))
                    {
                        <div class="tf-nav-sub-item">
                            <NavLink class="tf-nav-sub-link" href="/danh-muc/san-pham" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-nav-sub-dot"></span>Danh sách sản phẩm
                            </NavLink>
                        </div>
                    }
                    @if (Has("ProductCategories", "View"))
                    {
                        <div class="tf-nav-sub-item">
                            <NavLink class="tf-nav-sub-link" href="/danh-muc/danh-muc-san-pham" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-nav-sub-dot"></span>Danh mục sản phẩm
                            </NavLink>
                        </div>
                    }
                    @if (Has("PriceLists", "View"))
                    {
                        <div class="tf-nav-sub-item">
                            <NavLink class="tf-nav-sub-link" href="/bang-gia" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-nav-sub-dot"></span>Bảng giá sản phẩm
                            </NavLink>
                        </div>
                    }
                </div>
            </div>
        }

        @* 6. KHO HÀNG *@
        @if (Has("Warehouses", "View"))
        {
            <div class="tf-nav-item @(IsSectionActive("kho") ? "is-active" : "")">
                <button type="button" class="tf-nav-link @(IsSectionActive("kho") ? "active" : "")" @onclick="ToggleWarehouse" title="Kho hàng">
                    <span class="tf-nav-icon">
                        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                            <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/>
                            <polyline points="9 22 9 12 15 12 15 22"/>
                        </svg>
                    </span>
                    <span class="tf-nav-label">Kho hàng</span>
                    <span class="tf-nav-chevron-wrap">
                        <svg class="tf-nav-chevron @(IsWarehouseOpen ? "expanded" : "")" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                            <polyline points="9 18 15 12 9 6"/>
                        </svg>
                    </span>
                </button>

                @* Desktop Fly-out Submenu *@
                <div class="tf-nav-flyout">
                    <div class="tf-nav-flyout-header">Kho hàng</div>
                    <div class="tf-nav-flyout-body">
                        <NavLink class="tf-flyout-link" href="/kho/ton-kho" ActiveClass="active" @onclick="HandleLinkClick">
                            <span class="tf-flyout-dot"></span>
                            <span>Tồn kho thực tế</span>
                        </NavLink>
                        <NavLink class="tf-flyout-link" href="/kho/phieu-kho" ActiveClass="active" @onclick="HandleLinkClick">
                            <span class="tf-flyout-dot"></span>
                            <span>Phiếu nhập / xuất kho</span>
                        </NavLink>
                        <NavLink class="tf-flyout-link" href="/kho/kiem-kho" ActiveClass="active" @onclick="HandleLinkClick">
                            <span class="tf-flyout-dot"></span>
                            <span>Kiểm kê kho</span>
                        </NavLink>
                        <NavLink class="tf-flyout-link" href="/kho/hang-tra" ActiveClass="active" @onclick="HandleLinkClick">
                            <span class="tf-flyout-dot"></span>
                            <span>Hàng trả lại</span>
                        </NavLink>
                        <NavLink class="tf-flyout-link" href="/kho/hang-loi" ActiveClass="active" @onclick="HandleLinkClick">
                            <span class="tf-flyout-dot"></span>
                            <span>Hàng lỗi / Cách ly</span>
                        </NavLink>
                        <NavLink class="tf-flyout-link" href="/kho/bien-dong" ActiveClass="active" @onclick="HandleLinkClick">
                            <span class="tf-flyout-dot"></span>
                            <span>Sổ thẻ kho &amp; Biến động</span>
                        </NavLink>
                        <NavLink class="tf-flyout-link" href="/kho-hang" ActiveClass="active" @onclick="HandleLinkClick">
                            <span class="tf-flyout-dot"></span>
                            <span>Danh sách kho hàng</span>
                        </NavLink>
                    </div>
                </div>

                @* Mobile / Click Accordion Submenu *@
                <div class="tf-nav-sub @(IsWarehouseOpen ? "expanded" : "")">
                    <div class="tf-nav-sub-item">
                        <NavLink class="tf-nav-sub-link" href="/kho/ton-kho" ActiveClass="active" @onclick="HandleLinkClick">
                            <span class="tf-nav-sub-dot"></span>Tồn kho thực tế
                        </NavLink>
                    </div>
                    <div class="tf-nav-sub-item">
                        <NavLink class="tf-nav-sub-link" href="/kho/phieu-kho" ActiveClass="active" @onclick="HandleLinkClick">
                            <span class="tf-nav-sub-dot"></span>Phiếu nhập / xuất kho
                        </NavLink>
                    </div>
                    <div class="tf-nav-sub-item">
                        <NavLink class="tf-nav-sub-link" href="/kho/kiem-kho" ActiveClass="active" @onclick="HandleLinkClick">
                            <span class="tf-nav-sub-dot"></span>Kiểm kê kho
                        </NavLink>
                    </div>
                    <div class="tf-nav-sub-item">
                        <NavLink class="tf-nav-sub-link" href="/kho/hang-tra" ActiveClass="active" @onclick="HandleLinkClick">
                            <span class="tf-nav-sub-dot"></span>Hàng trả lại
                        </NavLink>
                    </div>
                    <div class="tf-nav-sub-item">
                        <NavLink class="tf-nav-sub-link" href="/kho/hang-loi" ActiveClass="active" @onclick="HandleLinkClick">
                            <span class="tf-nav-sub-dot"></span>Hàng lỗi / Cách ly
                        </NavLink>
                    </div>
                    <div class="tf-nav-sub-item">
                        <NavLink class="tf-nav-sub-link" href="/kho/bien-dong" ActiveClass="active" @onclick="HandleLinkClick">
                            <span class="tf-nav-sub-dot"></span>Sổ thẻ kho &amp; Biến động
                        </NavLink>
                    </div>
                    <div class="tf-nav-sub-item">
                        <NavLink class="tf-nav-sub-link" href="/kho-hang" ActiveClass="active" @onclick="HandleLinkClick">
                            <span class="tf-nav-sub-dot"></span>Danh sách kho
                        </NavLink>
                    </div>
                </div>
            </div>
        }

        @* 7. ĐỐI TÁC *@
        @if (HasAny(("Customers", "View"), ("Suppliers", "View")))
        {
            <div class="tf-nav-item @(IsSectionActive("doi-tac") ? "is-active" : "")">
                <button type="button" class="tf-nav-link @(IsSectionActive("doi-tac") ? "active" : "")" @onclick="TogglePartners" title="Đối tác">
                    <span class="tf-nav-icon">
                        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                            <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/>
                            <circle cx="9" cy="7" r="4"/>
                            <path d="M23 21v-2a4 4 0 0 0-3-3.87"/>
                            <path d="M16 3.13a4 4 0 0 1 0 7.75"/>
                        </svg>
                    </span>
                    <span class="tf-nav-label">Đối tác</span>
                    <span class="tf-nav-chevron-wrap">
                        <svg class="tf-nav-chevron @(IsPartnersOpen ? "expanded" : "")" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                            <polyline points="9 18 15 12 9 6"/>
                        </svg>
                    </span>
                </button>

                @* Desktop Fly-out Submenu *@
                <div class="tf-nav-flyout">
                    <div class="tf-nav-flyout-header">Đối tác</div>
                    <div class="tf-nav-flyout-body">
                        @if (Has("Customers", "View"))
                        {
                            <NavLink class="tf-flyout-link" href="/doi-tac/khach-hang" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-flyout-dot"></span>
                                <span>Khách hàng</span>
                            </NavLink>
                        }
                        @if (Has("Suppliers", "View"))
                        {
                            <NavLink class="tf-flyout-link" href="/doi-tac/nha-cung-cap" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-flyout-dot"></span>
                                <span>Nhà cung cấp</span>
                            </NavLink>
                        }
                        @if (Has("Customers", "View"))
                        {
                            <NavLink class="tf-flyout-link" href="/doi-tac/cong-no" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-flyout-dot"></span>
                                <span>Công nợ</span>
                            </NavLink>
                        }
                    </div>
                </div>

                @* Mobile / Click Accordion Submenu *@
                <div class="tf-nav-sub @(IsPartnersOpen ? "expanded" : "")">
                    @if (Has("Customers", "View"))
                    {
                        <div class="tf-nav-sub-item">
                            <NavLink class="tf-nav-sub-link" href="/doi-tac/khach-hang" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-nav-sub-dot"></span>Khách hàng
                            </NavLink>
                        </div>
                    }
                    @if (Has("Suppliers", "View"))
                    {
                        <div class="tf-nav-sub-item">
                            <NavLink class="tf-nav-sub-link" href="/doi-tac/nha-cung-cap" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-nav-sub-dot"></span>Nhà cung cấp
                            </NavLink>
                        </div>
                    }
                    @if (Has("Customers", "View"))
                    {
                        <div class="tf-nav-sub-item">
                            <NavLink class="tf-nav-sub-link" href="/doi-tac/cong-no" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-nav-sub-dot"></span>Công nợ
                            </NavLink>
                        </div>
                    }
                </div>
            </div>
        }

        @* 8. HỢP ĐỒNG (STANDALONE DIRECT LINK) *@
        @if (Has("Contracts", "View"))
        {
            <div class="tf-nav-item @(IsSectionActive("hop-dong") ? "is-active" : "")">
                <NavLink class="tf-nav-link" href="/hop-dong" Match="NavLinkMatch.Prefix" ActiveClass="active" @onclick="HandleLinkClick" title="Hợp đồng">
                    <span class="tf-nav-icon">
                        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                            <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
                            <polyline points="14 2 14 8 20 8"/>
                            <line x1="16" y1="13" x2="8" y2="13"/>
                            <line x1="16" y1="17" x2="8" y2="17"/>
                            <polyline points="10 9 9 9 8 9"/>
                        </svg>
                    </span>
                    <span class="tf-nav-label">Hợp đồng</span>
                </NavLink>
                <div class="tf-nav-flyout tf-nav-flyout-single">
                    <div class="tf-nav-flyout-header" style="border-bottom:none; margin-bottom:0; padding:6px 12px; font-size:12px; font-weight:600; color:#1e293b; text-transform:none;">Hợp đồng</div>
                </div>
            </div>
        }

        @* 9. HỒ SƠ & CHỨNG TỪ *@
        @if (Has("Documents", "View"))
        {
            <div class="tf-nav-item @(IsSectionActive("tai-lieu") ? "is-active" : "")">
                <button type="button" class="tf-nav-link @(IsSectionActive("tai-lieu") ? "active" : "")" @onclick="ToggleDocuments" title="Hồ sơ &amp; Chứng từ">
                    <span class="tf-nav-icon">
                        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                            <path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/>
                        </svg>
                    </span>
                    <span class="tf-nav-label">Chứng từ</span>
                    <span class="tf-nav-chevron-wrap">
                        <svg class="tf-nav-chevron @(IsDocumentsOpen ? "expanded" : "")" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                            <polyline points="9 18 15 12 9 6"/>
                        </svg>
                    </span>
                </button>

                @* Desktop Fly-out Submenu *@
                <div class="tf-nav-flyout">
                    <div class="tf-nav-flyout-header">Hồ sơ &amp; Chứng từ</div>
                    <div class="tf-nav-flyout-body">
                        <NavLink class="tf-flyout-link" href="/tai-lieu" Match="NavLinkMatch.All" ActiveClass="active" @onclick="HandleLinkClick">
                            <span class="tf-flyout-dot"></span>
                            <span>Tài liệu lưu trữ</span>
                        </NavLink>
                        <NavLink class="tf-flyout-link" href="/tai-lieu/danh-muc" Match="NavLinkMatch.All" ActiveClass="active" @onclick="HandleLinkClick">
                            <span class="tf-flyout-dot"></span>
                            <span>Danh mục chứng từ</span>
                        </NavLink>
                    </div>
                </div>

                @* Mobile / Click Accordion Submenu *@
                <div class="tf-nav-sub @(IsDocumentsOpen ? "expanded" : "")">
                    <div class="tf-nav-sub-item">
                        <NavLink class="tf-nav-sub-link" href="/tai-lieu" Match="NavLinkMatch.All" ActiveClass="active" @onclick="HandleLinkClick">
                            <span class="tf-nav-sub-dot"></span>Tài liệu lưu trữ
                        </NavLink>
                    </div>
                    <div class="tf-nav-sub-item">
                        <NavLink class="tf-nav-sub-link" href="/tai-lieu/danh-muc" Match="NavLinkMatch.All" ActiveClass="active" @onclick="HandleLinkClick">
                            <span class="tf-nav-sub-dot"></span>Danh mục chứng từ
                        </NavLink>
                    </div>
                </div>
            </div>
        }

        @* 10. NHÂN VIÊN *@
        @if (HasAny(("Employees", "View"), ("EmployeeRequests", "View")))
        {
            <div class="tf-nav-item @(IsSectionActive("nhan-vien") ? "is-active" : "")">
                <button type="button" class="tf-nav-link @(IsSectionActive("nhan-vien") ? "active" : "")" @onclick="ToggleEmployees" title="Nhân viên">
                    <span class="tf-nav-icon">
                        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                            <rect x="3" y="4" width="18" height="16" rx="2"/>
                            <circle cx="9" cy="10" r="2"/>
                            <line x1="15" y1="8" x2="17" y2="8"/>
                            <line x1="15" y1="12" x2="17" y2="12"/>
                            <path d="M6 16a3 3 0 0 1 6 0"/>
                        </svg>
                    </span>
                    <span class="tf-nav-label">Nhân viên</span>
                    <span class="tf-nav-chevron-wrap">
                        <svg class="tf-nav-chevron @(IsEmployeesOpen ? "expanded" : "")" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                            <polyline points="9 18 15 12 9 6"/>
                        </svg>
                    </span>
                </button>

                @* Desktop Fly-out Submenu *@
                <div class="tf-nav-flyout">
                    <div class="tf-nav-flyout-header">Nhân sự &amp; Nhân viên</div>
                    <div class="tf-nav-flyout-body">
                        @if (Has("Employees", "View"))
                        {
                            <NavLink class="tf-flyout-link" href="/nhan-vien" Match="NavLinkMatch.All" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-flyout-dot"></span>
                                <span>Danh sách nhân viên</span>
                            </NavLink>
                        }
                        @if (Has("EmployeeRequests", "View"))
                        {
                            <NavLink class="tf-flyout-link" href="/nhan-vien?tab=requests" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-flyout-dot"></span>
                                <span>Duyệt yêu cầu</span>
                            </NavLink>
                        }
                    </div>
                </div>

                @* Mobile / Click Accordion Submenu *@
                <div class="tf-nav-sub @(IsEmployeesOpen ? "expanded" : "")">
                    @if (Has("Employees", "View"))
                    {
                        <div class="tf-nav-sub-item">
                            <NavLink class="tf-nav-sub-link" href="/nhan-vien" Match="NavLinkMatch.All" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-nav-sub-dot"></span>Danh sách nhân viên
                            </NavLink>
                        </div>
                    }
                    @if (Has("EmployeeRequests", "View"))
                    {
                        <div class="tf-nav-sub-item">
                            <NavLink class="tf-nav-sub-link" href="/nhan-vien?tab=requests" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-nav-sub-dot"></span>Duyệt yêu cầu
                            </NavLink>
                        </div>
                    }
                </div>
            </div>
        }

        @* 11. CÀI ĐẶT & HỆ THỐNG *@
        @if (HasAny(("CompanySettings", "View"), ("UnitOfMeasures", "View"), ("Users", "View"), ("Roles", "View"), ("AuditLog", "View"), ("TechnicalSupport", "View")))
        {
            <div class="tf-nav-item align-bottom @(IsSectionActive("cai-dat") || IsSectionActive("ho-tro") || IsSectionActive("ho-tro-ky-thuat") ? "is-active" : "")">
                <button type="button" class="tf-nav-link @(IsSectionActive("cai-dat") || IsSectionActive("ho-tro") || IsSectionActive("ho-tro-ky-thuat") ? "active" : "")" @onclick="ToggleSettings" title="Cài đặt &amp; Hệ thống">
                    <span class="tf-nav-icon">
                        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                            <circle cx="12" cy="12" r="3"/>
                            <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"/>
                        </svg>
                    </span>
                    <span class="tf-nav-label">Cài đặt</span>
                    <span class="tf-nav-chevron-wrap">
                        <svg class="tf-nav-chevron @(IsSettingsOpen ? "expanded" : "")" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                            <polyline points="9 18 15 12 9 6"/>
                        </svg>
                    </span>
                </button>

                @* Desktop Fly-out Submenu *@
                <div class="tf-nav-flyout">
                    <div class="tf-nav-flyout-header">Trung tâm Cài đặt</div>
                    <div class="tf-nav-flyout-body">
                        <NavLink class="tf-flyout-link" href="/cai-dat" Match="NavLinkMatch.All" ActiveClass="active" @onclick="HandleLinkClick">
                            <span class="tf-flyout-dot"></span>
                            <span style="font-weight: 600;">Trung tâm Cài đặt (Hub)</span>
                        </NavLink>
                        @if (Has("Roles", "View"))
                        {
                            <NavLink class="tf-flyout-link" href="/cai-dat/vai-tro" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-flyout-dot"></span>
                                <span style="font-weight:600; color:#4f46e5;">Vai trò &amp; Phân quyền</span>
                            </NavLink>
                        }
                        @if (Has("CompanySettings", "View"))
                        {
                            <NavLink class="tf-flyout-link" href="/cai-dat/cong-ty" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-flyout-dot"></span>
                                <span>Hệ thống &amp; Thương hiệu</span>
                            </NavLink>
                        }
                        @if (Has("UnitOfMeasures", "View"))
                        {
                            <NavLink class="tf-flyout-link" href="/danh-muc/don-vi-tinh" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-flyout-dot"></span>
                                <span>Danh mục &amp; Quy ước</span>
                            </NavLink>
                        }
                        @if (Has("Users", "View"))
                        {
                            <NavLink class="tf-flyout-link" href="/cai-dat/nguoi-dung" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-flyout-dot"></span>
                                <span>Người dùng &amp; An ninh</span>
                            </NavLink>
                        }
                        @if (Has("AuditLog", "View"))
                        {
                            <NavLink class="tf-flyout-link" href="/cai-dat/nhat-ky-he-thong" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-flyout-dot"></span>
                                <span>Nhật ký &amp; Kiểm toán</span>
                            </NavLink>
                        }
                        @if (Has("CompanySettings", "View"))
                        {
                            <NavLink class="tf-flyout-link" href="/cai-dat/sao-luu" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-flyout-dot"></span>
                                <span>Dữ liệu &amp; Kiểm soát</span>
                            </NavLink>
                        }
                        @if (Has("TechnicalSupport", "View"))
                        {
                            <NavLink class="tf-flyout-link" href="/cai-dat/ho-tro" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-flyout-dot"></span>
                                <span>Hỗ trợ &amp; Trợ giúp</span>
                            </NavLink>
                        }
                    </div>
                </div>

                @* Mobile / Click Accordion Submenu *@
                <div class="tf-nav-sub @(IsSettingsOpen ? "expanded" : "")">
                    <div class="tf-nav-sub-item">
                        <NavLink class="tf-nav-sub-link" href="/cai-dat" Match="NavLinkMatch.All" ActiveClass="active" @onclick="HandleLinkClick">
                            <span class="tf-nav-sub-dot"></span><span style="font-weight: 600;">Trung tâm Cài đặt (Hub)</span>
                        </NavLink>
                    </div>
                    @if (Has("Roles", "View"))
                    {
                        <div class="tf-nav-sub-item">
                            <NavLink class="tf-nav-sub-link" href="/cai-dat/vai-tro" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-nav-sub-dot"></span><span style="font-weight:600; color:#4f46e5;">Vai trò &amp; Phân quyền</span>
                            </NavLink>
                        </div>
                    }
                    @if (Has("CompanySettings", "View"))
                    {
                        <div class="tf-nav-sub-item">
                            <NavLink class="tf-nav-sub-link" href="/cai-dat/cong-ty" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-nav-sub-dot"></span>Hệ thống &amp; Thương hiệu
                            </NavLink>
                        </div>
                    }
                    @if (Has("UnitOfMeasures", "View"))
                    {
                        <div class="tf-nav-sub-item">
                            <NavLink class="tf-nav-sub-link" href="/danh-muc/don-vi-tinh" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-nav-sub-dot"></span>Danh mục &amp; Quy ước
                            </NavLink>
                        </div>
                    }
                    @if (Has("Users", "View"))
                    {
                        <div class="tf-nav-sub-item">
                            <NavLink class="tf-nav-sub-link" href="/cai-dat/nguoi-dung" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-nav-sub-dot"></span>Người dùng &amp; An ninh
                            </NavLink>
                        </div>
                    }
                    @if (Has("AuditLog", "View"))
                    {
                        <div class="tf-nav-sub-item">
                            <NavLink class="tf-nav-sub-link" href="/cai-dat/nhat-ky-he-thong" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-nav-sub-dot"></span>Nhật ký &amp; Kiểm toán
                            </NavLink>
                        </div>
                    }
                    @if (Has("CompanySettings", "View"))
                    {
                        <div class="tf-nav-sub-item">
                            <NavLink class="tf-nav-sub-link" href="/cai-dat/sao-luu" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-nav-sub-dot"></span>Dữ liệu &amp; Kiểm soát
                            </NavLink>
                        </div>
                    }
                    @if (Has("TechnicalSupport", "View"))
                    {
                        <div class="tf-nav-sub-item">
                            <NavLink class="tf-nav-sub-link" href="/cai-dat/ho-tro" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-nav-sub-dot"></span>Hỗ trợ &amp; Trợ giúp
                            </NavLink>
                        </div>
                    }
                </div>
            </div>
        }

    </div>

    @* Bottom Pinned Dock — Collapse Toggle Button *@
    <div class="tf-nav-bottom">
        <button type="button" class="tf-nav-bottom-item tf-nav-collapse-btn" @onclick="OnToggleCollapse" title="@(IsCollapsed ? "Mở rộng thanh menu (Ctrl+B)" : "Thu gọn thanh menu (Ctrl+B)")" aria-label="@(IsCollapsed ? "Mở rộng thanh menu" : "Thu gọn thanh menu")" aria-expanded="@(!IsCollapsed)" style="width: 100%; border: none; background: transparent; cursor: pointer;">
            <span class="tf-nav-icon">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="transform:@(IsCollapsed ? "rotate(180deg)" : "rotate(0deg)"); transition: transform 0.2s;">
                    <polyline points="11 19 4 12 11 5"/>
                    <polyline points="18 19 11 12 18 5"/>
                </svg>
            </span>
            <span class="tf-nav-label">Thu gọn</span>
        </button>
    </div>
</nav>

@code {
    [Parameter] public bool IsCollapsed { get; set; }
    [Parameter] public EventCallback OnToggleCollapse { get; set; }
    [Parameter] public EventCallback OnCloseMobile { get; set; }

    private bool IsInvoiceOpen { get; set; }
    private bool IsCatalogOpen { get; set; }
    private bool IsWarehouseOpen { get; set; }
    private bool IsPartnersOpen { get; set; }
    private bool IsDocumentsOpen { get; set; }
    private bool IsEmployeesOpen { get; set; }
    private bool IsSettingsOpen { get; set; }

    private string _currentUrl = "";
    private bool _isAuthenticated;
    private bool _isAdmin;
    private HashSet<string> _permissions = new(StringComparer.OrdinalIgnoreCase);

    protected override async Task OnInitializedAsync()
    {
        _currentUrl = Nav.ToBaseRelativePath(Nav.Uri).TrimStart('/');
        AutoExpandSection(_currentUrl);
        Nav.LocationChanged += OnLocationChanged;

        try
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            _isAuthenticated = user.Identity?.IsAuthenticated == true;
            if (_isAuthenticated)
            {
                _isAdmin = user.IsInRole("Administrator");
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    _permissions = await PermissionService.GetEffectivePermissionsAsync(userId);
                }
            }
        }
        catch
        {
            // Fallback gracefully
        }
    }

    private bool Has(string resource, string action = "View")
    {
        if (_isAdmin) return true;
        return _permissions.Contains($"{resource}:{action}");
    }

    private bool HasAny(params (string resource, string action)[] checks)
    {
        if (_isAdmin) return true;
        return checks.Any(c => _permissions.Contains($"{c.resource}:{c.action}"));
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        _currentUrl = Nav.ToBaseRelativePath(e.Location).TrimStart('/');
        AutoExpandSection(_currentUrl);
        StateHasChanged();
    }

    private async Task HandleLinkClick()
    {
        await OnCloseMobile.InvokeAsync();
    }

    private bool IsSectionActive(string prefix)
    {
        if (string.IsNullOrEmpty(_currentUrl)) return false;
        return _currentUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsInvoiceSectionActive()
    {
        return IsSectionActive("hoa-don") 
            || IsSectionActive("ban-hang/don-ban-hang") 
            || IsSectionActive("ban-hang/bao-gia");
    }

    private void AutoExpandSection(string url)
    {
        if (url.StartsWith("hoa-don", StringComparison.OrdinalIgnoreCase) 
            || url.StartsWith("ban-hang/don-ban-hang", StringComparison.OrdinalIgnoreCase) 
            || url.StartsWith("ban-hang/bao-gia", StringComparison.OrdinalIgnoreCase)) 
        {
            IsInvoiceOpen = true;
        }
        if (url.StartsWith("danh-muc", StringComparison.OrdinalIgnoreCase) || url.StartsWith("bang-gia", StringComparison.OrdinalIgnoreCase)) IsCatalogOpen = true;
        if (url.StartsWith("kho", StringComparison.OrdinalIgnoreCase)) IsWarehouseOpen = true;
        if (url.StartsWith("doi-tac", StringComparison.OrdinalIgnoreCase)) IsPartnersOpen = true;
        if (url.StartsWith("tai-lieu", StringComparison.OrdinalIgnoreCase)) IsDocumentsOpen = true;
        if (url.StartsWith("nhan-vien", StringComparison.OrdinalIgnoreCase)) IsEmployeesOpen = true;
        if (url.StartsWith("cai-dat", StringComparison.OrdinalIgnoreCase) || url.StartsWith("ho-tro", StringComparison.OrdinalIgnoreCase)) IsSettingsOpen = true;
    }

    private void ToggleInvoice() => IsInvoiceOpen = !IsInvoiceOpen;
    private void ToggleCatalog() => IsCatalogOpen = !IsCatalogOpen;
    private void ToggleWarehouse() => IsWarehouseOpen = !IsWarehouseOpen;
    private void TogglePartners() => IsPartnersOpen = !IsPartnersOpen;
    private void ToggleDocuments() => IsDocumentsOpen = !IsDocumentsOpen;
    private void ToggleEmployees() => IsEmployeesOpen = !IsEmployeesOpen;
    private void ToggleSettings() => IsSettingsOpen = !IsSettingsOpen;

    public void Dispose()
    {
        Nav.LocationChanged -= OnLocationChanged;
    }
}
`;

fs.writeFileSync('src/TradeFlow.Web/Components/Layout/NavMenu.razor', navMenuCode, 'utf8');
console.log('NavMenu.razor successfully updated with full RBAC auto-filtering!');
