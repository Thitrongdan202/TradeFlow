param(
    [string]$ZipPath,
    [string]$OutputDir
)

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

$manifestEntry = $zip.GetEntry("manifest.json")
if ($manifestEntry -ne $null) {
    $stream = $manifestEntry.Open()
    $reader = New-Object System.IO.StreamReader($stream)
    $text = $reader.ReadToEnd()
    $reader.Close()
    $stream.Close()
    [System.IO.File]::WriteAllText("$OutputDir/manifest.json", $text, $utf8NoBom)
}

$checksumsEntry = $zip.GetEntry("checksums.json")
if ($checksumsEntry -ne $null) {
    $stream = $checksumsEntry.Open()
    $reader = New-Object System.IO.StreamReader($stream)
    $text = $reader.ReadToEnd()
    $reader.Close()
    $stream.Close()
    [System.IO.File]::WriteAllText("$OutputDir/checksums.json", $text, $utf8NoBom)
}

$entries = $zip.Entries | Select-Object -ExpandProperty FullName
$zip.Dispose()

$entries | Out-File -FilePath "$OutputDir/entries.txt" -Encoding utf8
Write-Host "SUCCESS: Extracted metadata for $($entries.Count) entries."
