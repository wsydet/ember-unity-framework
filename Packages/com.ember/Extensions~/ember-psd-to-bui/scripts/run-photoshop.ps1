param([Parameter(Mandatory)][string]$Request)
$ErrorActionPreference = 'Stop'
$utf8 = [System.Text.UTF8Encoding]::new($false, $true)
$requestPath = (Resolve-Path -LiteralPath $Request).Path
$requestText = [System.IO.File]::ReadAllText($requestPath, $utf8)
$null = $requestText | ConvertFrom-Json
$scriptText = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot 'photoshop-document.jsx'), $utf8)
$photoshopApp = New-Object -ComObject Photoshop.Application
$photoshopApp.DoJavaScript("var EMBER_REQUEST = $requestText;`n$scriptText")
