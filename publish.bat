@echo off & cd /d %~dp0

set Output_dir=%cd%\abin\bin
set app_dir=%Output_dir%\..\a

set cd0=%cd%
mkdir "%Output_dir%" 1>nul 2>nul

cd Server2
dotnet publish -c debug -o "%Output_dir%"
cd %cd0%

cd Client
dotnet publish -c debug -o "%Output_dir%\_c"
for /r "%Output_dir%\_c" %%f in (*.*) do (
	copy /y "%%f" "%Output_dir%\" 1>nul 2>nul
)
rd /s/q "%Output_dir%\_c"
del "%Output_dir%\jobs.json" 1>nul 2>nul
copy /y "jobs.json" "%Output_dir%\..\"
md "%app_dir%" 1>nul 2>nul
copy /y "jobs.json" "%app_dir%"
cd %cd0%

del "%Output_dir%\appsettings.json" 1>nul 2>nul
del "%Output_dir%\appsettings.*.json" 1>nul 2>nul
del "%Output_dir%\nlog.config" 1>nul 2>nul
del "%Output_dir%\nlog.*.config" 1>nul 2>nul
del "%Output_dir%\web.config" 1>nul 2>nul
del "%Output_dir%\web.*.config" 1>nul 2>nul

::copy /y "js\*.js" "%Output_dir%\..\*.js" 1>nul 2>nul
for /r "js" %%f in (*.js) do (
	copy /y "%%f" "%app_dir%"
)

goto :end
rem ---------------------------------------------------------------------------------
:end
pause