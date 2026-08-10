#!/usr/bin/env pwsh
# Trust the AgentGuard alpha code-signing certificate on Windows — narrowly, as a Trusted Publisher for code
# signing ONLY, and NEVER as a Root CA (decision tester-trust-public-cer). Run this once before launching a
# downloaded alpha build. It installs into your per-user store only; no admin rights, no machine-wide change.
#
# Usage: ./trust-cert.ps1 [path\to\agentguard-windows-public.cer]
#   Defaults to agentguard-windows-public.cer in the current directory (as attached to the release).
[CmdletBinding()]
param([string]$CerPath = 'agentguard-windows-public.cer')

$ErrorActionPreference = 'Stop'
if (-not (Test-Path $CerPath)) {
    Write-Error "Certificate '$CerPath' not found. Pass the path to the .cer you downloaded from the release."
}

Write-Host "Adding '$CerPath' to your Trusted Publishers store (CurrentUser; never a Root CA)."
# CurrentUser\TrustedPublisher is the narrow code-signing trust bin — NOT Root (decision tester-trust-public-cer).
$cert = Import-Certificate -FilePath $CerPath -CertStoreLocation 'Cert:\CurrentUser\TrustedPublisher'

Write-Host "Done. AgentGuard is now a Trusted Publisher for you (thumbprint $($cert.Thumbprint))."
Write-Host "SmartScreen may still warn on first launch — see docs/running-alpha-builds.md for the 'Run anyway' step."
Write-Host "To undo later: remove the cert from Cert:\CurrentUser\TrustedPublisher (run certmgr.msc)."
