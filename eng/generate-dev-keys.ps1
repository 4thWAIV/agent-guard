#!/usr/bin/env pwsh
# One script to set up AgentGuard dev signing on Windows (decisions 20, 24, 36):
#   1. mints the strong-name key + code-signing cert via the single-file C# program (no SLN/csproj)
#   2. imports the code-signing cert into the current user's personal store
#   3. adds it to Trusted Publishers (narrow) so signtool-signed builds are trusted locally, never Root
#
# Usage: eng/generate-dev-keys.ps1 [output-dir]   (default: eng/signing/local, git-ignored)
[CmdletBinding()]
param([string]$OutDir)

$ErrorActionPreference = 'Stop'
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $OutDir) { $OutDir = Join-Path $ScriptDir 'signing/local' }
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

Write-Host "==> minting keys into $OutDir"
dotnet run (Join-Path $ScriptDir 'generate-dev-keys.cs') -- $OutDir

$Pfx = Join-Path $OutDir 'agentguard-codesign.pfx'

Write-Host "==> importing code-signing cert into CurrentUser\My"
# No password on the pfx (decision 22).
$empty = New-Object System.Security.SecureString
$cert = Import-PfxCertificate -FilePath $Pfx -CertStoreLocation 'Cert:\CurrentUser\My' -Password $empty

Write-Host "==> adding to CurrentUser Trusted Publishers (narrow, code-signing; never Root) — decision 24"
$pub = New-Object System.Security.Cryptography.X509Certificates.X509Store('TrustedPublisher','CurrentUser')
$pub.Open('ReadWrite'); $pub.Add($cert); $pub.Close()
# NOTE (for the CI/Windows-signing work): decision 24 is "never Root", so this does NOT add the cert to any
# Root store. A self-signed cert will therefore not chain under `signtool verify /pa` on its own — resolving
# how Windows verifies a self-signed dev signature without a Root anchor is part of the Windows-signing step,
# and any need to relax "never Root" is a decision to raise, not to code around here.

Write-Host "Done. Keys are in $OutDir (git-ignored). Thumbprint: $($cert.Thumbprint)"
