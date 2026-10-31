const fs = require('fs');
const file = 'C:/Users/thitr/.gemini/antigravity/scratch/TradeFlow/src/TradeFlow.Web/Components/Pages/Contracts/ContractTemplateGallery.razor';
let content = fs.readFileSync(file, 'utf8');

// 1. Ensure @key="t.Code" on tf-gallery-card
if (!content.includes('@key="t.Code"')) {
    content = content.replace(
        /(<div class="tf-gallery-card [^"]*")(\s+tabindex="0")/g,
        '$1 @key="t.Code"$2'
    );
    console.log('Added @key="t.Code"');
}

// 2. Fix broken SVG path
content = content.replace(
    'd="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8z"',
    'd="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7Z"'
);

// 3. Category bar loop
const catRegex = /@foreach\s*\(\s*var cat in AvailableCategories\s*\)[\s\S]*?<\/button>\s*}/;
const catNew = `@foreach (var catItem in _categoriesWithCount)
            {
                var isCatActive = selectedCategory == catItem.Category;
                <button type="button"
                        class="tf-cat-pill @(isCatActive ? "active" : "")"
                        @onclick="() => SelectCategory(catItem.Category)"
                        role="tab"
                        aria-selected="@isCatActive">
                    <span class="tf-cat-name">@catItem.Category</span>
                    <span class="tf-cat-count">@catItem.Count</span>
                </button>
            }`;

if (catRegex.test(content)) {
    content = content.replace(catRegex, catNew);
    console.log('Replaced category loop with _categoriesWithCount');
}

// 4. Update FilteredTemplates in markup
content = content.replace(/@FilteredTemplates\.Count\(\)/g, '@_cachedFilteredTemplates.Count');
content = content.replace(/!FilteredTemplates\.Any\(\)/g, '!_cachedFilteredTemplates.Any()');
content = content.replace(/var t in FilteredTemplates/g, 'var t in _cachedFilteredTemplates');

// 5. Replace fields and properties
const propRegex = /private IEnumerable<string> AvailableCategories[\s\S]*?private void SelectCategory\(string\? category\)[\s\S]*?activeCardCode = null;\s*}/;
const propNew = `private List<(string Category, int Count)> _categoriesWithCount = new();
    private List<ContractTemplateDto> _cachedFilteredTemplates = new();
    private List<ContractTemplateDto>? _lastTemplates;

    protected override void OnParametersSet()
    {
        if (Templates != _lastTemplates)
        {
            _lastTemplates = Templates;
            ComputeCategories();
            ComputeFilteredTemplates();
        }
    }

    private void ComputeCategories()
    {
        if (Templates == null || Templates.Count == 0)
        {
            _categoriesWithCount.Clear();
            return;
        }

        _categoriesWithCount = Templates
            .GroupBy(t => t.Category)
            .Select(g => (Category: g.Key, Count: g.Count()))
            .OrderBy(g => g.Category)
            .ToList();
    }

    private void ComputeFilteredTemplates()
    {
        if (Templates == null)
        {
            _cachedFilteredTemplates = new List<ContractTemplateDto>();
            return;
        }

        IEnumerable<ContractTemplateDto> list = Templates;

        if (!string.IsNullOrWhiteSpace(selectedCategory))
        {
            list = list.Where(t => t.Category.Equals(selectedCategory, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var q = searchQuery.Trim().ToLowerInvariant();
            list = list.Where(t =>
                t.Title.ToLowerInvariant().Contains(q) ||
                t.Category.ToLowerInvariant().Contains(q) ||
                t.Description.ToLowerInvariant().Contains(q) ||
                t.TypicalUseCase.ToLowerInvariant().Contains(q) ||
                t.LegalBasis.ToLowerInvariant().Contains(q) ||
                t.Code.ToLowerInvariant().Contains(q)
            );
        }

        _cachedFilteredTemplates = list.OrderBy(t => t.DisplayOrder).ToList();
    }

    private void SelectCategory(string? category)
    {
        if (selectedCategory == category) return;
        selectedCategory = category;
        activeCardCode = null;
        ComputeFilteredTemplates();
    }`;

if (propRegex.test(content)) {
    content = content.replace(propRegex, propNew);
    console.log('Replaced FilteredTemplates and AvailableCategories with cached logic');
}

// 6. Replace OnSearchInput, ClearSearch, ResetFilters to call ComputeFilteredTemplates
content = content.replace(
    'searchQuery = e.Value?.ToString() ?? string.Empty;\r\n        activeCardCode = null;',
    'searchQuery = e.Value?.ToString() ?? string.Empty;\r\n        activeCardCode = null;\r\n        ComputeFilteredTemplates();'
);
content = content.replace(
    'searchQuery = string.Empty;\r\n        activeCardCode = null;',
    'searchQuery = string.Empty;\r\n        activeCardCode = null;\r\n        ComputeFilteredTemplates();'
);
content = content.replace(
    'selectedCategory = null;\r\n        searchQuery = string.Empty;\r\n        activeCardCode = null;',
    'selectedCategory = null;\r\n        searchQuery = string.Empty;\r\n        activeCardCode = null;\r\n        ComputeFilteredTemplates();'
);

// 7. Remove refreshGalleryMotion in OnAfterRenderAsync
const renderRegex = /if \(firstRender\)[\s\S]*?refreshGalleryMotion[\s\S]*?}\s*}/;
const renderNew = `if (firstRender)
            {
                motionModule = await JS.InvokeAsync<IJSObjectReference>("import", "./js/contract-gallery-motion.js");
                await motionModule.InvokeVoidAsync("initGalleryMotion", galleryContainer);
            }`;

if (renderRegex.test(content)) {
    content = content.replace(renderRegex, renderNew);
    console.log('Removed refreshGalleryMotion SignalR call');
}

fs.writeFileSync(file, content, 'utf8');
console.log('Updated ContractTemplateGallery.razor successfully!');
