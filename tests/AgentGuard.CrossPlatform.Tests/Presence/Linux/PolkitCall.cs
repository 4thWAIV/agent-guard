// Copyright (c) 4thWAIV. All rights reserved.

using AgentGuard.CrossPlatform.Linux;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// The arguments of a single <c>CheckAuthorization</c> call, captured as one value so the shared
/// <see cref="SingleCallRecorder{TArg,TResult}"/> can hold them for <see cref="FakePolkitAuthority"/>.
/// </summary>
/// <param name="Subject">The unix-process subject passed to the port.</param>
/// <param name="ActionId">The polkit action id passed to the port.</param>
/// <param name="Message">The per-call message passed to the port.</param>
internal sealed record PolkitCall(PolkitSubject Subject, string ActionId, string Message);
