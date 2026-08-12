// Copyright (c) 4thWAIV. All rights reserved.

using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

public class FilesystemOnlyInBoundariesAnalyzerTests
{
    private const string FileExistsSource = """
        using System.IO;

        public class Sample
        {
            public bool Check(string path) => File.Exists(path);
        }
        """;

    private const string DirectoryCreateSource = """
        using System.IO;

        public class Sample
        {
            public void Make(string path) => Directory.CreateDirectory(path);
        }
        """;

    private const string FileStreamSource = """
        using System.IO;

        public class Sample
        {
            public Stream Open(string path) => new FileStream(path, FileMode.Open);
        }
        """;

    private const string DirectoryInfoEnumerateSource = """
        using System.IO;

        public class Sample
        {
            public FileSystemInfo[] Walk(string path) => new DirectoryInfo(path).GetFileSystemInfos();
        }
        """;

    // The single-owner proof: a stub owning interface in AgentGuard.Abstractions, and TWO classes in the SAME
    // assembly (compiled into AgentGuard.CrossPlatform, where the file-op owners live) — the owner implementing the
    // interface (its File.Exists is exempt) and a sibling that does not implement it (its Directory.CreateDirectory is
    // RED). This is what proves the tightening from an assembly-wide exemption to the one owner class.
    private const string OwnerAndSiblingSource = """
        using System.IO;

        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IFileReader { }
        }

        namespace Engine
        {
            public sealed class FileReader : AgentGuard.Abstractions.Contracts.IFileReader
            {
                public bool Check(string path) => File.Exists(path);
            }

            public sealed class Sibling
            {
                public void Make(string path) => Directory.CreateDirectory(path);
            }
        }
        """;

    // A class implementing ONLY IFileWriter, calling File.Exists (IFileReader's member). one-owner-class-per-primitive
    // maps each member to its ONE owning interface, so implementing a different one of the four does not exempt this
    // cross-owner call — it is still RED.
    private const string FileWriterOwnerCallingFileExistsSource = """
        using System.IO;

        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IFileWriter { }
        }

        namespace Boundaries
        {
            public sealed class FileWriter : AgentGuard.Abstractions.Contracts.IFileWriter
            {
                public bool Check(string path) => File.Exists(path);
            }
        }
        """;

    // The same IFileWriter-only owner calling Directory.CreateDirectory (IDirectoryWriter's member) — also RED, because
    // CreateDirectory is owned by IDirectoryWriter, not IFileWriter.
    private const string FileWriterOwnerCallingCreateDirectorySource = """
        using System.IO;

        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IFileWriter { }
        }

        namespace Boundaries
        {
            public sealed class FileWriter : AgentGuard.Abstractions.Contracts.IFileWriter
            {
                public void Make(string path) => Directory.CreateDirectory(path);
            }
        }
        """;

    // Each member's correct single-owner class calling one of its OWN members — all four exempt. This also proves the
    // type disambiguation of the shared names: File.Exists routes to IFileReader while Directory.Exists routes to
    // IDirectoryEnumerator, and File.Delete routes to IFileWriter while Directory.Delete routes to IDirectoryWriter.
    private const string EachOwnerExemptForItsMemberSource = """
        using System.Collections.Generic;
        using System.IO;

        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IFileReader { }
            public interface IDirectoryEnumerator { }
            public interface IFileWriter { }
            public interface IDirectoryWriter { }
        }

        namespace Boundaries
        {
            public sealed class FileReader : AgentGuard.Abstractions.Contracts.IFileReader
            {
                public bool Check(string path) => File.Exists(path);
                public string Read(string path) => File.ReadAllText(path);
            }

            public sealed class SystemDirectoryEnumerator : AgentGuard.Abstractions.Contracts.IDirectoryEnumerator
            {
                public bool Has(string path) => Directory.Exists(path);
                public IEnumerable<string> List(string path) => Directory.EnumerateFiles(path, "*");
            }

            public sealed class FileWriter : AgentGuard.Abstractions.Contracts.IFileWriter
            {
                public void Write(string path, string text) => File.WriteAllText(path, text);
                public void Remove(string path) => File.Delete(path);
            }

            public sealed class DirectoryWriter : AgentGuard.Abstractions.Contracts.IDirectoryWriter
            {
                public void Make(string path) => Directory.CreateDirectory(path);
                public void Drop(string path) => Directory.Delete(path, true);
            }
        }
        """;

