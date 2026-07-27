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
