# Running the alpha (self-signed) builds

AgentGuard's alpha releases are **signed with our own self-signed certificate**, not a paid Apple/Microsoft
identity and not notarized. The signature proves the binary came from us and has not been tampered with, but
because the certificate is not issued by a public authority, macOS Gatekeeper and Windows SmartScreen will
warn the first time you run it. This page shows the one-time steps to trust and launch an alpha build.

Every release attaches, per platform:

- the binary — `guard-<rid>` (macOS/Linux) or `guard-<rid>.exe` (Windows), e.g. `guard-osx-arm64`, `guard-win-x64.exe`;
- its checksum — `guard-<rid>.sha256` (GNU two-column format);
- its cosign signature — `guard-<rid>.cosign.bundle` (keyless, recorded in the public Rekor transparency log);
- the public certificate — `agentguard-macos-public.cer` and `agentguard-windows-public.cer`;
- the trust scripts — `trust-cert.sh` (macOS) and `trust-cert.ps1` (Windows).

Pick the file whose `<rid>` matches your machine: `osx-arm64` (Apple Silicon Mac), `osx-x64` (Intel Mac),
`win-x64` / `win-arm64` (Windows), `linux-x64` / `linux-arm64` (Linux).

---

## Verify the download first (optional but recommended)

**Checksum** — confirms the bytes are intact:

```sh
# macOS
shasum -a 256 -c guard-osx-arm64.sha256
# Linux
sha256sum -c guard-linux-x64.sha256
# Windows (PowerShell) — compare the printed hash against the first column of the .sha256 file
Get-FileHash -Algorithm SHA256 guard-win-x64.exe
```

**cosign** — confirms it was built and signed by our official CI workflow (needs
[cosign](https://docs.sigstore.dev/system_config/installation/) installed). Use `refs/heads/main` for a full
release or `refs/heads/dev` for a dev preview:

```sh
cosign verify-blob \
  --certificate-identity 'https://github.com/4thWAIV/agent-guard/.github/workflows/ci.yml@refs/heads/main' \
  --certificate-oidc-issuer 'https://token.actions.githubusercontent.com' \
  --bundle guard-osx-arm64.cosign.bundle \
  guard-osx-arm64
```

---

## macOS

macOS quarantines files downloaded from the internet and Gatekeeper blocks un-notarized binaries by default.
Do the one-time trust step, then use any one of the launch options.

**1. Trust our certificate (once):**

```sh
chmod +x trust-cert.sh
./trust-cert.sh agentguard-macos-public.cer
```

This adds the certificate as a **code-signing anchor in your login keychain only** — never a system root. It
is reversible with `security remove-trusted-cert agentguard-macos-public.cer`.

**2. Make the binary executable and launch it — pick one:**

- **Right-click → Open.** In Finder, right-click (or Control-click) `guard-osx-arm64`, choose **Open**, then
  **Open** again in the dialog. This records your consent so future launches are silent.
- **Remove the quarantine flag** from the terminal:

  ```sh
  chmod +x guard-osx-arm64
  xattr -d com.apple.quarantine guard-osx-arm64
  ./guard-osx-arm64 version
  ```

- **Open Anyway** from **System Settings → Privacy & Security**: try to run the binary once, then click
  **Open Anyway** in that pane, and run it again.

Confirm it launched:

```sh
./guard-osx-arm64 version
```

---

## Windows

Windows SmartScreen warns on downloaded executables that lack a widely-recognized reputation. Trust our
certificate, then click through the one-time SmartScreen prompt.

**1. Trust our certificate (once):**

```powershell
.\trust-cert.ps1 agentguard-windows-public.cer
```

This installs the certificate into your **CurrentUser Trusted Publishers** store — the narrow code-signing
trust bin, never a Root CA. No administrator rights are needed.

**2. Launch past SmartScreen:**

When you first run `guard-win-x64.exe`, SmartScreen may show *"Windows protected your PC."* Click **More info**,
then **Run anyway**. (If your browser flagged the download, choose **Keep** first.)

```powershell
.\guard-win-x64.exe version
```

---

## Linux

Linux has no OS-level code-signing gate. Make the binary executable and run it; verify with the checksum and
cosign steps above if you want provenance.

```sh
chmod +x guard-linux-x64
./guard-linux-x64 version
```

---

These steps are only needed for the self-signed alpha builds. Once AgentGuard ships with a notarized Apple
identity and an EV/OV Windows certificate, Gatekeeper and SmartScreen will accept the binaries without any of
the trust steps above.
