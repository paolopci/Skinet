@echo off
setlocal
set COLLECTION=D:\test\angular\Skinet\postman\Skinet-Products.postman_collection.json
set ENV=D:\test\angular\Skinet\postman\Skinet.postman_environment.json
set REPORT_DIR=D:\test\angular\Skinet\Reports
set TS=%date:~6,4%%date:~3,2%%date:~0,2%_%time:~0,2%%time:~3,2%%time:~6,2%
set TS=%TS: =0%
set REPORT_FILE=%REPORT_DIR%\products-report-%TS%.html
set KEEP_REPORTS=10

for /f "usebackq delims=" %%F in (`powershell -NoProfile -Command "Get-Content '%COLLECTION%' | ConvertFrom-Json | Select-Object -ExpandProperty item | ForEach-Object { $_.item } | Where-Object { $_.name -like 'Get Products Filtered*' } | ForEach-Object { $_.request.url }"`) do echo Filtro in collection: %%F

newman run "%COLLECTION%" -e "%ENV%" -r cli,html --reporter-html-export "%REPORT_FILE%" --insecure
if errorlevel 1 (
  echo Newman ha rilevato errori.
  exit /b 1
)

echo Test completati con successo.
echo Report: %REPORT_FILE%

for /f "skip=%KEEP_REPORTS% delims=" %%F in ('dir /b /o-d "%REPORT_DIR%\products-report-*.html"') do del "%REPORT_DIR%\%%F"
endlocal
