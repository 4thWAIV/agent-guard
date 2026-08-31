// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace AgentGuard.CrossPlatform.Windows;

/// <summary>
/// The sole implementer of <see cref="IWindowsHelloNativeOps"/> and the ONE class that binds the WinRT
/// <c>UserConsentVerifier</c> interop — the Windows Hello native-ops owner (AG0113 confines its raw interop and
/// <c>Marshal</c> here; AG0114 permits one implementer). It is fully stateless (AG0116): every native handle it returns
/// is a per-call local the <see cref="WindowsUserPresence"/> orchestrator owns and releases, and it holds no field but
/// compile-time <c>const</c>s. Each method is a straight-line raw call with no branching (AG0106) and no task-completion
/// wiring (AG0115): the <c>TaskCompletionSource</c>, the <c>await</c>, the async-status decision, and the 7-way
/// Hello-result mapping all live in the fake-testable orchestrator, which composes these primitives. The COM-visible
/// completed handler (<see cref="AsyncCompletedHandler"/>) forwards the raw async status to the orchestrator's callback;
/// its whole body is one guarded try/catch (AG0105) so no managed exception unwinds across the native COM invocation.
/// </summary>
internal sealed partial class WindowsHelloNativeOps : IWindowsHelloNativeOps
{
    private const string ComBaseLibrary = "combase.dll";
    private const string KernelLibrary = "kernel32.dll";

    private const string UserConsentVerifierRuntimeClass = "Windows.Security.Credentials.UI.UserConsentVerifier";
    private const string UserConsentVerifierInteropIid = "39E050C3-4E74-441A-8DC0-B81104DF949C";

    // The IUserConsentVerifierStatics activation-factory interface (Windows.Security.Credentials.UI) — the statics door
    // to the non-interactive CheckAvailabilityAsync. CheckAvailabilityAsync is its FIRST vtable slot after IInspectable,
    // so only it is declared below (RequestVerificationAsync, the second slot, is unused here).
    private const string UserConsentVerifierStaticsIid = "AF4F3F91-564C-4DDC-B8B5-973447627C65";

    // The WinRT parameterized IID of IAsyncOperation<UserConsentVerificationResult>, computed from the WinRT signature
    // algorithm (verified against the known IAsyncOperation<bool> IID).
    private const string AsyncOperationResultIid = "FD596FFD-2318-558F-9DBE-D21DF43764A5";

    // The WinRT parameterized IID of IAsyncOperationCompletedHandler<UserConsentVerificationResult>.
    private const string AsyncCompletedHandlerIid = "0CFFC6C9-4C2B-5CD4-B38C-7B8DF3FF5AFB";

    // The WinRT parameterized IID of IAsyncOperation<UserConsentVerifierAvailability> and its completed handler, computed
    // by the same SHA-1 signature algorithm above (validated by reproducing the two ...Result IIDs exactly). The
    // availability async operation is a different parameterized type than the verification one, so it needs its own
    // GetResults interface and completed-handler interface.
    private const string AsyncOperationAvailabilityIid = "DDD384F3-D818-5D83-AB4B-32119C28587C";
    private const string AsyncCompletedHandlerAvailabilityIid = "28988174-ACE2-5C15-A0DF-580A26D94294";

    // The fixed (non-parameterized) IID of IAsyncInfo — the base every IAsyncOperation derives from — whose Cancel is the
    // cooperative-cancel primitive. Fixed across every result type, so one interface serves both the availability and the
    // verification operation.
    private const string AsyncInfoIid = "00000036-0000-0000-C000-000000000046";

    private const uint RoInitMultiThreaded = 1;

    /// <summary>
    /// The WinRT UserConsentVerifier interop factory interface (IInspectable-based). The CLR supplies IUnknown and
    /// IInspectable; only RequestVerificationForWindowAsync is declared.
    /// </summary>
    [ComImport]
    [Guid(UserConsentVerifierInteropIid)]
    [InterfaceType(ComInterfaceType.InterfaceIsIInspectable)]
    private interface IUserConsentVerifierInterop
    {
        /// <summary>Requests Hello verification for a window, returning the WinRT async operation.</summary>
        /// <param name="appWindow">The owner window handle.</param>
        /// <param name="message">The HSTRING prompt message.</param>
        /// <param name="riid">The requested async-operation interface id.</param>
        /// <param name="asyncOperation">The returned async operation.</param>
        void RequestVerificationForWindowAsync(
            IntPtr appWindow, IntPtr message, ref Guid riid, out IntPtr asyncOperation);
    }

