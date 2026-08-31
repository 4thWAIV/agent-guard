# Grounding — polkit as the Linux out-of-band presence check (2026-08-23)

Authoritative sources: freedesktop polkit reference (D-Bus interface + man pages, via the `polkit.pages.freedesktop.org` mirror), the Authority interface XML, linux-pam/fprintd CVE-2024-37408, Tmds.DBus, `pkcheck(1)`/`pkttyagent(1)`, SUSE polkit guide. Points not backed by a primary doc line are flagged.

## Why polkit (the driving reason)
A masked stdin read in our CLI is *in-band*: the caller (possibly the AI) controls stdin and can supply the password, so it is not a presence proof. Presence must be an *out-of-band* prompt shown by a channel the calling process cannot answer. macOS LocalAuthentication and Windows Hello already are; on Linux, polkit is the equivalent.

## 1. CheckAuthorization mechanics
`org.freedesktop.PolicyKit1.Authority.CheckAuthorization` on the **system bus** (`/org/freedesktop/PolicyKit1/Authority`).
`CheckAuthorization(Subject subject, String action_id, a{ss} details, Flags flags, String cancellation_id) → (Boolean is_authorized, Boolean is_challenge, a{ss} details)`.
- **subject** `(String kind, a{sv} details)`. `unix-process` accepts `{pidfd, uid}` (modern) or `{pid, start-time, uid}` (older, PID-race-hardened). `unix-session` = a session id (weaker — "someone in the session"). `system-bus-name` is for a *service* checking a *client* over D-Bus (not our case).
- **flags**: `None=0`, `AllowUserInteraction=1` ("attempt to authenticate if an agent is available … CheckAuthorization() will block while the user is asked").
- **result**: `is_authorized` (authorized now); `is_challenge` (TRUE = would pass if AllowUserInteraction/agent were available); `details` may carry `polkit.temporary_authorization_id`, `polkit.retains_authorization_after_challenge`, `polkit.dismissed`.
- **Best subject for "this invoking process, now":** `unix-process` with the **pidfd** form (kernel handle, no PID-reuse confusion); `pid,start-time,uid` triple is the fallback. pidfd needs a recent polkit — verify target-distro version.

## 2. Out-of-band guarantee (conditional)
polkitd dispatches the prompt to a registered **authentication agent** — a separate process in the user's session that runs the PAM conversation itself and returns only yes/no; the credential never flows back through the caller's return value. Only **one** agent per session/subject: GNOME/KDE register it at login and hold the slot, so a later AI process cannot displace it (second registration fails: "An authentication agent already exists for the given subject"). Weakness: in a session with **no** agent, a process can register its own or spawn `pkttyagent` (which reads the tty the caller controls) and answer itself.
**Rule:** the guard NEVER creates/spawns/registers an agent (no `pkttyagent`, no internal-agent flag) — it relies only on a pre-existing session agent; no agent → deny. Residual risk: a hostile pre-registered agent in an agent-less session (already-compromised session, above the lazy-AI bar).

## 3. Does polkit subsume fingerprint?
Yes, opportunistically. The agent authenticates via `/etc/pam.d/polkit-1`; where that includes `pam_fprintd`, the dialog offers fingerprint-or-password in one call. Crucially, the GNOME/KDE PolicyKit dialog is a vetted **attention front-end** explicitly NOT affected by CVE-2024-37408 (fingerprint hijacking), whereas a hand-rolled `pam_fprintd` front-end IS the vulnerable side. So polkit is safer than a direct fingerprint path → **polkit primary, no separate hand-rolled fingerprint path.** Biometric is offered where the distro wired it (not universal; never on headless).

## 4. Root-installed action file (the real cost)
A proper `auth_self` presence action must be installed as a root-owned `.policy` XML under `/usr/share/polkit-1/actions/`. No unprivileged runtime registration; an undeclared action resolves to implicit deny (can't get an interactive prompt from a made-up action — inferred from the implicit-default model + SUSE guide, not a single man-page line [FLAGGED]); no reliable stock action defaults to `auth_self` (most are `auth_admin`, wrong identity, often cached). So Linux needs a one-time privileged install step; without it → fail-closed deny. Want `auth_self` (owner of the session proves themselves), NOT `auth_admin`.

## 5. Headless / no-agent → fails closed
No agent (or AllowUserInteraction unset) → `is_authorized=FALSE, is_challenge=TRUE` (never a spurious yes). `pkcheck` mirrors: exit 0 authorized, 1 not, 2 no-agent/no-interaction, 3 dismissed. A DE agent auto-registers at login; nothing auto-registers on headless/SSH — the app would have to spawn `pkttyagent`, which we must NOT do.

## 6. Calling polkit from .NET (chosen: Tmds.DBus.Protocol, pinned)
| Option | New managed dep | Native lib at runtime | Note |
|---|---|---|---|
| `Tmds.DBus.Protocol` | Yes (1 NuGet, 0 transitive) | No (pure managed) | AOT/trim/single-file clean; only option passing pidfd cleanly; least code. **CHOSEN, pinned.** |
| P/Invoke `sd-bus` (libsystemd) | No | Yes (present on systemd distros) | Matches existing interop pattern; fiddly `a{sv}` marshalling. |
| Raw D-Bus wire | No | No | Re-implements SASL EXTERNAL + marshalling; most risk. |
| shell `pkcheck` | No | Needs `pkcheck` present | No pidfd via CLI; external-binary dep; spike-only. |

## 7. Never accept a cached "yes" (critical)
Paths that return authorized without a live human, each designed out:
- `allow_* = yes` → no prompt at all. Never use `yes`.
- `auth_self_keep`/`auth_admin_keep` → retained ~5 min; subsequent checks return authorized with no interaction. Use plain `auth_self` (no keep) → mints no temporary authorization, re-prompts every time.
- `/etc/polkit-1/rules.d/` admin override → out of app control (residual).
**Enforce:** action `auth_self` on all three scopes, no keep, no yes; `unix-process`+pidfd; `AllowUserInteraction`; reject any authorized-without-challenge result; optionally revoke temporary authorizations before each check. Same "fresh interaction, no cached yes" principle applies to macOS (fresh `LAContext`) and Windows (fresh Hello request).

## Bottom line
- polkit **replaces** the direct-libpam Linux design (safer per the CVE; simpler; unifies with mac/Win).
- `Tmds.DBus.Protocol`, pinned.
- Force fresh interaction; `auth_self` no-keep; reject cached yes.
- Cost: one-time root `.policy` install; fail-closed without it. Out-of-band strength depends on a pre-existing trusted session agent — guard never spawns one.

Citations: polkit.pages.freedesktop.org/polkit/ (Authority + AuthenticationAgent interfaces, polkit.8, polkit-apps), manpages.ubuntu.com (pkcheck.1, pkttyagent.1), github.com/linux-pam/linux-pam/issues/812 + CVE-2024-37408, github.com/tmds/Tmds.DBus, nuget.org/packages/Tmds.DBus.Protocol.
