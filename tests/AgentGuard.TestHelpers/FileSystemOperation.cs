// Copyright (c) 4thWAIV. All rights reserved.

namespace AgentGuard.TestHelpers;

/// <summary>
/// The filesystem operation a per-path <see cref="PathHandler"/> is asked to take control of. A test's
/// <c>OnFileSystem().Handle(path, ...)</c> lambda receives this so it can branch — throw for one operation, override
/// another, and pass the rest through — the way <see cref="SystemServicesBuilder"/>'s <c>MarkInaccessible</c>
/// convenience throws on <see cref="EnumerateChildren"/> while leaving <see cref="DirectoryExists"/> answering true.
/// </summary>
public enum FileSystemOperation
{
    /// <summary>A file-existence read (<c>IFileReader.Exists</c>).</summary>
    Exists,

    /// <summary>A file byte read (<c>IFileReader.ReadAllBytes</c>/<c>ReadAllBytesAsync</c>).</summary>
    ReadAllBytes,

    /// <summary>A file text read (<c>IFileReader.ReadAllText</c>/<c>ReadAllTextAsync</c>).</summary>
    ReadAllText,

    /// <summary>A file attributes read (<c>IFileReader.GetAttributes</c>).</summary>
    GetAttributes,

    /// <summary>A file last-write-time read (<c>IFileReader.GetLastWriteTimeUtc</c>).</summary>
    GetFileLastWriteTimeUtc,

    /// <summary>A directory-existence read (<c>IDirectoryEnumerator.DirectoryExists</c>).</summary>
    DirectoryExists,

    /// <summary>A directory child enumeration (<c>IDirectoryEnumerator.EnumerateChildren</c>).</summary>
    EnumerateChildren,

    /// <summary>A directory file-pattern enumeration (<c>IDirectoryEnumerator.EnumerateFiles</c>).</summary>
    EnumerateFiles,

    /// <summary>A directory subdirectory-pattern enumeration (<c>IDirectoryEnumerator.EnumerateDirectories</c>).</summary>
    EnumerateDirectories,

    /// <summary>A directory last-write-time read (<c>IDirectoryEnumerator.GetLastWriteTimeUtc</c>).</summary>
    GetDirectoryLastWriteTimeUtc,

    /// <summary>A file write (<c>IFileWriter.WriteAllText</c>/<c>WriteAllBytesAsync</c>).</summary>
    WriteFile,

    /// <summary>A file copy (<c>IFileWriter.Copy</c>).</summary>
    Copy,

    /// <summary>A file move (<c>IFileWriter.Move</c>).</summary>
    Move,

    /// <summary>A file delete (<c>IFileWriter.DeleteFile</c>).</summary>
    DeleteFile,

    /// <summary>A directory create (<c>IDirectoryWriter.CreateDirectory</c>).</summary>
    CreateDirectory,

    /// <summary>A directory delete (<c>IDirectoryWriter.DeleteDirectory</c>).</summary>
    DeleteDirectory,

    /// <summary>A directory last-write-time set (<c>IDirectoryWriter.SetLastWriteTimeUtc</c>).</summary>
    SetLastWriteTimeUtc,
}
