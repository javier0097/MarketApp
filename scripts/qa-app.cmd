@echo off
rem Runs the app as it is published: the frontend built and served by the backend, on one port.
rem The QA environment (appsettings.QA.json) uses http://localhost:5130 and a database that
rem keeps its data between sessions, in C:\ProgramData\MarketApp-QA.
setlocal
cd /d "%~dp0.."
rem Without a fresh build the backend would serve whatever an earlier build left in wwwroot
call npm run build --prefix frontend || exit /b 1
set ASPNETCORE_ENVIRONMENT=QA
rem The launch profile would set the Development environment and override the QA settings
dotnet run --project backend\MarketApp.Api --no-launch-profile
