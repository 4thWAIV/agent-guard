# appd-5-key-and-presence — what Tim decided in conversation

This run covers GitHub issue #67 in full. The live issue is the authority; where it and this file disagree, the issue wins — except where a decision below explicitly replaces something the issue says, which is noted inline.

This decision was settled while working on issue #63 and is recorded here because it governs #67.

## Decisions

**appd does not ask for the user's presence when it starts. It asks the first time a client requests work.** This replaces the issue text, which currently reads "appd authenticates the user once at start" and "On all three platforms appd prompts once at start." Under this decision appd starts silently at login holding no unlocked key, and the presence check happens when a client actually asks for something that needs it. Tim's decision, in his words: "we only need the presence check once a client requests work done.  so lazy verify is better here as we have exposed nothing and it would be user jarring to be getting a check on login every time." The reasoning he gave is that nothing has been exposed until a client asks, and a prompt at every login is jarring for no gain. Issue #67's body still carries the older wording and should be brought into line before that work starts.

**appd loads the key on the first request that needs it, not at startup.** This travels with the presence decision above rather than being a separate choice: if reading the key requires a prompt, then loading it at startup is the startup prompt, and moving the presence check without moving the key load would leave the prompt exactly where it was. Once loaded the key stays in the process, which is the whole reason appd is long-running. What protects the key at rest — where it sits when appd is not running, and whether reading it requires the user — is not settled and is issue #67's to answer; this decision holds whichever way that goes, because if reading prompts then lazy loading is forced, and if it does not, lazy loading still keeps unlocked key material out of memory until something needs it.
