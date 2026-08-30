// Copyright (c) 4thWAIV. All rights reserved.

using System;
using System.Runtime.InteropServices;

namespace AgentGuard.CrossPlatform.Windows;

/// <summary>
/// The plain result of the secure-desktop credential prompt (<c>CredUIPromptForWindowsCredentials</c>), returned by the
/// <see cref="ICredentialPromptNativeOps"/> native-ops seam so the <see cref="WindowsUserPresence"/> orchestrator can
/// branch on it — cancel, no interactive surface, or continue to validation — through the pure
/// <see cref="WindowsUserPresence.MapCredentialStatus"/> function without touching the native call itself. The
/// <see cref="Buffer"/> is a <c>CoTaskMem</c> block the orchestrator hands back to
/// <see cref="ICredentialPromptNativeOps.FreePromptBuffer"/>.
/// </summary>
/// <param name="Status">The prompt's return status (<c>ERROR_SUCCESS</c>, <c>ERROR_CANCELLED</c>, or another failure).</param>
/// <param name="Buffer">The packed authentication buffer the prompt allocated, or <see cref="IntPtr.Zero"/> when none.</param>
/// <param name="BufferSize">The size, in bytes, of <paramref name="Buffer"/>.</param>
[StructLayout(LayoutKind.Auto)]
internal readonly record struct CredentialPromptOutcome(uint Status, IntPtr Buffer, uint BufferSize);
