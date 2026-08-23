// Copyright (c) 4thWAIV. All rights reserved.

using System;
using AgentGuard.Abstractions;
using AgentGuard.Engine;
using AgentGuard.TestHelpers;
using Microsoft.Extensions.Time.Testing;

namespace AgentGuard.Tests;

/// <summary>
/// Shared helpers for building normalized calls, environments, and pipeline options in the behavioral tests.
/// </summary>
internal static class TestSupport
{
    /// <summary>
    /// Starts a builder over the in-memory fakes for a pipeline test, supplying the one service the pipeline touches
    /// at construction that <c>Fake()</c> has no built-in for — the real Ed25519 signature service. It is never
    /// exercised in these tests (no grant tokens are present, so grant verification short-circuits before the
    /// verifier is called), so this reaches the real trust anchor without masking anything. The fake filesystem's
    /// syntactic-identity canonicalization (no base platform resolving symlinks) is what lets a test key
    /// <c>Handle</c>/<c>MarkInaccessible</c> on a raw path.
    /// </summary>
    /// <returns>A fake builder with the real signature service supplied.</returns>
    internal static SystemServicesBuilder FakeServices() =>
        SystemServicesBuilder.Fake().With(SystemServicesBuilder.Real().Build().Signatures);

    /// <summary>
    /// Builds pipeline options for a project root with no grant key, using the given clock.
    /// </summary>
    /// <param name="root">The project root.</param>
    /// <param name="time">The clock.</param>
    /// <returns>The options.</returns>
    internal static GuardEngineOptions Options(string root, FakeTimeProvider time) =>
        Options(root, time, ReadOnlyMemory<byte>.Empty);

    /// <summary>
    /// Builds pipeline options with an explicit grant public key.
    /// </summary>
    /// <param name="root">The project root.</param>
    /// <param name="time">The clock.</param>
    /// <param name="grantPublicKey">The grant public key.</param>
    /// <returns>The options.</returns>
    internal static GuardEngineOptions Options(string root, FakeTimeProvider time, ReadOnlyMemory<byte> grantPublicKey) =>
        new(root, SystemServicesBuilder.Real().With((TimeProvider)time).Build(), grantPublicKey);

    /// <summary>
    /// Builds pipeline options with a tiny per-file snapshot ceiling to exercise the capture-failure path.
    /// </summary>
    /// <param name="root">The project root.</param>
    /// <param name="time">The clock.</param>
    /// <param name="perFileCeiling">The per-file snapshot ceiling, in bytes.</param>
    /// <returns>The options.</returns>
    internal static GuardEngineOptions OptionsWithCeiling(string root, FakeTimeProvider time, long perFileCeiling) =>
        new(root, SystemServicesBuilder.Real().With((TimeProvider)time).Build(), ReadOnlyMemory<byte>.Empty, perFileCeiling);

    /// <summary>
    /// Builds an Edit tool call over the given target paths.
    /// </summary>
    /// <param name="toolUseId">The tool-use id.</param>
    /// <param name="paths">The target paths.</param>
    /// <returns>The tool call.</returns>
    internal static ToolCall Edit(string toolUseId, params string[] paths) =>
        new(new ToolCallId(toolUseId), "Edit", new FileWriteInput(paths));

    /// <summary>
    /// Builds a Bash tool call with the given command.
    /// </summary>
    /// <param name="toolUseId">The tool-use id.</param>
    /// <param name="command">The command line.</param>
    /// <returns>The tool call.</returns>
    internal static ToolCall Bash(string toolUseId, string command) =>
        new(new ToolCallId(toolUseId), "Bash", new ShellCommandInput(command));

    /// <summary>
    /// Builds a call environment for the given project root and event.
    /// </summary>
    /// <param name="root">The project root.</param>
    /// <param name="hookEvent">The lifecycle event.</param>
    /// <returns>The environment.</returns>
    internal static CallEnvironment Env(string root, HookEvent hookEvent) => new(root, hookEvent, null);
}
