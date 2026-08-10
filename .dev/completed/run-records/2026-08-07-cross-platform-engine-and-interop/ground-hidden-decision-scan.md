# GROUND run-record — hidden-decision scan of the CrossPlatform contract

Run of `hidden-decision-scan` over `contract.md` + the live code (2026-08-08). 6 agents, 0 errors, 5 lenses; hunted 13 candidates, 7 survived the care-filter, 6 dropped as noise. These 7 are forced choices the contract does not pin. No contract advances to IMPLEMENT with any of these unresolved.

## No open decision

The scan's finding #1 ("harden the interop rule against suppression") is NOT a new decision — it restates the project's premise. Every rule is suppressible until the crypto-minting lock exists; once minting is in, any suppression without a signed grant is rejected (that is `PLAN-crypto-minting-and-presence`, already scoped). The CODEOWNERS gate the scan recommended is a redundant second mechanism — scrapped. The interop rule is therefore a decided-correct item (below, #1), built like the existing AG-rules; the hardening comes from the signing work, not from this contract.

## Decided-correct (conform to conventions Tim already set — adopt into the contract's Decisions section, Tim may veto)

1. The interop-boundary rule is built at the same severity as `AG0001`–`AG0007`: a warning that the global `TreatWarningsAsErrors` turns into a build error. Suppression-locking is the crypto-minting work, not this contract's job. (high)

2. Windows native failures **wrap into `System.IO.IOException`** — matches the engine's ~12 existing `catch (IOException)` sites; a bare Win32 exception would slip past all of them and crash on first real Windows I/O failure. (high)
3. The engine selects its per-OS `AgentGuard.CrossPlatform.*` assembly by **target `$(RuntimeIdentifier)`, with a host-OS fallback only for the no-RID build** — never by build-host OS, which is a latent wrong-assembly crash the moment someone cross-publishes (e.g. `dotnet publish -r win-x64` from a Mac). (high)
4. `Remove(linkPath)` on an already-absent link is a **no-op (idempotent) on all three OS**, with a spec test — matches the existing `SymlinkOps.DeleteIfExists`; the contract's uniformity rule requires it. (medium)
5. Windows path marshalling is **UTF-16** (`StringMarshalling.Utf16` / `*W` entry point) — the POSIX sibling's UTF-8 is wrong for Win32 and silently mangles non-ASCII paths, which no current spec test exercises. (medium)
6. The **"enable Developer Mode / run elevated" message fires only on the specific Win32 privilege error code**, converted via the existing `RepairOutcome.NotRepairable` path — not the generic catch, which would mislabel a locked file or full disk. (medium)
7. The engine's `IPlatformFileSystem`/`IPlatformServices` test doubles are a **hand-rolled shared fake** (SetupHarness-style), not a new mocking-library dependency — a mocking-lib `PackageReference` would be an unauthorized dependency and breaks with every other test in the repo. (low)
