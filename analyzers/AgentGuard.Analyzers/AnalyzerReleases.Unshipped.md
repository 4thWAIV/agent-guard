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
AG0022  | AgentGuard.Architecture | Warning | ContainerInterfaceSingleOwnerAnalyzer
AG0023  | AgentGuard.Architecture | Warning | BoundariesToCrossPlatformOneDoorAnalyzer
AG0024  | AgentGuard.Architecture | Warning | NoStaticServiceHolderAnalyzer
AG0025  | AgentGuard.Architecture | Warning | OneOwnerPerInterfaceAnalyzer
AG0026  | AgentGuard.Architecture | Warning | NoOsSkipInTestsAnalyzer
AG0027  | AgentGuard.Architecture | Warning | GuardedConstructionAnalyzer (InMemoryFileSystemStore single construction)
AG0029  | AgentGuard.Architecture | Warning | BoundariesToPerOsOneDoorAnalyzer
AG0030  | AgentGuard.Architecture | Warning | LeafPlatformSingleImplementerAnalyzer
AG0031  | AgentGuard.Architecture | Warning | NoServiceAsParameterAnalyzer
AG0032  | AgentGuard.Architecture | Warning | NoCoverageOptOutAnalyzer
AG0033  | AgentGuard.Architecture | Warning | GuardedConstructionAnalyzer (*Info wrapper construction)
AG0034  | AgentGuard.Architecture | Warning | SystemServicesMemberMustBeServiceAccessorAnalyzer
AG0035  | AgentGuard.Architecture | Warning | NoLiteralFakeRootAnalyzer
AG0036  | AgentGuard.Architecture | Warning | NoLiteralCaseModeSeedAnalyzer
AG0037  | AgentGuard.Architecture | Warning | NoOsBranchingOutsideCrossPlatformAnalyzer (single-OS branch inside a per-OS implementation library)
AG0038  | AgentGuard.Architecture | Warning | TimeoutMustUseTimeProviderAnalyzer (bounded waits/timeouts flow through the injected TimeProvider)
AG0039  | AgentGuard.Architecture | Warning | NoCallerFilePathAnalyzer ([CallerFilePath] banned production and test alike, wherever the custom analyzers are wired in; reach the base directory through IEnvironment.GetBaseDirectory())
AG0101  | AgentGuard.Architecture | Warning | OsDivergentFilesystemOnlyInCrossPlatformAnalyzer (shrunk to static OS-divergent members, Marshal, and P/Invoke)
AG0102  | AgentGuard.Architecture | Warning | PInvokeSignatureMustBeBlittableAnalyzer (no object/dynamic/unconstrained-generic/variadic in a P/Invoke signature)
AG0103  | AgentGuard.Architecture | Warning | MacOsNativeBoolMustBeI1Analyzer (macOS native bool marshalled as UnmanagedType.I1)
AG0104  | AgentGuard.Architecture | Warning | NativeCallbackMustUseFunctionPointerAnalyzer (native callback via static [UnmanagedCallersOnly] &method, not a Marshal delegate pointer)
AG0105  | AgentGuard.Architecture | Warning | NativeCallbackBodyMustBeGuardedAnalyzer (native callback body — [UnmanagedCallersOnly] OR a ComVisible-interface completed-handler — is one try/catch(Exception) returning a native error code)
AG0106  | AgentGuard.Architecture | Warning | NoControlFlowInNativeOpsAnalyzer (no control flow — if/ternary, loop, switch statement/expression, coalesce/coalesce-assignment, conditional-access, short-circuit and/or, pattern and/or combinator — in a native-ops implementer; try/catch exempt)
AG0107  | AgentGuard.Architecture | Warning | NoTimeoutInPresenceImplAnalyzer (no timeout inside a presence impl, its flow port, or its native-ops owner; the gate owns the one 60s bound)
AG0108  | AgentGuard.Architecture | Warning | PresenceCheckOnlyFromApprovalGateAnalyzer (IPresenceCheck.Check invoked only from ApprovalGate in production)
AG0109  | AgentGuard.Architecture | Warning | NoInlinePromptLiteralAtPresenceCallAnalyzer (no inline prompt literal at a production presence call site)
AG0110  | AgentGuard.Architecture | Warning | DbusOnlyInPolkitAuthorityAnalyzer (Tmds.DBus.Protocol confined to the IPolkitAuthority owner; high-level Tmds.DBus banned)
AG0111  | AgentGuard.Architecture | Warning | NoAuthenticationAgentRegistrationAnalyzer (value-scan: never register/spawn a polkit authentication agent)
AG0112  | AgentGuard.Architecture | Warning | NoCachedNativeAuthContextAnalyzer (no cached native auth-context handle field in a native presence port)
AG0113  | AgentGuard.Architecture | Warning | PresenceNativeInteropOwnerAnalyzer (presence-family P/Invoke + Marshal confined to the per-OS native-ops owner — IObjCRuntime / IWindowsHelloNativeOps / ICredentialPromptNativeOps; family-scoped, paired with AG0101)
AG0114  | AgentGuard.Architecture | Warning | NativePresencePortSingleImplementerAnalyzer (each per-OS native port AND native-ops owner has exactly one implementer)
AG0115  | AgentGuard.Architecture | Warning | NoAsyncOrchestrationInNativeOpsAnalyzer (no await, await using, or await foreach; no call returning a Task/Task<T>/ValueTask/ValueTask<T>; no TaskCompletionSource — in a native-ops implementer)
AG0116  | AgentGuard.Architecture | Warning | NoStateInNativeOpsAnalyzer (a native-ops implementer is fully stateless — no field, auto-property, or field-like event; const exempt)
AGS5443 | AgentGuard.Architecture | Warning | TempRootBackDoorAnalyzer (IEnvironment.GetTempDirectory call sites; owned equivalent of Sonar S5443)
