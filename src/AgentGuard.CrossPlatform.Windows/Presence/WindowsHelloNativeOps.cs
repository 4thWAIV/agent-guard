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

    // The WinRT parameterized IID of IAsyncOperation<UserConsentVerificationResult>, computed from the WinRT signature
    // algorithm (verified against the known IAsyncOperation<bool> IID).
    private const string AsyncOperationResultIid = "FD596FFD-2318-558F-9DBE-D21DF43764A5";

    // The WinRT parameterized IID of IAsyncOperationCompletedHandler<UserConsentVerificationResult>.
    private const string AsyncCompletedHandlerIid = "0CFFC6C9-4C2B-5CD4-B38C-7B8DF3FF5AFB";

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

    // The managed completed handler WinRT invokes when the verification finishes; it forwards the raw async status to
    // the orchestrator's callback, which owns the TaskCompletionSource and the async-status decision (AG0115). Its whole
    // callback body is one guarded try/catch so no managed exception unwinds across the native COM invocation (AG0105).
    [ComVisible(true)]
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "A WinRT completed handler must never let a managed exception unwind across the native invocation; the orchestrator owns fail-closed completion.")]
    private sealed class AsyncCompletedHandler : IAsyncOperationCompletedHandlerUserConsent
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
