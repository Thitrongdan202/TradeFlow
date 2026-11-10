Add-Type -Path 'C:\Users\thitr\.gemini\antigravity\scratch\TradeFlow\src\TradeFlow.Web\bin\Debug\net10.0\Npgsql.dll'
$conn = New-Object Npgsql.NpgsqlConnection('Host=localhost;Database=tradeflow_dev;Username=postgres;Password=postgres')
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT table_name FROM information_schema.tables WHERE table_schema='public'"
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    Write-Output $reader.GetString(0)
}
$reader.Close()

$cmd2 = $conn.CreateCommand()
$cmd2.CommandText = "SELECT column_name FROM information_schema.columns WHERE table_name='Roles'"
$reader2 = $cmd2.ExecuteReader()
Write-Output "--- Roles Columns ---"
while ($reader2.Read()) {
    Write-Output $reader2.GetString(0)
}
$reader2.Close()

$conn.Close()
