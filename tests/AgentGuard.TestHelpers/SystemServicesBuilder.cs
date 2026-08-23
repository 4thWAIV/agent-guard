// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Runtime.InteropServices;
using AgentGuard.Abstractions.Contracts;
using AgentGuard.Boundaries;
using Microsoft.Extensions.Time.Testing;

namespace AgentGuard.TestHelpers;

/// <summary>
/// The single approved way a test builds the <see cref="ISystemServices"/> container. It mirrors the container's shape at
/// every nesting level (builder-mirrors-container-nesting, enforced by AG0019): each direct leaf service is substituted
/// with <c>With</c> and wrapped with <c>Wrap</c> on this top-level scope, and each nested container is reached through a
/// navigator that follows the container's own shape — <see cref="OnFileSystem"/> for the filesystem leaves and
/// <see cref="OnPlatform"/> for the platform. It is the only class anywhere with <c>With</c>/<c>Wrap</c>, and — besides
/// the one CLI composition method — the only caller of <c>SystemServices.Create()</c>. Its sub-builders are public nested
/// types (a public navigator must return a public type); its fake container implementers are internal nested types, so the
/// three container interfaces each have exactly one implementer here (AG0022). The one shared copy-on-write overlay
/// (<see cref="InMemoryFileSystemStore"/>) is constructed only inside this class (AG0027), at the single site
/// <see cref="NewOverlay"/>.
/// </summary>
public sealed class SystemServicesBuilder
{
    // The real host VALUES — process + OS architecture, home directory, temp root, and filesystem case-sensitivity —
    // read ONCE through the single sanctioned Boundaries entrypoint (SystemServices.Create(), the same door Real() uses)
    // and cached as plain VALUES (two enums, two strings, a bool) — never the container or a service, so there is no
    // per-build reconstruction and no AG0024 static service holder. The Fake() default environment reports these true
    // host values instead of hardcoded roots: because the simulator is copy-on-write over a real read-only base and never
    // writes to real disk, the real home and temp are safe to pass through to the fake (fake-reads-real-home-and-temp),
    // and a real home/temp is fully-qualified on every OS, which is what keeps the fake correct on Windows. Its current
    // directory and environment variables stay controlled (fake).
    private static readonly RealHost Host = ReadRealHost();

    private readonly bool _isFake;
    private readonly ISystemServices? _real;
    private readonly Slot<IEnvironment> _environment = new();
    private readonly Slot<IRandomGenerator> _random = new();
    private readonly Slot<IConsole> _console = new();
    private readonly Slot<ISignatureService> _signatures = new();
    private readonly Slot<IBuildInfo> _buildInfo = new();
    private readonly Slot<TimeProvider> _clock = new();
    private readonly Slot<IFileReader> _fileReader = new();
    private readonly Slot<IDirectoryEnumerator> _directoryReader = new();
    private readonly Slot<IFileWriter> _fileWriter = new();
    private readonly Slot<IDirectoryWriter> _directoryWriter = new();
    private readonly Slot<IPlatformFileSystem> _platform = new();
    private InMemoryFileSystemStore? _store;

    private SystemServicesBuilder(bool isFake, ISystemServices? real, InMemoryFileSystemStore? store)
    {
        _isFake = isFake;
        _real = real;
        _store = store;
    }

    /// <summary>
    /// Starts a builder over the REAL services (<c>SystemServices.Create()</c>): every leaf defaults to the real adapter,
    /// and a test overrides only the pieces it needs. <see cref="SimulateFileSystem"/> layers a copy-on-write overlay over
    /// the real filesystem for a unit test; an integration test leaves it real.
    /// </summary>
    /// <returns>A builder over the real container.</returns>
    public static SystemServicesBuilder Real() => new(isFake: false, real: SystemServices.Create(), store: null);

