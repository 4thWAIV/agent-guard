; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
AG0001  | AgentGuard.Architecture | Warning | NoClassInAbstractionsAnalyzer
AG0002  | AgentGuard.Design | Warning | ReturnTypesMustNotBeTuplesAnalyzer
AG0003  | AgentGuard.Architecture | Warning | ContractConstructorMustBePrivateAnalyzer
AG0004  | AgentGuard.Architecture | Warning | ContractMustNotExposeConcreteTypeAnalyzer
AG0005  | AgentGuard.Architecture | Warning | ContractPublicSurfaceMustMatchInterfaceAnalyzer
AG0006  | AgentGuard.Architecture | Warning | ContractConcreteTypeMustNotBeReferencedAnalyzer
AG0007  | AgentGuard.Architecture | Warning | ContractConcreteTypeMustNotBeCastToAnalyzer
AG0008  | AgentGuard.Architecture | Warning | InteropOnlyInCrossPlatformLibrariesAnalyzer
AG0009  | AgentGuard.Architecture | Warning | NoOsBranchingOutsideCrossPlatformAnalyzer
AG0010  | AgentGuard.Architecture | Warning | PlatformFactoryMustReturnContainerAnalyzer
AG0011  | AgentGuard.Architecture | Warning | FilesystemOnlyInBoundariesAnalyzer
AG0012  | AgentGuard.Architecture | Warning | EnvironmentOnlyInBoundariesAnalyzer
AG0013  | AgentGuard.Architecture | Warning | ProcessIsForbiddenAnalyzer
AG0014  | AgentGuard.Architecture | Warning | RandomnessOnlyInBoundariesAnalyzer
AG0015  | AgentGuard.Architecture | Warning | TimeMustUseTimeProviderAnalyzer
AG0016  | AgentGuard.Architecture | Warning | ConsoleOnlyInBoundariesAnalyzer
AG0017  | AgentGuard.Architecture | Warning | SystemServicesCreateOnlyAtCompositionAnalyzer
AG0018  | AgentGuard.Architecture | Warning | TestHelpersOnlyInTestsAnalyzer
AG0019  | AgentGuard.Architecture | Warning | BuilderCompletenessAnalyzer
AG0020  | AgentGuard.Architecture | Warning | PathPurityAnalyzer
AG0021  | AgentGuard.Architecture | Warning | CryptoOnlyInSignatureVerifierAnalyzer
AG0023  | AgentGuard.Architecture | Warning | BoundariesToCrossPlatformOneDoorAnalyzer
AG0024  | AgentGuard.Architecture | Warning | NoStaticServiceHolderAnalyzer
AG0025  | AgentGuard.Architecture | Warning | OneOwnerPerInterfaceAnalyzer
AG0028  | AgentGuard.Architecture | Warning | VersionReadsOwnedAnalyzer
AG0029  | AgentGuard.Architecture | Warning | BoundariesToPerOsOneDoorAnalyzer
AG0031  | AgentGuard.Architecture | Warning | NoServiceAsParameterAnalyzer
AG0032  | AgentGuard.Architecture | Warning | NoCoverageOptOutAnalyzer
AG0101  | AgentGuard.Architecture | Warning | OsDivergentFilesystemOnlyInCrossPlatformAnalyzer
