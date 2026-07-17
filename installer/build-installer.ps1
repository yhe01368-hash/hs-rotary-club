# =============================================================
# Inno Setup installer build for hs-rotary-club
# PowerShell 5.x cp950 環境;路徑不可裸打;逐行不合併
# =============================================================

# 找 ISCC.exe
$iscc = $null
$candidates = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
)
foreach ($p in $candidates) {
    if (Test-Path -LiteralPath $p) { $iscc = $p; break }
}
if (-not $iscc) {
    Write-Host "ERR: ISCC.exe not found. 請先用 winget 裝 Inno Setup 6.x (Get-WmiObject / Get-Command 找)" -ForegroundColor Red
    exit 2
}
Write-Host "[iscc]  $iscc"

# 進到 installer/ 
Set-Location -LiteralPath $PSScriptRoot
Write-Host "[pwd]   $PWD"

# Step 1: dotnet publish (framework-dependent x64, single-file)
$appProj = "..\src\HsRotaryClub.App\HsRotaryClub.App.csproj"
Write-Host "[publish] dotnet publish $appProj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true"
& dotnet publish $appProj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERR: dotnet publish failed (exit $LASTEXITCODE)" -ForegroundColor Red
    exit $LASTEXITCODE
}
$publishDir = "..\src\HsRotaryClub.App\bin\publish\win-x64"
if (-not (Test-Path -LiteralPath $publishDir)) {
    Write-Host "ERR: publish dir not found: $publishDir" -ForegroundColor Red
    exit 3
}

# 確認主 exe 真的 build 出來
$exe = Join-Path $publishDir "HsRotaryClub.App.exe"
if (-not (Test-Path -LiteralPath $exe)) {
    Write-Host "ERR: $exe not found" -ForegroundColor Red
    exit 4
}
Write-Host "[exe]    $exe  ($( (Get-Item -LiteralPath $exe).Length ) bytes)"

# Step 2: ISCC 編譯 installer
Write-Host "[iscc compile]"
& $iscc HsRotaryClub.iss
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERR: ISCC failed (exit $LASTEXITCODE)" -ForegroundColor Red
    exit $LASTEXITCODE
}

# Step 3: 列出輸出
$outDir = "bin"
if (Test-Path -LiteralPath $outDir) {
    Write-Host "[output]"
    Get-ChildItem -LiteralPath $outDir -Filter "*.exe" | ForEach-Object {
        Write-Host (" - {0}  ({1:N0} bytes)" -f $_.Name, $_.Length)
    }
}
