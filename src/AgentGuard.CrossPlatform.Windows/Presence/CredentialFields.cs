// Copyright (c) 4thWAIV. All rights reserved.

using System;

namespace AgentGuard.CrossPlatform.Windows;

/// <summary>
/// The unpacked fields of a typed credential (<c>CredUnPackAuthenticationBuffer</c>), returned by the
/// <see cref="ICredentialPromptNativeOps"/> native-ops seam. Each field is a native buffer the orchestrator hands back
/// to <see cref="ICredentialPromptNativeOps.FreeFields"/> (which zeroes the password) once validation is done; the
/// orchestrator reads only <see cref="Success"/> to branch, never the buffer contents. <see cref="Success"/> is
/// <see langword="false"/> when the buffer could not be unpacked, so the orchestrator maps it to an error.
/// </summary>
/// <param name="Success">Whether the authentication buffer was unpacked into the field buffers.</param>
/// <param name="User">The unpacked user-name buffer.</param>
/// <param name="Domain">The unpacked domain buffer.</param>
/// <param name="Password">The unpacked password buffer (zeroed by <see cref="ICredentialPromptNativeOps.FreeFields"/>).</param>
internal readonly record struct CredentialFields(bool Success, IntPtr User, IntPtr Domain, IntPtr Password);