    // Wrong-assembly self-grant probe: a class implementing IFileReader (the file-op interface) that calls File.Exists.
    // In its owner assembly AgentGuard.CrossPlatform it is exempt; in any other assembly the same class is RED — the
    // assembly half of the conjunction blocks the self-grant a test fake (e.g. RecordingFileReader in TestHelpers)
    // would otherwise get merely by declaring ': IFileReader'.
    private const string FileReaderOwnerCallingFileExistsSource = """
        using System.IO;

        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IFileReader { }
        }

        namespace CrossPlatform
        {
            public sealed class FileReader : AgentGuard.Abstractions.Contracts.IFileReader
            {
                public bool Check(string path) => File.Exists(path);
            }
        }
        """;

    // FIX B: IDirectoryWriter has no move/rename member (CreateDirectory, DeleteDirectory, SetLastWriteTimeUtc), so a
    // directory-side Directory.Move has NO owner and is banned everywhere — even inside the DirectoryWriter owner class
    // compiled into its owner assembly AgentGuard.CrossPlatform, where both halves of the usual conjunction pass. A
    // test-only exemption there would silently green a raw call the interface cannot back
    // (complete-the-set-not-a-test-fake).
    private const string DirectoryWriterOwnerCallingDirectoryMoveSource = """
        using System.IO;

        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IDirectoryWriter { }
        }

        namespace CrossPlatform
        {
            public sealed class DirectoryWriter : AgentGuard.Abstractions.Contracts.IDirectoryWriter
            {
                public void Rename(string from, string to) => Directory.Move(from, to);
            }
        }
        """;

    // FIX A (shared-name timestamp disambiguation). GetLastWriteTimeUtc is declared on BOTH System.IO.File and
    // System.IO.Directory, so the declaring type resolves the owner: File.GetLastWriteTimeUtc is IFileReader's member
    // (the contract completed the set on IFileReader), Directory.GetLastWriteTimeUtc is IDirectoryEnumerator's. The
    // FileReader owner reading File.GetLastWriteTimeUtc is exempt only in its owner assembly AgentGuard.CrossPlatform.
    private const string FileReaderOwnerReadingFileLastWriteTimeSource = """
        using System;
        using System.IO;

        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IFileReader { }
        }

        namespace CrossPlatform
        {
            public sealed class FileReader : AgentGuard.Abstractions.Contracts.IFileReader
            {
                public DateTime When(string path) => File.GetLastWriteTimeUtc(path);
            }
        }
        """;

    // A plain non-owner class reading File.GetLastWriteTimeUtc — the shape of the live test-file sites
    // (InitCommandTests / InstallCommandTests / RemoveCommandTests). RED anywhere but the FileReader owner.
    private const string FileLastWriteTimeInPlainClassSource = """
        using System;
        using System.IO;

        public class Sample
        {
            public DateTime When(string path) => File.GetLastWriteTimeUtc(path);
        }
        """;

    // Cross-owner: an IDirectoryEnumerator owner reading File.GetLastWriteTimeUtc is RED even in its owner assembly —
    // the File side routes to IFileReader, not to the IDirectoryEnumerator this class implements. Proves the File side
    // is disambiguated to IFileReader specifically, not lumped with the directory timestamp getter.
    private const string DirectoryEnumeratorOwnerReadingFileLastWriteTimeSource = """
        using System;
        using System.IO;

        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IDirectoryEnumerator { }
        }

        namespace CrossPlatform
        {
            public sealed class SystemDirectoryEnumerator : AgentGuard.Abstractions.Contracts.IDirectoryEnumerator
            {
                public DateTime When(string path) => File.GetLastWriteTimeUtc(path);
            }
        }
        """;