    /// <summary>
    /// Starts a builder over BUILT-IN fakes: an in-memory filesystem and platform, a recording console, a fake
    /// environment, a deterministic random generator, and a fake clock. It never falls back to a real adapter — a service
    /// with no built-in fake (the signature service and the build-info reader) throws when used, naming the fix, unless
    /// the test supplies one through <c>With</c>.
    /// </summary>
    /// <returns>A builder over the built-in fakes.</returns>
    public static SystemServicesBuilder Fake() =>
        new(
            isFake: true,
            real: null,
            store: NewOverlay(
                baseReader: null,
                baseEnumerator: null,
                basePlatform: null,
                tempRoot: Host.Temp,
                caseSensitive: Host.CaseSensitive));

    /// <summary>Substitutes the environment service.</summary>
    /// <param name="environment">The environment fake to install.</param>
    /// <returns>This builder, for chaining.</returns>
    public SystemServicesBuilder With(IEnvironment environment)
    {
        _environment.Override(environment);
        return this;
    }

    /// <summary>Substitutes the random generator.</summary>
    /// <param name="random">The random-generator fake to install.</param>
    /// <returns>This builder, for chaining.</returns>
    public SystemServicesBuilder With(IRandomGenerator random)
    {
        _random.Override(random);
        return this;
    }

    /// <summary>Substitutes the console service.</summary>
    /// <param name="console">The console fake to install.</param>
    /// <returns>This builder, for chaining.</returns>
    public SystemServicesBuilder With(IConsole console)
    {
        _console.Override(console);
        return this;
    }

    /// <summary>Substitutes the signature service.</summary>
    /// <param name="signatures">The signature service to install.</param>
    /// <returns>This builder, for chaining.</returns>
    public SystemServicesBuilder With(ISignatureService signatures)
    {
        _signatures.Override(signatures);
        return this;
    }

    /// <summary>Substitutes the build-info reader.</summary>
    /// <param name="buildInfo">The build-info reader to install.</param>
    /// <returns>This builder, for chaining.</returns>
    public SystemServicesBuilder With(IBuildInfo buildInfo)
    {
        _buildInfo.Override(buildInfo);
        return this;
    }

    /// <summary>Substitutes the clock.</summary>
    /// <param name="clock">The clock to install.</param>
    /// <returns>This builder, for chaining.</returns>
    public SystemServicesBuilder With(TimeProvider clock)
    {
        _clock.Override(clock);
        return this;
    }

    /// <summary>Wraps the environment service, receiving the current instance and returning a proxy over it.</summary>
    /// <param name="proxy">The wrap that maps the current environment to a proxy.</param>
    /// <returns>This builder, for chaining.</returns>
    public SystemServicesBuilder Wrap(Func<IEnvironment, IEnvironment> proxy)
    {
        _environment.AddWrap(proxy);
        return this;
    }

    /// <summary>Wraps the random generator.</summary>
    /// <param name="proxy">The wrap that maps the current random generator to a proxy.</param>
    /// <returns>This builder, for chaining.</returns>
    public SystemServicesBuilder Wrap(Func<IRandomGenerator, IRandomGenerator> proxy)
    {
        _random.AddWrap(proxy);
        return this;
    }

    /// <summary>Wraps the console service.</summary>
    /// <param name="proxy">The wrap that maps the current console to a proxy.</param>
    /// <returns>This builder, for chaining.</returns>
    public SystemServicesBuilder Wrap(Func<IConsole, IConsole> proxy)
    {
        _console.AddWrap(proxy);
        return this;
    }

    /// <summary>Wraps the signature service.</summary>
    /// <param name="proxy">The wrap that maps the current signature service to a proxy.</param>
    /// <returns>This builder, for chaining.</returns>
    public SystemServicesBuilder Wrap(Func<ISignatureService, ISignatureService> proxy)
    {
        _signatures.AddWrap(proxy);
        return this;
    }

