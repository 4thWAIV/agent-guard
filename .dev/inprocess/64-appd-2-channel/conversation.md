# appd channel — planning brief

Status: Current scope for planning. Tim and Codex own the design work. Claude handles the authorized investigation and workflow execution, including GROUND, prior-art discovery, and the hidden-decision scan, and returns findings for the design discussion. No workflow stage has started.

The live channel issue still describes the earlier per-user server dependency. Its replacement wording must be approved and published before GROUND.

## Delivery

Deliver the communication channel before the appd process. It establishes connections, sends and receives messages, closes connections, and exposes OS-derived peer information. The CLI, server, and viewer will use the same transport implementation.

The channel is independently testable without an installed appd service. Both endpoints can run in one process through the actual OS transport. Identity-related checks can compare the reported peer with the known current process. Distinct-process and distinct-user tests cover the boundaries that require them.

## Replaceable message protocol

Message structure is separate from the underlying communication transport. Replacing how messages are structured must preserve the transport libraries and their platform implementations.

The design must show where message encoding, decoding, and message-boundary handling belong. Transport-specific framing requirements must be distinguished from the application's message format. The protocol replacement must work through the designed interfaces rather than require edits to native transport code.

Demonstrate the separation with two message-format implementations using the same transport. The test must change the message representation, not merely substitute different values in one fixed representation.  An example may be one JSON format and one BSON format.

## Identity and verification interfaces

This delivery includes the interface points needed by the later identity and verification system. The transport supplies OS-derived evidence associated with the actual connection. The design supports verification of the peer at both endpoints.

The design must show how a verifier receives that evidence, how the caller supplies an expected process instance when needed, and how its result controls use of the connection. It must also show where checks required around an exchange can run and how evidence remains valid for the lifetime in which it is used.

The initial wired implementation always returns the acceptance result without performing identity checks. Both endpoints call it through the verification interface in the normal communication path. The concrete result type and names are part of the interface design. The actual identity implementation later replaces this implementation through composition, without changing the transport, message protocol, or verification call sites.

Before approving the interface, map each requirement of the planned identity implementation to the OS evidence or capability it needs. Establish how that information is obtained on each platform, how it is associated with the connection and process instance, and how long it remains usable. Exercise the real evidence path in this delivery even though the initial verifier accepts unconditionally. Supplying the actual verifier later must not require restructuring the channel to obtain missing information.

The verification interface must accommodate acceptance, rejection, and inability to establish identity. Use test verifiers to demonstrate these paths and show that a rejected or unverifiable peer cannot proceed where verification is required.

The later identity implementation supplies the product checks. This delivery supplies and exercises the integration points. The message-format implementation must not be responsible for deciding whether the peer is trusted.

## Platform requirements

Support Windows, macOS, and Linux. Endpoint ownership and access must support the intended machine-wide server and its separate user/session clients. The exact endpoint names and access policy require design review.

Preserve the channel issue's local-IPC requirements and the XPC-from-.NET feasibility investigation. Establish what each OS actually exposes for both peers and identify any limitation that affects the identity interfaces before those interfaces are approved.

## Design review

Tim and Codex develop the design from the investigation findings. Claude's findings inform that work; they do not settle interface or architecture choices on Tim's behalf.

Present the actual C# interfaces and their relationships at the transport, message-protocol, and peer-verification boundaries. Show callers, assembly ownership, platform implementations, and connection lifetime. Reuse existing owners and libraries where suitable.

Tim approves the interface declarations before implementation. The proof must establish both protocol replacement and verification integration without restructuring the transport.

## Resume point

First reconcile the live channel issue and the proposed dependency changes with this brief. The previous per-user endpoint decisions remain in git history at `6c601cb:.dev/backlog/64-appd-2-channel/conversation.md`. The appd architecture context is in [the process brief](../../backlog/63-appd-1-process/conversation.md).

Tim intends Claude to work on static-class evaluation before resuming the channel. After Tim approves the channel wording and authorizes publication, synchronize the issue and its local copy. Claude can then run the authorized workflow. The appd process work remains paused in backlog.
