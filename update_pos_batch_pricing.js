const fs = require('fs');
const file = 'C:/Users/thitr/.gemini/antigravity/scratch/TradeFlow/src/TradeFlow.Web/Components/Pages/Sales/Pos/PosIndex.razor';
let content = fs.readFileSync(file, 'utf8');

const target1 = `        // Cache prices for catalog products
        foreach (var p in _catalogProducts)
        {
            if (!_productPrices.ContainsKey(p.Id))
            {
                var priceResult = await SalesService.GetProductPriceAsync(p.Id, ActiveTab.PriceListId, ActiveTab.OrderDate);
                _productPrices[p.Id] = priceResult.Price;
            }

            if (!_productImages.ContainsKey(p.Id))
            {
                var primaryImg = p.Images.FirstOrDefault(i => i.IsPrimary) ?? p.Images.FirstOrDefault();
                _productImages[p.Id] = primaryImg != null ? FileStorage.GetPublicUrl(primaryImg.StorageReference) : null;
            }
        }`.replace(/\r?\n/g, '\r\n');

const replacement1 = `        // Cache prices in batch for catalog products
        var unpricedIds = _catalogProducts.Where(p => !_productPrices.ContainsKey(p.Id)).Select(p => p.Id).ToList();
        if (unpricedIds.Any())
        {
            var batchPrices = await SalesService.GetProductPricesBatchAsync(unpricedIds, ActiveTab.PriceListId, ActiveTab.OrderDate);
            foreach (var kvp in batchPrices)
            {
                _productPrices[kvp.Key] = kvp.Value;
            }
        }

        foreach (var p in _catalogProducts)
        {
            if (!_productImages.ContainsKey(p.Id))
            {
                var primaryImg = p.Images.FirstOrDefault(i => i.IsPrimary) ?? p.Images.FirstOrDefault();
                _productImages[p.Id] = primaryImg != null ? FileStorage.GetPublicUrl(primaryImg.StorageReference) : null;
            }
        }`.replace(/\r?\n/g, '\r\n');

const target2 = `            foreach (var prod in _f3Results)
            {
                if (!_productPrices.ContainsKey(prod.Id))
                {
                    var priceResult = await SalesService.GetProductPriceAsync(prod.Id, ActiveTab.PriceListId, ActiveTab.OrderDate);
                    _productPrices[prod.Id] = priceResult.Price;
                }
            }`.replace(/\r?\n/g, '\r\n');

const replacement2 = `            var unpricedF3Ids = _f3Results.Where(p => !_productPrices.ContainsKey(p.Id)).Select(p => p.Id).ToList();
            if (unpricedF3Ids.Any())
            {
                var batchPrices = await SalesService.GetProductPricesBatchAsync(unpricedF3Ids, ActiveTab.PriceListId, ActiveTab.OrderDate);
                foreach (var kvp in batchPrices)
                {
                    _productPrices[kvp.Key] = kvp.Value;
                }
            }`.replace(/\r?\n/g, '\r\n');

content = content.replace(target1, replacement1);
content = content.replace(target2, replacement2);

fs.writeFileSync(file, content, 'utf8');
console.log('Successfully updated PosIndex.razor with batch pricing!');
