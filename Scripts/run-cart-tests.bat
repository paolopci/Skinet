@echo off
setlocal
set COLLECTION=D:\test\angular\Skinet\Scripts\Skinet-Cart.postman_collection.json
set ENV=D:\test\angular\Skinet\postman\Skinet.postman_environment.json
set REPORT_DIR=D:\test\angular\Skinet\Reports
set TS=%date:~6,4%%date:~3,2%%date:~0,2%_%time:~0,2%%time:~3,2%%time:~6,2%
set TS=%TS: =0%
set REPORT_FILE=%REPORT_DIR%\cart-report-%TS%.html
set KEEP_REPORTS=10

echo Avvio test Cart con Newman...

where newman >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
  echo Newman non trovato. Installa con: npm i -g newman
  exit /b 1
)

if not exist "%COLLECTION%" (
  echo Collection non trovata: %COLLECTION%
  exit /b 1
)

if not exist "%ENV%" (
  echo Environment non trovato: %ENV%
  exit /b 1
)

for /f "usebackq delims=" %%B in (`powershell -NoProfile -Command "(Get-Content '%ENV%' -Raw | ConvertFrom-Json).values | Where-Object { $_.key -eq 'baseUrl' } | Select-Object -ExpandProperty value"`) do set BASEURL=%%B
if "%BASEURL%"=="" (
  echo baseUrl mancante o vuoto nell'environment: %ENV%
  exit /b 1
)

if not exist "%REPORT_DIR%" (
  echo Cartella report non trovata: %REPORT_DIR%
  exit /b 1
)

newman run "%COLLECTION%" -e "%ENV%" -r cli,html --reporter-html-export "%REPORT_FILE%" --insecure
set NEWMAN_EXIT=%ERRORLEVEL%

set STYLE_APPLIED=0
for /l %%I in (1,1,5) do (
  powershell -NoProfile -ExecutionPolicy Bypass -File D:\test\angular\Skinet\Scripts\postprocess-newman-report.ps1 -ReportPath "%REPORT_FILE%"
  findstr /c:"codex-failed-style" "%REPORT_FILE%" >nul
  if not errorlevel 1 (
    set STYLE_APPLIED=1
    goto :afterstyle
  )
  timeout /t 1 >nul
)
:afterstyle
if %STYLE_APPLIED% EQU 0 (
  timeout /t 1 >nul
  powershell -NoProfile -ExecutionPolicy Bypass -File D:\test\angular\Skinet\Scripts\postprocess-newman-report.ps1 -ReportPath "%REPORT_FILE%"
)

if %NEWMAN_EXIT% NEQ 0 (
  echo Newman ha rilevato errori.
  echo Report: %REPORT_FILE%
  exit /b %NEWMAN_EXIT%
)

echo Test completati con successo.
echo Report: %REPORT_FILE%

for /f "skip=%KEEP_REPORTS% delims=" %%F in ('dir /b /o-d "%REPORT_DIR%\cart-report-*.html"') do del "%REPORT_DIR%\%%F"
endlocal
