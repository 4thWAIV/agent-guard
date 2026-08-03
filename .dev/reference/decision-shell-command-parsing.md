# Decision — shell-command parsing in the File Guard

Decided 2026-07-25. A concise record so this is not re-derived from memory. Complements
`DESIGN-guard-engine-and-file-guard.md`.

## Superseded in part (2026-07-26)
The Pre/Post division has since changed (see §6 of the design doc): **Precheck now blocks only the core system**
(Sealed and System), and **Postcheck carries the full drift-and-revert** for ordinary protected files. That
removes reason 1 below — Precheck no longer extracts every write target from an arbitrary command to protect
ordinary files, because Post catches those from the bytes. Reason 2 still holds but softens: the Sealed set can
only be protected before the write, yet that check can be a conservative "deny on any reference to these fixed
paths," which does not need full write-target extraction. Net effect: **the parser is no longer core to the
first build.** Its remaining uses are the deferred approved-command match at Postcheck and, optionally,
hardening the conservative Pre scan. **Decided 2026-07-26: the parser is deferred out of v1.** It does not close
the runtime gaps (globs, variables, and `$(…)` resolve at run time, not parse time), and its only real job — the
approved-command match — is itself deferred. v1 protects the core system with the conservative reference-match
described in §6 of the design doc.

## Why the File Guard parses a Bash command
A protected file can be changed by a shell command without going through a normal file-edit tool: a
redirection (`>`, `>>`), or a command like `rm`, `cp`, `mv`, `tee`, `sed -i`, `dd`, `truncate`. The File Guard
parses the command to find which files it would write or delete, so it can stop a change to a protected file.

This is CORE, not a later phase, for two reasons:
1. Whole-file protection has to catch `rm`/`cp`/`mv`/redirection against a protected file at the Pre stage,
   to block it before it lands rather than only revert it after.
2. The Context store — the before-change snapshots the revert reads from — is excluded from the after-the-fact
   drift comparison, so the drift-revert canNOT protect it. Only the Pre parse can stop a Bash `rm`/`mv`
   against the Context store or the Grant store, i.e. the Sealed set. This is verified against 4thWAIV's
   `drift-detect-and-revert.md` residual-risk note: `.protected-snapshots/**` is pruned from the tree walk, so
   a write into it never appears in the drift diff and the Post check can never fire for it; its only
   protection is the Pre extraction.

## The parser
- Library: **ShellSyntaxTree** (NuGet), Apache-2.0, pure-managed, no native dependency, fails closed on
  unparseable input (`IsUnparseable`). Chosen over hand-rolling our own and over the tree-sitter route (native
  per-platform dependency cost). Author Aaron Stannard (Akka.NET). It is pre-1.0, so we wrap it and pin it.
- It is used ONLY behind a local interface; none of its types cross that boundary, so it is replaceable.
- It gives syntax only. Which parsed paths are actually WRITES (`rm` writes its args, `cp` writes its last
  arg, a `>` writes its target, `cat` writes nothing) is OUR logic, kept in our own code, not the library's.

## The local interface and neutral types (proposed; names still to confirm)
```csharp
interface IShellCommandParser { ShellCommand Parse(string commandLine); }

ShellCommand    { bool Parsed; string? UnparsedReason; IReadOnlyList<CommandStep> Steps; }
CommandStep     { StepConnector Connector; string Program; IReadOnlyList<CommandArgument> Arguments;
                  IReadOnlyList<Redirection> Redirections; bool WrapsInnerCommandLine; string? InnerCommandLine; }
CommandArgument { string Text; string? NormalizedPath; bool IsFlag; }
Redirection     { string Target; RedirectionKind Kind; }
enum StepConnector   { None, AndThen, OrElse, Sequence, Pipe }   // None = first step; joins to the previous
enum RedirectionKind { Input, Overwrite, Append, ErrorOverwrite, ErrorAppend }
```
`StepConnector` carries pipes and the other joins so the representation is lossless (an earlier draft dropped
it, which meant it could not represent a pipe at all).

## To verify against the real parser when we build the adapter
- Whether `bash -c "..."` returns the inner command as a re-parsed structure or a raw string we re-parse.
- Whether a background `&` and a combined `&>` are modeled.
- Exact single/double-quote and escape handling.
Our fail-closed rule covers all three: if the structure does not come back clean, the File Guard denies.

## Deferred (NOT in the first build)
The parser itself (the ShellSyntaxTree adapter and the neutral types above), the approved-command allowance (for
example `dotnet add`), the language Providers, and the schema-aware Part grants. With the Pre/Post split the
write-target extraction is no longer core to v1 (see the superseded note above); v1 protects the core system
with the conservative reference-match instead.
