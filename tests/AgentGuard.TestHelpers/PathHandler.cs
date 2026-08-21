// Copyright (c) 4thWAIV. All rights reserved.

using System;

namespace AgentGuard.TestHelpers;

/// <summary>
/// A per-path interceptor a test installs through <c>OnFileSystem().Handle(path, ...)</c> — the first link in the
/// resolution chain (before the overlay and the real base). For a matched path the handler runs first and either
/// throws (an injected failure), returns an override value for the operation, or delegates to
/// <paramref name="passThrough"/> to let the normal overlay-then-base chain answer.
/// </summary>
/// <param name="operation">The filesystem operation being taken control of.</param>
/// <param name="passThrough">Invokes the normal overlay-then-base resolution and returns its (boxed) result, so the
/// handler can pass an operation through unchanged.</param>
/// <returns>The (boxed) result for the operation — the override the handler chose, or the value
/// <paramref name="passThrough"/> produced. A void operation returns <see langword="null"/>.</returns>
public delegate object? PathHandler(FileSystemOperation operation, Func<object?> passThrough);