    /// <summary>Wraps the build-info reader.</summary>
    /// <param name="proxy">The wrap that maps the current build-info reader to a proxy.</param>
    /// <returns>This builder, for chaining.</returns>
    public SystemServicesBuilder Wrap(Func<IBuildInfo, IBuildInfo> proxy)
    {
        _buildInfo.AddWrap(proxy);
        return this;
    }

    /// <summary>Wraps the clock.</summary>
    /// <param name="proxy">The wrap that maps the current clock to a proxy.</param>
    /// <returns>This builder, for chaining.</returns>
    public SystemServicesBuilder Wrap(Func<TimeProvider, TimeProvider> proxy)
    {
        _clock.AddWrap(proxy);
        return this;
    }

    /// <summary>
    /// Navigates to the filesystem sub-builder, which substitutes and wraps the four filesystem leaves and installs the
    /// per-path failure seam (<see cref="FileSystemBuilder.Handle"/> / <see cref="FileSystemBuilder.MarkInaccessible"/>).
    /// </summary>
    /// <returns>The filesystem sub-builder over this builder.</returns>
    public FileSystemBuilder OnFileSystem() => new(this);

    /// <summary>Navigates to the platform sub-builder, which substitutes and wraps the platform file system.</summary>
    /// <returns>The platform sub-builder over this builder.</returns>
    public PlatformBuilder OnPlatform() => new(this);

    /// <summary>
    /// Layers the ONE shared copy-on-write overlay over the real filesystem: the four leaves and the platform file system
    /// all read the overlay, with the real filesystem injected as the overlay's read-only base, so a write or symlink a
    /// test makes lands in memory and the real fixture is never mutated. Requires <see cref="Real"/>.
    /// </summary>
    /// <returns>This builder, for chaining.</returns>
    public SystemServicesBuilder SimulateFileSystem()
    {
        if (_real is null)
        {
            throw new InvalidOperationException(
                "SimulateFileSystem() layers an overlay over the real filesystem; start from SystemServicesBuilder.Real().");
        }

        string realHome = _real.Environment.GetHomeDirectory();
        _store = NewOverlay(
            _real.FileSystem.GetFileReader(),
            _real.FileSystem.GetDirectoryReader(),
            _real.Platform.FileSystem,
            tempRoot: _real.Environment.GetTempDirectory(),
            caseSensitive: _real.Platform.FileSystem.IsCaseSensitive(realHome));
        return this;
    }

    /// <summary>
    /// Builds the container, resolving each service as its override (or built-in fake or real base) with any wrap applied.
    /// A fake service with no built-in fake and no override throws when its accessor is used, naming the fix.
    /// </summary>
    /// <returns>The assembled container.</returns>
    public ISystemServices Build()
    {
        IFileReader BaseFileReader() =>
            _store is not null ? InMemoryFileSystem.AsFileReader(_store) : _real!.FileSystem.GetFileReader();
        IDirectoryEnumerator BaseDirectoryReader() =>
            _store is not null ? InMemoryFileSystem.AsDirectoryEnumerator(_store) : _real!.FileSystem.GetDirectoryReader();
        IFileWriter BaseFileWriter() =>
            _store is not null ? InMemoryFileSystem.AsFileWriter(_store) : _real!.FileSystem.GetFileWriter();
        IDirectoryWriter BaseDirectoryWriter() =>
            _store is not null ? InMemoryFileSystem.AsDirectoryWriter(_store) : _real!.FileSystem.GetDirectoryWriter();
        IPlatformFileSystem BasePlatform() =>
            _store is not null ? ManagedPlatformFileSystem.Create(_store) : _real!.Platform.FileSystem;

        IEnvironment environment = _environment.Resolve(() =>
            _isFake
                ? FakeEnvironment.Create(
                    Host.Home,
                    tempDirectory: Host.Temp,
                    processArchitecture: Host.Process,
                    osArchitecture: Host.Os)
                : _real!.Environment);
        IRandomGenerator random = _random.Resolve(() => _isFake ? FixedGuidFactory.Create() : _real!.Random);
        IConsole console = _console.Resolve(() => _isFake ? new RecordingConsole().Console : _real!.Console);
        TimeProvider clock = _clock.Resolve(() => _isFake ? new FakeTimeProvider() : _real!.Clock);

        IFileReader fileReader = _fileReader.Resolve(BaseFileReader);
        IDirectoryEnumerator directoryReader = _directoryReader.Resolve(BaseDirectoryReader);
        IFileWriter fileWriter = _fileWriter.Resolve(BaseFileWriter);
        IDirectoryWriter directoryWriter = _directoryWriter.Resolve(BaseDirectoryWriter);
        IPlatformFileSystem platform = _platform.Resolve(BasePlatform);

        ISignatureService? signatures = ResolveOptional(_signatures, () => _real!.Signatures);
        IBuildInfo? buildInfo = ResolveOptional(_buildInfo, () => _real!.BuildInfo);

        IFileSystem fileSystem = FakeFileSystem.Create(
            fileReader, directoryReader, fileWriter, directoryWriter, _real?.FileSystem);
        IPlatformServices platformServices = FakePlatformServices.Create(platform);
        return FakeSystemServices.Create(
            fileSystem, environment, random, console, platformServices, signatures, buildInfo, clock);
    }

