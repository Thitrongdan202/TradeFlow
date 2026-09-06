[Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

# 1. Login POST
$loginParams = @{
    Uri = 'https://localhost:7185/Account/Login'
    Method = 'POST'
    Body = '_handler=login&Input.Username=admin&Input.Password=tradecore123'
    ContentType = 'application/x-www-form-urlencoded'
    WebSession = $session
}
try {
    Invoke-WebRequest @loginParams -MaximumRedirection 0 -ErrorAction Stop | Out-Null
} catch {
    Write-Host "Login response status: " $_.Exception.Response.StatusCode
}

Write-Host "Cookies:" $session.Cookies.GetCookies('https://localhost:7185').Count

# 2. Get Dashboard
try {
    $dashParams = @{
        Uri = 'https://localhost:7185/'
        Method = 'GET'
        WebSession = $session
        Headers = @{
            'Accept' = 'text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8'
            'User-Agent' = 'Mozilla/5.0'
        }
    }
    $dashResp = Invoke-WebRequest @dashParams
    Write-Host "Dashboard Status: " $dashResp.StatusCode
} catch {
    Write-Host "Dashboard Failed: " $_.Exception.Message
    if ($_.Exception.Response) {
        Write-Host "Dashboard Status: " $_.Exception.Response.StatusCode
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        Write-Host "Dashboard Body: " $reader.ReadToEnd()
    }
}