    /// <summary>
    /// IAsyncOperation&lt;UserConsentVerificationResult&gt; (IInspectable-based); the vtable order after IInspectable is
    /// put_Completed, get_Completed, GetResults.
    /// </summary>
    [ComImport]
    [Guid(AsyncOperationResultIid)]
    [InterfaceType(ComInterfaceType.InterfaceIsIInspectable)]
    private interface IAsyncOperationUserConsent
    {
        /// <summary>Sets the completed handler (the WinRT put_Completed slot).</summary>
        /// <param name="handler">The completed handler.</param>
        void SetCompleted(IAsyncOperationCompletedHandlerUserConsent handler);

        /// <summary>Gets the completed handler (the WinRT get_Completed slot).</summary>
        /// <returns>The current handler pointer.</returns>
        IntPtr GetCompleted();

        /// <summary>Gets the verification result code once the operation has completed.</summary>
        /// <returns>The UserConsentVerificationResult value.</returns>
        int GetResults();
    }

    /// <summary>
    /// The completed-handler delegate interface (IUnknown-based) this class implements as a CCW and hands to WinRT.
    /// </summary>
    [Guid(AsyncCompletedHandlerIid)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]
    private interface IAsyncOperationCompletedHandlerUserConsent
    {
        /// <summary>Invoked by WinRT when the verification finishes.</summary>
        /// <param name="asyncOperation">The completed async operation.</param>
        /// <param name="status">The async status.</param>
        void Invoke(IntPtr asyncOperation, int status);
    }

    /// <summary>
    /// The UserConsentVerifier statics interface (IInspectable-based) — the door to the non-interactive
    /// CheckAvailabilityAsync. CheckAvailabilityAsync is the first slot after IInspectable, so declaring it alone lands on
    /// the correct vtable offset; RequestVerificationAsync (the second slot) is not needed here.
    /// </summary>
    [ComImport]
    [Guid(UserConsentVerifierStaticsIid)]
    [InterfaceType(ComInterfaceType.InterfaceIsIInspectable)]
    private interface IUserConsentVerifierStatics
    {
        /// <summary>Issues the non-interactive availability check, returning the WinRT async operation.</summary>
        /// <param name="asyncOperation">The returned IAsyncOperation&lt;UserConsentVerifierAvailability&gt;.</param>
        void CheckAvailabilityAsync(out IntPtr asyncOperation);
    }

    /// <summary>
    /// IAsyncOperation&lt;UserConsentVerifierAvailability&gt; (IInspectable-based); the vtable order after IInspectable is
    /// put_Completed, get_Completed, GetResults — the same shape as the verification operation, a different parameterized
    /// IID. SetCompleted takes the availability completed-handler interface (the one CCW below implements both).
    /// </summary>
    [ComImport]
    [Guid(AsyncOperationAvailabilityIid)]
    [InterfaceType(ComInterfaceType.InterfaceIsIInspectable)]
    private interface IAsyncOperationUserConsentAvailability
    {
        /// <summary>Sets the completed handler (the WinRT put_Completed slot).</summary>
        /// <param name="handler">The completed handler.</param>
        void SetCompleted(IAsyncOperationCompletedHandlerAvailability handler);

        /// <summary>Gets the completed handler (the WinRT get_Completed slot).</summary>
        /// <returns>The current handler pointer.</returns>
        IntPtr GetCompleted();

        /// <summary>Gets the availability result code once the operation has completed.</summary>
        /// <returns>The UserConsentVerifierAvailability value.</returns>
        int GetResults();
    }

