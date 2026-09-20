import { defineConfig, globalIgnores } from "eslint/config";
import nextVitals from "eslint-config-next/core-web-vitals";
import nextTypeScript from "eslint-config-next/typescript";

export default defineConfig([
  ...nextVitals,
  ...nextTypeScript,
  {
    rules: {
      // All applications use the App Router and live below apps/*; there is no
      // repository-root pages directory for this legacy-only rule to inspect.
      "@next/next/no-html-link-for-pages": "off",
    },
  },
  globalIgnores([
    "**/.next/**",
    "**/node_modules/**",
    "**/dist/**",
    "**/out/**",
    "src/**",
  ]),
]);
