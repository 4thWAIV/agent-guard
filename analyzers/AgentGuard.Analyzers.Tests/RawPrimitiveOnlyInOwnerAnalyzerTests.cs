// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using AgentGuard.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// The consolidated owner rule (AG0011) — every primitive's owner is proven here after the bridge folded the former
/// AG0012/0013/0014/0016/0021/0028 and the *Info half of AG0101 into one table-driven rule. Every case that fired
/// under those ids now fires under AG0011.
/// </summary>
public class RawPrimitiveOnlyInOwnerAnalyzerTests
{
    private const string FileExistsSource = """
        using System.IO;

        public class Sample
        {
            public bool Check(string path) => File.Exists(path);
        }
        """;

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

    private const string InfoConstructionSource = """
        using System.IO;

        public class Sample
        {
            public FileInfo File(string path) => new FileInfo(path);
            public DirectoryInfo Dir(string path) => new DirectoryInfo(path);
        }
        """;

    // The wrapper owner: a class implementing IFileInfo/IDirectoryInfo in AgentGuard.CrossPlatform is exempt for raw
    // *Info construction and every *Info member — this is the AbstractedFileInfo/AbstractedDirectoryInfo shape.
    private const string InfoWrapperOwnerSource = """
        using System.IO;

        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IFileSystemInfo { }
            public interface IFileInfo : IFileSystemInfo { }
            public interface IDirectoryInfo : IFileSystemInfo { }
        }

        namespace CrossPlatform
        {
            public sealed class AbstractedFileInfo : AgentGuard.Abstractions.Contracts.IFileInfo
            {
                private readonly FileInfo info;
                public AbstractedFileInfo(string path) => this.info = new FileInfo(path);
                public string? Link() => this.info.LinkTarget;
            }

            public sealed class AbstractedDirectoryInfo : AgentGuard.Abstractions.Contracts.IDirectoryInfo
            {
                private readonly DirectoryInfo info;
                public AbstractedDirectoryInfo(string path) => this.info = new DirectoryInfo(path);
            }
        }
        """;

    // A DirectoryInfo walk in a class that implements IDirectoryEnumerator (not IDirectoryInfo): now RED — DirectoryInfo
    // is owned by IDirectoryInfo (the wrapper), not by the enumerator, which must go through the factory.
    private const string EnumeratorWalkingRawDirectoryInfoSource = """
        using System.IO;

        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IDirectoryEnumerator { }
        }

        namespace CrossPlatform
        {
            public sealed class SystemDirectoryEnumerator : AgentGuard.Abstractions.Contracts.IDirectoryEnumerator
            {
                public FileSystemInfo[] Walk(string path) => new DirectoryInfo(path).GetFileSystemInfos();
            }
        }
        """;

    private const string EnvironmentAdapterOwnerSource = """
        using System;

        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IEnvironment { }
        }

        namespace Boundaries
        {
            public sealed class EnvironmentAdapter : AgentGuard.Abstractions.Contracts.IEnvironment
            {
                public string Home() => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
        }
        """;

    // The randomness owner after the rename (iguidfactory-renamed-to-irandomgenerator): a class implementing
    // IRandomGenerator in AgentGuard.CrossPlatform is the one place a raw Guid.NewGuid() is legal.
    private const string RandomGeneratorOwnerSource = """
        using System;

        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IRandomGenerator { }
        }

        namespace CrossPlatform
        {
            public sealed class RandomGeneratorAdapter : AgentGuard.Abstractions.Contracts.IRandomGenerator
            {
                public string Name() => Guid.NewGuid().ToString("N");
            }
        }
        """;

    // The OLD owner interface, IGuidFactory, no longer grants the exemption after the rename: a class implementing
    // IGuidFactory and calling Guid.NewGuid() is RED — the forcing function for renaming GuidFactoryAdapter.
    private const string OldGuidFactoryOwnerSource = """
        using System;

        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IGuidFactory { }
        }

        namespace CrossPlatform
        {
            public sealed class GuidFactoryAdapter : AgentGuard.Abstractions.Contracts.IGuidFactory
            {
                public string Name() => Guid.NewGuid().ToString("N");
            }
        }
        """;

    // The Directory.CreateTempSubdirectory owner: a class implementing IDirectoryWriter in AgentGuard.CrossPlatform.
    private const string CreateTempSubdirectoryOwnerSource = """
        using System.IO;

        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IDirectoryWriter { }
        }

        namespace CrossPlatform
        {
            public sealed class DirectoryWriter : AgentGuard.Abstractions.Contracts.IDirectoryWriter
            {
                public DirectoryInfo Make(string prefix) => Directory.CreateTempSubdirectory(prefix);
            }
        }
        """;