    // The IDirectoryEnumerator owner reading Directory.GetLastWriteTimeUtc — exempt in its owner assembly, proving the
    // Directory side of the same shared name routes to IDirectoryEnumerator.
    private const string DirectoryEnumeratorOwnerReadingDirectoryLastWriteTimeSource = """
        using System;
        using System.IO;

        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IDirectoryEnumerator { }
        }

        namespace CrossPlatform
        {
            public sealed class SystemDirectoryEnumerator : AgentGuard.Abstractions.Contracts.IDirectoryEnumerator
            {
                public DateTime When(string path) => Directory.GetLastWriteTimeUtc(path);
            }
        }
        """;

    // Cross-owner mirror: an IFileReader owner reading Directory.GetLastWriteTimeUtc is RED even in its owner assembly —
    // the Directory side routes to IDirectoryEnumerator, not to the IFileReader this class implements.
    private const string FileReaderOwnerReadingDirectoryLastWriteTimeSource = """
        using System;
        using System.IO;

        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IFileReader { }
        }

        namespace CrossPlatform
        {
            public sealed class FileReader : AgentGuard.Abstractions.Contracts.IFileReader
            {
                public DateTime When(string path) => Directory.GetLastWriteTimeUtc(path);
            }
        }
        """;

    // FIX A (no-owner File side of a shared setter). File.SetLastWriteTimeUtc has NO owner — IFileWriter has no
    // timestamp setter — so it is RED everywhere, including inside the FileWriter owner class compiled into its owner
    // assembly AgentGuard.CrossPlatform, where both halves of the usual conjunction pass. The empty owner set means the
    // class is never exempt for it (complete-the-set-not-a-test-fake), unlike the directory side, which IDirectoryWriter
    // owns.
    private const string FileWriterOwnerSettingFileLastWriteTimeSource = """
        using System;
        using System.IO;

        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IFileWriter { }
        }

        namespace CrossPlatform
        {
            public sealed class FileWriter : AgentGuard.Abstractions.Contracts.IFileWriter
            {
                public void Touch(string path, DateTime time) => File.SetLastWriteTimeUtc(path, time);
            }
        }
        """;

    // The Directory side of the same setter — Directory.SetLastWriteTimeUtc IS IDirectoryWriter's member, so the
    // DirectoryWriter owner is exempt in its owner assembly. Proves the setter's two sides split: File no-owner, Directory owned.
    private const string DirectoryWriterOwnerSettingDirectoryLastWriteTimeSource = """
        using System;
        using System.IO;

        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IDirectoryWriter { }
        }

        namespace CrossPlatform
        {
            public sealed class DirectoryWriter : AgentGuard.Abstractions.Contracts.IDirectoryWriter
            {
                public void Touch(string path, DateTime time) => Directory.SetLastWriteTimeUtc(path, time);
            }
        }
        """;