    /// <summary>
    /// The availability completed-handler delegate interface (IUnknown-based) — the same Invoke shape as the verification
    /// handler with a different parameterized IID, so put_Completed on the availability operation can bind the CCW under
    /// the correct interface id. The one CCW below implements this and the verification handler through a single Invoke.
    /// </summary>
    [Guid(AsyncCompletedHandlerAvailabilityIid)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]
    private interface IAsyncOperationCompletedHandlerAvailability
    {
        /// <summary>Invoked by WinRT when the availability check finishes.</summary>
        /// <param name="asyncOperation">The completed async operation.</param>
        /// <param name="status">The async status.</param>
        void Invoke(IntPtr asyncOperation, int status);
    }

    /// <summary>
    /// IAsyncInfo (IInspectable-based) — the base of every IAsyncOperation, holding the cooperative Cancel. Its members
    /// get_Id, get_Status, get_ErrorCode precede Cancel and Close in the vtable, so all five are declared in order for
    /// Cancel to land on the correct slot; only Cancel is called.
    /// </summary>
    [ComImport]
    [Guid(AsyncInfoIid)]
    [InterfaceType(ComInterfaceType.InterfaceIsIInspectable)]
    private interface IAsyncInfo
    {
        /// <summary>Gets the async operation's id (the WinRT get_Id slot).</summary>
        /// <returns>The operation id.</returns>
        uint GetId();

        /// <summary>Gets the async operation's status (the WinRT get_Status slot).</summary>
        /// <returns>The async status.</returns>
        int GetStatus();

        /// <summary>Gets the async operation's error HRESULT (the WinRT get_ErrorCode slot).</summary>
        /// <returns>The error HRESULT.</returns>
        int GetErrorCode();

        /// <summary>Requests cooperative cancellation of the pending operation.</summary>
        void Cancel();

        /// <summary>Closes the async operation.</summary>
        void Close();
    }

    /// <inheritdoc />
    [SupportedOSPlatform("windows")]
    public IntPtr BeginAvailabilityCheck(Action<int> onCompleted)
    {
        // WinRT calls require an initialized apartment; S_FALSE / RPC_E_CHANGED_MODE are benign, so the HRESULT is
        // intentionally not inspected.
        _ = RoInitialize(RoInitMultiThreaded);

        IntPtr classId = CreateHString(UserConsentVerifierRuntimeClass);
        try
        {
            var staticsIid = new Guid(UserConsentVerifierStaticsIid);
            Marshal.ThrowExceptionForHR(RoGetActivationFactory(classId, ref staticsIid, out IntPtr factory));
            var statics = (IUserConsentVerifierStatics)Marshal.GetObjectForIUnknown(factory);
            Marshal.Release(factory);

            statics.CheckAvailabilityAsync(out IntPtr asyncPointer);

            // Register the completed handler on the availability operation; the raw asyncPointer is returned to the
            // orchestrator (which reads the code via GetAvailabilityResult and drops the reference via ReleaseOperation).
            var operation = (IAsyncOperationUserConsentAvailability)Marshal.GetObjectForIUnknown(asyncPointer);
            operation.SetCompleted(new AsyncCompletedHandler(onCompleted));
            return asyncPointer;
        }
        finally
        {
            _ = WindowsDeleteString(classId);
        }
    }

    /// <inheritdoc />
    [SupportedOSPlatform("windows")]
    public int GetAvailabilityResult(IntPtr asyncOperation)
    {
        var operation = (IAsyncOperationUserConsentAvailability)Marshal.GetObjectForIUnknown(asyncOperation);
        return operation.GetResults();
    }

    /// <inheritdoc />
    [SupportedOSPlatform("windows")]
    public IntPtr BeginVerification(string reason, Action<int> onCompleted)
    {
        // WinRT calls require an initialized apartment; S_FALSE / RPC_E_CHANGED_MODE are benign, so the HRESULT is
        // intentionally not inspected.
        _ = RoInitialize(RoInitMultiThreaded);

        IntPtr classId = CreateHString(UserConsentVerifierRuntimeClass);
        try
        {
            var interopIid = new Guid(UserConsentVerifierInteropIid);
            Marshal.ThrowExceptionForHR(RoGetActivationFactory(classId, ref interopIid, out IntPtr factory));
            var interop = (IUserConsentVerifierInterop)Marshal.GetObjectForIUnknown(factory);
            Marshal.Release(factory);

            IntPtr message = CreateHString(reason);
            try
            {
                var asyncIid = new Guid(AsyncOperationResultIid);
                interop.RequestVerificationForWindowAsync(
                    GetConsoleWindow(), message, ref asyncIid, out IntPtr asyncPointer);

                // Wrap the async operation to register the completed handler; the raw asyncPointer is returned to the
                // orchestrator (which reads the result via GetVerificationResult and drops the reference via
                // ReleaseOperation). The completed handler forwards the raw async status to the orchestrator's callback.
                var operation = (IAsyncOperationUserConsent)Marshal.GetObjectForIUnknown(asyncPointer);
                operation.SetCompleted(new AsyncCompletedHandler(onCompleted));
                return asyncPointer;
            }
            finally
            {
                _ = WindowsDeleteString(message);
            }
        }
        finally
        {
            _ = WindowsDeleteString(classId);
        }
    }