    private const string ConsoleAdapterOwnerSource = """
        using System;

        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IConsole { }
        }

        namespace Boundaries
        {
            public sealed class ConsoleAdapter : AgentGuard.Abstractions.Contracts.IConsole
            {
                public void Say() => Console.WriteLine("hi");
            }
        }
        """;

    private const string SiblingUsingSignerSource = """
        namespace Org.BouncyCastle.Crypto.Signers
        {
            public class Ed25519Signer { }
        }

        namespace App
        {
            public class Sample
            {
                public object Make() => new Org.BouncyCastle.Crypto.Signers.Ed25519Signer();
            }
        }
        """;

    private const string OwnerUsingSignerSource = """
        namespace AgentGuard.Abstractions.Contracts
        {
            public interface ISignatureService { }
        }

        namespace Org.BouncyCastle.Crypto.Signers
        {
            public class Ed25519Signer { }
        }

        namespace App
        {
            public sealed class Ed25519SignatureService : AgentGuard.Abstractions.Contracts.ISignatureService
            {
                public object Make() => new Org.BouncyCastle.Crypto.Signers.Ed25519Signer();
            }
        }
        """;

    private const string GetEntryAssemblySource = """
        using System.Reflection;

        namespace App
        {
            public class Sample
            {
                public Assembly? Entry() => Assembly.GetEntryAssembly();
            }
        }
        """;

    private const string OwnerReadingVersionSource = """
        using System.Reflection;

        namespace AgentGuard.Abstractions.Contracts
        {
            public interface IBuildInfo { }
        }

        namespace App
        {
            public sealed class BuildInfo : AgentGuard.Abstractions.Contracts.IBuildInfo
            {
                public Assembly? Entry() => Assembly.GetEntryAssembly();
            }
        }
        """;

    private const string ProcessStartSource = """
        using System.Diagnostics;

        public class Sample
        {
            public void Launch() => Process.Start("guard");
        }
        """;

