// PreToolUse hook for the test-writer agent: it may read, edit and create test files only.
// Claude Code runs it before every Read, Edit and Write, passing the tool call as JSON on stdin.
// Exit code 0 lets the call through; exit code 2 blocks it and shows stderr to the agent.
// Anything unexpected blocks: a broken input must not open the production code.
import path from "node:path";

const repoRoot = path.resolve(import.meta.dirname, "..", "..");

function isTestFile(filePath) {
  const relative = path
    .relative(repoRoot, path.resolve(repoRoot, filePath))
    .replaceAll("\\", "/")
    .toLowerCase();

  const inTestProject =
    relative.startsWith("backend/marketapp.api.tests/") &&
    !relative.includes("/bin/") &&
    !relative.includes("/obj/");
  const frontendTest =
    relative.startsWith("frontend/src/") && /\.test\.tsx?$/.test(relative);

  return inTestProject || frontendTest;
}

function block(message) {
  process.stderr.write(message);
  process.exit(2);
}

try {
  let input = "";
  for await (const chunk of process.stdin) {
    input += chunk;
  }

  const filePath = JSON.parse(input).tool_input?.file_path;
  if (typeof filePath !== "string" || !isTestFile(filePath)) {
    block(
      "Only test files can be used: backend/MarketApp.Api.Tests/ and frontend/src/**/*.test.ts. The production code is off limits.",
    );
  }
} catch {
  block("The hook could not read the tool call, so it was blocked.");
}
