# Local, OPT-IN cosign co-signing (decision cosign-keyless-and-community-cosign) — Windows companion to
# eng/local-cosign.sh. OFF by default: set AGENTGUARD_COSIGN=1 to co-sign a LOCALLY-produced binary with your
# OWN Sigstore identity via the interactive browser login, so a small contributor community can verify and
# trust each other's local builds. NOT the CI path (CI signs keyless with the ambient GitHub OIDC identity);
# skipped in CI and when headless, so a normal local or CI build is unaffected.
#
# Usage: eng/local-cosign.ps1 <binary> [<bundle-out>]   (default bundle: <binary>.cosign.bundle)
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Binary,
    [string]$BundleOut
)
$ErrorActionPreference = 'Stop'
if (-not $BundleOut) { $BundleOut = "$Binary.cosign.bundle" }

function Test-True($v) { $v -in @('1', 'true', 'TRUE', 'yes', 'YES', 'on', 'ON') }

if (-not (Test-True $env:AGENTGUARD_COSIGN)) {
    Write-Host 'AGENTGUARD_COSIGN is off — skipping local cosign co-signing (opt-in only).'
    exit 0
}
if ($env:CI -or $env:GITHUB_ACTIONS) {
    Write-Host 'Running in CI — skipping the LOCAL cosign pass (CI uses the ambient GitHub OIDC identity).'
    exit 0
}
if ([Console]::IsInputRedirected -or [Console]::IsOutputRedirected) {
    Write-Host 'No interactive console — skipping local cosign (the Sigstore login needs a browser).'
    exit 0
}
if (-not (Get-Command cosign -ErrorAction SilentlyContinue)) {
    Write-Error 'cosign is not installed — install it to use AGENTGUARD_COSIGN (https://docs.sigstore.dev).'
}
if (-not (Test-Path $Binary)) { Write-Error "Binary '$Binary' not found." }

Write-Host "Local cosign co-sign of '$Binary' with YOUR interactive Sigstore identity (a browser window will open)..."
# No --yes: keep the confirmation + browser OIDC flow interactive (the developer's own identity, keyless).
cosign sign-blob --bundle $BundleOut $Binary
Write-Host "Wrote '$BundleOut'. Share it so community members can verify this local build by your Sigstore identity."
