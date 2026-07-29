// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Engine.Abstractions;

/// <summary>
/// The breadth of change a <see cref="Grant"/> authorizes for a covered path.
/// </summary>
public enum GrantScope
{
    /// <summary>
    /// Any change to the covered path is authorized. The only scope implemented in v1.
    /// </summary>
    All,

    /// <summary>
    /// Only a change inside a later, language-specific envelope is authorized. Reserved for a future version.
    /// </summary>
    Part,
}
