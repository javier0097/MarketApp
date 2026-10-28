@echo off
rem Opens the test-writer agent as its own session. It must not see how the code is built:
rem auto memory is turned off, and test-writer.settings.json skips every CLAUDE.md and runs
rem a hook that limits reading, editing and creating files to test files.
rem Extra arguments are passed on to claude.
setlocal
cd /d "%~dp0.."
set CLAUDE_CODE_DISABLE_AUTO_MEMORY=1
claude --agent test-writer --settings .claude\test-writer.settings.json %*
