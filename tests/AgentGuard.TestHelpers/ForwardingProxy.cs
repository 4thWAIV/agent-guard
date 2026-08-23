// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.TestHelpers;

/// <summary>
/// The generic base for a <c>Wrap</c> proxy: it holds the inner service a proxy forwards to, so a concrete proxy in this
/// assembly overrides only the one member it wants to intercept and forwards the rest to <see cref="Inner"/>. It lives in
/// <c>AgentGuard.TestHelpers</c> because a <c>.Tests</c> project may not implement a leaf/platform interface itself
/// (AG0030) — a proxy that wraps a leaf is built here and reached through <see cref="SystemServicesBuilder"/>'s <c>Wrap</c>
/// seam. It is abstract: a concrete subclass supplies the interface implementation that delegates to <see cref="Inner"/>.
/// </summary>
/// <typeparam name="TService">The owned service interface the concrete proxy wraps.</typeparam>
internal abstract class ForwardingProxy<TService>
{
    /// <summary>Initializes a new instance of the <see cref="ForwardingProxy{TService}"/> class over its inner service.</summary>
    /// <param name="inner">The inner service a non-overridden member forwards to.</param>
    protected ForwardingProxy(TService inner) => Inner = inner;

    /// <summary>Gets the inner service a non-overridden member forwards to.</summary>
    protected TService Inner { get; }
}