    [Fact]
    public async Task FileExists_OutsideOwner_IsReported()
    {
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(FileExistsSource, "AgentGuard.Engine"));
        Assert.Equal("AG0011", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task FileCall_InPerOsPlatformLibrary_IsStillReported()
    {
        // The file-op owners live in the AgentGuard.CrossPlatform contract assembly, not the per-OS .MacOS library.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(FileExistsSource, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0011", diagnostic.Id);
    }

    [Fact]
    public async Task FileStreamConstruction_OutsideOwner_IsReported()
    {
        const string source = """
            using System.IO;

            public class Sample
            {
                public Stream Open(string path) => new FileStream(path, FileMode.Open);
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0011", diagnostic.Id);
    }

    [Fact]
    public async Task FileReaderOwner_InWrongAssembly_IsReported_SelfGrantBlocked()
    {
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(
                FileReaderOwnerCallingFileExistsSource, "AgentGuard.Engine"));
        Assert.Equal("AG0011", diagnostic.Id);
        Assert.Contains(
            "File.Exists", AnalyzerRunner.SpanText(FileReaderOwnerCallingFileExistsSource, diagnostic), StringComparison.Ordinal);
    }

    [Fact]
    public async Task FileReaderOwner_InOwnerAssembly_IsExempt()
    {
        Assert.Empty(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(
                FileReaderOwnerCallingFileExistsSource, "AgentGuard.CrossPlatform"));
    }

    [Fact]
    public async Task FileWriterOwner_OnFileReaderMember_FileExists_IsStillReported()
    {
        // Cross-owner: implementing IFileWriter does not exempt File.Exists, which IFileReader owns — RED in the owner
        // assembly too, so the RED is purely the cross-owner mismatch.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(
                FileWriterOwnerCallingFileExistsSource, "AgentGuard.CrossPlatform"));
        Assert.Equal("AG0011", diagnostic.Id);
    }

    [Fact]
    public async Task DirectoryMove_InDirectoryWriterOwner_InOwnerAssembly_IsStillReported()
    {
        // Directory.Move resolves to the empty owner set (IDirectoryWriter has no move member), so it is banned
        // everywhere — RED even in the DirectoryWriter owner in its owner assembly.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(
                DirectoryWriterOwnerCallingDirectoryMoveSource, "AgentGuard.CrossPlatform"));
        Assert.Equal("AG0011", diagnostic.Id);
        Assert.Contains(
            "Directory.Move", AnalyzerRunner.SpanText(DirectoryWriterOwnerCallingDirectoryMoveSource, diagnostic), StringComparison.Ordinal);
    }

    [Fact]
    public async Task EachSingleOwnerClass_IsExempt_ForItsOwnMember()
    {
        Assert.Empty(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(
                EachOwnerExemptForItsMemberSource, "AgentGuard.CrossPlatform"));
    }

    [Fact]
    public async Task FileGetLastWriteTimeUtc_InFileReaderOwner_InOwnerAssembly_IsExempt()
    {
        // Shared-name disambiguation: File.GetLastWriteTimeUtc is IFileReader's member, exempt in its owner assembly.
        Assert.Empty(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(
                FileReaderOwnerReadingFileLastWriteTimeSource, "AgentGuard.CrossPlatform"));
    }

    [Fact]
    public async Task FileGetLastWriteTimeUtc_InDirectoryEnumeratorOwner_IsReported_CrossOwner()
    {
        // Cross-owner: File.GetLastWriteTimeUtc routes to IFileReader, so an IDirectoryEnumerator owner is RED even in
        // the owner assembly — the File side is not lumped with the directory timestamp getter.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(
                DirectoryEnumeratorOwnerReadingFileLastWriteTimeSource, "AgentGuard.CrossPlatform"));
        Assert.Equal("AG0011", diagnostic.Id);
        Assert.Contains(
            "File.GetLastWriteTimeUtc",
            AnalyzerRunner.SpanText(DirectoryEnumeratorOwnerReadingFileLastWriteTimeSource, diagnostic),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task FileSetLastWriteTimeUtc_InFileWriterOwner_InOwnerAssembly_IsStillReported()
    {
        // File.SetLastWriteTimeUtc has no owner (IFileWriter has no timestamp setter), so it is RED everywhere —
        // even inside the FileWriter owner in its owner assembly.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(
                FileWriterOwnerSettingFileLastWriteTimeSource, "AgentGuard.CrossPlatform"));
        Assert.Equal("AG0011", diagnostic.Id);
        Assert.Contains(
            "File.SetLastWriteTimeUtc",
            AnalyzerRunner.SpanText(FileWriterOwnerSettingFileLastWriteTimeSource, diagnostic),
            StringComparison.Ordinal);
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

        Assert.Empty(await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(source, "AgentGuard.Engine"));
    }

    [Fact]
    public async Task InfoConstruction_OutsideOwner_IsReported()
    {
        // fileinfo-abstraction-stays-in-ag0011: a raw new FileInfo / new DirectoryInfo is now owned wholesale by
        // IFileInfo/IDirectoryInfo — RED outside the wrapper owner (was AG0101's).
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(InfoConstructionSource, "AgentGuard.Engine");
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => Assert.Equal("AG0011", diagnostic.Id));
        Assert.Contains(
            diagnostics, diagnostic => AnalyzerRunner.SpanText(InfoConstructionSource, diagnostic).Contains("FileInfo", StringComparison.Ordinal));
        Assert.Contains(
            diagnostics, diagnostic => AnalyzerRunner.SpanText(InfoConstructionSource, diagnostic).Contains("DirectoryInfo", StringComparison.Ordinal));
    }

    [Fact]
    public async Task LinkTargetRead_OutsideOwner_IsReported()
    {
        // The *Info instance member LinkTarget is now AG0011's (it belongs to the wrapper), not AG0101's.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(
                SharedAnalyzerSources.LinkTargetSource, "AgentGuard.Engine"));
        Assert.Equal("AG0011", diagnostic.Id);
        Assert.Contains(
            "LinkTarget", AnalyzerRunner.SpanText(SharedAnalyzerSources.LinkTargetSource, diagnostic), StringComparison.Ordinal);
    }

    [Fact]
    public async Task InfoConstructionAndMembers_InWrapperOwner_InOwnerAssembly_IsExempt()
    {
        // The AbstractedFileInfo/AbstractedDirectoryInfo wrapper — implementing IFileInfo/IDirectoryInfo in
        // AgentGuard.CrossPlatform — is exempt for raw *Info construction and every *Info member, fully.
        Assert.Empty(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(InfoWrapperOwnerSource, "AgentGuard.CrossPlatform"));
    }

    [Fact]
    public async Task InfoWrapperOwner_InWrongAssembly_IsReported_SelfGrantBlocked()
    {
        // Implementing IFileInfo is not enough — the wrapper must also compile into AgentGuard.CrossPlatform.
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(InfoWrapperOwnerSource, "AgentGuard.Engine");
        Assert.NotEmpty(diagnostics);
        Assert.All(diagnostics, diagnostic => Assert.Equal("AG0011", diagnostic.Id));
    }

    [Fact]
    public async Task EnumeratorWalkingRawDirectoryInfo_IsReported_CrossOwner()
    {
        // DirectoryInfo is owned by IDirectoryInfo (the wrapper), NOT by IDirectoryEnumerator, so an enumerator owner
        // walking a RAW DirectoryInfo is RED even in the owner assembly — it must go through the factory.
        ImmutableArray<Diagnostic> diagnostics =
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(
                EnumeratorWalkingRawDirectoryInfoSource, "AgentGuard.CrossPlatform");
        Assert.NotEmpty(diagnostics);
        Assert.All(diagnostics, diagnostic => Assert.Equal("AG0011", diagnostic.Id));
    }

    [Fact]
    public async Task EnvironmentMember_OutsideOwner_IsReported()
    {
        const string source = """
            using System;

            public class Sample
            {
                public string Home() => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0011", diagnostic.Id);
    }

    [Fact]
    public async Task GetCurrentDirectory_OutsideOwner_IsReported()
    {
        // Directory.GetCurrentDirectory is an environment read on the filesystem type → IEnvironment (was AG0012).
        const string source = """
            using System.IO;

            public class Sample
            {
                public string Cwd() => Directory.GetCurrentDirectory();
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0011", diagnostic.Id);
    }

    [Fact]
    public async Task AssemblyLocation_OutsideOwner_IsReported()
    {
        const string source = """
            using System.Reflection;

            public class Sample
            {
                public string Where(Assembly assembly) => assembly.Location;
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0011", diagnostic.Id);
    }

    [Fact]
    public async Task EnvironmentTickCount_IsNotReported_ItBelongsToAG0015()
    {
        const string source = """
            using System;

            public class Sample
            {
                public int Ticks() => Environment.TickCount;
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(source, "AgentGuard.Engine"));
    }

    [Fact]
    public async Task PathGetFullPath_IsNotClaimed_ItBelongsToAG0020()
    {
        // All of System.IO.Path is owned by the default-deny Path-purity rule AG0020; the owner rule never claims it.
        const string singleArg = """
            using System.IO;

            public class Sample
            {
                public string Full(string path) => Path.GetFullPath(path);
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(singleArg, "AgentGuard.Engine"));
    }

    [Fact]
    public async Task EnvironmentAdapterOwner_InOwnerAssembly_IsExempt()
    {
        Assert.Empty(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(EnvironmentAdapterOwnerSource, "AgentGuard.Boundaries"));
    }

    [Fact]
    public async Task EnvironmentAdapterOwner_InWrongAssembly_IsReported_SelfGrantBlocked()
    {
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(EnvironmentAdapterOwnerSource, "AgentGuard.Engine"));
        Assert.Equal("AG0011", diagnostic.Id);
    }

    [Fact]
    public async Task GuidNewGuid_OutsideOwner_IsReported()
    {
        const string source = """
            using System;

            public class Sample
            {
                public string Name() => Guid.NewGuid().ToString("N");
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0011", diagnostic.Id);
    }

    [Fact]
    public async Task NewRandom_OutsideOwner_IsReported()
    {
        const string source = """
            using System;

            public class Sample
            {
                public Random Make() => new Random();
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0011", diagnostic.Id);
    }

    [Fact]
    public async Task GuidParse_IsNotReported()
    {
        const string source = """
            using System;

            public class Sample
            {
                public Guid Parse(string text) => Guid.Parse(text);
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(source, "AgentGuard.Engine"));
    }

    [Fact]
    public async Task RandomGeneratorOwner_InOwnerAssembly_IsExempt()
    {
        Assert.Empty(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(RandomGeneratorOwnerSource, "AgentGuard.CrossPlatform"));
    }

    [Fact]
    public async Task RandomGeneratorOwner_InWrongAssembly_IsReported_SelfGrantBlocked()
    {
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(RandomGeneratorOwnerSource, "AgentGuard.Boundaries"));
        Assert.Equal("AG0011", diagnostic.Id);
    }

    [Fact]
    public async Task OldGuidFactoryOwner_IsNoLongerExempt_AfterRename()
    {
        // After the owner rename (IGuidFactory -> IRandomGenerator), a class implementing the OLD IGuidFactory calling
        // Guid.NewGuid() is no longer exempt and goes RED — the forcing function for renaming GuidFactoryAdapter to
        // RandomGeneratorAdapter in the IMPLEMENT pass.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(OldGuidFactoryOwnerSource, "AgentGuard.CrossPlatform"));
        Assert.Equal("AG0011", diagnostic.Id);
    }

    [Fact]
    public async Task CreateTempSubdirectory_InDirectoryWriterOwner_IsExempt()
    {
        // Directory.CreateTempSubdirectory is reused behind IDirectoryWriter: legal inside the IDirectoryWriter
        // implementer in AgentGuard.CrossPlatform.
        Assert.Empty(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(CreateTempSubdirectoryOwnerSource, "AgentGuard.CrossPlatform"));
    }

    [Fact]
    public async Task CreateTempSubdirectory_OutsideOwner_IsReported()
    {
        // Outside the IDirectoryWriter owner it is RED — the existing raw calls in the test harness stay RED until
        // IMPLEMENT routes them through IDirectoryWriter.CreateTempSubdirectory.
        const string source = """
            using System.IO;

            public class Sample
            {
                public DirectoryInfo Make(string prefix) => Directory.CreateTempSubdirectory(prefix);
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(source, "AgentGuard.CrossPlatform"));
        Assert.Equal("AG0011", diagnostic.Id);
        Assert.Contains("CreateTempSubdirectory", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConsoleWriteLine_OutsideOwner_IsReported()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Say() => Console.WriteLine("hi");
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(source, "AgentGuard.Cli"));
        Assert.Equal("AG0011", diagnostic.Id);
    }

    [Fact]
    public async Task ConsoleAdapterOwner_InOwnerAssembly_IsExempt()
    {
        Assert.Empty(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(ConsoleAdapterOwnerSource, "AgentGuard.Boundaries"));
    }

    [Fact]
    public async Task ConsoleAdapterOwner_InWrongAssembly_IsReported_SelfGrantBlocked()
    {
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(ConsoleAdapterOwnerSource, "AgentGuard.Cli"));
        Assert.Equal("AG0011", diagnostic.Id);
    }

    [Fact]
    public async Task RawSigner_OutsideOwner_IsReported()
    {
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(SiblingUsingSignerSource, "AgentGuard.Engine"));
        Assert.Equal("AG0011", diagnostic.Id);
        Assert.Contains("Ed25519Signer", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RawSigner_InOwnerClassAndAssembly_IsExempt()
    {
        Assert.Empty(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(OwnerUsingSignerSource, "AgentGuard.Boundaries"));
    }

    [Fact]
    public async Task RawSigner_OwnerClass_InWrongAssembly_IsReported()
    {
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(OwnerUsingSignerSource, "AgentGuard.Engine"));
        Assert.Equal("AG0011", diagnostic.Id);
    }

    [Fact]
    public async Task GetEntryAssembly_OutsideOwner_IsReported()
    {
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(GetEntryAssemblySource, "AgentGuard.Engine"));
        Assert.Equal("AG0011", diagnostic.Id);
        Assert.Contains("GetEntryAssembly", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AssemblyNameVersion_OutsideOwner_IsReported()
    {
        const string source = """
            using System.Reflection;

            namespace App
            {
                public class Sample
                {
                    public object? Version(AssemblyName name) => name.Version;
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0011", diagnostic.Id);
    }

    [Fact]
    public async Task VersionRead_InOwnerClassAndAssembly_IsExempt()
    {
        Assert.Empty(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(OwnerReadingVersionSource, "AgentGuard.Boundaries"));
    }

    [Fact]
    public async Task NonVersionAssemblyMember_IsNotReported()
    {
        // Assembly.FullName is neither a version read nor Assembly.Location, so it is not claimed.
        const string source = """
            using System.Reflection;

            namespace App
            {
                public class Sample
                {
                    public string Where(Assembly assembly) => assembly.FullName!;
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(source, "AgentGuard.Engine"));
    }

    [Fact]
    public async Task ProcessStart_IsReportedEverywhere_BannedWithNoOwner()
    {
        // Process launching has no owner — banned everywhere, including AgentGuard.Boundaries.
        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(ProcessStartSource, "AgentGuard.Boundaries"));
        Assert.Equal("AG0011", diagnostic.Id);
    }

    [Fact]
    public async Task ProcessStartInfoConstruction_IsReported()
    {
        const string source = """
            using System.Diagnostics;

            public class Sample
            {
                public ProcessStartInfo Info() => new ProcessStartInfo("guard");
            }
            """;

        Diagnostic diagnostic = Assert.Single(
            await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(source, "AgentGuard.Engine"));
        Assert.Equal("AG0011", diagnostic.Id);
    }

    [Fact]
    public async Task NonPrimitiveType_IsNotReported()
    {
        const string source = """
            public class Sample
            {
                public int Add(int a, int b) => a + b;
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<RawPrimitiveOnlyInOwnerAnalyzer>(source, "AgentGuard.Engine"));
    }
}
