# 67 — appd 5 of 5: the key and presence — signing becomes possible

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/67

---

Part 5 of the appd work. Parent: #62. Depends on #63, #64, #65 and #66.

## What this delivers

appd authenticates the user once at start, holds the Ed25519 private key, and signs on request for verified callers only. This is the part that makes signed decisions possible.

## Why it has to work this way

Today the guard never holds a private key — the grant path is verify-only and `GrantStore` never mints. Presence shipped, so proving the user is physically present is solved, but nothing turns that into a signature.

The OS secure stores cannot solve it. Research established that no cross-platform mechanism lets a CLI authenticate once and then sign without prompting on later runs. macOS Authorization Services caches credentials across processes, but its Touch ID path is hard-coded to Apple-signed executables, so a third-party CLI only ever gets a password prompt. Windows `KeyCredentialCacheConfiguration` is entirely undocumented — Microsoft publishes no description for any of its fields. polkit on Linux caches per shell rather than per binary, and its own manual states a retained authorization succeeds even when the details differ, so a second, different decision inside the window would be approved without ever showing its message. For a decision-signing tool that is a correctness failure, not a UX one.

A long-running process avoids all of it. Authenticate once at start, hold the key, sign on request.

## The point of the whole design

Because signing only happens through appd, and appd only answers a verified guard binary, the AI cannot sign by calling the crypto itself. It has to come through the door, and the presence check sits at that door. That is what makes the check unfakeable.

## The two modes

**appd runs either way and never refuses to hold the key because of an OS setting.** It detects the environment's protection state at start. The dashboard shows visually which mode is active. When the OS is already in the strong mode there is nothing to display; when it is in the weaker one the dashboard shows the option and how to move up.

Decided by Tim: where an OS offers a stronger posture, warn the user in the dashboard when appd starts and tell them how to reach it. The spec always describes both modes; the dashboard shows the upgrade path only where the OS has one and the machine is in the weaker mode.

**Which OS settings matter, and whether each platform even has two modes, is unresearched.** macOS has System Integrity Protection, which is a real setting the user can turn back on — Tim's machine currently has it disabled, most likely from the .NET-on-Intel work. Linux has `ptrace_scope`, a sysctl. Windows is unknown, and if it has nothing to promote to, Windows shows one mode with no upgrade path. Establish this before building the indicator.

## Done when

On all three platforms appd prompts once at start, holds the key, signs for a verified caller, refuses an unverified one, the dashboard shows the active mode, and shows the upgrade path only where the OS has one.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

https://claude.ai/code/session_012FokkiXZDtUrvFdGDfj75s
