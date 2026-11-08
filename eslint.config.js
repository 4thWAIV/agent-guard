// Lints the TypeScript side. The .NET side is fenced by the analyzers in analyzers/ and is not
// this config's business.
//
// Type-aware rules are on. Without the type information, a lint rule can only see the shape of
// the text, and the faults worth catching here — a promise nobody awaited, a value narrowed
// wrongly — are only visible to something that knows the types.

import js from '@eslint/js'
import tseslint from 'typescript-eslint'

export default tseslint.config(
  { ignores: ['node_modules', '**/*.built.js', 'ts/metalang/metalang.html'] },
  js.configs.recommended,
  tseslint.configs.strictTypeChecked,
  tseslint.configs.stylisticTypeChecked,
  {
    languageOptions: {
      parserOptions: { projectService: true, tsconfigRootDir: import.meta.dirname },
    },
    rules: {
      // The language's own vocabulary is read from YAML, so a document's values arrive as
      // unknown and are narrowed by hand. That narrowing is the checker's whole job and the
      // rule would flag every step of it.
      '@typescript-eslint/no-unsafe-assignment': 'off',
      '@typescript-eslint/no-unsafe-member-access': 'off',

      // A template holding a value the checker has already narrowed to a string is the normal
      // way every message in this project is written.
      '@typescript-eslint/restrict-template-expressions': ['error', { allowNumber: true }],
    },
  },
)
