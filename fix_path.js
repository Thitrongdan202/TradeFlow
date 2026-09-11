const fs = require('fs');
let file = 'src/TradeFlow.Web/Program.cs';
let content = fs.readFileSync(file, 'utf8');

content = content.replace(
    'var path = @"wwwroot/uploads/pricing_originals/2139c1f289974538bb5021b3b090d461_0908. GIA D?I LY_ LACASA.xlsx";',
    'var path = System.IO.Directory.GetFiles(@"wwwroot/uploads/pricing_originals", "*.xlsx")[0];'
);

fs.writeFileSync(file, content, 'utf8');
