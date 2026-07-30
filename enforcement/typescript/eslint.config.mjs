// Enforcement for carestechs ADR constraints (TypeScript CLI stack).
// npm i -D eslint typescript-eslint eslint-plugin-import
import eslint from "@eslint/js";
import tseslint from "typescript-eslint";
import importPlugin from "eslint-plugin-import";

export default tseslint.config(
  eslint.configs.recommended,
  ...tseslint.configs.recommended,
  {
    plugins: { import: importPlugin },
    rules: {
      // adrs/typescript/strict-typescript.md - no `any`
      "@typescript-eslint/no-explicit-any": "error",
      // adrs/typescript/strict-typescript.md - no @ts-ignore / @ts-expect-error / @ts-nocheck
      "@typescript-eslint/ban-ts-comment": [
        "error",
        { "ts-expect-error": true, "ts-ignore": true, "ts-nocheck": true },
      ],
      // adrs/typescript/named-exports.md - named exports only
      "import/no-default-export": "error",
    },
  },
);
