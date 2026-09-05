# appd-1-process — what the next design round has to work out

Things that came up after the first DESIGN round and are not settled. Each one needs a design before it can be built. This file exists so none of them is remembered rather than written down.

## Reading the login-item templates out of the binary

The three login-item files — the macOS property list, the Linux systemd unit, and the Windows Task Scheduler definition — ship as templates embedded in the binary rather than as loose files on disk. Tim's call, and it is the right one: nothing extra to install, nothing to go missing, and the templates are covered by the same signature and the same install-integrity hash that already protect the binary, so an agent cannot edit a loose file to redirect the login item somewhere else.

None of this exists yet. There is no embedded resource anywhere in the tree and no abstraction for reading one. The next design round has to settle three things. Who owns reading an embedded resource, given that pulling one out of the assembly is reflection on that assembly and the fence already gives reflection an owner — version reading belongs to `IBuildInfo` — so a raw `GetManifestResourceStream` call needs an owner of its own rather than sitting wherever it is convenient. What the interface looks like, since it has to be fakeable for unit tests that never touch a real assembly. And where the placeholder substitution happens, because each template carries the launcher path and somebody has to fill it in.

## Answering "is the lock held" without taking the lock

The chosen design splits the lock into taking it and asking whether anyone holds it. On Windows the question is free — you can ask whether a named mutex exists without acquiring it. On macOS and Linux opening the file exclusively *is* taking it, and the design never says how the check avoids that. Left as it stands, a CLI probe holds the lock for an instant, and a daemon starting in that same instant fails to acquire and exits.

Tim's answer, recorded in `conversation.md`, is a double-checked lock — a preparation lock and a real one — and the next design round has to either work that out concretely or produce a check that genuinely does not acquire. Note that the failure is fail-closed: the daemon exits rather than a second one starting, so this is correctness of the start path rather than a hole in the single-instance guarantee.

## Whether writing the registration is enough to start it

Added by Claude rather than named by Tim — strike it if it does not belong here.

Writing the property list or the unit file may not be sufficient for an immediate start in the same login session that is already running. launchd may need the job bootstrapped into the session before `kickstart` can find it, and systemd may need a daemon reload before `systemctl --user start` sees a newly written unit. Nobody has run this, on any platform. It bears directly on the issue's requirement that installing onto an already-running session leaves appd running, so a design round that assumes write-then-start works is assuming the acceptance criterion.
