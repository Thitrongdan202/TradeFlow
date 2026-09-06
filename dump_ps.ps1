[Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
$response = Invoke-WebRequest -Uri https://localhost:7185/Account/Login -MaximumRedirection 0
$response.Content | Out-File login_ps.html
