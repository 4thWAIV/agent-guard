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
AG0101  | AgentGuard.Architecture | Warning | OsDivergentFilesystemOnlyInCrossPlatformAnalyzer