    // The SINGLE construction site of the shared overlay (AG0027 pins new InMemoryFileSystemStore to this class). Fake()
    // passes a null base (a pure in-memory filesystem); SimulateFileSystem() passes the real leaves as the read-only
    // base. The tempRoot and caseSensitive seed VALUES are real-host-sourced by the caller (Fake() from the cached host
    // values, SimulateFileSystem() from the injected real base), never a hardcoded literal (AG0035/AG0036).
    private static InMemoryFileSystemStore NewOverlay(
        IFileReader? baseReader,
        IDirectoryEnumerator? baseEnumerator,
        IPlatformFileSystem? basePlatform,
        string tempRoot,
        bool caseSensitive) =>
        new(baseReader, baseEnumerator, basePlatform, comparer: null, tempRoot: tempRoot, caseSensitive: caseSensitive);

    // Reads the real host VALUES ONCE through the one sanctioned Boundaries door (SystemServices.Create(), the same
    // entrypoint Real() calls — legal here because SystemServicesBuilder is a composition caller, AG0017), then discards
    // the container and keeps only plain VALUES: the two architectures, the home directory, the temp root, and the host
    // filesystem's case-sensitivity (read through the case-sensitivity detection the platform already carries, keyed on
    // the real home). GetTempDirectory() is the one AGS5443 owner-exempt call — SystemServicesBuilder is copy-on-write
    // over the real host and reads the real temp root once here to seed the in-memory fake (temp-root-owner-exemption).
    // Caching the values, not the container or a service, keeps clear of the static-service-holder rule (AG0024).
    private static RealHost ReadRealHost()
    {
        ISystemServices services = SystemServices.Create();
        IEnvironment environment = services.Environment;
        string home = environment.GetHomeDirectory();
        return new RealHost(
            environment.GetProcessArchitecture(),
            environment.GetOSArchitecture(),
            home,
            environment.GetTempDirectory(),
            services.Platform.FileSystem.IsCaseSensitive(home));
    }

    // A service with no built-in fake (signatures, build-info): resolve its override or, in Real mode, the real base; in
    // Fake mode with no override return null so the container accessor throws only when the service is actually used.
    private T? ResolveOptional<T>(Slot<T> slot, Func<T> realBase)
        where T : class =>
        slot.HasOverride || !_isFake ? slot.Resolve(realBase) : null;

    private InMemoryFileSystemStore RequireStore() =>
        _store ?? throw new InvalidOperationException(
            "The per-path failure seam needs a simulated or fake filesystem; call Fake() or Real().SimulateFileSystem() first.");

