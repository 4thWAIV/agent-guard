// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace AgentGuard.CrossPlatform.MacOS;

/// <summary>
/// The sole implementer of <see cref="IObjCRuntime"/> and the ONE class that binds the macOS Objective-C runtime through
/// <c>objc_msgSend</c> and its siblings — the presence NATIVE-OPS owner (AG0113 confines raw presence P/Invoke and
/// <c>Marshal</c> here; AG0114 permits one implementer). It is fully stateless (AG0116): every native handle it returns
/// is a per-call local the <see cref="LocalAuthentication"/> orchestrator owns and releases, and it holds no field but
/// compile-time <c>const</c>s. Each method is a straight-line raw call with no branching (AG0106) and no task-completion
/// wiring (AG0115): the flow, mapping, <c>TaskCompletionSource</c>/<c>GCHandle</c> wiring, and cancel-invalidate logic
/// all live in the fake-testable <see cref="LocalAuthentication"/> orchestrator that composes these primitives. The
/// interactive <c>evaluatePolicy:localizedReason:reply:</c> completion block is a manually assembled Objective-C block
/// whose invoke is the static <c>[UnmanagedCallersOnly]</c> <see cref="EvaluateReplyCallback"/> reached by a function
/// pointer (AG0104), its whole body one guarded try/catch (AG0105); the block captures the orchestrator-supplied
/// completion handle and the callback merely recovers and invokes the orchestrator's reply handler.
/// </summary>
internal sealed partial class ObjCRuntime : IObjCRuntime
{
    // The macOS runtime libraries. libobjc.A.dylib and libSystem.B.dylib have no on-disk file on modern macOS; they
    // resolve from the dyld shared cache, and a by-path P/Invoke still binds (proven by the objc_msgSend spike).
    private const string ObjcLibrary = "/usr/lib/libobjc.A.dylib";
    private const string SystemLibrary = "/usr/lib/libSystem.B.dylib";
    private const string LocalAuthenticationFramework =
        "/System/Library/Frameworks/LocalAuthentication.framework/LocalAuthentication";

    // Objective-C class and selector names.
    private const string LAContextClass = "LAContext";
    private const string NSStringClass = "NSString";
    private const string AllocSelector = "alloc";
    private const string InitSelector = "init";
    private const string InitWithUtf8Selector = "initWithUTF8String:";
    private const string ReleaseSelector = "release";
    private const string InvalidateSelector = "invalidate";
    private const string CodeSelector = "code";
    private const string CanEvaluatePolicyErrorSelector = "canEvaluatePolicy:error:";
    private const string EvaluatePolicyReplySelector = "evaluatePolicy:localizedReason:reply:";

    // The Objective-C data symbol a stack block's isa points at; resolved through dlsym(RTLD_DEFAULT, ...).
    private const string ConcreteStackBlockSymbol = "_NSConcreteStackBlock";

    // dlsym's RTLD_DEFAULT pseudo-handle on macOS is ((void *)-2); an int sign-extends to the full pointer value.
    private const int RtldDefault = -2;

    static ObjCRuntime() =>

        // Load the framework once so objc_getClass("LAContext") resolves; the handle is intentionally discarded (the
        // image stays loaded process-wide) so no native handle is cached in a field (AG0116).
        NativeLibrary.Load(LocalAuthenticationFramework);

    /// <inheritdoc />
    public IntPtr CreateContext()
    {
        IntPtr laContextClass = ObjcGetClass(LAContextClass);
        IntPtr allocated = MsgSend(laContextClass, SelRegisterName(AllocSelector));
        return MsgSend(allocated, SelRegisterName(InitSelector));
    }

