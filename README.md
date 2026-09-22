# agent-guard
Guards critical files like project configs from agent changes to ensure project rules are not disabled by agents.

## Contributing

Start new work from current `origin/dev`. `dev` is the integration branch; `main` holds milestone releases.
For a fresh checkout:

```sh
git clone --branch dev git@github.com:4thWAIV/agent-guard.git
cd agent-guard
```

For an existing checkout, preserve local changes, fetch `origin`, and create your work branch from `origin/dev`.

Install the exact .NET SDK specified in [global.json](global.json); roll-forward is disabled. Git must be
available. The Makefile also requires `make`; use the equivalent `dotnet` commands below when it is unavailable.
Run these commands from the repository root:

```sh
dotnet --version
make build
make test
```

`make build` restores packages and builds the solution. `make test` builds first and then runs the tests.
Without `make`, run `dotnet restore`, `dotnet build`, and `dotnet test`. These commands run the build-time
analyzers; CI additionally enforces coverage and platform checks in [ci.yml](.github/workflows/ci.yml).
Ordinary local builds do not require signing keys. For signing-specific work, use the existing
[signing guide](eng/signing/README.md).

Before making changes, read [how to work in this codebase](.agents/skills/rails-read-me/SKILL.md).
It links to the skills that own approvals, contracts, workflow execution, and verification. See
[the .dev folder guide](.dev/README.md) for work-item records. The
[GitHub issues](https://github.com/4thWAIV/agent-guard/issues) track planned and open work.

## Using the guard

The current CLI wires Claude Code's `PreToolUse` and `PostToolUse` hooks. The engine checks core paths before a
tool call, captures protected files, and checks for unauthorized changes afterward. Where possible, the post
check restores the protected content. This targets agents taking shortcuts; it is not a security boundary
against a malicious attacker.

The CLI commands are `hook <pre|post>`, `install`, `init`, `remove`, `doctor` (optionally `--fix`), and `version`.
`install` installs the binary under `~/.agentguard`; `init` wires the current repository; `remove` removes that
repository's wiring. Those three commands require OS-backed human presence approval. `doctor` checks the
installation and wiring; `doctor --fix` repairs them without that presence prompt.

Project configuration lives in `.agentguard/config.json`, whose `protectedPaths` array adds project rules.
The C# provider also protects its built-in build-configuration files. Configuration types and accepted path
patterns are defined in [ProjectConfig](src/AgentGuard.Engine/ProjectConfig.cs) and
[PatternMatcherFactory](src/AgentGuard.Engine/PatternMatcherFactory.cs).
For the executable command surface, see [Program.cs](src/AgentGuard.Cli/Program.cs).
Historical designs and approved plans can describe capabilities not yet implemented; they are not a CLI reference.

## Running alpha builds

Alpha releases are signed with a self-signed certificate, so macOS Gatekeeper and Windows SmartScreen warn on
first launch. See [Running the alpha (self-signed) builds](docs/running-alpha-builds.md) for the one-time steps
to trust and run them on macOS, Windows, and Linux.
