// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;

namespace AgentGuard.TestSupport;

/// <summary>
/// The single owner of the "delete a throwaway temporary directory, best effort" cleanup that the test fixtures
/// perform on disposal. Every fixture that owns a disposable temp tree — the setup harness, the on-disk project
/// fixture, and the CLI runner — deletes it through here, so the tolerate-a-transient-lock cleanup is written once.
/// The source file is linked into each test project that needs it.
/// </summary>
internal static class TestTempDirectory
{
    /// <summary>
    /// Deletes a directory and its contents, swallowing the two failures a throwaway temp tree can raise on
    /// teardown — a transient lock (<see cref="IOException"/>) or a permission race
    /// (<see cref="UnauthorizedAccessException"/>). Cleanup must never fail a test; any other exception still
    /// surfaces.
    /// </summary>
    /// <param name="path">The directory to delete.</param>
    public static void DeleteBestEffort(string path)
    {
        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup of a throwaway temporary directory.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup of a throwaway temporary directory.
        }
    }
}
