import js from "@eslint/js";
import globals from "globals";
import reactHooks from "eslint-plugin-react-hooks";
import reactRefresh from "eslint-plugin-react-refresh";
import tseslint from "typescript-eslint";
import { defineConfig } from "eslint/config";

export default defineConfig([
  {
    files: ["**/*.{ts,tsx}"],
    extends: [
      js.configs.recommended,
      tseslint.configs.recommended,
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      globals: globals.browser,
    },
    rules: {
      // typescript-eslint turns it off because tsc detects it, but tsc only greys it out
      "no-unreachable": "error",
      "@typescript-eslint/no-unused-vars": [
        "error",
        { args: "all", argsIgnorePattern: "^_" },
      ],
      // A missing dependency leaves stale values on screen without any error
      "react-hooks/exhaustive-deps": "error",
      "no-restricted-syntax": [
        "error",
        {
          selector:
            "CallExpression[callee.property.name=/^(then|catch|finally)$/]",
          message: "Use async/await with try/catch instead of promise chains.",
        },
      ],
    },
  },
]);