    /// <inheritdoc />
    [SupportedOSPlatform("windows")]
    public int GetVerificationResult(IntPtr asyncOperation)
    {
        var operation = (IAsyncOperationUserConsent)Marshal.GetObjectForIUnknown(asyncOperation);
        return operation.GetResults();
    }

    /// <inheritdoc />
    [SupportedOSPlatform("windows")]
    public void CancelOperation(IntPtr asyncOperation)
    {
        // Reach the async operation's IAsyncInfo base — its fixed IID is the same for both operation types — and ask it
        // to cancel. WinRT cooperative cancellation unblocks the pending await with a cancelled async status.
        var info = (IAsyncInfo)Marshal.GetObjectForIUnknown(asyncOperation);
        info.Cancel();
    }

    /// <inheritdoc />
    public void ReleaseOperation(IntPtr asyncOperation) => Marshal.Release(asyncOperation);

    private static IntPtr CreateHString(string value)
    {
        Marshal.ThrowExceptionForHR(WindowsCreateString(value, (uint)value.Length, out IntPtr hstring));
        return hstring;
    }

    [LibraryImport(ComBaseLibrary)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int RoInitialize(uint initType);

    [LibraryImport(ComBaseLibrary, StringMarshalling = StringMarshalling.Utf16)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int WindowsCreateString(string sourceString, uint length, out IntPtr hstring);

    [LibraryImport(ComBaseLibrary)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int WindowsDeleteString(IntPtr hstring);

    [LibraryImport(ComBaseLibrary)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int RoGetActivationFactory(IntPtr activatableClassId, ref Guid iid, out IntPtr factory);

    [LibraryImport(KernelLibrary)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial IntPtr GetConsoleWindow();

    // The managed completed handler WinRT invokes when the availability check OR the verification finishes; it forwards
    // the raw async status to the orchestrator's callback, which owns the TaskCompletionSource and the async-status
    // decision (AG0115). It implements BOTH parameterized completed-handler interfaces through one Invoke, so a single CCW
    // binds to put_Completed on either operation under the correct interface id (the two differ only by IID; the vtable
    // Invoke shape is identical). Its whole callback body is one guarded try/catch so no managed exception unwinds across
    // the native COM invocation (AG0105).
    [ComVisible(true)]
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "A WinRT completed handler must never let a managed exception unwind across the native invocation; the orchestrator owns fail-closed completion.")]
    private sealed class AsyncCompletedHandler
        : IAsyncOperationCompletedHandlerUserConsent, IAsyncOperationCompletedHandlerAvailability
    {
        // AsyncStatus.Error (Windows.Foundation): the substitute status forwarded fail-closed if the callback faults, so
        // the orchestrator maps it to a non-verified outcome instead of leaving the awaiting task hanging.
        private const int AsyncStatusError = 3;

        private readonly Action<int> _onCompleted;

        internal AsyncCompletedHandler(Action<int> onCompleted) => _onCompleted = onCompleted;

        public void Invoke(IntPtr asyncOperation, int status)
        {
            try
            {
                _onCompleted(status);
            }
            catch (Exception)
            {
                // Never unwind across the native COM invocation (AG0105). Fail-closed: forward an error status so the
                // orchestrator's completion is set and the await does not hang; it maps a non-complete status to a
                // non-verified outcome.
                _onCompleted(AsyncStatusError);
            }
        }
    }
}
