// Copyright (c) 4thWAIV. All rights reserved.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace AgentGuard.Analyzers.Tests;

/// <summary>
/// AG0116 (no-state-in-native-ops): a class implementing a per-OS native-ops interface (IObjCRuntime and the two Windows
/// native-ops interfaces) must be fully stateless — no instance or static field of any type, no auto-property of any
/// accessor shape (get-only OR settable, with or without an initializer — each has a compiler-synthesized backing field
/// that can cache construction-time state), and no field-like event; a const is exempt. Those three — non-const field,
/// backed auto-property, field-like event — are exactly the implicitly-backed stored-state spellings, so the set is
/// closed. A fourth check on the implementer TYPE closes the base-class loophole: the member scans see only the
/// implementer's OWN members, so state on a BASE CLASS is invisible to them; a native-ops implementer must derive
/// directly from System.Object, and a non-object base is reported on the implementer class. This is the broader superset
/// of AG0112
/// (cached native handle) scoped to the native-ops layer. The native-ops owner interface stub is the shared owner
/// SharedAnalyzerSources.ObjCRuntimeNativeOps; the implementer sits in a second MacOS namespace block. The analyzer
/// registers its symbol actions through the shared RegisterInMacOsOrWindows gate (mirroring AG0106/AG0113/AG0115), so it
/// fires ONLY inside the macOS/Windows PRODUCTION assemblies: the RED fixtures compile the stateful implementer into
/// AgentGuard.CrossPlatform.MacOS, and the GUARD fixture proves the same stateful shape compiled into a non-production
/// (test) assembly is NOT reported — pinning that the production-only gate lets the stateful native-ops test fakes hold
/// state. Preventive against production.
/// </summary>
public class NoStateInNativeOpsAnalyzerTests
{
    private const string NativeOps = SharedAnalyzerSources.ObjCRuntimeNativeOps;