    // The real host VALUES the Fake() default and the overlay source from the real environment: process + OS
    // architecture, home directory, temp root, and filesystem case-sensitivity. A value type over two enums, two
    // strings, and a bool — not a service — so caching it in a static is not an AG0024 static service holder.
    // LayoutKind.Auto (MA0008): this is a managed-only value holder, never used for interop, so the runtime picks the
    // layout.
    [StructLayout(LayoutKind.Auto)]
    private readonly record struct RealHost(
        Architecture Process, Architecture Os, string Home, string Temp, bool CaseSensitive);

    /// <summary>
    /// The filesystem sub-builder: substitutes and wraps the four filesystem leaves on the correct nested scope (AG0019),
    /// and installs the per-path failure seam over the shared overlay.
    /// </summary>
    public sealed class FileSystemBuilder
    {
        private readonly SystemServicesBuilder _parent;

        /// <summary>Initializes a new instance of the <see cref="FileSystemBuilder"/> class over its parent builder.</summary>
        /// <param name="parent">The parent builder whose filesystem slots this sub-builder mutates.</param>
        internal FileSystemBuilder(SystemServicesBuilder parent) => _parent = parent;

        /// <summary>Substitutes the file reader.</summary>
        /// <param name="reader">The file reader to install.</param>
        /// <returns>This sub-builder, for chaining.</returns>
        public FileSystemBuilder With(IFileReader reader)
        {
            _parent._fileReader.Override(reader);
            return this;
        }

        /// <summary>Substitutes the directory reader.</summary>
        /// <param name="reader">The directory enumerator to install.</param>
        /// <returns>This sub-builder, for chaining.</returns>
        public FileSystemBuilder With(IDirectoryEnumerator reader)
        {
            _parent._directoryReader.Override(reader);
            return this;
        }

        /// <summary>Substitutes the file writer.</summary>
        /// <param name="writer">The file writer to install.</param>
        /// <returns>This sub-builder, for chaining.</returns>
        public FileSystemBuilder With(IFileWriter writer)
        {
            _parent._fileWriter.Override(writer);
            return this;
        }

        /// <summary>Substitutes the directory writer.</summary>
        /// <param name="writer">The directory writer to install.</param>
        /// <returns>This sub-builder, for chaining.</returns>
        public FileSystemBuilder With(IDirectoryWriter writer)
        {
            _parent._directoryWriter.Override(writer);
            return this;
        }

        /// <summary>Wraps the file reader.</summary>
        /// <param name="proxy">The wrap that maps the current file reader to a proxy.</param>
        /// <returns>This sub-builder, for chaining.</returns>
        public FileSystemBuilder Wrap(Func<IFileReader, IFileReader> proxy)
        {
            _parent._fileReader.AddWrap(proxy);
            return this;
        }

        /// <summary>Wraps the directory reader.</summary>
        /// <param name="proxy">The wrap that maps the current directory enumerator to a proxy.</param>
        /// <returns>This sub-builder, for chaining.</returns>
        public FileSystemBuilder Wrap(Func<IDirectoryEnumerator, IDirectoryEnumerator> proxy)
        {
            _parent._directoryReader.AddWrap(proxy);
            return this;
        }

        /// <summary>Wraps the file writer.</summary>
        /// <param name="proxy">The wrap that maps the current file writer to a proxy.</param>
        /// <returns>This sub-builder, for chaining.</returns>
        public FileSystemBuilder Wrap(Func<IFileWriter, IFileWriter> proxy)
        {
            _parent._fileWriter.AddWrap(proxy);
            return this;
        }

        /// <summary>Wraps the directory writer.</summary>
        /// <param name="proxy">The wrap that maps the current directory writer to a proxy.</param>
        /// <returns>This sub-builder, for chaining.</returns>
        public FileSystemBuilder Wrap(Func<IDirectoryWriter, IDirectoryWriter> proxy)
        {
            _parent._directoryWriter.AddWrap(proxy);
            return this;
        }

