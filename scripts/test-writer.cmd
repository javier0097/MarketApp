@echo off
rem Opens the test-writer agent as its own session. It must not see how the code is built:
rem auto memory is turned off, and isolated-agent.settings.json skips every CLAUDE.md.
rem Extra arguments are passed on to claude.
setlocal
cd /d "%~dp0.."
set CLAUDE_CODE_DISABLE_AUTO_MEMORY=1
claude --agent test-writer --settings .claude\isolated-agent.settings.json %*
