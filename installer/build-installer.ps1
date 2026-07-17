# =============================================================
# Inno Setup installer build for hs-rotary-club
# PowerShell 5.x cp950 ???;?豲??豲??擏???;????豲????# =============================================================

# ??ISCC.exe
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
    Write-Host "ERR: ISCC.exe not found. ?嚚????winget ??Inno Setup 6.x (Get-WmiObject / Get-Command ??" -ForegroundColor Red
    exit 2
}
Write-Host "[iscc]  $iscc"

# ???? installer/ 
Set-Location -LiteralPath $PSScriptRoot
Write-Host "[pwd]   $PWD"

# Step 1: dotnet publish (framework-dependent x64, single-file)
$appProj = "..\src\HsRotaryClub.App\HsRotaryClub.App.csproj"
Write-Host "[publish] dotnet publish $appProj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false"
& dotnet publish $appProj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERR: dotnet publish failed (exit $LASTEXITCODE)" -ForegroundColor Red
    exit $LASTEXITCODE
}
# dotnet publish ???格??豲? bin\Release\<tfm>\<rid>\publish\
$publishDir = "..\src\HsRotaryClub.App\bin\Release\net8.0-windows\win-x64\publish"
if (-not (Test-Path -LiteralPath $publishDir)) {
    Write-Host "ERR: publish dir not found: $publishDir" -ForegroundColor Red
    exit 3
}

# ??????exe ?鞈? build ???
$exe = Join-Path $publishDir "HsRotaryClub.App.exe"
if (-not (Test-Path -LiteralPath $exe)) {
    Write-Host "ERR: $exe not found" -ForegroundColor Red
    exit 4
}
Write-Host "[exe]    $exe  ($( (Get-Item -LiteralPath $exe).Length ) bytes)"

# Step 2: ISCC ?蝞???installer
Write-Host "[iscc compile]"
& $iscc HsRotaryClub.iss
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERR: ISCC failed (exit $LASTEXITCODE)" -ForegroundColor Red
    exit $LASTEXITCODE
}

# Step 3: ?雓???謜眾??$outDir = "bin"
if (Test-Path -LiteralPath $outDir) {
    Write-Host "[output]"
    Get-ChildItem -LiteralPath $outDir -Filter "*.exe" | ForEach-Object {
        Write-Host (" - {0}  ({1:N0} bytes)" -f $_.Name, $_.Length)
    }
}