        /// <summary>
        /// Takes control of a specific path — the first link in the resolution chain. For a matched path the handler runs
        /// first and either throws an injected failure, returns an override, or delegates to the pass-through to let the
        /// normal overlay-then-base chain answer. Requires a simulated or fake filesystem.
        /// </summary>
        /// <param name="path">The path the handler takes control of.</param>
        /// <param name="handler">The handler invoked first for every operation on <paramref name="path"/>.</param>
        /// <returns>This sub-builder, for chaining.</returns>
        public FileSystemBuilder Handle(string path, PathHandler handler)
        {
            _parent.RequireStore().Handle(path, handler);
            return this;
        }

        /// <summary>
        /// Marks a directory inaccessible: enumerating its children throws <see cref="UnauthorizedAccessException"/> while
        /// existence checks still answer. The kept convenience over <see cref="Handle"/> for the fail-closed tests.
        /// </summary>
        /// <param name="path">The directory path to mark inaccessible.</param>
        /// <returns>This sub-builder, for chaining.</returns>
        public FileSystemBuilder MarkInaccessible(string path)
        {
            _parent.RequireStore().MarkInaccessible(path);
            return this;
        }
    }

    /// <summary>
    /// The platform sub-builder: substitutes and wraps the platform file system on the correct nested scope (AG0019).
    /// </summary>
    public sealed class PlatformBuilder
    {
        private readonly SystemServicesBuilder _parent;

        /// <summary>Initializes a new instance of the <see cref="PlatformBuilder"/> class over its parent builder.</summary>
        /// <param name="parent">The parent builder whose platform slot this sub-builder mutates.</param>
        internal PlatformBuilder(SystemServicesBuilder parent) => _parent = parent;

        /// <summary>Substitutes the platform file system.</summary>
        /// <param name="platform">The platform file system to install.</param>
        /// <returns>This sub-builder, for chaining.</returns>
        public PlatformBuilder With(IPlatformFileSystem platform)
        {
            _parent._platform.Override(platform);
            return this;
        }

        /// <summary>Wraps the platform file system.</summary>
        /// <param name="proxy">The wrap that maps the current platform file system to a proxy.</param>
        /// <returns>This sub-builder, for chaining.</returns>
        public PlatformBuilder Wrap(Func<IPlatformFileSystem, IPlatformFileSystem> proxy)
        {
            _parent._platform.AddWrap(proxy);
            return this;
        }

        /// <summary>
        /// Sets the value the platform fake's <c>NeedsExecutableFlag()</c> returns (copy-on-write-simulator-design),
        /// overriding the default that follows the running OS through the real platform — so a unit test can simulate
        /// POSIX (<see langword="true"/>) or Windows (<see langword="false"/>) and exercise the executable-bit path either
        /// way. Requires a simulated or fake filesystem.
        /// </summary>
        /// <param name="needsExecutableFlag">The simulated executable-bit support.</param>
        /// <returns>This sub-builder, for chaining.</returns>
        public PlatformBuilder SimulateExecutableFlag(bool needsExecutableFlag)
        {
            _parent.RequireStore().SetNeedsExecutableFlag(needsExecutableFlag);
            return this;
        }

        /// <summary>
        /// Sets the simulator's case mode (copy-on-write-simulator-design), which drives BOTH the platform fake's
        /// <c>IsCaseSensitive</c> and the overlay's path comparer from the one place they both read. Switching to
        /// case-insensitive first scans the current store and throws when two existing paths collide under case-folding.
        /// Requires a simulated or fake filesystem.
        /// </summary>
        /// <param name="caseSensitive"><see langword="true"/> for a case-sensitive filesystem; <see langword="false"/>
        /// for a case-insensitive one.</param>
        /// <returns>This sub-builder, for chaining.</returns>
        public PlatformBuilder SimulateCaseSensitivity(bool caseSensitive)
        {
            _parent.RequireStore().SetCaseSensitive(caseSensitive);
            return this;
        }
    }

