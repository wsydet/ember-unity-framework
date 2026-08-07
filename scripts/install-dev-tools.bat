@echo off
chcp 65001 >nul
setlocal

echo ============================================
echo   Ember Framework - 开发工具一键安装
echo ============================================
echo.
echo 即将安装以下工具：
echo   1. Python 3.12  - 运行 MCP 服务器等外部工具
echo   2. uv           - Python 包管理器（比 pip 快 10-100 倍）
echo   3. Unity CLI    - Unity 官方命令行工具
echo   4. UPM CLI      - Unity Package Manager 命令行工具
echo.
echo 安装位置：%%USERPROFILE%%\.unity\cli\  （仅当前用户）
echo 无需管理员权限
echo ============================================
echo.

set "HAS_ERROR=0"

:: ──────────────────────────────────────────
:: 1. 安装 Python 3.12
:: ──────────────────────────────────────────
echo [1/4] 正在检查 Python 环境 ...

set "PYTHON_OK=0"
python --version >nul 2>&1 && set "PYTHON_OK=1"

if %PYTHON_OK% equ 1 (
    for /f "tokens=2 delims= " %%v in ('python --version 2^>^&1') do set "PY_VER=%%v"
    echo [ OK ] 已安装 Python %PY_VER%
) else (
    echo [INFO] 未检测到 Python，正在通过 winget 安装 Python 3.12 ...
    winget install Python.Python.3.12 --accept-package-agreements --accept-source-agreements >nul 2>&1
    if %ERRORLEVEL% neq 0 (
        echo [WARN] winget 安装失败，请手动安装：
        echo        https://www.python.org/downloads/
        echo        安装时务必勾选 "Add Python to PATH"
        set "HAS_ERROR=1"
    ) else (
        echo [ OK ] Python 3.12 安装完成
        echo [INFO] 请关闭此窗口，重新打开后再次运行本脚本以继续后续安装
        pause
        exit /b 1
    )
)
echo.

:: ──────────────────────────────────────────
:: 2. 安装 uv（Python 包管理器）
:: ──────────────────────────────────────────
echo [2/4] 正在检查 uv ...

set "UV_OK=0"
uv --version >nul 2>&1 && set "UV_OK=1"

if %UV_OK% equ 1 (
    for /f "tokens=2 delims= " %%v in ('uv --version 2^>^&1') do set "UV_VER=%%v"
    echo [ OK ] 已安装 uv %UV_VER%
) else (
    echo [INFO] 正在通过 PowerShell 安装 uv ...
    powershell -NoProfile -Command "irm https://astral.sh/uv/install.ps1 | iex"
    if %ERRORLEVEL% neq 0 (
        echo [ERROR] uv 安装失败，请检查网络连接后重试
        set "HAS_ERROR=1"
    ) else (
        echo [ OK ] uv 安装完成
        echo [INFO] PATH 已更新，新终端中生效
    )
)
echo.

:: ──────────────────────────────────────────
:: 3. 安装 Unity CLI
:: ──────────────────────────────────────────
echo [3/4] 正在安装 Unity CLI ...
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
:: 4. 安装 UPM CLI
:: ──────────────────────────────────────────
echo [4/4] 正在安装 UPM CLI ...
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
echo   python --version
echo   uv --version
echo   unity --version
echo   upm --version
echo.
echo 如果提示"命令找不到"，请重启电脑后重试。
echo ============================================
pause
