# 30 — owner-declares-what-it-replaces: one self-describing source for AG0011 ownership + the 'use X instead' message

**The GitHub issue is the authority. This document is a starting point only, and the GitHub issue always supersedes it.**

State: OPEN
Labels: none
Link: https://github.com/4thWAIV/agent-guard/issues/30

---

## Problem

The guard keeps "who owns which raw primitive" and "which interfaces are services" in more than one place, and they drift. We just hit this: `IFileSystem` was added as a service on `ISystemServices` but the hand-maintained `BoundaryServices.OwnerInterfaces` list was never updated, so AG0024/AG0025/AG0031 were blind to it.

Piece one (a separate, already-scoped change) fixes **service-ness** by deriving the service set from `ISystemServices` at compile time (walk the parameterless interface-typed accessors, recursed; exclude parameterized factory methods). This issue is **piece two**: make each owner **declare what raw primitive(s) it replaces and what to use instead**, so AG0011's raw→owner lookup *and* the diagnostic message both flow from one self-describing source, cross-checked against the derived service set. Nothing hand-duplicated; the message can never disagree with the rule.

## The clean core

A whole-type owner carries a declaration — e.g. an attribute `[OwnsPrimitive("System", "Environment")]` on `IEnvironment` — that drives both the AG0011 owner lookup for that type and the "use `IEnvironment` off `ISystemServices` instead of raw `System.Environment`" message. This fits the whole-type owners directly:

- `System.Environment` → `IEnvironment`
- `System.Console` → `IConsole`
- `System.Random` / `System.Security.Cryptography.RandomNumberGenerator` → `IGuidFactory`
- BouncyCastle `Ed25519Signer` / `Ed25519PublicKeyParameters` → `ISignatureService`
- `AssemblyInformationalVersionAttribute` / `AssemblyFileVersionAttribute` → `IBuildInfo`

Constraint: `AgentGuard.Abstractions.Contracts` must stay dependency-light, so the declaration references primitives by **string identity `(namespace, name)`**, never `typeof` — otherwise `[OwnsPrimitive(typeof(Ed25519Signer))]` would pull BouncyCastle into Contracts. String identity matches how `WellKnownType` already keys everything.

## The hard cases — the reason this needs a real GROUND/DESIGN, not a bolt-on

A type-level attribute does not express what the current `OwnedPrimitives` table does for most of its entries. Each of these needs a deliberate design decision (Tim believes counter-rules can make a clean attribute approach cover even these — that exploration is the heart of this ticket):

- **Member-split.** `File`/`Directory` split per member across `IFileReader`/`IDirectoryEnumerator`/`IFileWriter`/`IDirectoryWriter`, plus the `Directory.GetCurrentDirectory`→`IEnvironment` carve-out and the OS-divergent members (`CreateSymbolicLink`, `ResolveLinkTarget`, `Get/SetUnixFileMode`) that belong to AG0101. A single `[OwnsPrimitive("System.IO","File")]` cannot say this.
- **Member-disambiguation within one type.** `System.Reflection.Assembly`: `GetEntryAssembly`/`GetExecutingAssembly`/`GetCallingAssembly`→`IBuildInfo` but `Location`→`IEnvironment`. Also `Guid` (only the impure subset `NewGuid`/`CreateVersion7`/`CreateVersion1`→`IGuidFactory`, `Guid.Parse` legal), `AppContext`/`AppDomain.BaseDirectory`, and `RuntimeInformation` (host-description properties→`IEnvironment` but `IsOSPlatform`→AG0009).
- **Bans with no owner.** `Process`/`ProcessStartInfo`, streams/watchers/drives, `System.IO.Enumeration.*` are banned everywhere. There is no interface to hang an attribute on, so a ban must have its own declaration home.
- **Owners that are not services.** `IFileInfo`/`IDirectoryInfo`/`IFileSystemInfo` own raw `FileInfo`/`DirectoryInfo`/`FileSystemInfo` but are reached through a factory, not injected off the container — they are owners but NOT services. Ownership and service-ness are two orthogonal axes; the `[OwnsPrimitive]` declaration lives on the **owner** (service or wrapper), while service-ness comes from the `ISystemServices` walk. Keep them separate and **cross-check**, never conflate.
- **Standalone rules that don't fit the "interface owns a type" shape at all.** The clock (AG0015) spans raw wall-clock reads plus `TimeProvider` acquisition; Path-purity (AG0020) is a default-deny allowlist. These stay standalone.

## What "done" looks like

- One source of truth for owner→primitive; AG0011's raw→owner lookup and its diagnostic message both derive from it — no second hand-list, no hand-written owner label.
- The member-split, member-disambiguation, and no-owner-ban cases are all expressible in the chosen mechanism (or a deliberately-decided small residual table), with a written rationale for whichever cases stay tabular.
- The owner declarations are cross-checked against the derived service set from piece one: an owner that is also a service must be reachable from `ISystemServices`, and a service that owns a primitive must carry its declaration — a mismatch fails the build.
- L1: full rule-phase, all adversaries, RED-then-clean, nothing suppressed.

## Depends on / relationship

- Builds on **piece one** (derive the service set from `ISystemServices`; the completeness cross-check hook lands there).
- Related: #28 (deferred rule folds), #29 (sharpen the DRY path). This is the DRY-path idea applied to the guard's own ownership model.