    /// <summary>
    /// The one <see cref="ISystemServices"/> implementer in this compilation (AG0022): it holds each resolved service and
    /// throws a helpful message when a service with no built-in fake (signatures, build-info) is accessed without being
    /// supplied. It is <c>internal</c> so <see cref="Build"/> can reach its factory; the containing builder cannot call a
    /// nested private constructor, and a private factory returning an interface trips CA1859, so <c>internal</c> is the
    /// tightest accessibility the analyzer rules permit for a nested contract fake.
    /// </summary>
    internal sealed class FakeSystemServices : ISystemServices
    {
        private readonly ISignatureService? _signatures;
        private readonly IBuildInfo? _buildInfo;

        private FakeSystemServices(
            IFileSystem fileSystem,
            IEnvironment environment,
            IRandomGenerator random,
            IConsole console,
            IPlatformServices platform,
            ISignatureService? signatures,
            IBuildInfo? buildInfo,
            TimeProvider clock)
        {
            FileSystem = fileSystem;
            Environment = environment;
            Random = random;
            Console = console;
            Platform = platform;
            _signatures = signatures;
            _buildInfo = buildInfo;
            Clock = clock;
        }

        /// <inheritdoc />
        public IFileSystem FileSystem { get; }

        /// <inheritdoc />
        public IEnvironment Environment { get; }

        /// <inheritdoc />
        public IRandomGenerator Random { get; }

        /// <inheritdoc />
        public IConsole Console { get; }

        /// <inheritdoc />
        public IPlatformServices Platform { get; }

        /// <inheritdoc />
        public ISignatureService Signatures => _signatures
            ?? throw new InvalidOperationException(
                "Fake() has no built-in signature service; supply one with SystemServicesBuilder.With(ISignatureService).");

        /// <inheritdoc />
        public IBuildInfo BuildInfo => _buildInfo
            ?? throw new InvalidOperationException(
                "Fake() has no built-in build-info reader; supply one with SystemServicesBuilder.With(IBuildInfo).");

        /// <inheritdoc />
        public TimeProvider Clock { get; }

        /// <summary>Creates the container fake over the already-resolved services.</summary>
        /// <param name="fileSystem">The resolved file system.</param>
        /// <param name="environment">The resolved environment.</param>
        /// <param name="random">The resolved random generator.</param>
        /// <param name="console">The resolved console.</param>
        /// <param name="platform">The resolved platform services.</param>
        /// <param name="signatures">The resolved signature service, or <see langword="null"/> when none was supplied.</param>
        /// <param name="buildInfo">The resolved build-info reader, or <see langword="null"/> when none was supplied.</param>
        /// <param name="clock">The resolved clock.</param>
        /// <returns>The container, as its interface.</returns>
        internal static ISystemServices Create(
            IFileSystem fileSystem,
            IEnvironment environment,
            IRandomGenerator random,
            IConsole console,
            IPlatformServices platform,
            ISignatureService? signatures,
            IBuildInfo? buildInfo,
            TimeProvider clock) =>
            new FakeSystemServices(fileSystem, environment, random, console, platform, signatures, buildInfo, clock);
    }

    /// <summary>
    /// The one <see cref="IFileSystem"/> implementer in this compilation (AG0022): it returns the resolved leaves. The
    /// engine reaches the filesystem only through the leaf services, so <see cref="GetFileInfo"/>/<see cref="GetDirectoryInfo"/>
    /// delegate to the real filesystem when present (an integration path) and throw under a pure fake, where no <c>*Info</c>
    /// wrapper is modeled.
    /// </summary>
    internal sealed class FakeFileSystem : IFileSystem
    {
        private readonly IFileReader _fileReader;
        private readonly IDirectoryEnumerator _directoryReader;
        private readonly IFileWriter _fileWriter;
        private readonly IDirectoryWriter _directoryWriter;
        private readonly IFileSystem? _real;

