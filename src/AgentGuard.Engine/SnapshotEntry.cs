// Copyright (c) 4thWAIV. All rights reserved.

using System;

namespace AgentGuard.Engine;

/// <summary>
/// One protected path's record in a pre-image snapshot: its canonical path, whether it existed at capture
/// (distinct from an empty file), and its captured bytes when it did.
/// </summary>
/// <param name="Path">The canonical path of the protected file.</param>
/// <param name="Present">Whether the file existed at capture time.</param>
/// <param name="Content">The captured bytes when <paramref name="Present"/> is <see langword="true"/>; otherwise empty.</param>
internal sealed record SnapshotEntry(string Path, bool Present, ReadOnlyMemory<byte> Content);
