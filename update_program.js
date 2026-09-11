const fs = require('fs');
let file = 'src/TradeFlow.Web/Program.cs';
let content = fs.readFileSync(file, 'utf8');

let endpointCode = `
app.MapGet("/test-import", async (TradeFlow.Application.Common.Interfaces.IExcelPricingService excelService) => 
{ 
    try 
    { 
        var path = @"src/TradeFlow.Web/wwwroot/uploads/pricing_originals/2139c1f289974538bb5021b3b090d461_0908. GIA D?I LY_ LACASA.xlsx"; 
        using var stream = System.IO.File.OpenRead(path); 
        var result = await excelService.AnalyzeAndDryRunAsync(stream, "test.xlsx"); 
        var commitReq = new TradeFlow.Application.Common.Models.Pricing.ExcelImportCommitRequest 
        { 
            OriginalFileName = result.FileName, 
            TempFileReference = result.TempFileReference, 
            AutoCreateProducts = true, 
            Items = result.Items 
        }; 
        var priceList = await excelService.CommitImportAsync(commitReq, "admin"); 
        return Microsoft.AspNetCore.Http.Results.Ok(priceList); 
    } 
    catch (Exception ex) 
    { 
        return Microsoft.AspNetCore.Http.Results.Problem(ex.ToString()); 
    } 
});
`;

if (!content.includes('/test-import')) {
    content = content.replace('app.Run();', endpointCode + '\napp.Run();');
    fs.writeFileSync(file, content, 'utf8');
}
