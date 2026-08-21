// Copyright (c) 4thWAIV. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Versioning;
using AgentGuard.Abstractions.Contracts;
using AgentGuard.TestHelpers;
using FluentAssertions;
using Xunit;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// Spec for <see cref="IPlatformFileSystem"/>. Every test asserts the spec through the interface and never branches
/// on OS; scaffolding (temp dirs, target dirs, marker files) is routed through the owned filesystem interfaces off a
/// real container (<c>SystemServicesBuilder.Real().Build()</c>), never a raw <c>System.IO</c> call, and the impl
/// under test is the real per-OS platform on that container (<c>Real().Build().Platform.FileSystem</c>) — the per-OS
/// assembly selected by the csproj. The same tests therefore prove identical behavior on macOS, Linux, and Windows,
/// each in its own CI leg.
/// </summary>
public sealed class PlatformFileSystemSpecTests : IDisposable
{
    private readonly ISystemServices services;
    private readonly string root;
    private readonly IPlatformFileSystem fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlatformFileSystemSpecTests"/> class.
    /// </summary>
    public PlatformFileSystemSpecTests()
    {
        services = SystemServicesBuilder.Real().Build();
        root = services.FileSystem.GetDirectoryWriter().CreateTempSubdirectory("agentguard-platform-spec-");
        fileSystem = services.Platform.FileSystem;
    }

    [Fact]
    public void MakeLinkTarget_WhenLinkAbsent_CreatesTheLinkAtTheTarget()
    {
        MakeTargetDirectory("target-a");
        string link = Path.Combine(root, "current");

        fileSystem.MakeLinkTarget(link, "target-a");

        fileSystem.ReadLinkTarget(link).Should().Be("target-a");
    }

    [Fact]
    public void MakeLinkTarget_CalledTwiceWithSameTarget_LeavesTheDirectoryWithExactlyTheLinkAndTarget()
    {
        MakeTargetDirectory("target-a");
        string link = Path.Combine(root, "current");

        fileSystem.MakeLinkTarget(link, "target-a");
        fileSystem.MakeLinkTarget(link, "target-a");

        fileSystem.ReadLinkTarget(link).Should().Be("target-a");

        // Tightened no-stray-temp assertion: the directory holds exactly the target and the link, nothing else — a
        // leftover temporary sibling would show up here.
        Entries().Should().BeEquivalentTo("target-a", "current");
    }

    [Fact]
    public void MakeLinkTarget_OverAnExistingLink_AtomicallyPointsAtTheNewTarget()
    {
        MakeTargetDirectory("target-a");
        MakeTargetDirectory("target-b");
        string link = Path.Combine(root, "current");

        fileSystem.MakeLinkTarget(link, "target-a");
        fileSystem.MakeLinkTarget(link, "target-b");

        fileSystem.ReadLinkTarget(link).Should().Be("target-b");
        Entries().Should().BeEquivalentTo("target-a", "target-b", "current");
    }

    [Fact]
    public void MakeLinkTarget_MultiSegmentTarget_RoundTripsByteIdenticalWithForwardSlashes()
    {
        string link = Path.Combine(root, "current");

        // scan-resolutions #3: the raw target reads back byte-identical on all three OS, forward slashes preserved,
        // even for a multi-segment relative target whose destination does not yet exist.
        fileSystem.MakeLinkTarget(link, "../versions/1.2.3");

        fileSystem.ReadLinkTarget(link).Should().Be("../versions/1.2.3");
    }

    [Fact]
    public void MakeLinkTarget_WhenParentDirectoryMissing_CreatesTheParentDirectory()
    {
        string link = Path.Combine(root, "nested", "deep", "current");

        // behavioral-uniformity-proven-by-spec: MakeLinkTarget creates any missing parent directory, identically on
        // all three OS (a managed Directory.CreateDirectory).
        fileSystem.MakeLinkTarget(link, "target");

        DirectoryExists(Path.Combine(root, "nested", "deep")).Should().BeTrue();
        fileSystem.ReadLinkTarget(link).Should().Be("target");
    }

    [Fact]
    public void MakeLinkTarget_ToADirectoryTarget_ResolvesAndTraversesAsADirectory()
    {
        // refute-round2 option B: a link to a directory target resolves and traverses as a directory, identically on
        // all three OS (Windows infers the kind from the target inside its own impl; POSIX has no kind distinction).
        // Precondition: the target exists when the link is created — it does here.
        string targetDirectory = MakeTargetDirectory("target-a");
        string marker = Path.Combine(targetDirectory, "inside.txt");
        WriteText(marker, "reachable through the link");
        string link = Path.Combine(root, "current");

        fileSystem.MakeLinkTarget(link, "target-a");

        DirectoryExists(link).Should().BeTrue();
        string throughLink = Path.Combine(link, "inside.txt");
        FileExists(throughLink).Should().BeTrue();
        ReadText(throughLink).Should().Be("reachable through the link");
    }