    [Fact]
    public async Task InstanceFieldInNativeOps_IsReported()
    {
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    private int _count;
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoStateInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0116", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task StaticFieldInNativeOps_IsReported()
    {
        // A field of ANY type is state — not merely a native handle (that narrower shape is AG0112). A static reference
        // field is reported here.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    private static readonly object Gate = new object();
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoStateInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0116", diagnostic.Id);
    }

    [Fact]
    public async Task AutoPropertyWithSetterInNativeOps_IsReported()
    {
        // A settable auto-property's backing field is compiler-synthesized and implicitly declared, so it never surfaces
        // to the field symbol action — a field-only scan would miss it. Stored state is a violation regardless of
        // spelling, so the property symbol action reports it. This is the SOLID-1 gap the fix closes.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal object Cached { get; set; }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoStateInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0116", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public async Task FieldLikeEventInNativeOps_IsReported()
    {
        // A field-like event (`event EventHandler Changed;`) holds a subscribable delegate list in a compiler-synthesized
        // backing field that is implicitly declared, so — like a settable auto-property — it never surfaces to the field
        // symbol action; a field-only scan would miss it. Stored state is a violation regardless of spelling, so the event
        // symbol action reports it. This is the third and last implicitly-backed spelling, closing AG0116's stored-state
        // set (non-const field, settable auto-property, field-like event).
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System;
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal event EventHandler Changed;
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoStateInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0116", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal("Changed", AnalyzerRunner.SpanText(source, diagnostic));
    }

    [Fact]
    public async Task CustomEventWithExplicitAccessorsInNativeOps_ReportsFieldOnly()
    {
        // A custom event with explicit add/remove synthesizes no backing field; its subscriber list lives in an explicit
        // field the field scan already reports. The event action must not double-count it — exactly one diagnostic, on the
        // explicit field (not the event). This is the event analogue of the manual-property double-report guard.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System;
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    private EventHandler _changed;
                    internal event EventHandler Changed { add { _changed += value; } remove { _changed -= value; } }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoStateInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0116", diagnostic.Id);
        Assert.Equal("_changed", AnalyzerRunner.SpanText(source, diagnostic));
    }

    [Fact]
    public async Task StatefulNativeOpsInTestAssembly_IsNotReported()
    {
        // The GUARD for the production-only gate: the SAME stateful native-ops shape the RED fixtures above report — a
        // non-const field AND a settable auto-property in a class implementing the native-ops interface — is NOT reported
        // when it is compiled into a non-production (test) assembly. AgentGuard.CrossPlatform.Tests is where the stateful
        // native-ops fakes (composing SingleCallRecorder for per-method call counts) legitimately live through the
        // verified InternalsVisibleTo grants; a test fake CAN implement the internal native-ops interface, so the
        // internal-interface visibility alone would not keep AG0116 off it — the RegisterInMacOsOrWindows gate does. This
        // pins that the analyzer fires only inside the macOS/Windows production assemblies (mirroring AG0106/AG0113/
        // AG0115), letting the test fakes hold state.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                internal sealed class FakeObjCRuntime : IObjCRuntime
                {
                    private int _count;
                    internal object Cached { get; set; }
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoStateInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.Tests"));
    }

    [Fact]
    public async Task GetOnlyAutoPropertyInNativeOps_IsReported()
    {
        // A get-only auto-property has a compiler-synthesized backing field that is implicitly declared, so it never
        // surfaces to the field symbol action — a field-only scan would miss it — and that field CAN hold live state
        // (assigned in the constructor). The design is fully stateless (no fields at all; const exempt), and a get-only
        // auto-property IS a backing field, so the property symbol action reports it. This is the lie-catcher gap the fix
        // closes: the former get-only exemption let stored state hide.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal object Library { get; }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoStateInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0116", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal("Library", AnalyzerRunner.SpanText(source, diagnostic));
    }

    [Fact]
    public async Task GetOnlyAutoPropertyWithInitializerInNativeOps_IsReported()
    {
        // The concrete leak the get-only exemption let hide: `internal IntPtr Handle { get; } = ...;` caches a native
        // handle in the synthesized backing field at construction — exactly the stored state AG0116 and AG0112 exist to
        // prevent. A get-only auto-property WITH an initializer is a backing field holding live construction-time state, so
        // it is reported just like the no-initializer form above.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System;
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal IntPtr Handle { get; } = new IntPtr(1);
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoStateInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0116", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal("Handle", AnalyzerRunner.SpanText(source, diagnostic));
    }

    [Fact]
    public async Task ManualPropertyWithSetterInNativeOps_ReportsFieldOnly()
    {
        // A manually-implemented property with a setter synthesizes no backing field; its storage is an explicit field
        // the field scan already reports. The property action must not double-count it — exactly one diagnostic, on the
        // field (not the property).
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System;
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    private IntPtr _handle;
                    internal IntPtr Handle { get => _handle; set => _handle = value; }
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoStateInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0116", diagnostic.Id);
        Assert.Equal("_handle", AnalyzerRunner.SpanText(source, diagnostic));
    }

    [Fact]
    public async Task ExpressionBodiedPropertyInNativeOps_IsNotReported()
    {
        // A manually-implemented (expression-bodied) property synthesizes no backing field — its storage, if any, is an
        // explicit field the field scan already catches — so the property action does not double-count it.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                using System;
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    internal IntPtr Zero => IntPtr.Zero;
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoStateInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task ConstInNativeOps_IsNotReported()
    {
        // A const is a compile-time literal that holds no live state — the native library paths, selector names, and
        // error codes the class needs. It is exempt.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                    private const string ObjcLibrary = "/usr/lib/libobjc.A.dylib";
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoStateInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task FieldOutsideNativeOps_IsNotReported()
    {
        // A class that does NOT implement a native-ops interface may hold fields freely — this rule constrains only the
        // native-ops layer.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                internal sealed class Ordinary
                {
                    private int _count;
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoStateInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }

    [Fact]
    public async Task NativeOpsWithNonObjectBaseClass_IsReported()
    {
        // The base-class loophole the structural close shuts: AG0116's field/property/event scans see only the
        // implementer's OWN members, so a field declared on a BASE CLASS (`_count` on NativeOpsBase) is invisible to them
        // and would pass clean. A native-ops implementer must derive directly from System.Object; any base class is a
        // place upstream state can hide, so a non-object base is reported on the implementer class itself — no hierarchy
        // walk, because no base class means nowhere for state to hide. Exactly one diagnostic, on the implementer.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                internal abstract class NativeOpsBase
                {
                    protected int _count;
                }
                internal sealed class ObjCRuntime : NativeOpsBase, IObjCRuntime
                {
                }
            }
            """;

        Diagnostic diagnostic = Assert.Single(await AnalyzerRunner.RunAsync<NoStateInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
        Assert.Equal("AG0116", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal("ObjCRuntime", AnalyzerRunner.SpanText(source, diagnostic));
    }

    [Fact]
    public async Task NativeOpsWithObjectBase_IsNotReportedForInheritance()
    {
        // The GUARD for the structural close: a native-ops implementer that derives directly from System.Object (the one
        // permitted base) and holds no state is NOT reported. Object is the only base a native-ops implementer may have,
        // so the base-class check stays silent on the required shape.
        string source = NativeOps + """

            namespace AgentGuard.CrossPlatform.MacOS
            {
                internal sealed class ObjCRuntime : IObjCRuntime
                {
                }
            }
            """;

        Assert.Empty(await AnalyzerRunner.RunAsync<NoStateInNativeOpsAnalyzer>(source, "AgentGuard.CrossPlatform.MacOS"));
    }
}
