// Copyright (c) 4thWAIV. All rights reserved.

// AgentGuard dev-key generator (decision 36): a single-file C# program, run by
// generate-dev-keys.sh / .ps1 via `dotnet run generate-dev-keys.cs -- <outDir>`.
// No SLN/csproj, no openssl. Cross-platform (pure .NET crypto), so it produces the
// identical key material on macOS, Windows, and Linux.
//
// It writes FOUR files into <outDir> (decision 21: one strong-name key + one code-signing cert):
//   agentguard-strongname.snk        - CAPI PRIVATEKEYBLOB, the strong-name key (SignAssembly)
//   agentguard-strongname.publickey  - hex of the strong-name PUBLIC key blob, for InternalsVisibleTo (decision 35)
//   agentguard-codesign.pfx          - self-signed Code Signing cert, no password (decision 22), used by
//                                      codesign (macOS) and signtool (Windows); the SAME cert both OSes (decision 21)
//   agentguard-codesign.cer          - the public half, for testers to trust (decision 24)
//
// The strong-name key material must be a Microsoft CAPI blob; openssl cannot emit it and `sn` is Windows-only,
// which is exactly why this is a .NET program rather than a shell + openssl pipeline.

// MA0048 (file name must match type name) is a false positive here: a file-based program compiles to a
// compiler-synthesized 'Program' type that no source name can match. This is the single, scoped suppression.
#pragma warning disable MA0048

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

string outDir = args.Length > 0 ? args[0] : ".";
Directory.CreateDirectory(outDir);

// --- strong-name key (.snk) + its public-key blob, both derived from one RSA-2048 key ---
using (var rsa = RSA.Create(2048))
{
    RSAParameters p = rsa.ExportParameters(includePrivateParameters: true);

    byte[] snk = ToCapiPrivateKeyBlob(p);
    string snkPath = Path.Combine(outDir, "agentguard-strongname.snk");
    await File.WriteAllBytesAsync(snkPath, snk).ConfigureAwait(false);
    Console.WriteLine($"wrote {snkPath} ({snk.Length} bytes)");

    string pubHex = Convert.ToHexString(ToStrongNamePublicKey(p));
    string pubPath = Path.Combine(outDir, "agentguard-strongname.publickey");
    await File.WriteAllTextAsync(pubPath, pubHex).ConfigureAwait(false);
    Console.WriteLine($"wrote {pubPath} ({pubHex.Length} hex chars)");
}

// --- code-signing cert (.pfx, no password) : self-signed, Code Signing EKU (1.3.6.1.5.5.7.3.3) ---
using (var certRsa = RSA.Create(2048))
{
    var req = new CertificateRequest(
        "CN=AgentGuard untrusted-local-dev-only-use, O=4thWAIV",
        certRsa,
        HashAlgorithmName.SHA256,
        RSASignaturePadding.Pkcs1);
    req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, critical: true));
    req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, critical: true));
    req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
        new OidCollection { new Oid("1.3.6.1.5.5.7.3.3") }, critical: true));

    // 1-year validity (decision 23); back-dated a day so clock skew never makes a fresh cert "not yet valid".
    DateTimeOffset notBefore = DateTimeOffset.UtcNow.AddDays(-1);
    DateTimeOffset notAfter = DateTimeOffset.UtcNow.AddYears(1);
    using X509Certificate2 cert = req.CreateSelfSigned(notBefore, notAfter);

    byte[] pfx = cert.Export(X509ContentType.Pkcs12); // empty password (decision 22)
    string pfxPath = Path.Combine(outDir, "agentguard-codesign.pfx");
    await File.WriteAllBytesAsync(pfxPath, pfx).ConfigureAwait(false);
    Console.WriteLine($"wrote {pfxPath} ({pfx.Length} bytes), expires {cert.NotAfter:yyyy-MM-dd}");

    // Public-only cert (DER) that a tester installs to trust the signature (decision 24). Emitted by the
    // generator itself so no openssl is needed anywhere in the chain.
    byte[] cer = cert.Export(X509ContentType.Cert);
    string cerPath = Path.Combine(outDir, "agentguard-codesign.cer");
    await File.WriteAllBytesAsync(cerPath, cer).ConfigureAwait(false);
    Console.WriteLine($"wrote {cerPath} ({cer.Length} bytes)");
}

// The strong-name PUBLIC key blob = a 12-byte header (SigAlgId, HashAlgId, cbPublicKey) in front of a CAPI
// PUBLICKEYBLOB. This hex is what InternalsVisibleTo needs; the build injects it (decision 35).
static byte[] ToStrongNamePublicKey(RSAParameters p)
{
    int mod = p.Modulus!.Length; // 256 for RSA-2048
    using var capiMs = new MemoryStream();
    using (var cw = new BinaryWriter(capiMs))
    {
        cw.Write((byte)0x06);       // PUBLICKEYBLOB
        cw.Write((byte)0x02);       // version 2
        cw.Write((ushort)0);        // reserved
        cw.Write(0x2400U);          // CALG_RSA_SIGN
        cw.Write(0x31415352U);      // "RSA1"
        cw.Write((uint)(mod * 8));  // bit length
        uint exp = 0;
        foreach (byte b in p.Exponent!)
        {
            exp = (exp << 8) | b;
        }

        cw.Write(exp);
        for (int i = 0; i < mod; i++)
        {
            cw.Write(p.Modulus![mod - 1 - i]); // modulus, little-endian
        }
    }

    byte[] capi = capiMs.ToArray();
    using var ms = new MemoryStream();
    using var w = new BinaryWriter(ms);
    w.Write(0x2400U);             // SigAlgId  CALG_RSA_SIGN
    w.Write(0x8004U);             // HashAlgId CALG_SHA1
    w.Write((uint)capi.Length);   // cbPublicKey
    w.Write(capi);
    return ms.ToArray();
}

// The strong-name private key (.snk) = a CAPI PRIVATEKEYBLOB, built from the same RSA key.
static byte[] ToCapiPrivateKeyBlob(RSAParameters p)
{
    int mod = p.Modulus!.Length; // 256
    int half = mod / 2;          // 128
    using var ms = new MemoryStream();
    using var w = new BinaryWriter(ms);
    w.Write((byte)0x07);       // PRIVATEKEYBLOB
    w.Write((byte)0x02);       // version 2
    w.Write((ushort)0);        // reserved
    w.Write(0x2400U);          // CALG_RSA_SIGN
    w.Write(0x32415352U);      // "RSA2"
    w.Write((uint)(mod * 8));  // bit length
    uint exp = 0;
    foreach (byte b in p.Exponent!)
    {
        exp = (exp << 8) | b;
    }

    w.Write(exp);
    WriteLE(w, p.Modulus!, mod);
    WriteLE(w, p.P!, half);
    WriteLE(w, p.Q!, half);
    WriteLE(w, p.DP!, half);
    WriteLE(w, p.DQ!, half);
    WriteLE(w, p.InverseQ!, half);
    WriteLE(w, p.D!, mod);
    return ms.ToArray();

    static void WriteLE(BinaryWriter w, byte[] bigEndian, int size)
    {
        var buf = new byte[size];
        for (int i = 0; i < bigEndian.Length && i < size; i++)
        {
            buf[i] = bigEndian[bigEndian.Length - 1 - i]; // reverse to little-endian
        }

        w.Write(buf);
    }
}
