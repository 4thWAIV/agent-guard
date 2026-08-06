.PHONY: restore version build test clean publish

# CHANNEL=dev produces a -pre-release stamp; empty => release. The version step runs once here so every
# `make build` stamps a fresh, shared version (decisions 15/17/25). A bare `dotnet build` (IDE) skips it and
# gets a clearly non-authoritative local fallback.

restore:
	dotnet restore

version:
	./eng/version.sh $(CHANNEL)

build: restore version
	dotnet build

test: build
	dotnet test

publish: version
	dotnet publish src/AgentGuard.Cli/AgentGuard.Cli.csproj -c Release

clean:
	dotnet clean
	rm -rf eng/obj
