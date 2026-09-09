const fs = require('fs');
let code = fs.readFileSync('src/TradeFlow.Infrastructure/Services/SalesService.cs', 'utf8');

// Update SearchCustomersAsync
code = code.replace(
    'query = query.Where(x => x.Name.ToLower().Contains(lowerTerm) \n                                  || (x.TaxCode != null && x.TaxCode.ToLower().Contains(lowerTerm))\n                                  || (x.Phone != null && x.Phone.ToLower().Contains(lowerTerm)));',
    'var likeTerm = $"%{searchTerm}%";\n            query = query.Where(x => EF.Functions.ILike(x.Name, likeTerm) \n                                  || (x.TaxCode != null && EF.Functions.ILike(x.TaxCode, likeTerm))\n                                  || (x.Phone != null && EF.Functions.ILike(x.Phone, likeTerm)));'
);

// Update SearchProductsAsync
code = code.replace(
    'var lowerTerm = searchTerm.ToLower();\n            query = query.Where(x => x.Name.ToLower().Contains(lowerTerm) \n                                  || x.Code.ToLower().Contains(lowerTerm)\n                                  || (x.NewCode != null && x.NewCode.ToLower().Contains(lowerTerm))\n                                  || (x.LegacyCode != null && x.LegacyCode.ToLower().Contains(lowerTerm)));',
    'var likeTerm = $"%{searchTerm}%";\n            query = query.Where(x => EF.Functions.ILike(x.Name, likeTerm) \n                                  || EF.Functions.ILike(x.Code, likeTerm)\n                                  || (x.NewCode != null && EF.Functions.ILike(x.NewCode, likeTerm))\n                                  || (x.LegacyCode != null && EF.Functions.ILike(x.LegacyCode, likeTerm)));'
);

fs.writeFileSync('src/TradeFlow.Infrastructure/Services/SalesService.cs', code);
