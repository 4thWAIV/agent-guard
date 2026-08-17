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
AG0011  | AgentGuard.Architecture | Warning | RawPrimitiveOnlyInOwnerAnalyzer (folds the former AG0012/0013/0014/0016/0021/0028 and the *Info half of AG0101)
AG0015  | AgentGuard.Architecture | Warning | TimeMustUseTimeProviderAnalyzer
AG0017  | AgentGuard.Architecture | Warning | GuardedConstructionAnalyzer (container factory)
AG0018  | AgentGuard.Architecture | Warning | TestHelpersOnlyInTestsAnalyzer
AG0019  | AgentGuard.Architecture | Warning | BuilderCompletenessAnalyzer
AG0020  | AgentGuard.Architecture | Warning | PathPurityAnalyzer
AG0023  | AgentGuard.Architecture | Warning | BoundariesToCrossPlatformOneDoorAnalyzer
AG0024  | AgentGuard.Architecture | Warning | NoStaticServiceHolderAnalyzer
AG0025  | AgentGuard.Architecture | Warning | OneOwnerPerInterfaceAnalyzer
AG0029  | AgentGuard.Architecture | Warning | BoundariesToPerOsOneDoorAnalyzer
AG0031  | AgentGuard.Architecture | Warning | NoServiceAsParameterAnalyzer
AG0032  | AgentGuard.Architecture | Warning | NoCoverageOptOutAnalyzer
AG0033  | AgentGuard.Architecture | Warning | GuardedConstructionAnalyzer (*Info wrapper construction)
AG0034  | AgentGuard.Architecture | Warning | SystemServicesMemberMustBeServiceAccessorAnalyzer
AG0101  | AgentGuard.Architecture | Warning | OsDivergentFilesystemOnlyInCrossPlatformAnalyzer (shrunk to static OS-divergent members, Marshal, and P/Invoke)