        private FakeFileSystem(
            IFileReader fileReader,
            IDirectoryEnumerator directoryReader,
            IFileWriter fileWriter,
            IDirectoryWriter directoryWriter,
            IFileSystem? real)
        {
            _fileReader = fileReader;
            _directoryReader = directoryReader;
            _fileWriter = fileWriter;
            _directoryWriter = directoryWriter;
            _real = real;
        }

        /// <inheritdoc />
        public IFileInfo GetFileInfo(string path) => _real is not null
            ? _real.GetFileInfo(path)
            : throw new NotSupportedException("The in-memory fake exposes the filesystem through its leaf services, not *Info wrappers.");

        /// <inheritdoc />
        public IDirectoryInfo GetDirectoryInfo(string path) => _real is not null
            ? _real.GetDirectoryInfo(path)
            : throw new NotSupportedException("The in-memory fake exposes the filesystem through its leaf services, not *Info wrappers.");

        /// <inheritdoc />
        public IFileReader GetFileReader() => _fileReader;

        /// <inheritdoc />
        public IDirectoryEnumerator GetDirectoryReader() => _directoryReader;

        /// <inheritdoc />
        public IFileWriter GetFileWriter() => _fileWriter;

        /// <inheritdoc />
        public IDirectoryWriter GetDirectoryWriter() => _directoryWriter;

        /// <summary>Creates the file-system fake over the resolved leaves and the optional real filesystem.</summary>
        /// <param name="fileReader">The resolved file reader.</param>
        /// <param name="directoryReader">The resolved directory enumerator.</param>
        /// <param name="fileWriter">The resolved file writer.</param>
        /// <param name="directoryWriter">The resolved directory writer.</param>
        /// <param name="real">The real filesystem for <c>*Info</c> factories on an integration path, or
        /// <see langword="null"/>.</param>
        /// <returns>The file system, as its interface.</returns>
        internal static IFileSystem Create(
            IFileReader fileReader,
            IDirectoryEnumerator directoryReader,
            IFileWriter fileWriter,
            IDirectoryWriter directoryWriter,
            IFileSystem? real) =>
            new FakeFileSystem(fileReader, directoryReader, fileWriter, directoryWriter, real);
    }

    /// <summary>
    /// The one <see cref="IPlatformServices"/> implementer in this compilation (AG0022): it exposes the resolved platform
    /// file system.
    /// </summary>
    internal sealed class FakePlatformServices : IPlatformServices
    {
        private FakePlatformServices(IPlatformFileSystem fileSystem) => FileSystem = fileSystem;

        /// <inheritdoc />
        public IPlatformFileSystem FileSystem { get; }

        /// <summary>Creates the platform-services fake over the resolved platform file system.</summary>
        /// <param name="fileSystem">The resolved platform file system.</param>
        /// <returns>The platform services, as its interface.</returns>
        internal static IPlatformServices Create(IPlatformFileSystem fileSystem) => new FakePlatformServices(fileSystem);
    }

    // One override slot plus a composed wrap for a single service. Resolve applies the override (or the supplied base)
    // and then any wrap; a later Wrap composes over the earlier one, outermost last.
    private sealed class Slot<T>
        where T : class
    {
        private T? _override;
        private Func<T, T>? _wrap;

        internal bool HasOverride => _override is not null;

        internal void Override(T value)
        {
            ArgumentNullException.ThrowIfNull(value);
            _override = value;
        }

        internal void AddWrap(Func<T, T> wrap)
        {
            ArgumentNullException.ThrowIfNull(wrap);
            Func<T, T>? previous = _wrap;
            _wrap = previous is null ? wrap : inner => wrap(previous(inner));
        }

        internal T Resolve(Func<T> baseFactory)
        {
            T value = _override ?? baseFactory();
            return _wrap is null ? value : _wrap(value);
        }
    }
}
