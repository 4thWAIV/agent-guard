// Copyright (c) 4thWAIV. All rights reserved.

using System;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// The one shared test-double recorder the three per-OS native-port fakes compose — macOS
/// <c>FakeLocalAuthentication</c>, Windows <c>FakeWindowsUserPresence</c>, and Linux <c>FakePolkitAuthority</c> — instead
/// of each hand-building the same "count the calls, capture the last argument, hand back a fixed native result (or throw
/// to model a boundary fault)" shape (prior-art-ledger: per-OS native-port test recorder — no prior owner existed, so it
/// is EXTRACTed to this single owner and the three fakes reference it). It touches no native library, and it lives
/// directly under <c>Presence/</c> so it is compiled on every OS leg, while the fakes that supply its per-OS
/// <typeparamref name="TArg"/>/<typeparamref name="TResult"/> stay in their per-OS folders.
/// </summary>
/// <typeparam name="TArg">The single captured argument of the port call (a per-OS record when the call has several).</typeparam>
/// <typeparam name="TResult">The plain native result the port returns.</typeparam>
internal sealed class SingleCallRecorder<TArg, TResult>
{
    private readonly TResult _result;
    private readonly Func<Exception>? _fault;

    private SingleCallRecorder(TResult result, Func<Exception>? fault)
    {
        _result = result;
        _fault = fault;
    }

    /// <summary>Gets the number of times <see cref="Record"/> has been invoked.</summary>
    internal int CallCount { get; private set; }

    /// <summary>Gets the argument captured on the most recent call, or the default when none has occurred.</summary>
    internal TArg? LastArg { get; private set; }

    /// <summary>Creates a recorder that returns <paramref name="result"/> on every call.</summary>
    /// <param name="result">The result every call returns.</param>
    /// <returns>The recorder.</returns>
    internal static SingleCallRecorder<TArg, TResult> Returning(TResult result) => new(result, fault: null);

    /// <summary>Creates a recorder that throws the produced exception on every call, modelling a boundary fault.</summary>
    /// <param name="fault">The factory for the exception thrown on each call.</param>
    /// <returns>The faulting recorder.</returns>
    internal static SingleCallRecorder<TArg, TResult> Faulting(Func<Exception> fault) => new(default!, fault);

    /// <summary>Counts the call, captures <paramref name="arg"/>, then returns the fixed result or throws the fault.</summary>
    /// <param name="arg">The argument this call was given.</param>
    /// <returns>The configured result.</returns>
    internal TResult Record(TArg arg)
    {
        CallCount++;
        LastArg = arg;
        return _fault is null ? _result : throw _fault();
    }
}