    [Fact]
    public void ReadLinkTarget_OnANonLink_ReturnsNull()
    {
        string regularFile = Path.Combine(root, "not-a-link.txt");
        WriteText(regularFile, "plain file");

        fileSystem.ReadLinkTarget(regularFile).Should().BeNull();
    }

    [Fact]
    public void IsLinkTarget_IsTrueForALinkAndFalseForANonLinkOrAbsentPath()
    {
        MakeTargetDirectory("target-a");
        string link = Path.Combine(root, "current");
        fileSystem.MakeLinkTarget(link, "target-a");
        string regularFile = Path.Combine(root, "plain.txt");
        WriteText(regularFile, "plain file");

        fileSystem.IsLinkTarget(link).Should().BeTrue();
        fileSystem.IsLinkTarget(regularFile).Should().BeFalse();
        fileSystem.IsLinkTarget(Path.Combine(root, "absent")).Should().BeFalse();
    }

    [Fact]
    public void RemoveLinkTarget_RemovesTheLinkButLeavesTheTargetAndItsContents()
    {
        string targetDirectory = MakeTargetDirectory("target-a");
        string marker = Path.Combine(targetDirectory, "keep.txt");
        WriteText(marker, "must survive");
        string link = Path.Combine(root, "current");
        fileSystem.MakeLinkTarget(link, "target-a");

        fileSystem.RemoveLinkTarget(link);

        fileSystem.ReadLinkTarget(link).Should().BeNull();
        fileSystem.IsLinkTarget(link).Should().BeFalse();
        DirectoryExists(targetDirectory).Should().BeTrue();
        FileExists(marker).Should().BeTrue();
    }

    [Fact]
    public void MakeLinkTarget_LeavesTheDirectoryWithExactlyTheExpectedEntries()
    {
        MakeTargetDirectory("target-a");
        string link = Path.Combine(root, "current");

        fileSystem.MakeLinkTarget(link, "target-a");

        Entries().Should().BeEquivalentTo("target-a", "current");
    }

    [Fact]
    public void MakeLinkTarget_OnARealFileOrDirectory_RefusesAndLeavesItUntouched()
    {
        // scan-resolutions #4: MakeLinkTarget refuses a path that holds a real entry — proven for BOTH a real file
        // and a real directory — so a real file or directory is never replaced by a link.
        MakeTargetDirectory("target-a");

        string realFile = Path.Combine(root, "real.txt");
        WriteText(realFile, "keep me");

        Action pointOntoFile = () => fileSystem.MakeLinkTarget(realFile, "target-a");

        pointOntoFile.Should().Throw<IOException>();
        ReadText(realFile).Should().Be("keep me");
        fileSystem.IsLinkTarget(realFile).Should().BeFalse();

        string realDirectory = Path.Combine(root, "realdir");
        MakeDirectory(realDirectory);
        string directoryMarker = Path.Combine(realDirectory, "keep.txt");
        WriteText(directoryMarker, "must survive");

        Action pointOntoDirectory = () => fileSystem.MakeLinkTarget(realDirectory, "target-a");

        pointOntoDirectory.Should().Throw<IOException>();
        DirectoryExists(realDirectory).Should().BeTrue();
        FileExists(directoryMarker).Should().BeTrue();
        fileSystem.IsLinkTarget(realDirectory).Should().BeFalse();
    }

    [Fact]
    public void RemoveLinkTarget_OnARealFileOrDirectory_RefusesAndLeavesItUntouched()
    {
        // scan-resolutions #4: RemoveLinkTarget refuses a path that holds a real entry — proven for BOTH a real file
        // and a real directory — so a real file or directory is never deleted.
        string realFile = Path.Combine(root, "real.txt");
        WriteText(realFile, "keep me");

        Action removeFile = () => fileSystem.RemoveLinkTarget(realFile);

        removeFile.Should().Throw<IOException>();
        FileExists(realFile).Should().BeTrue();
        ReadText(realFile).Should().Be("keep me");

        string realDirectory = Path.Combine(root, "realdir");
        MakeDirectory(realDirectory);
        string directoryMarker = Path.Combine(realDirectory, "keep.txt");
        WriteText(directoryMarker, "must survive");

        Action removeDirectory = () => fileSystem.RemoveLinkTarget(realDirectory);

        removeDirectory.Should().Throw<IOException>();
        DirectoryExists(realDirectory).Should().BeTrue();
        FileExists(directoryMarker).Should().BeTrue();
    }

