# PowerShell Build Script
# Encoding: UTF-8

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
Write-Host "Building War3Trainer Release version..." -ForegroundColor Green
Write-Host ""

# Find MSBuild
$msbuildPaths = @(
    "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe",
    "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe",
    "C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
)

$msbuildPath = $null
foreach ($path in $msbuildPaths) {
    if (Test-Path $path) {
        $msbuildPath = $path
        break
    }
}

if ($null -eq $msbuildPath) {
    Write-Host "Error: MSBuild.exe not found" -ForegroundColor Red
    Write-Host "Please ensure Visual Studio or .NET Framework SDK is installed" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "Using MSBuild: $msbuildPath" -ForegroundColor Cyan
Write-Host ""

# Clean previous build
Write-Host "Cleaning previous build..." -ForegroundColor Yellow
& $msbuildPath War3Trainer.sln /t:Clean /p:Configuration=Release /p:Platform="Any CPU" /v:minimal

# Build Release version
Write-Host ""
Write-Host "Building Release version..." -ForegroundColor Yellow
& $msbuildPath War3Trainer.sln /t:Build /p:Configuration=Release /p:Platform="Any CPU" /v:minimal

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Build successful!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    
    $exePath = "War3Trainer\bin\Release\War3Trainer.exe"
    if (Test-Path $exePath) {
        $fileInfo = Get-Item $exePath
        Write-Host "Output exe file location:" -ForegroundColor Cyan
        Write-Host "  $($fileInfo.FullName)" -ForegroundColor White
        Write-Host ""
        Write-Host "File size: $([math]::Round($fileInfo.Length / 1KB, 2)) KB" -ForegroundColor Cyan
        Write-Host ""
        
        # Package files to a release folder
        Write-Host "Packaging files for distribution..." -ForegroundColor Yellow
        $releaseFolder = "Release"
        if (Test-Path $releaseFolder) {
            Remove-Item $releaseFolder -Recurse -Force
        }
        New-Item -ItemType Directory -Path $releaseFolder | Out-Null
        
        # Copy exe file
        Copy-Item $exePath -Destination $releaseFolder -Force
        
        # Copy config file if exists
        $configPath = "War3Trainer\bin\Release\War3Trainer.exe.config"
        if (Test-Path $configPath) {
            Copy-Item $configPath -Destination $releaseFolder -Force
        }
        
        # Copy any other DLL files (if any)
        $dllFiles = Get-ChildItem "War3Trainer\bin\Release\*.dll" -ErrorAction SilentlyContinue
        foreach ($dll in $dllFiles) {
            Copy-Item $dll.FullName -Destination $releaseFolder -Force
        }
        
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "Package created successfully!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "Release package location:" -ForegroundColor Cyan
        $releaseFullPath = (Resolve-Path $releaseFolder).Path
        Write-Host "  $releaseFullPath" -ForegroundColor White
        Write-Host ""
        Write-Host "Files in package:" -ForegroundColor Cyan
        Get-ChildItem $releaseFolder | ForEach-Object {
            $size = [math]::Round($_.Length / 1KB, 2)
            Write-Host "  - $($_.Name) ($size KB)" -ForegroundColor White
        }
    } else {
        Write-Host "Warning: Exe file not found at expected location" -ForegroundColor Yellow
    }
} else {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "Build failed!" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "Please check the error messages above" -ForegroundColor Yellow
}

Write-Host ""
Read-Host "Press Enter to exit"
