@echo off
chcp 65001 >nul
setlocal

echo ============================================
echo   Ember Framework - 开发工具一键安装
echo ============================================
echo.
echo 即将安装以下工具：
echo   1. Unity CLI   - Unity 官方命令行工具
echo   2. UPM CLI      - Unity Package Manager 命令行工具
echo.
echo 安装位置：%%USERPROFILE%%\.unity\cli\  （仅当前用户）
echo 无需管理员权限
echo ============================================
echo.

set "HAS_ERROR=0"

:: ──────────────────────────────────────────
:: 1. 安装 Unity CLI
:: ──────────────────────────────────────────
echo [1/2] 正在安装 Unity CLI ...
powershell -NoProfile -Command ^
    "$env:UNITY_CLI_CHANNEL='beta'; iex (irm https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.ps1)"

if %ERRORLEVEL% neq 0 (
    echo [ERROR] Unity CLI 安装失败，请检查网络连接后重试
    set "HAS_ERROR=1"
) else (
    echo [ OK ] Unity CLI 安装完成
)
echo.

:: ──────────────────────────────────────────
:: 2. 安装 UPM CLI
:: ──────────────────────────────────────────
echo [2/2] 正在安装 UPM CLI ...
powershell -NoProfile -Command ^
    "iex (irm https://cdn.packages.unity.com/upm-cli/install.ps1)"

if %ERRORLEVEL% neq 0 (
    echo [ERROR] UPM CLI 安装失败，请检查网络连接后重试
    set "HAS_ERROR=1"
) else (
    echo [ OK ] UPM CLI 安装完成
)
echo.

:: ──────────────────────────────────────────
:: 结果汇总
:: ──────────────────────────────────────────
echo ============================================
if %HAS_ERROR% equ 1 (
    echo  部分工具安装失败，请检查上方错误信息。
    echo ============================================
    pause
    exit /b 1
)

echo   全部安装完成！
echo ============================================
echo.
echo 请关闭当前终端，打开新的 PowerShell 或 CMD，
echo 然后验证安装：
echo.
echo   unity --version
echo   upm --version
echo.
echo 如果提示"命令找不到"，请重启电脑后重试。
echo ============================================
pause
