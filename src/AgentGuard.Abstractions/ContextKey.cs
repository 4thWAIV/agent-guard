// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.Abstractions;

/// <summary>
/// The triple that addresses one record in the Context store: which call, which Guard, and which kind of data.
/// </summary>
/// <param name="ToolCall">The call whose Pre and Post share this record.</param>
/// <param name="Guard">The name of the Guard that owns the record.</param>
/// <param name="Kind">The Guard-defined label for the kind of data stored (for the File Guard, the snapshot).</param>
public sealed record ContextKey(ToolCallId ToolCall, string Guard, string Kind);
