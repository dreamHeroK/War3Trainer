@echo off
chcp 65001 >nul
echo 正在构建 War3Trainer Release 版本...
echo.

REM 查找 MSBuild.exe - 使用 where 命令或直接检查常见路径
set MSBUILD_PATH=

REM 尝试使用 where 命令查找
where msbuild >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    for /f "delims=" %%i in ('where msbuild') do set "MSBUILD_PATH=%%i"
)

REM 如果 where 命令没找到，尝试常见路径
if "%MSBUILD_PATH%"=="" (
    if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" (
        set "MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
    )
)
if "%MSBUILD_PATH%"=="" (
    if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" (
        set "MSBUILD_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
    )
)
if "%MSBUILD_PATH%"=="" (
    if exist "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" (
        set "MSBUILD_PATH=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
    )
)
if "%MSBUILD_PATH%"=="" (
    if exist "C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe" (
        set "MSBUILD_PATH=C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
    )
)

if "%MSBUILD_PATH%"=="" (
    echo 错误: 未找到 MSBuild.exe
    echo 请确保已安装 Visual Studio 或 .NET Framework SDK
    echo.
    echo 您可以手动指定 MSBuild 路径，或安装 Visual Studio
    timeout /t 5 >nul
    exit /b 1
)

echo 使用 MSBuild: %MSBUILD_PATH%
echo.

REM 清理之前的构建
echo 清理之前的构建...
"%MSBUILD_PATH%" War3Trainer.sln /t:Clean /p:Configuration=Release /p:Platform="Any CPU" /v:minimal

REM 构建 Release 版本
echo.
echo 开始构建 Release 版本...
"%MSBUILD_PATH%" War3Trainer.sln /t:Build /p:Configuration=Release /p:Platform="Any CPU" /v:minimal

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo 构建成功！
    echo ========================================
    echo.
    echo 生成的 exe 文件位置:
    echo War3Trainer\bin\Release\War3Trainer.exe
    echo.
    if exist "War3Trainer\bin\Release\War3Trainer.exe" (
        echo 文件大小:
        dir "War3Trainer\bin\Release\War3Trainer.exe" | findstr "War3Trainer.exe"
    )
) else (
    echo.
    echo ========================================
    echo 构建失败！
    echo ========================================
    echo 请检查错误信息
)

echo.
echo 构建完成，按任意键退出...
pause >nul

