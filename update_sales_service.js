const fs = require('fs');
const file = 'C:/Users/thitr/.gemini/antigravity/scratch/TradeFlow/src/TradeFlow.Infrastructure/Services/SalesService.cs';
let content = fs.readFileSync(file, 'utf8');

const target = '    public async Task<decimal> GetActivePriceAsync(int productId, DateTime targetDate, CancellationToken cancellationToken = default)\r\n    {\r\n        var priceInfo = await GetProductPriceAsync(productId, null, targetDate, cancellationToken);\r\n        return priceInfo.Price ?? 0m;\r\n    }';

const batchMethod = `    public async Task<decimal> GetActivePriceAsync(int productId, DateTime targetDate, CancellationToken cancellationToken = default)
    {
        var priceInfo = await GetProductPriceAsync(productId, null, targetDate, cancellationToken);
        return priceInfo.Price ?? 0m;
    }

    public async Task<Dictionary<int, decimal>> GetProductPricesBatchAsync(IEnumerable<int> productIds, int? priceListId, DateTime targetDate, CancellationToken cancellationToken = default)
    {
        var pids = productIds.Distinct().ToList();
        var result = new Dictionary<int, decimal>();
        if (pids.Count == 0) return result;

        var utcTarget = NormalizeToUtc(targetDate);

        // 1. Specified priceListId
        if (priceListId.HasValue)
        {
            var items = await _context.PriceListItems
                .AsNoTracking()
                .Where(x => pids.Contains(x.ProductId) && x.PriceListId == priceListId.Value)
                .ToListAsync(cancellationToken);
            foreach (var item in items)
            {
                result[item.ProductId] = item.UnitPrice;
            }
        }

        // 2. Try active price list covering date
        var remainingIds = pids.Where(id => !result.ContainsKey(id)).ToList();
        if (remainingIds.Count > 0)
        {
            var activeItems = await _context.PriceListItems
                .AsNoTracking()
                .Include(x => x.PriceList)
                .Where(x => remainingIds.Contains(x.ProductId)
                         && x.PriceList!.Status == TradeFlow.Domain.Enums.PriceListStatus.Active
                         && (x.PriceList.EffectiveFrom == null || x.PriceList.EffectiveFrom <= utcTarget)
                         && (x.PriceList.EffectiveTo == null || x.PriceList.EffectiveTo >= utcTarget))
                .OrderByDescending(x => x.PriceList!.EffectiveFrom)
                .ToListAsync(cancellationToken);

            foreach (var item in activeItems)
            {
                if (!result.ContainsKey(item.ProductId))
                {
                    result[item.ProductId] = item.UnitPrice;
                }
            }
        }

        // 3. Fallback: latest Active PriceList item for this product
        remainingIds = pids.Where(id => !result.ContainsKey(id)).ToList();
        if (remainingIds.Count > 0)
        {
            var fallbackItems = await _context.PriceListItems
                .AsNoTracking()
                .Include(x => x.PriceList)
                .Where(x => remainingIds.Contains(x.ProductId)
                         && x.PriceList!.Status == TradeFlow.Domain.Enums.PriceListStatus.Active)
                .OrderByDescending(x => x.PriceList!.Year)
                .ThenByDescending(x => x.PriceList!.Quarter)
                .ThenByDescending(x => x.PriceList!.EffectiveFrom)
                .ToListAsync(cancellationToken);

            foreach (var item in fallbackItems)
            {
                if (!result.ContainsKey(item.ProductId))
                {
                    result[item.ProductId] = item.UnitPrice;
                }
            }
        }

        // 4. Fallback for any remainder
        remainingIds = pids.Where(id => !result.ContainsKey(id)).ToList();
        foreach (var rid in remainingIds)
        {
            var single = await GetProductPriceAsync(rid, priceListId, targetDate, cancellationToken);
            if (single.Price.HasValue)
            {
                result[rid] = single.Price.Value;
            }
        }

        return result;
    }`.replace(/\r?\n/g, '\r\n');

content = content.replace(target, batchMethod);
fs.writeFileSync(file, content, 'utf8');
console.log('Added GetProductPricesBatchAsync to SalesService.cs');
