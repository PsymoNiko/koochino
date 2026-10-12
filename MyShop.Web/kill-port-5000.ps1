# آزاد کردن پورت 5000 - اجرا با PowerShell
$port = 5000
$conn = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
if ($conn) {
    $conn | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue }
    Write-Host "پورت $port آزاد شد."
} else {
    Write-Host "هیچ فرایندی روی پورت $port نیست."
}
