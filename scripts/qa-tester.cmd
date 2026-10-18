@echo off
rem Opens the qa-tester agent as its own session, testing the QA app (scripts\qa-app.cmd).
rem Like test-writer.cmd, it starts without auto memory or any CLAUDE.md, and with only the
rem MCP servers in qa-tester.mcp.json (the Playwright browser). Extra arguments go to claude.
setlocal
cd /d "%~dp0.."
set QA_URL=http://localhost:5130
set STARTED_APP=

rem An app that was already running is used as it is, and left running at the end
curl -s -f -o nul %QA_URL%/api/health && goto run
set STARTED_APP=1
start "MarketApp QA" cmd /c scripts\qa-app.cmd
echo Waiting for the QA app at %QA_URL%...
set /a attempts=0
:wait
rem ping is a two-second pause that also works when input is redirected, unlike timeout
ping -n 3 127.0.0.1 > nul
curl -s -f -o nul %QA_URL%/api/health && goto run
set /a attempts+=1
if %attempts% lss 90 goto wait
echo The QA app did not start. Check its window for errors.
exit /b 1

:run
set CLAUDE_CODE_DISABLE_AUTO_MEMORY=1
claude --agent qa-tester --settings .claude\isolated-agent.settings.json --mcp-config .claude\qa-tester.mcp.json --strict-mcp-config %*

if not defined STARTED_APP goto :eof
rem Found through the port, not the window title: npm and dotnet change the title while they run.
rem netstat lists the port for IPv4 and IPv6, so the second taskkill finds nothing left: silenced
for /f "tokens=5" %%p in ('netstat -ano ^| findstr /r /c:":5130 .*LISTENING"') do taskkill /pid %%p /t /f > nul 2>&1
