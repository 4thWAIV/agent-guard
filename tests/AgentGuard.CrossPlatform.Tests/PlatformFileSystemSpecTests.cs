// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using FluentAssertions;
using Xunit;

namespace AgentGuard.CrossPlatform.Tests;

/// <summary>
/// Spec for <see cref="IPlatformFileSystem"/>. Every test asserts the spec through the interface and never branches
/// on OS; scaffolding (temp dirs, target dirs, marker files) is plain <c>System.IO</c>, and the impl under test is
/// resolved via <c>Platform.Create()</c> — the per-OS assembly selected by the csproj. The same tests therefore
/// prove identical behavior on macOS, Linux, and Windows, each in its own CI leg.
/// </summary>
public sealed class PlatformFileSystemSpecTests : IDisposable
{
    private readonly string root;
    private readonly IPlatformFileSystem fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlatformFileSystemSpecTests"/> class.
    /// </summary>
    public PlatformFileSystemSpecTests()
    {
        root = Path.Combine(Path.GetTempPath(), "agentguard-platform-spec-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        fileSystem = Platform.Create().FileSystem;
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

        Directory.Exists(Path.Combine(root, "nested", "deep")).Should().BeTrue();
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
        File.WriteAllText(marker, "reachable through the link");
        string link = Path.Combine(root, "current");

        fileSystem.MakeLinkTarget(link, "target-a");

        Directory.Exists(link).Should().BeTrue();
        string throughLink = Path.Combine(link, "inside.txt");
        File.Exists(throughLink).Should().BeTrue();
        File.ReadAllText(throughLink).Should().Be("reachable through the link");
    }

    [Fact]
    public void ReadLinkTarget_OnANonLink_ReturnsNull()
    {
        string regularFile = Path.Combine(root, "not-a-link.txt");
        File.WriteAllText(regularFile, "plain file");

        fileSystem.ReadLinkTarget(regularFile).Should().BeNull();
    }

    [Fact]
    public void IsLinkTarget_IsTrueForALinkAndFalseForANonLinkOrAbsentPath()
    {
        MakeTargetDirectory("target-a");
        string link = Path.Combine(root, "current");
        fileSystem.MakeLinkTarget(link, "target-a");
        string regularFile = Path.Combine(root, "plain.txt");
        File.WriteAllText(regularFile, "plain file");

        fileSystem.IsLinkTarget(link).Should().BeTrue();
        fileSystem.IsLinkTarget(regularFile).Should().BeFalse();
        fileSystem.IsLinkTarget(Path.Combine(root, "absent")).Should().BeFalse();
    }

    [Fact]
    public void RemoveLinkTarget_RemovesTheLinkButLeavesTheTargetAndItsContents()
    {
        string targetDirectory = MakeTargetDirectory("target-a");
        string marker = Path.Combine(targetDirectory, "keep.txt");
        File.WriteAllText(marker, "must survive");
        string link = Path.Combine(root, "current");
        fileSystem.MakeLinkTarget(link, "target-a");

        fileSystem.RemoveLinkTarget(link);

        fileSystem.ReadLinkTarget(link).Should().BeNull();
        fileSystem.IsLinkTarget(link).Should().BeFalse();
        Directory.Exists(targetDirectory).Should().BeTrue();
        File.Exists(marker).Should().BeTrue();
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
        File.WriteAllText(realFile, "keep me");

        Action pointOntoFile = () => fileSystem.MakeLinkTarget(realFile, "target-a");

        pointOntoFile.Should().Throw<IOException>();
        File.ReadAllText(realFile).Should().Be("keep me");
        fileSystem.IsLinkTarget(realFile).Should().BeFalse();

        string realDirectory = Path.Combine(root, "realdir");
        Directory.CreateDirectory(realDirectory);
        string directoryMarker = Path.Combine(realDirectory, "keep.txt");
        File.WriteAllText(directoryMarker, "must survive");

        Action pointOntoDirectory = () => fileSystem.MakeLinkTarget(realDirectory, "target-a");

        pointOntoDirectory.Should().Throw<IOException>();
        Directory.Exists(realDirectory).Should().BeTrue();
        File.Exists(directoryMarker).Should().BeTrue();
        fileSystem.IsLinkTarget(realDirectory).Should().BeFalse();
    }

    [Fact]
    public void RemoveLinkTarget_OnARealFileOrDirectory_RefusesAndLeavesItUntouched()
    {
        // scan-resolutions #4: RemoveLinkTarget refuses a path that holds a real entry — proven for BOTH a real file
        // and a real directory — so a real file or directory is never deleted.
        string realFile = Path.Combine(root, "real.txt");
        File.WriteAllText(realFile, "keep me");

        Action removeFile = () => fileSystem.RemoveLinkTarget(realFile);

        removeFile.Should().Throw<IOException>();
        File.Exists(realFile).Should().BeTrue();
        File.ReadAllText(realFile).Should().Be("keep me");

        string realDirectory = Path.Combine(root, "realdir");
        Directory.CreateDirectory(realDirectory);
        string directoryMarker = Path.Combine(realDirectory, "keep.txt");
        File.WriteAllText(directoryMarker, "must survive");

        Action removeDirectory = () => fileSystem.RemoveLinkTarget(realDirectory);

        removeDirectory.Should().Throw<IOException>();
        Directory.Exists(realDirectory).Should().BeTrue();
        File.Exists(directoryMarker).Should().BeTrue();
    }

    [Fact]
    public void ExecutableFlag_IsUniformlyProvenByNeedsExecutableFlag()
    {
        string file = Path.Combine(root, "guard");
        File.WriteAllText(file, "binary");

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
            File.SetUnixFileMode(file, initialMode);

            fileSystem.MakeExecutable(file);

            fileSystem.IsExecutable(file).Should().BeTrue();
            File.GetUnixFileMode(file).Should().Be(initialMode | executeBits);
            File.ReadAllText(file).Should().Be("binary");

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
            Directory.Delete(root, recursive: true);
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
        Directory.CreateDirectory(path);
        return path;
    }

    private string[] Entries() =>
        Directory.EnumerateFileSystemEntries(root).Select(Path.GetFileName).OfType<string>().ToArray();
}