    [Fact]
    [SuppressMessage(
        "AgentGuard.Architecture",
        "AG0101:OsDivergentFilesystemOnlyInCrossPlatform",
        Justification = "This lone pointed-integration test proves the real PosixFileSystem.MakeExecutable adds only the execute bits and preserves the file's other permission bits — a 0755 replace would leak group/other read onto a private file. That is a real-disk security property the in-memory simulator cannot verify (its exec bit is a single boolean flag), and IPlatformFileSystem exposes no owned raw Unix-mode get/set by design (the engine never needs one). Tim approved this ordinary AG0101 suppression on 2026-08-21 as the interim mechanism: raw-primitive test exceptions are suppressed the existing way and monitored manually until the signed-exception system lands, which will flag every unsigned exception so the backlog can be signed.")]
    public void ExecutableFlag_IsUniformlyProvenByNeedsExecutableFlag()
    {
        string file = Path.Combine(root, "guard");
        WriteText(file, "binary");

        if (ExecutableBitsSupported())
        {
            // POSIX (scan-resolutions #5): set a distinctive non-default initial mode — owner read+write only, no
            // group, no other, no execute — then prove MakeExecutable adds ONLY the execute bits and leaves every
            // other bit exactly as it was. A fixed-mode 0755 replace (rwxr-xr-x) would introduce group/other read
            // bits and so fail this exact-equality assertion. MakeNonExecutable is the symmetric inverse: it removes
            // only the execute bits and restores the original mode.
            const UnixFileMode initialMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            const UnixFileMode executeBits =
                UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;

            // The exact-mode set and read below have no owned-interface path by design: IPlatformFileSystem exposes
            // MakeExecutable/IsExecutable but no raw Unix-mode get/set (the engine never needs one), so this exact-bits
            // proof runs raw on real disk under the method-level AG0101 suppression Tim approved (see the attribute) —
            // an interim ordinary suppression, monitored manually until the signed-exception system lands.
            File.SetUnixFileMode(file, initialMode);

            fileSystem.MakeExecutable(file);

            fileSystem.IsExecutable(file).Should().BeTrue();
            File.GetUnixFileMode(file).Should().Be(initialMode | executeBits);
            ReadText(file).Should().Be("binary");

            fileSystem.MakeNonExecutable(file);

            fileSystem.IsExecutable(file).Should().BeFalse();
            File.GetUnixFileMode(file).Should().Be(initialMode);
        }
        else
        {
            // Windows: there is no executable bit, so the other three throw where NeedsExecutableFlag() is false.
            FluentActions.Invoking(() => fileSystem.IsExecutable(file)).Should().Throw<PlatformNotSupportedException>();
            FluentActions.Invoking(() => fileSystem.MakeExecutable(file)).Should().Throw<PlatformNotSupportedException>();
            FluentActions.Invoking(() => fileSystem.MakeNonExecutable(file)).Should().Throw<PlatformNotSupportedException>();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        try
        {
            services.FileSystem.GetDirectoryWriter().DeleteDirectory(root, recursive: true);
        }
        catch (DirectoryNotFoundException)
        {
            // Nothing to clean up.
        }
    }

    // Bridges the platform capability check to the .NET platform-compatibility analyzer (CA1416). The spec branches
    // on the CAPABILITY NeedsExecutableFlag(), never on the OS, but the analyzer cannot see that a false-on-Windows
    // capability means the Unix-mode APIs below are unreachable on Windows. [UnsupportedOSPlatformGuard("windows")]
    // states that true invariant (NeedsExecutableFlag() is hard-false on Windows), so File.Get/SetUnixFileMode are
    // proven safe here without an OS branch (AG0009) and without suppressing CA1416.
    [UnsupportedOSPlatformGuard("windows")]
    private bool ExecutableBitsSupported() => fileSystem.NeedsExecutableFlag();

    private string MakeTargetDirectory(string name)
    {
        string path = Path.Combine(root, name);
        MakeDirectory(path);
        return path;
    }

    private void MakeDirectory(string path) => services.FileSystem.GetDirectoryWriter().CreateDirectory(path);

    private void WriteText(string path, string contents) =>
        services.FileSystem.GetFileWriter().WriteAllText(path, contents);

    private string ReadText(string path) => services.FileSystem.GetFileReader().ReadAllText(path);

    private bool FileExists(string path) => services.FileSystem.GetFileReader().Exists(path);

    private bool DirectoryExists(string path) => services.FileSystem.GetDirectoryReader().DirectoryExists(path);

    private string[] Entries() =>
        services.FileSystem.GetDirectoryReader().EnumerateChildren(root)
            .Select(child => Path.GetFileName(child.FullPath)).OfType<string>().ToArray();
}
