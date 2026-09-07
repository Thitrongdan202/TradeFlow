$file = "src\TradeFlow.Web\Components\Pages\Sales\Invoices\Detail.razor"
$content = [System.IO.File]::ReadAllText($file, [System.Text.Encoding]::UTF8)

$search = "</tr>`n            </table>"
$replace = "</tr>`n                <tr>`n                    <td colspan=""2"" style=""padding:12px 0 0 0; font-style:italic; font-size:14px; text-align:right; color:#64748b;"">`n                        Số tiền viết bằng chữ: @TradeFlow.Application.Common.Helpers.NumberToTextHelper.ConvertToWords((long)_invoice.GrandTotal) đồng`n                    </td>`n                </tr>`n            </table>"

$content = $content.Replace($search, $replace)
[System.IO.File]::WriteAllText($file, $content, [System.Text.Encoding]::UTF8)
