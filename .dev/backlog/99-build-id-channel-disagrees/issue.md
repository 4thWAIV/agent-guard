The build id is assembled by two copies of the same formula, an arrangement the files themselves declare and authorize: `eng/compute-build-id.sh` for CI, and `eng/version.compute.targets` for a local build. The channel-to-suffix mapping has drifted between them.

`eng/compute-build-id.sh` maps three cases:

```
case "$CHANNEL" in
  dev)  PRE="-pre-release" ;;
  pr-*) PRE="-$CHANNEL" ;;
  *)    PRE="" ;;
esac
```

`eng/version.compute.targets` maps one:

```
<_AgPre Condition="'$(AgentGuardChannel)' == 'dev'">-pre-release</_AgPre>
```

The pull-request channel added by the `pr-build-version-channel` decision went into the shell copy only. A build run locally or in any other MSBuild path with `-p:AgentGuardChannel=pr-123` is stamped `0.1.<combined>`, with no `-pr-123`, so a pull-request build is labelled as a release.

Both copies also treat an unrecognized channel as a release rather than refusing it. A mistyped channel such as `devv` produces `0.1.<combined>`, which is indistinguishable from a build off `main`. This is the silent-failure class the project's first rule forbids: a value that could not be interpreted is passed through instead of surfaced.

## What needs to happen

1. Make the channel-to-suffix mapping agree across both copies, or hold it in one place both read.
2. Make an unrecognized channel a loud failure in both copies, so only an empty channel means a release.
3. Cover the mapping with a test that fails if the two copies disagree.

Found while summarizing how build numbers are generated; no code was changed.