    [Fact]
    public async Task FileReaderOwner_InWrongAssembly_IsReported_SelfGrantBlocked()
    {
        // FIX 1: implementing IFileReader is not enough — the class must also compile into AgentGuard.CrossPlatform. In
        // AgentGuard.Engine (the wrong assembly) the owner's File.Exists is RED, so a fake that declares ': IFileReader'
        // cannot launder a raw call.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(
                FileReaderOwnerCallingFileExistsSource, "AgentGuard.Engine"));
        Assert.Equal("AG0011", diagnostic.Id);
        Assert.Contains(
            "File.Exists",
            AnalyzerRunner.SpanText(FileReaderOwnerCallingFileExistsSource, diagnostic),
            System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task FileReaderOwner_InOwnerAssembly_IsExempt()
    {
        // The same class compiled into its owner assembly AgentGuard.CrossPlatform is exempt: both halves of the
        // conjunction pass.
        Assert.Empty(
            await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(
                FileReaderOwnerCallingFileExistsSource, "AgentGuard.CrossPlatform"));
    }

    [Fact]
    public async Task DirectoryMove_InDirectoryWriterOwner_InOwnerAssembly_IsStillReported()
    {
        // FIX B: Directory.Move resolves to the empty owner set (IDirectoryWriter has no move member), so it is RED
        // even in the DirectoryWriter owner class in its owner assembly AgentGuard.CrossPlatform — the empty set means
        // the class is never exempt for Directory.Move.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(
                DirectoryWriterOwnerCallingDirectoryMoveSource, "AgentGuard.CrossPlatform"));
        Assert.Equal("AG0011", diagnostic.Id);
        Assert.Contains(
            "Directory.Move",
            AnalyzerRunner.SpanText(DirectoryWriterOwnerCallingDirectoryMoveSource, diagnostic),
            System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task FileWriterOwner_OnFileReaderMember_FileExists_IsStillReported()
    {
        // AG0011 cross-owner leak: implementing IFileWriter does not exempt File.Exists, which IFileReader owns. In
        // the owner assembly (AgentGuard.CrossPlatform) too, so the RED is purely the cross-owner mismatch, not the
        // assembly gate.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(
                FileWriterOwnerCallingFileExistsSource, "AgentGuard.CrossPlatform"));
        Assert.Equal("AG0011", diagnostic.Id);
        Assert.Contains(
            "File.Exists",
            AnalyzerRunner.SpanText(FileWriterOwnerCallingFileExistsSource, diagnostic),
            System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task FileWriterOwner_OnDirectoryWriterMember_CreateDirectory_IsStillReported()
    {
        // AG0011 cross-owner leak: implementing IFileWriter does not exempt Directory.CreateDirectory, which
        // IDirectoryWriter owns. In the owner assembly (AgentGuard.CrossPlatform) too, so the RED is purely the
        // cross-owner mismatch, not the assembly gate.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(
                FileWriterOwnerCallingCreateDirectorySource, "AgentGuard.CrossPlatform"));
        Assert.Equal("AG0011", diagnostic.Id);
        Assert.Contains(
            "CreateDirectory",
            AnalyzerRunner.SpanText(FileWriterOwnerCallingCreateDirectorySource, diagnostic),
            System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task EachSingleOwnerClass_IsExempt_ForItsOwnMember()
    {
        // The correct single owner of each member is exempt for it — no diagnostics across all four classes, compiled
        // into their owner assembly AgentGuard.CrossPlatform.
        Assert.Empty(
            await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(
                EachOwnerExemptForItsMemberSource, "AgentGuard.CrossPlatform"));
    }

    [Fact]
    public async Task FileExists_OutsideBoundaries_IsReported()
    {
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(FileExistsSource, "AgentGuard.Engine");

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("AG0011", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task DirectoryCreate_OutsideBoundaries_IsReported()
    {
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(DirectoryCreateSource, "AgentGuard.Engine"));
        Assert.Equal("AG0011", diagnostic.Id);
    }

    [Fact]
    public async Task FileStreamConstruction_OutsideBoundaries_IsReported()
    {
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(FileStreamSource, "AgentGuard.Engine"));
        Assert.Equal("AG0011", diagnostic.Id);
    }

    [Fact]
    public async Task FileCall_InPerOsPlatformLibrary_IsStillReported()
    {
        // The file-op owners live in the AgentGuard.CrossPlatform contract assembly (owners-live-at-lowest-consumer),
        // NOT the per-OS AgentGuard.CrossPlatform.MacOS library. A plain class's raw File call there is still RED —
        // the forcing function that injects the file interface into the platform code instead of calling File raw.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(FileExistsSource, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0011", diagnostic.Id);
    }

    [Fact]
    public async Task OwnerImplementingInterface_IsExempt_SiblingInSameAssembly_IsStillReported()
    {
        // one-owner-class-per-primitive: only the class implementing the owning interface is exempt, resolved
        // structurally from the enclosing type's implemented interfaces. Compiled into AgentGuard.CrossPlatform (the
        // file-op owner assembly) to prove class membership, not assembly membership, grants the exemption — the
        // owner's File.Exists is clean, but the sibling's Directory.CreateDirectory in the SAME assembly is still RED.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(OwnerAndSiblingSource, "AgentGuard.CrossPlatform"));
        Assert.Equal("AG0011", diagnostic.Id);
        Assert.Contains(
            "CreateDirectory", AnalyzerRunner.SpanText(OwnerAndSiblingSource, diagnostic), System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task LinkTargetRead_IsNotReported_ItBelongsToAG0101()
    {
        // The LinkTarget member is OS-divergent — AG0101 owns it, not AG0011. Uses the one shared LinkTargetSource
        // fixture the AG0101 test also reads (byte-identical, hoisted to one owner); it reads LinkTarget off a passed-in
        // FileInfo, so this proves AG0011 ignores the member alone. The *Info construction is proved separately below.
        Assert.Empty(await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(
            SharedAnalyzerSources.LinkTargetSource, "AgentGuard.Engine"));
    }

    [Fact]
    public async Task InfoConstruction_IsNotReported_ItBelongsToAG0101()
    {
        // info-construction-behind-getfileinfo: a raw new FileInfo / new DirectoryInfo is a banned primitive AG0101
        // owns, not AG0011 — exactly like the symlink and Unix-mode members are carved out here. AG0011 stays silent on
        // the bare construction; the OS-uniform member read is what AG0011 catches.
        const string source = """
            using System.IO;

            public class Sample
            {
                public FileInfo File(string path) => new FileInfo(path);
                public DirectoryInfo Dir(string path) => new DirectoryInfo(path);
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(source, "AgentGuard.Engine"));
    }

    [Fact]
    public async Task DirectoryInfoEnumerateMember_OutsideBoundaries_IsReported()
    {
        // The construction is carved out to AG0101, but the GetFileSystemInfos member is an OS-uniform read — reported
        // outside Boundaries so the directory-enumerator adapter is forced to move there.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(DirectoryInfoEnumerateSource, "AgentGuard.Engine"));
        Assert.Equal("AG0011", diagnostic.Id);
    }

    [Fact]
    public async Task DirectoryEnumeratorOwner_WalkingDirectoryInfo_IsNotReported()
    {
        // The DirectoryInfo walk is green in the class that implements IDirectoryEnumerator — the owner — when it is
        // compiled into its owner assembly AgentGuard.CrossPlatform. The GetFileSystemInfos member is exempt in the
        // owner; the bare construction is carved out to AG0101, so AG0011 never reports it here regardless of assembly.
        const string source = """
            using System.IO;

            namespace AgentGuard.Abstractions.Contracts
            {
                public interface IDirectoryEnumerator { }
            }

            public sealed class SystemDirectoryEnumerator : AgentGuard.Abstractions.Contracts.IDirectoryEnumerator
            {
                public FileSystemInfo[] Walk(string path) => new DirectoryInfo(path).GetFileSystemInfos();
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(source, "AgentGuard.CrossPlatform"));
    }

    [Fact]
    public async Task FileGetLastWriteTimeUtc_InFileReaderOwner_InOwnerAssembly_IsExempt()
    {
        // FIX A: File.GetLastWriteTimeUtc is IFileReader's member (contract completed the set), so the FileReader owner
        // compiled into AgentGuard.CrossPlatform is exempt — the one place it may be called raw.
        Assert.Empty(
            await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(
                FileReaderOwnerReadingFileLastWriteTimeSource, "AgentGuard.CrossPlatform"));
    }

    [Fact]
    public async Task FileGetLastWriteTimeUtc_InFileReaderOwner_InWrongAssembly_IsReported()
    {
        // The same FileReader owner in the wrong assembly (AgentGuard.Engine) is RED — the assembly half of the
        // conjunction is not satisfied, so declaring ': IFileReader' cannot launder the raw timestamp read.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(
                FileReaderOwnerReadingFileLastWriteTimeSource, "AgentGuard.Engine"));
        Assert.Equal("AG0011", diagnostic.Id);
        Assert.Contains(
            "File.GetLastWriteTimeUtc",
            AnalyzerRunner.SpanText(FileReaderOwnerReadingFileLastWriteTimeSource, diagnostic),
            System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task FileGetLastWriteTimeUtc_InPlainClass_IsReported()
    {
        // The live test-file sites' shape: a plain non-owner class reading File.GetLastWriteTimeUtc is RED, routed to
        // IFileReader — the forcing function for the setup tests to read the stamp through IFileReader.GetLastWriteTimeUtc.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(
                FileLastWriteTimeInPlainClassSource, "AgentGuard.Engine"));
        Assert.Equal("AG0011", diagnostic.Id);
        Assert.Contains(
            "File.GetLastWriteTimeUtc",
            AnalyzerRunner.SpanText(FileLastWriteTimeInPlainClassSource, diagnostic),
            System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task FileGetLastWriteTimeUtc_InDirectoryEnumeratorOwner_IsReported_CrossOwner()
    {
        // Cross-owner: File.GetLastWriteTimeUtc routes to IFileReader, so an IDirectoryEnumerator owner is RED for it
        // even in the owner assembly — the File side is not lumped with the directory timestamp getter.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(
                DirectoryEnumeratorOwnerReadingFileLastWriteTimeSource, "AgentGuard.CrossPlatform"));
        Assert.Equal("AG0011", diagnostic.Id);
        Assert.Contains(
            "File.GetLastWriteTimeUtc",
            AnalyzerRunner.SpanText(DirectoryEnumeratorOwnerReadingFileLastWriteTimeSource, diagnostic),
            System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task DirectoryGetLastWriteTimeUtc_InDirectoryEnumeratorOwner_IsExempt()
    {
        // The Directory side of the shared getter routes to IDirectoryEnumerator, so its owner is exempt in the owner
        // assembly.
        Assert.Empty(
            await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(
                DirectoryEnumeratorOwnerReadingDirectoryLastWriteTimeSource, "AgentGuard.CrossPlatform"));
    }

    [Fact]
    public async Task DirectoryGetLastWriteTimeUtc_InFileReaderOwner_IsReported_CrossOwner()
    {
        // Cross-owner mirror: Directory.GetLastWriteTimeUtc routes to IDirectoryEnumerator, so an IFileReader owner is
        // RED for it even in the owner assembly.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(
                FileReaderOwnerReadingDirectoryLastWriteTimeSource, "AgentGuard.CrossPlatform"));
        Assert.Equal("AG0011", diagnostic.Id);
        Assert.Contains(
            "Directory.GetLastWriteTimeUtc",
            AnalyzerRunner.SpanText(FileReaderOwnerReadingDirectoryLastWriteTimeSource, diagnostic),
            System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task FileSetLastWriteTimeUtc_InFileWriterOwner_InOwnerAssembly_IsStillReported()
    {
        // FIX A: File.SetLastWriteTimeUtc has no owner (IFileWriter has no timestamp setter), so it is RED everywhere —
        // even inside the FileWriter owner class in its owner assembly AgentGuard.CrossPlatform, where both halves of the
        // usual conjunction pass. The empty owner set means the class is never exempt for it.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(
                FileWriterOwnerSettingFileLastWriteTimeSource, "AgentGuard.CrossPlatform"));
        Assert.Equal("AG0011", diagnostic.Id);
        Assert.Contains(
            "File.SetLastWriteTimeUtc",
            AnalyzerRunner.SpanText(FileWriterOwnerSettingFileLastWriteTimeSource, diagnostic),
            System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task DirectorySetLastWriteTimeUtc_InDirectoryWriterOwner_IsExempt()
    {
        // The Directory side of the same setter IS IDirectoryWriter's member, so its owner is exempt in the owner
        // assembly — proving the setter's two sides split (File no-owner, Directory owned).
        Assert.Empty(
            await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(
                DirectoryWriterOwnerSettingDirectoryLastWriteTimeSource, "AgentGuard.CrossPlatform"));
    }

    [Fact]
    public async Task PurePathMember_IsNotReported()
    {
        const string source = """
            using System.IO;

            public class Sample
            {
                public string Join(string a, string b) => Path.Combine(a, b);
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<FilesystemOnlyInBoundariesAnalyzer>(source, "AgentGuard.Engine"));
    }
}