    /// <inheritdoc />
    public IntPtr CreateReasonString(string reason)
    {
        IntPtr nsStringClass = ObjcGetClass(NSStringClass);
        IntPtr allocated = MsgSend(nsStringClass, SelRegisterName(AllocSelector));

        byte[] utf8 = new byte[Encoding.UTF8.GetByteCount(reason) + 1];
        Encoding.UTF8.GetBytes(reason, utf8);
        IntPtr buffer = Marshal.AllocHGlobal(utf8.Length);
        try
        {
            Marshal.Copy(utf8, 0, buffer, utf8.Length);
            return MsgSendArg(allocated, SelRegisterName(InitWithUtf8Selector), buffer);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    /// <inheritdoc />
    public bool CanEvaluate(IntPtr context, nint policy, out IntPtr error)
    {
        error = IntPtr.Zero;
        return MsgSendCanEvaluate(context, SelRegisterName(CanEvaluatePolicyErrorSelector), policy, ref error);
    }

    /// <inheritdoc />
    public long GetErrorCode(IntPtr error) => MsgSend(error, SelRegisterName(CodeSelector)).ToInt64();

    /// <inheritdoc />
    public IntPtr BuildReplyBlock(IntPtr capturedCompletion)
    {
        int literalSize = Marshal.SizeOf<BlockLiteral>();
        int descriptorSize = Marshal.SizeOf<BlockDescriptor>();
        IntPtr buffer = Marshal.AllocHGlobal(literalSize + descriptorSize);
        IntPtr descriptorPointer = buffer + literalSize;

        Marshal.StructureToPtr(
            new BlockDescriptor { Reserved = 0, Size = (nuint)literalSize }, descriptorPointer, fDeleteOld: false);
        Marshal.StructureToPtr(
            new BlockLiteral
            {
                Isa = Dlsym((IntPtr)RtldDefault, ConcreteStackBlockSymbol),
                Flags = 0,
                Reserved = 0,
                Invoke = ReplyInvokePointer(),
                Descriptor = descriptorPointer,
                Context = capturedCompletion,
            },
            buffer,
            fDeleteOld: false);
        return buffer;
    }

    /// <inheritdoc />
    public void Evaluate(IntPtr context, nint policy, IntPtr reason, IntPtr replyBlock) =>
        MsgSendEvaluate(context, SelRegisterName(EvaluatePolicyReplySelector), policy, reason, replyBlock);

    /// <inheritdoc />
    public void Invalidate(IntPtr context) => MsgSend(context, SelRegisterName(InvalidateSelector));

    /// <inheritdoc />
    public void Release(IntPtr instance) => MsgSend(instance, SelRegisterName(ReleaseSelector));

    /// <inheritdoc />
    public void FreeReplyBlock(IntPtr replyBlock) => Marshal.FreeHGlobal(replyBlock);

    // The Objective-C reply block invoke. AG0104 requires it be a static [UnmanagedCallersOnly] method reached by a
    // function pointer, and AG0105 requires its whole body be one try that catches every managed exception and never
    // propagates, so no exception unwinds across the native block invocation. It recovers the orchestrator's captured
    // reply handler (an Action<byte, IntPtr> — no branch, no mapping, which are the orchestrator's) and invokes it with
    // the raw (success, error) reply. On a caught fault the thin native-ops layer only swallows: it holds no
    // TaskCompletionSource (AG0115), so fail-closed completion is the orchestrator's handler's own responsibility.
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "AG0105 mandates a native callback body of one try/catch(Exception) that never propagates across the native frame.")]
    private static void EvaluateReplyCallback(IntPtr block, byte success, IntPtr error)
    {
        try
        {
            RecoverReplyHandler(block)(success, error);
        }
        catch (Exception)
        {
            // A managed exception must never unwind across the native block invocation (AG0105). Fail-closed: recover
            // the orchestrator's reply handler again and complete it with a failure reply (no success, no error pointer
            // -> the orchestrator maps it to Unknown -> Error) so the awaiting task denies at once instead of hanging
            // for the gate's bound. Recovering our own GCHandle cannot itself throw in practice.
            RecoverReplyHandler(block)(0, IntPtr.Zero);
        }
    }

    // Recovers the orchestrator's captured reply handler (the Action<byte, IntPtr> the block literal carries in its
    // captured completion handle) so the callback can hand it the raw (success, error) reply.
    private static Action<byte, IntPtr> RecoverReplyHandler(IntPtr block) =>
        (Action<byte, IntPtr>)GCHandle.FromIntPtr(ReadCapturedContext(block)).Target!;

    // Reads the captured completion handle back out of the (copied) block literal ObjC handed the callback.
    private static IntPtr ReadCapturedContext(IntPtr block) => Marshal.PtrToStructure<BlockLiteral>(block).Context;

    // The one unsafe expression the design needs: AG0104 requires the native callback be reached by a function pointer
    // (&Method), which is only expressible in an unsafe context. The pointer targets the static [UnmanagedCallersOnly]
    // EvaluateReplyCallback, whose signature is blittable.
    [SuppressMessage(
        "Major Code Smell",
        "S6640:Make sure that using \"unsafe\" is safe here.",
        Justification = "AG0104 requires the native callback be reached by a function pointer (&Method), expressible only in an unsafe context; it targets a static [UnmanagedCallersOnly] method with a blittable signature.")]
    private static unsafe IntPtr ReplyInvokePointer() =>
        (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, byte, IntPtr, void>)&EvaluateReplyCallback;

    [LibraryImport(ObjcLibrary, EntryPoint = "objc_getClass", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr ObjcGetClass(string name);

    [LibraryImport(ObjcLibrary, EntryPoint = "sel_registerName", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr SelRegisterName(string name);

    [LibraryImport(ObjcLibrary, EntryPoint = "objc_msgSend")]
    private static partial IntPtr MsgSend(IntPtr receiver, IntPtr selector);

    [LibraryImport(ObjcLibrary, EntryPoint = "objc_msgSend")]
    private static partial IntPtr MsgSendArg(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [LibraryImport(ObjcLibrary, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static partial bool MsgSendCanEvaluate(IntPtr receiver, IntPtr selector, nint policy, ref IntPtr error);

    [LibraryImport(ObjcLibrary, EntryPoint = "objc_msgSend")]
    private static partial void MsgSendEvaluate(
        IntPtr receiver, IntPtr selector, nint policy, IntPtr reason, IntPtr reply);

    [LibraryImport(SystemLibrary, EntryPoint = "dlsym", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr Dlsym(IntPtr handle, string symbol);

    // The Objective-C block literal ABI: isa, flags, reserved, invoke, descriptor, then the captured completion handle.
    [StructLayout(LayoutKind.Sequential)]
    private struct BlockLiteral
    {
        public IntPtr Isa;
        public int Flags;
        public int Reserved;
        public IntPtr Invoke;
        public IntPtr Descriptor;
        public IntPtr Context;
    }

    // The Objective-C block descriptor ABI: reserved (0) and the block literal's size.
    [StructLayout(LayoutKind.Sequential)]
    private struct BlockDescriptor
    {
        public nuint Reserved;
        public nuint Size;
    }
}
